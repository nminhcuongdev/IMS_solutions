package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.ui.HtMsg
import com.imssolutions.ht.data.DisposalLine
import com.imssolutions.ht.data.InventoryRepository
import com.imssolutions.ht.data.Reason
import com.imssolutions.ht.scan.QrPayload
import com.imssolutions.ht.scan.QrScan

class DisposalViewModel : ViewModel() {

    private val repo = InventoryRepository()
    private val _state = MutableStateFlow(DisposalState())
    val state: StateFlow<DisposalState> = _state.asStateFlow()

    init { loadReasons() }

    private fun loadReasons() {
        viewModelScope.launch {
            runCatching { repo.disposalReasons() }
                .onSuccess { rs -> _state.update { it.copy(reasons = rs) } }
                .onFailure { e -> _state.update { it.copy(error = e.toPair()) } }
        }
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
            runCatching { repo.resolveScan(itemCode, lot) }
                .onSuccess { line ->
                    if (line == null) {
                        _state.update {
                            it.copy(
                                busy = false,
                                error = "SYS-404" to itemCode,
                            )
                        }
                    } else {
                        val fromLabel = payload.variableCount?.toDoubleOrNull()
                        val qty = (fromLabel ?: 1.0).coerceAtMost(line.stockQty)
                        _state.update {
                            it.copy(busy = false, lines = it.lines + line.copy(qty = qty))
                        }
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }

    fun setQty(key: String, raw: String) {
        val v = raw.trim().toDoubleOrNull() ?: return
        _state.update { s ->
            s.copy(lines = s.lines.map { if (it.key == key) it.copy(qty = v) else it })
        }
    }

    fun remove(key: String) =
        _state.update { s -> s.copy(lines = s.lines.filterNot { it.key == key }) }

    fun setReason(code: String) = _state.update { it.copy(reasonCode = code, error = null) }

    fun clearError() = _state.update { it.copy(error = null) }

    fun clearToast() = _state.update { it.copy(toast = null) }

    fun reset() = _state.update {
        DisposalState(reasons = it.reasons)
    }

    fun confirm() {
        val s = _state.value
        if (s.busy) return
        if (s.lines.isEmpty()) {
            _state.update { it.copy(error = "STK-005" to "Chưa quét nhãn nào.") }
            return
        }
        val reason = s.reasonCode?.takeIf { it.isNotBlank() }
        if (reason == null) {
            _state.update { it.copy(error = "RSN-001" to "Vui lòng chọn lý do huỷ.") }
            return
        }
        s.lines.firstOrNull { it.qty <= 0 || it.overStock }?.let { bad ->
            _state.update {
                it.copy(error = "QTY-001" to "${bad.itemCode}: số lượng phải trong khoảng 1..${fmt(bad.stockQty)}.")
            }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.disposeAll(s.lines, reason) }
                .onSuccess { r ->
                    _state.update {
                        it.copy(
                            busy = false,
                            done = r.skipped.isEmpty(),
                            toast = if (r.skipped.isEmpty()) {
                                HtMsg("htDisposalDoneN", "Đã huỷ {0} dòng.", r.updated)
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

data class DisposalState(
    val lines: List<DisposalLine> = emptyList(),
    val reasons: List<Reason> = emptyList(),
    val reasonCode: String? = null,
    val busy: Boolean = false,
    val error: Pair<String, String>? = null,
    val toast: HtMsg? = null,
    val done: Boolean = false,
) {
    val empty: Boolean get() = lines.isEmpty()

    val totalQty: Double get() = lines.sumOf { it.qty }

    val hasBadQty: Boolean get() = lines.any { it.qty <= 0 || it.overStock }
}

private fun Throwable.toPair(): Pair<String, String> = when (this) {
    is com.imssolutions.ht.data.ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}
