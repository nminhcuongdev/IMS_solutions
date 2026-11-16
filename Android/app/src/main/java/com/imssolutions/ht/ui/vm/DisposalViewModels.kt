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
import com.imssolutions.ht.data.StockLine
import com.imssolutions.ht.data.StockScanRules
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
        val fromLabel = payload.variableCount?.toDoubleOrNull()

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
                        fresh.size == 1 -> add(fresh[0], fromLabel)
                        else -> _state.update { it.copy(busy = false, choices = fresh, pendingQty = fromLabel) }
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }

    fun choose(serialNo: String) {
        val s = _state.value
        val line = s.choices.firstOrNull { it.serialNo == serialNo } ?: return
        add(line, s.pendingQty)
    }

    fun dismissChoices() = _state.update { it.copy(choices = emptyList(), pendingQty = null) }

    private fun add(line: StockLine, fromLabel: Double?) {
        val disposal = StockScanRules.toDisposal(line)
        val qty = (fromLabel ?: 1.0).coerceAtMost(disposal.stockQty)
        _state.update {
            it.copy(busy = false, choices = emptyList(), pendingQty = null, lines = it.lines + disposal.copy(qty = qty))
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
            _state.update { it.copy(error = "STK-005" to "No labels have been scanned yet.") }
            return
        }
        val reason = s.reasonCode?.takeIf { it.isNotBlank() }
        if (reason == null) {
            _state.update { it.copy(error = "RSN-001" to "Please select a reason.") }
            return
        }
        s.lines.firstOrNull { it.qty <= 0 }?.let {
            _state.update { st -> st.copy(error = "QTY-003" to "Please enter a quantity greater than 0.") }
            return
        }
        s.lines.firstOrNull { it.overStock }?.let {
            _state.update { st -> st.copy(error = "INV-009" to "There is not enough stock for the entered quantity.") }
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
                                HtMsg("htDisposalDoneN", "Disposed {0} rows.", r.updated)
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

data class DisposalState(
    val lines: List<DisposalLine> = emptyList(),
    val reasons: List<Reason> = emptyList(),
    val reasonCode: String? = null,
    val choices: List<StockLine> = emptyList(),
    val pendingQty: Double? = null,
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
