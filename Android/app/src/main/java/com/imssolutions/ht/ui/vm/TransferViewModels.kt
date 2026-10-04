package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.ui.HtMsg
import com.imssolutions.ht.data.InventoryRepository
import com.imssolutions.ht.data.Location
import com.imssolutions.ht.data.TransferLine
import com.imssolutions.ht.data.Zone
import com.imssolutions.ht.scan.QrPayload
import com.imssolutions.ht.scan.QrScan

class TransferViewModel : ViewModel() {

    private val repo = InventoryRepository()
    private val _state = MutableStateFlow(TransferState())
    val state: StateFlow<TransferState> = _state.asStateFlow()

    init { loadZones() }

    private fun loadZones() {
        _state.update { it.copy(busy = true) }
        viewModelScope.launch {
            runCatching { repo.zones() }
                .onSuccess { zs ->
                    _state.update { it.copy(busy = false, zones = zs) }
                    zs.firstOrNull()?.let { selectZone(it.zoneCode) }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }

    fun selectZone(zoneCode: String) {
        _state.update {
            it.copy(zoneCode = zoneCode, toLocationCode = null, locations = emptyList(), error = null)
        }
        viewModelScope.launch {
            runCatching { repo.locationsIn(zoneCode) }
                .onSuccess { ls ->
                    _state.update {
                        if (it.zoneCode != zoneCode) it
                        else it.copy(locations = ls, toLocationCode = ls.firstOrNull()?.locationCode)
                    }
                }
                .onFailure { e -> _state.update { it.copy(error = e.toPair()) } }
        }
    }

    fun selectLocation(code: String) = _state.update { it.copy(toLocationCode = code, error = null) }

    fun beginScan(): Boolean {
        if (_state.value.toLocationCode.isNullOrBlank()) {
            _state.update { it.copy(error = "LOC-001" to "Chưa chọn vị trí đích.") }
            return false
        }
        _state.update { it.copy(lines = emptyList(), error = null, toast = null, done = false) }
        return true
    }

    fun onScan(raw: String) {
        val payload = when (val r = QrPayload.parse(raw)) {
            is QrScan.Ok -> r.payload
            is QrScan.Invalid -> {
                _state.update { it.copy(error = "SCN-001" to "Mã quét không đúng định dạng.") }
                return
            }
        }
        val itemCode = payload.itemCode?.trim().orEmpty()
        if (itemCode.isEmpty()) {
            _state.update { it.copy(error = "SCN-006" to "Nhãn không có mã hàng.") }
            return
        }
        val lot = payload.lotNo?.trim()

        if (_state.value.lines.any {
                it.itemCode.equals(itemCode, ignoreCase = true) &&
                    it.lotNo?.trim().orEmpty().equals(lot.orEmpty(), ignoreCase = true)
            }
        ) {
            _state.update { it.copy(error = "SCN-007" to "Nhãn này đã được quét rồi.") }
            return
        }

        if (_state.value.busy) return
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.resolveTransferScan(itemCode, lot) }
                .onSuccess { line ->
                    when {
                        line == null -> _state.update {
                            it.copy(
                                busy = false,
                                error = "SYS-404" to itemCode,
                            )
                        }
                        line.locationCode.equals(_state.value.toLocationCode, ignoreCase = true) ->
                            _state.update {
                                it.copy(
                                    busy = false,
                                    error = "LOC-001" to "${line.itemCode} đã ở ${line.locationCode} rồi.",
                                )
                            }
                        else -> _state.update { it.copy(busy = false, lines = it.lines + line) }
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }

    fun remove(key: String) =
        _state.update { s -> s.copy(lines = s.lines.filterNot { it.key == key }) }

    fun clearError() = _state.update { it.copy(error = null) }

    fun clearToast() = _state.update { it.copy(toast = null) }

    fun resetScans() = _state.update {
        it.copy(lines = emptyList(), error = null, toast = null, done = false)
    }

    fun confirm() {
        val s = _state.value
        if (s.busy) return
        val to = s.toLocationCode?.takeIf { it.isNotBlank() }
        if (to == null) {
            _state.update { it.copy(error = "LOC-001" to "Chưa chọn vị trí đích.") }
            return
        }
        if (s.lines.isEmpty()) {
            _state.update { it.copy(error = "STK-005" to "Chưa quét nhãn nào.") }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.transferAll(s.lines, to) }
                .onSuccess { r ->
                    _state.update {
                        it.copy(
                            busy = false,
                            done = r.skipped.isEmpty(),
                            toast = if (r.skipped.isEmpty()) {
                                HtMsg("htTransferDoneN", "Đã chuyển {0} dòng sang {1}.", r.updated, to)
                            } else {
                                HtMsg(
                                    "htBatchPartial", "Đã xử lý {0} dòng. Lỗi: {1}",
                                    r.updated, r.skipped.joinToString(", "),
                                )
                            },
                            lines = it.lines.filter { l -> l.key in r.skipped },
                        )
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }
}

data class TransferState(
    val zones: List<Zone> = emptyList(),
    val locations: List<Location> = emptyList(),
    val zoneCode: String? = null,
    val toLocationCode: String? = null,
    val lines: List<TransferLine> = emptyList(),
    val busy: Boolean = false,
    val error: Pair<String, String>? = null,
    val toast: HtMsg? = null,
    val done: Boolean = false,
) {
    val empty: Boolean get() = lines.isEmpty()

    val totalQty: Double get() = lines.sumOf { it.qty }

    val canProceed: Boolean get() = !toLocationCode.isNullOrBlank()
}

private fun Throwable.toPair(): Pair<String, String> = when (this) {
    is com.imssolutions.ht.data.ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}
