package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.ui.HtMsg
import com.imssolutions.ht.data.PickingPlan
import com.imssolutions.ht.data.PickingRepository
import com.imssolutions.ht.data.STATUS_PICKED


private fun Throwable.toPair(): Pair<String, String> = when (this) {
    is com.imssolutions.ht.data.ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}


enum class PickFilter(val labelKey: String, val label: String) {
    ALL("htFilterAll", "All"),
    COMPLETED("htFilterPickCompleted", "Picked"),
    SHORTAGE("htFilterShortage", "Shortage"),
    NOT_COMPLETED("htFilterPickNotCompleted", "Not Picked"),
    ACTIVE("htFilterPickActive", "Not Picked + Shortage"),
}

data class PickingListState(
    val loading: Boolean = true,
    val lines: List<PickingPlan> = emptyList(),
    val filter: PickFilter = PickFilter.ACTIVE,
    val date: String? = null,
    val query: String = "",
    val error: Pair<String, String>? = null,
) {
    val visible: List<PickingPlan> = lines.filter { l ->
        when (filter) {
            PickFilter.ALL -> true
            PickFilter.COMPLETED -> l.status == STATUS_PICKED
            PickFilter.SHORTAGE -> l.isPartial
            PickFilter.NOT_COMPLETED -> l.isOpen && !l.isPartial
            PickFilter.ACTIVE -> l.isOpen
        }
    }.filter { it.soNo.contains(query.trim(), ignoreCase = true) }
}

class PickingListViewModel : ViewModel() {
    private val repo = PickingRepository()
    private val _state = MutableStateFlow(PickingListState())
    val state: StateFlow<PickingListState> = _state.asStateFlow()

    init { load() }

    fun setFilter(v: PickFilter) = _state.update { it.copy(filter = v) }
    fun setQuery(v: String) = _state.update { it.copy(query = v) }

    fun setDate(v: String?) { _state.update { it.copy(date = v) }; load() }

    fun load() {
        val s = _state.value
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.listAllPlans(date = s.date) }
                .onSuccess { r -> _state.update { it.copy(loading = false, lines = r.data) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }
}


data class PickingDetailState(
    val loading: Boolean = true,
    val line: PickingPlan? = null,
    val actualQty: String = "",
    val locationCode: String = "",
    val lotNo: String = "",
    val lotFromLabel: Boolean = false,
    val busy: Boolean = false,
    val error: Pair<String, String>? = null,
    val toast: HtMsg? = null,
    val done: Boolean = false,
) {
    val planned: Double get() = line?.plannedQty ?: 0.0

    val diff: Double? get() = actualQty.trim().toDoubleOrNull()?.let { it - planned }

    val short: Boolean get() = (diff ?: 0.0) < 0

    val overPicked: Boolean get() = (diff ?: 0.0) > 0

    val locationEditable: Boolean get() = line?.locationCode.isNullOrBlank()
}

class PickingDetailViewModel : ViewModel() {
    private val repo = PickingRepository()
    private val _state = MutableStateFlow(PickingDetailState())
    val state: StateFlow<PickingDetailState> = _state.asStateFlow()

    fun setActual(v: String) = _state.update { it.copy(actualQty = v, error = null) }
    fun setLocation(v: String) = _state.update { it.copy(locationCode = v, error = null) }
    fun setLot(v: String) = _state.update { it.copy(lotNo = v, error = null) }

    fun onScan(raw: String) {
        val line = _state.value.line ?: return
        val payload = when (val r = com.imssolutions.ht.scan.QrPayload.parse(raw)) {
            is com.imssolutions.ht.scan.QrScan.Ok -> r.payload
            is com.imssolutions.ht.scan.QrScan.Invalid -> {
                if (_state.value.locationEditable) setLocation(raw.trim())
                else _state.update { it.copy(error = "SCN-001" to "The scanned code is not in a valid format.") }
                return
            }
        }
        if (payload.kind == com.imssolutions.ht.scan.QrLabelKind.Location) {
            if (_state.value.locationEditable) setLocation(payload.locationCode.orEmpty())
            return
        }
        val item = payload.itemCode?.trim()
        if (item.isNullOrEmpty()) {
            _state.update { it.copy(error = "VAL-001" to "fldItemCode") }
            return
        }
        if (!item.equals(line.itemCode, ignoreCase = true)) {
            _state.update { it.copy(error = "SCN-005" to "fldItemCode") }
            return
        }
        val lot = payload.lotNo?.trim().orEmpty()
        _state.update { it.copy(lotNo = lot, lotFromLabel = lot.isNotEmpty(), error = null) }
    }
    fun clearToast() = _state.update { it.copy(toast = null) }
    fun clearError() = _state.update { it.copy(error = null) }

    fun load(soNo: String, serialNo: String) {
        _state.update { it.copy(loading = true, error = null, done = false) }
        viewModelScope.launch {
            runCatching { repo.line(soNo, serialNo) }
                .onSuccess { line ->
                    _state.update {
                        it.copy(
                            loading = false,
                            line = line,
                            actualQty = "",
                            locationCode = "",
                            lotNo = "",
                            lotFromLabel = false,
                            error = if (line == null) {
                                "SYS-404" to serialNo
                            } else null,
                        )
                    }
                }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }

    fun confirm() {
        val s = _state.value
        val line = s.line ?: return
        if (s.busy) return

        val qty = s.actualQty.trim().toDoubleOrNull()
        if (qty == null || qty < 0) {
            _state.update { it.copy(error = "QTY-001" to "Please enter a valid quantity (0 or more).") }
            return
        }
        if (s.locationEditable && s.locationCode.isBlank()) {
            _state.update {
                it.copy(error = "LOC-001" to "Please enter a Location.")
            }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.confirmLine(line, qty, s.locationCode, s.lotNo) }
                .onSuccess { row ->
                    val msg = if (qty >= line.plannedQty) {
                        HtMsg("htPickedFull", "Picked all {0}", fmt(row.qty))
                    } else {
                        HtMsg(
                            "htPickedShort", "Saved {0}/{1}; the line is still open",
                            fmt(row.qty), fmt(line.plannedQty),
                        )
                    }
                    _state.update { it.copy(busy = false, toast = msg, done = true) }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }
}
