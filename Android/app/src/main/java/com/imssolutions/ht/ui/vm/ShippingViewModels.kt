package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.ui.HtMsg
import com.imssolutions.ht.data.HtConfig
import com.imssolutions.ht.data.Reason
import com.imssolutions.ht.data.ReceivingRepository
import com.imssolutions.ht.data.STATUS_CANCELLED
import com.imssolutions.ht.data.STATUS_NOT_SHIPPED
import com.imssolutions.ht.data.STATUS_PARTIAL_SHIP
import com.imssolutions.ht.data.STATUS_SHIPPED
import com.imssolutions.ht.data.ShippingPlan
import com.imssolutions.ht.data.ShippingRepository


private fun Throwable.toPair(): Pair<String, String> = when (this) {
    is com.imssolutions.ht.data.ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}


enum class ShpFilter(val labelKey: String, val label: String) {
    ALL("htFilterAll", "All"),
    COMPLETED("htFilterShpCompleted", "Shipped"),
    SHORTAGE("htFilterShpShortage", "Partial"),
    NOT_COMPLETED("htFilterShpNotCompleted", "Not Shipped"),
    ACTIVE("htFilterShpActive", "Not Shipped + Partial"),
}

data class ShippingListState(
    val loading: Boolean = true,
    val plans: List<ShippingPlan> = emptyList(),
    val filter: ShpFilter = ShpFilter.ACTIVE,
    val date: String? = null,
    val query: String = "",
    val error: Pair<String, String>? = null,
) {
    private val allSlips: List<ShpSlip> = plans.groupBy { it.slipNo }.map { (no, lines) ->
        ShpSlip(
            slipNo = no,
            customerName = lines.firstNotNullOfOrNull { it.customerName },
            scheduledDate = lines.firstNotNullOfOrNull { it.scheduledDate },
            gateCode = lines.firstNotNullOfOrNull { it.gateCode },
            lineCount = lines.size,
            status = worstShippingStatus(lines),
        )
    }.sortedBy { it.scheduledDate }

    val slips: List<ShpSlip> = allSlips.filter { s ->
        when (filter) {
            ShpFilter.ALL -> true
            ShpFilter.COMPLETED -> s.status == STATUS_SHIPPED
            ShpFilter.SHORTAGE -> s.status == STATUS_PARTIAL_SHIP
            ShpFilter.NOT_COMPLETED -> s.status == STATUS_NOT_SHIPPED
            ShpFilter.ACTIVE -> s.status == STATUS_NOT_SHIPPED || s.status == STATUS_PARTIAL_SHIP
        }
    }.filter { it.slipNo.contains(query.trim(), ignoreCase = true) }
}

data class ShpSlip(
    val slipNo: String,
    val customerName: String?,
    val scheduledDate: String?,
    val gateCode: String?,
    val lineCount: Int,
    val status: String,
)

fun worstShippingStatus(lines: List<ShippingPlan>): String = when {
    lines.any { it.status == STATUS_PARTIAL_SHIP } -> STATUS_PARTIAL_SHIP
    lines.any { it.status == STATUS_NOT_SHIPPED } -> STATUS_NOT_SHIPPED
    lines.any { it.status == STATUS_SHIPPED } -> STATUS_SHIPPED
    else -> STATUS_CANCELLED
}

class ShippingListViewModel : ViewModel() {
    private val repo = ShippingRepository()
    private val _state = MutableStateFlow(ShippingListState())
    val state: StateFlow<ShippingListState> = _state.asStateFlow()

    init { load() }

    fun setFilter(v: ShpFilter) = _state.update { it.copy(filter = v) }
    fun setQuery(v: String) = _state.update { it.copy(query = v) }

    fun setDate(v: String?) { _state.update { it.copy(date = v) }; load() }

    fun load() {
        val s = _state.value
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.listAllPlans(date = s.date) }
                .onSuccess { r -> _state.update { it.copy(loading = false, plans = r.data) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }
}


data class ShippingItemsState(
    val loading: Boolean = true,
    val slipNo: String = "",
    val lines: List<ShippingPlan> = emptyList(),
    val filter: ItemFilter = ItemFilter.ACTIVE,
    val error: Pair<String, String>? = null,
) {
    val visible: List<ShippingPlan> = lines.filter {
        when (filter) {
            ItemFilter.ALL -> true
            ItemFilter.COMPLETED -> it.status == STATUS_SHIPPED
            ItemFilter.ACTIVE -> it.status == STATUS_NOT_SHIPPED || it.status == STATUS_PARTIAL_SHIP
        }
    }
}

class ShippingItemsViewModel : ViewModel() {
    private val repo = ShippingRepository()
    private val _state = MutableStateFlow(ShippingItemsState())
    val state: StateFlow<ShippingItemsState> = _state.asStateFlow()

    fun setFilter(v: ItemFilter) = _state.update { it.copy(filter = v) }

    fun load(slipNo: String) {
        _state.update { it.copy(loading = true, slipNo = slipNo, error = null) }
        viewModelScope.launch {
            runCatching { repo.linesOf(slipNo) }
                .onSuccess { rows -> _state.update { it.copy(loading = false, lines = rows) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }
}


data class ShippingDetailState(
    val loading: Boolean = true,
    val line: ShippingPlan? = null,
    val reasons: List<Reason> = emptyList(),
    val config: HtConfig = HtConfig(),
    val lotNo: String = "",
    val actualQty: String = "",
    val reasonCode: String? = null,
    val busy: Boolean = false,
    val error: Pair<String, String>? = null,
    val toast: HtMsg? = null,
    val done: Boolean = false,
    val fromScan: Boolean = false,
) {
    val planned: Double get() = line?.plannedQty ?: 0.0

    val diff: Double? get() = actualQty.trim().toDoubleOrNull()?.let { it - planned }

    val needsReason: Boolean get() = (diff ?: 0.0) < 0

    val overShipped: Boolean get() = (diff ?: 0.0) > 0

    val showLabelButton: Boolean get() = config.labelModeShp != "preIssue"

    val labelIssued: Boolean get() = line?.labelIssued == true

    val canEditActual: Boolean get() = true

    val showLabel: Boolean get() = !fromScan && showLabelButton && !labelIssued
}

class ShippingDetailViewModel : ViewModel() {
    private val repo = ShippingRepository()
    private val receiving = ReceivingRepository()
    private val _state = MutableStateFlow(ShippingDetailState())
    val state: StateFlow<ShippingDetailState> = _state.asStateFlow()

    fun setLot(v: String) = _state.update { it.copy(lotNo = v, error = null) }
    fun setReason(v: String?) = _state.update { it.copy(reasonCode = v, error = null) }
    fun clearToast() = _state.update { it.copy(toast = null) }
    fun clearError() = _state.update { it.copy(error = null) }

    fun setActual(v: String) = _state.update {
        val diff = v.trim().toDoubleOrNull()?.minus(it.planned)
        it.copy(
            actualQty = v, error = null,
            reasonCode = if ((diff ?: 0.0) >= 0) null else it.reasonCode,
        )
    }

    fun load(slipNo: String, serialNo: String, scannedQty: String = "", scannedLot: String = "") {
        _state.update { it.copy(loading = true, error = null, done = false) }
        viewModelScope.launch {
            runCatching {
                Triple(repo.linesOf(slipNo), repo.shortageReasons(), receiving.config())
            }.onSuccess { (lines, reasons, cfg) ->
                val line = lines.firstOrNull { it.serialNo == serialNo }
                _state.update {
                    it.copy(
                        loading = false,
                        line = line,
                        reasons = reasons,
                        config = cfg,
                        lotNo = scannedLot,
                        actualQty = "",
                        fromScan = scannedQty.isNotBlank() || scannedLot.isNotBlank(),
                        reasonCode = null,
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
            _state.update { it.copy(error = "QTY-001" to "Please enter a valid quantity (0 or more).") }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.confirmLine(line, qty, s.lotNo, s.reasonCode) }
                .onSuccess { row ->
                    val msg = if (row.status == STATUS_SHIPPED) {
                        HtMsg("htShippedFull", "Shipped in full: {0}", fmt(row.qty))
                    } else {
                        HtMsg("htShippedShort", "Saved a short shipment: {0}", fmt(row.qty))
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
            _state.update { it.copy(error = "QTY-001" to "Please enter a valid quantity (0 or more).") }
            return
        }
        if (printerDeviceCode.isNullOrBlank()) {
            _state.update { it.copy(error = "PRN-001" to "Select a printer in Settings.") }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.issueLabel(line, qty, printerDeviceCode) }
                .onSuccess {
                    _state.update {
                        it.copy(
                            busy = false,
                            toast = HtMsg("htLabelIssueDoneN", "Label printed ({0})", printerDeviceCode),
                            line = it.line?.copy(labelIssued = true),
                        )
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }
}
