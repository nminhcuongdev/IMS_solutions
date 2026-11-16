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
import com.imssolutions.ht.data.StockLine
import com.imssolutions.ht.data.StockScanRules
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
            _state.update { it.copy(error = "LOC-001" to "Please enter a Location.") }
            return false
        }
        _state.update { it.copy(lines = emptyList(), error = null, toast = null, done = false) }
        return true
    }

    fun onScan(raw: String) {
        val payload = when (val r = QrPayload.parse(raw)) {
            is QrScan.Ok -> r.payload
            is QrScan.Invalid -> {
                _state.update { it.copy(error = "SCN-001" to "The scanned code is not in a valid format.") }
                return
            }
        }
        val itemCode = payload.itemCode?.trim().orEmpty()
        if (itemCode.isEmpty()) {
            _state.update { it.copy(error = "VAL-001" to "fldItemCode") }
            return
        }
        val lot = payload.lotNo?.trim()
        val serial = payload.serialNo?.trim()?.takeIf { it.isNotEmpty() }

        if (_state.value.busy) return
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.candidates(itemCode, lot, serial) }
                .onSuccess { found ->
                    val fresh = found.filter { c -> _state.value.lines.none { it.serialNo == c.serialNo && it.itemCode == c.itemCode } }
                    when {
                        found.isEmpty() -> _state.update {
                            it.copy(busy = false, error = if (serial != null) "SCN-003" to serial else "SYS-404" to itemCode)
                        }
                        fresh.isEmpty() -> _state.update {
                            it.copy(busy = false, error = "SCN-007" to "This label has already been scanned or used.")
                        }
                        fresh.size == 1 -> add(fresh[0])
                        else -> _state.update { it.copy(busy = false, choices = fresh) }
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }

    fun choose(serialNo: String) {
        val line = _state.value.choices.firstOrNull { it.serialNo == serialNo } ?: return
        add(line)
    }

    fun dismissChoices() = _state.update { it.copy(choices = emptyList()) }

    private fun add(line: StockLine) {
        if (line.locationCode.equals(_state.value.toLocationCode, ignoreCase = true)) {
            _state.update { it.copy(busy = false, choices = emptyList(), error = "LOC-001" to "Please enter a Location.") }
            return
        }
        _state.update {
            it.copy(busy = false, choices = emptyList(), lines = it.lines + StockScanRules.toTransfer(line))
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
            _state.update { it.copy(error = "LOC-001" to "Please enter a Location.") }
            return
        }
        if (s.lines.isEmpty()) {
            _state.update { it.copy(error = "STK-005" to "No labels have been scanned yet.") }
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
                                HtMsg("htTransferDoneN", "Moved {0} rows to {1}.", r.updated, to)
                            } else {
                                HtMsg(
                                    "htBatchPartial", "Processed {0} rows. Failed: {1}",
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
    val choices: List<StockLine> = emptyList(),
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
