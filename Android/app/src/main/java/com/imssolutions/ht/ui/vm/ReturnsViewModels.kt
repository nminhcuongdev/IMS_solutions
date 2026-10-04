package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.ui.HtMsg
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.data.ApiException
import com.imssolutions.ht.data.HtConfig
import com.imssolutions.ht.data.Location
import com.imssolutions.ht.data.ReceivingRepository
import com.imssolutions.ht.data.ReturnPlan
import com.imssolutions.ht.data.ReturnsRepository
import com.imssolutions.ht.data.STATUS_CANCELLED
import com.imssolutions.ht.data.STATUS_PARTIAL_RETURN
import com.imssolutions.ht.data.STATUS_PENDING_RETURN
import com.imssolutions.ht.data.STATUS_RETURNED


private fun Throwable.toPair(): Pair<String, String> = when (this) {
    is ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}


enum class RtnFilter(val labelKey: String, val label: String) {
    ALL("htFilterAll", "All"),
    COMPLETED("htFilterRtnCompleted", "Returned"),
    SHORTAGE("htFilterRtnShortage", "Partial"),
    NOT_COMPLETED("htFilterRtnNotCompleted", "Pending"),
    ACTIVE("htFilterRtnActive", "Pending + Partial"),
}

data class ReturnListState(
    val loading: Boolean = true,
    val plans: List<ReturnPlan> = emptyList(),
    val filter: RtnFilter = RtnFilter.ACTIVE,
    val date: String? = null,
    val query: String = "",
    val error: Pair<String, String>? = null,
) {
    private val allSlips: List<RtnSlip> = plans.groupBy { it.returnNo }.map { (no, lines) ->
        RtnSlip(
            returnNo = no,
            returnedByName = lines.firstNotNullOfOrNull { it.returnedByName },
            returnDate = lines.firstNotNullOfOrNull { it.returnDate },
            lineCount = lines.size,
            status = worstReturnStatus(lines),
        )
    }.sortedBy { it.returnDate }

    val slips: List<RtnSlip> = allSlips.filter { s ->
        when (filter) {
            RtnFilter.ALL -> true
            RtnFilter.COMPLETED -> s.status == STATUS_RETURNED
            RtnFilter.SHORTAGE -> s.status == STATUS_PARTIAL_RETURN
            RtnFilter.NOT_COMPLETED -> s.status == STATUS_PENDING_RETURN
            RtnFilter.ACTIVE -> s.status == STATUS_PENDING_RETURN || s.status == STATUS_PARTIAL_RETURN
        }
    }.filter { it.returnNo.contains(query.trim(), ignoreCase = true) }
}

data class RtnSlip(
    val returnNo: String,
    val returnedByName: String?,
    val returnDate: String?,
    val lineCount: Int,
    val status: String,
)

fun worstReturnStatus(lines: List<ReturnPlan>): String = when {
    lines.any { it.status == STATUS_PARTIAL_RETURN } -> STATUS_PARTIAL_RETURN
    lines.any { it.status == STATUS_PENDING_RETURN } -> STATUS_PENDING_RETURN
    lines.any { it.status == STATUS_RETURNED } -> STATUS_RETURNED
    else -> STATUS_CANCELLED
}

class ReturnListViewModel : ViewModel() {
    private val repo = ReturnsRepository()
    private val _state = MutableStateFlow(ReturnListState())
    val state: StateFlow<ReturnListState> = _state.asStateFlow()

    init { load() }

    fun setFilter(v: RtnFilter) = _state.update { it.copy(filter = v) }
    fun setQuery(v: String) = _state.update { it.copy(query = v) }

    fun setDate(v: String?) { _state.update { it.copy(date = v) }; load() }

    fun load() {
        val s = _state.value
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.listPlans(date = s.date) }
                .onSuccess { r -> _state.update { it.copy(loading = false, plans = r.data) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }
}


data class ReturnItemsState(
    val loading: Boolean = true,
    val returnNo: String = "",
    val lines: List<ReturnPlan> = emptyList(),
    val filter: ItemFilter = ItemFilter.ACTIVE,
    val error: Pair<String, String>? = null,
) {
    val visible: List<ReturnPlan> = lines.filter {
        when (filter) {
            ItemFilter.ALL -> true
            ItemFilter.COMPLETED -> it.status == STATUS_RETURNED
            ItemFilter.ACTIVE -> it.status == STATUS_PENDING_RETURN || it.status == STATUS_PARTIAL_RETURN
        }
    }
}

class ReturnItemsViewModel : ViewModel() {
    private val repo = ReturnsRepository()
    private val _state = MutableStateFlow(ReturnItemsState())
    val state: StateFlow<ReturnItemsState> = _state.asStateFlow()

    fun setFilter(v: ItemFilter) = _state.update { it.copy(filter = v) }

    fun load(returnNo: String) {
        _state.update { it.copy(loading = true, returnNo = returnNo, error = null) }
        viewModelScope.launch {
            runCatching { repo.linesOf(returnNo) }
                .onSuccess { rows -> _state.update { it.copy(loading = false, lines = rows) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }
}


data class ReturnDetailState(
    val loading: Boolean = true,
    val line: ReturnPlan? = null,
    val locations: List<Location> = emptyList(),
    val config: HtConfig = HtConfig(),
    val actualQty: String = "",
    val locationCode: String = "",
    val lotInput: String = "",
    val busy: Boolean = false,
    val error: Pair<String, String>? = null,
    val toast: HtMsg? = null,
    val done: Boolean = false,
    val fromScan: Boolean = false,
) {
    val planned: Double get() = line?.expectedQty ?: 0.0

    val diff: Double? get() = actualQty.trim().toDoubleOrNull()?.let { it - planned }

    val lotNo: String get() = line?.lotNo?.takeIf { it.isNotBlank() } ?: lotInput.trim()

    val lotEditable: Boolean get() = line?.lotNo.isNullOrBlank()

    val showLabelButton: Boolean get() = config.labelModeRtn != "preIssue"

    val labelIssued: Boolean get() = line?.labelIssued == true

    val canEditActual: Boolean get() = !fromScan

    val showLabel: Boolean get() = !fromScan && showLabelButton && !labelIssued
}

class ReturnDetailViewModel : ViewModel() {
    private val repo = ReturnsRepository()
    private val receiving = ReceivingRepository()
    private val _state = MutableStateFlow(ReturnDetailState())
    val state: StateFlow<ReturnDetailState> = _state.asStateFlow()

    fun setActual(v: String) = _state.update { it.copy(actualQty = v, error = null) }
    fun setLot(v: String) = _state.update { it.copy(lotInput = v, error = null) }
    fun setLocation(v: String) = _state.update { it.copy(locationCode = v, error = null) }
    fun clearToast() = _state.update { it.copy(toast = null) }
    fun clearError() = _state.update { it.copy(error = null) }

    fun load(returnNo: String, serialNo: String, scannedQty: String = "", scannedLot: String = "") {
        _state.update { it.copy(loading = true, error = null, done = false) }
        viewModelScope.launch {
            runCatching {
                Triple(repo.linesOf(returnNo), repo.locations(), receiving.config())
            }.onSuccess { (lines, locs, cfg) ->
                val line = lines.firstOrNull { it.serialNo == serialNo }
                _state.update {
                    it.copy(
                        loading = false,
                        line = line,
                        locations = locs,
                        config = cfg,
                        actualQty = scannedQty,
                        fromScan = scannedQty.isNotBlank(),
                        lotInput = scannedLot,
                        locationCode = "",
                        error = if (line == null) "SYS-404" to serialNo else null,
                    )
                }
            }.onFailure { e ->
                _state.update { it.copy(loading = false, error = e.toPair()) }
            }
        }
    }

    fun confirm() {
        val s = _state.value
        val line = s.line ?: return
        if (s.busy) return

        val qty = s.actualQty.trim().toDoubleOrNull()
        if (qty == null || qty < 0) {
            _state.update { it.copy(error = "QTY-001" to "Vui lòng nhập số lượng hợp lệ (từ 0 trở lên).") }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.confirmLine(line, qty, s.locationCode, s.lotNo) }
                .onSuccess { row ->
                    val msg = if (row.status == STATUS_RETURNED) {
                        HtMsg("htReturnedFull", "Đã nhận lại đủ {0}", fmt(row.qty))
                    } else {
                        HtMsg("htReturnedShort", "Đã ghi nhận lại thiếu: {0}", fmt(row.qty))
                    }
                    _state.update { it.copy(busy = false, toast = msg, done = true) }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }

    fun issueLabel(printerDeviceCode: String?) {
        val s = _state.value
        val line = s.line ?: return
        if (s.busy) return

        val qty = s.actualQty.trim().toDoubleOrNull()
        if (qty == null || qty <= 0) {
            _state.update { it.copy(error = "QTY-001" to "Vui lòng nhập số lượng hợp lệ (từ 0 trở lên).") }
            return
        }
        if (printerDeviceCode.isNullOrBlank()) {
            _state.update { it.copy(error = "PRN-001" to "Chưa chọn máy in. Vào Settings để đặt.") }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.issueLabel(line, qty, printerDeviceCode) }
                .onSuccess {
                    _state.update {
                        it.copy(
                            busy = false,
                            toast = HtMsg("htLabelIssueDoneN", "Đã ghi phát hành nhãn ({0})", printerDeviceCode),
                            line = it.line?.copy(labelIssued = true),
                        )
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }
}
