package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.update.UpdateRules
import com.imssolutions.ht.ui.HtMsg
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.data.ApiException
import com.imssolutions.ht.data.ServerAddress
import com.imssolutions.ht.data.HtConfig
import com.imssolutions.ht.data.LoginRequest
import com.imssolutions.ht.data.Reason
import com.imssolutions.ht.data.ReceivingPlan
import com.imssolutions.ht.data.ReceivingRepository
import com.imssolutions.ht.data.STATUS_ARRIVED
import com.imssolutions.ht.data.STATUS_CANCELLED
import com.imssolutions.ht.data.STATUS_NOT_ARRIVED
import com.imssolutions.ht.data.STATUS_PARTIAL

private fun Throwable.toPair(): Pair<String, String> = when (this) {
    is ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}


data class LoginState(
    val userCode: String = "",
    val password: String = "",
    val rememberMe: Boolean = true,
    val loading: Boolean = false,
    val error: Pair<String, String>? = null,
    val success: Boolean = false,
)

class LoginViewModel : ViewModel() {
    private val _state = MutableStateFlow(LoginState())
    val state: StateFlow<LoginState> = _state.asStateFlow()

    init {
        viewModelScope.launch {
            val r = ApiClient.tokens.remembered.first()
            _state.update { it.copy(rememberMe = r.enabled, userCode = it.userCode.ifEmpty { r.userCode }) }
        }
    }

    fun setUserCode(v: String) = _state.update { it.copy(userCode = v, error = null) }
    fun setPassword(v: String) = _state.update { it.copy(password = v, error = null) }
    fun setRememberMe(v: Boolean) = _state.update { it.copy(rememberMe = v) }

    fun submit() {
        val s = _state.value
        if (s.loading) return
        if (s.userCode.isBlank()) {
            _state.update { it.copy(error = "AUTH-001" to "Vui lòng nhập User ID.") }
            return
        }
        if (s.password.isBlank()) {
            _state.update { it.copy(error = "AUTH-002" to "Vui lòng nhập cả User ID và mật khẩu.") }
            return
        }
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching {
                ApiClient.call { login(LoginRequest(userId = s.userCode.trim(), password = s.password)) }.data
            }.onSuccess { r ->
                if (r == null) {
                    _state.update { it.copy(loading = false, error = "ERR" to "Máy chủ trả về dữ liệu rỗng.") }
                } else {
                    ApiClient.tokens.save(r.token, r.userCode, r.userName, r.defaultLanguage)
                    ApiClient.tokens.saveRemembered(s.rememberMe, s.userCode.trim())
                    _state.update { it.copy(loading = false, success = true) }
                }
            }.onFailure { e ->
                _state.update { it.copy(loading = false, error = e.toPair()) }
            }
        }
    }
}


enum class PoFilter(val labelKey: String, val label: String) {
    ALL("htFilterAll", "All"),
    COMPLETED("htFilterRcvCompleted", "Arrived"),
    SHORTAGE("htFilterRcvShortage", "Partial"),
    NOT_COMPLETED("htFilterRcvNotCompleted", "Not Arrived"),
    ACTIVE("htFilterRcvActive", "Not Arrived + Partial"),
}

data class ReceivingListState(
    val loading: Boolean = true,
    val plans: List<ReceivingPlan> = emptyList(),
    val filter: PoFilter = PoFilter.ACTIVE,
    val date: String? = null,
    val query: String = "",
    val error: Pair<String, String>? = null,
) {
    private val allSlips: List<Slip> = plans.groupBy { it.poNo }.map { (po, lines) ->
        Slip(
            poNo = po,
            supplierName = lines.firstNotNullOfOrNull { it.supplierName },
            scheduledDate = lines.firstNotNullOfOrNull { it.scheduledDate },
            lineCount = lines.size,
            status = worstStatus(lines),
        )
    }.sortedBy { it.scheduledDate }

    val slips: List<Slip> = allSlips.filter { s ->
        when (filter) {
            PoFilter.ALL -> true
            PoFilter.COMPLETED -> s.status == STATUS_ARRIVED
            PoFilter.SHORTAGE -> s.status == STATUS_PARTIAL
            PoFilter.NOT_COMPLETED -> s.status == STATUS_NOT_ARRIVED
            PoFilter.ACTIVE -> s.status == STATUS_NOT_ARRIVED || s.status == STATUS_PARTIAL
        }
    }.filter { it.poNo.contains(query.trim(), ignoreCase = true) }
}

data class Slip(
    val poNo: String,
    val supplierName: String?,
    val scheduledDate: String?,
    val lineCount: Int,
    val status: String,
)

fun worstStatus(lines: List<ReceivingPlan>): String = when {
    lines.any { it.status == STATUS_PARTIAL } -> STATUS_PARTIAL
    lines.any { it.status == STATUS_NOT_ARRIVED } -> STATUS_NOT_ARRIVED
    lines.any { it.status == STATUS_ARRIVED } -> STATUS_ARRIVED
    else -> STATUS_CANCELLED
}

class ReceivingListViewModel : ViewModel() {
    private val repo = ReceivingRepository()
    private val _state = MutableStateFlow(ReceivingListState())
    val state: StateFlow<ReceivingListState> = _state.asStateFlow()

    init { load() }

    fun setFilter(v: PoFilter) = _state.update { it.copy(filter = v) }
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


enum class ItemFilter(val labelKey: String, val label: String) {
    ACTIVE("htFilterActive", "Not Completed + Shortage"),
    ALL("htFilterAll", "All"),
    COMPLETED("htFilterCompleted", "Completed"),
}

data class ReceivingItemsState(
    val loading: Boolean = true,
    val poNo: String = "",
    val lines: List<ReceivingPlan> = emptyList(),
    val filter: ItemFilter = ItemFilter.ACTIVE,
    val error: Pair<String, String>? = null,
) {
    val visible: List<ReceivingPlan> = lines.filter {
        when (filter) {
            ItemFilter.ALL -> true
            ItemFilter.COMPLETED -> it.status == STATUS_ARRIVED
            ItemFilter.ACTIVE -> it.status == STATUS_NOT_ARRIVED || it.status == STATUS_PARTIAL
        }
    }

    val supplierName: String? = lines.firstNotNullOfOrNull { it.supplierName }
    val scheduledDate: String? = lines.firstNotNullOfOrNull { it.scheduledDate }
}

class ReceivingItemsViewModel : ViewModel() {
    private val repo = ReceivingRepository()
    private val _state = MutableStateFlow(ReceivingItemsState())
    val state: StateFlow<ReceivingItemsState> = _state.asStateFlow()

    fun setFilter(v: ItemFilter) = _state.update { it.copy(filter = v) }

    fun load(po: String) {
        _state.update { it.copy(loading = true, poNo = po, error = null) }
        viewModelScope.launch {
            runCatching { repo.linesOf(po) }
                .onSuccess { rows -> _state.update { it.copy(loading = false, lines = rows) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }
}


data class ReceivingDetailState(
    val loading: Boolean = true,
    val line: ReceivingPlan? = null,
    val reasons: List<Reason> = emptyList(),
    val config: HtConfig = HtConfig(),
    val lotNo: String = "",
    val actualQty: String = "",
    val reasonCode: String? = null,
    val locationCode: String = "",
    val busy: Boolean = false,
    val error: Pair<String, String>? = null,
    val toast: HtMsg? = null,
    val done: Boolean = false,
    val fromScan: Boolean = false,
) {
    val planned: Double get() = line?.plannedQty ?: 0.0

    val expected: Double get() = line?.expectedQty ?: 0.0

    val packageCount: Double? get() = line?.caseCount

    val diff: Double? get() = actualQty.trim().toDoubleOrNull()?.let { it - expected }

    val needsReason: Boolean get() = (diff ?: 0.0) < 0

    val showLabelButton: Boolean get() = config.labelModeRcv != "preIssue"


    val canEditActual: Boolean get() = !fromScan

    val showConfirm: Boolean get() = fromScan || labelIssued || !showLabelButton

    val showLabel: Boolean get() = !fromScan && showLabelButton && !labelIssued

    val labelIssued: Boolean get() = line?.labelIssued == true

    val missingLocation: Boolean
        get() = locationCode.isBlank() && (actualQty.trim().toDoubleOrNull() ?: 0.0) > 0
}

sealed interface ReceivingScanTarget {
    data class Location(val code: String) : ReceivingScanTarget
    data class Lot(val lot: String) : ReceivingScanTarget
}

fun receivingScanTarget(raw: String): ReceivingScanTarget {
    val scan = com.imssolutions.ht.scan.QrPayload.parse(raw)
    val p = (scan as? com.imssolutions.ht.scan.QrScan.Ok)?.payload
        ?: return ReceivingScanTarget.Lot(raw.trim())
    return when {
        p.kind == com.imssolutions.ht.scan.QrLabelKind.Location ->
            ReceivingScanTarget.Location(p.locationCode!!.trim())
        else -> ReceivingScanTarget.Lot(p.lotNo ?: raw.trim())
    }
}

class ReceivingDetailViewModel : ViewModel() {
    private val repo = ReceivingRepository()
    private val _state = MutableStateFlow(ReceivingDetailState())
    val state: StateFlow<ReceivingDetailState> = _state.asStateFlow()

    fun setLot(v: String) = _state.update { it.copy(lotNo = v, error = null) }
    fun setReason(v: String?) = _state.update { it.copy(reasonCode = v, error = null) }

    fun onScan(raw: String) = when (val target = receivingScanTarget(raw)) {
        is ReceivingScanTarget.Location -> _state.update { it.copy(locationCode = target.code, error = null) }
        is ReceivingScanTarget.Lot -> setLot(target.lot)
    }

    private fun locationMissing(s: ReceivingDetailState): Boolean {
        if (!s.missingLocation) return false
        _state.update { it.copy(error = "LOC-001" to "Vui lòng quét tem vị trí.") }
        return true
    }

    fun setActual(v: String) = _state.update {
        val diff = v.trim().toDoubleOrNull()?.minus(it.planned)
        it.copy(
            actualQty = v, error = null,
            reasonCode = if ((diff ?: 0.0) >= 0) null else it.reasonCode,
        )
    }

    fun load(poNo: String, serialNo: String, scannedQty: String = "", scannedLot: String = "") {
        _state.update { it.copy(loading = true, error = null, done = false) }
        viewModelScope.launch {
            runCatching {
                Triple(repo.linesOf(poNo), repo.shortageReasons(), repo.config())
            }.onSuccess { (lines, reasons, cfg) ->
                val line = lines.firstOrNull { it.serialNo == serialNo }
                _state.update {
                    it.copy(
                        loading = false,
                        line = line,
                        reasons = reasons,
                        config = cfg,
                        lotNo = scannedLot.ifBlank { line?.lotNo.orEmpty() },
                        actualQty = scannedQty,
                        fromScan = scannedQty.isNotBlank(),
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
            _state.update { it.copy(error = "QTY-001" to "Vui lòng nhập số lượng hợp lệ (từ 0 trở lên).") }
            return
        }
        if (locationMissing(s)) return

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch { confirmNow(line, qty, s, printed = false) }
    }

    fun issueLabelAndConfirm(printerDeviceCode: String?) {
        val s = _state.value
        val line = s.line ?: return
        if (s.busy) return

        val qty = s.actualQty.trim().toDoubleOrNull()
        val step = labelConfirmStep(qty, s.labelIssued, printerDeviceCode)
        if (step is LabelConfirmStep.Refuse) {
            _state.update { it.copy(error = step.code to step.message) }
            return
        }
        if (locationMissing(s)) return
        val print = (step as LabelConfirmStep.Go).print

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            if (print) {
                val issued = runCatching { repo.issueLabel(line, qty!!, printerDeviceCode) }
                if (issued.isFailure) {
                    _state.update { it.copy(busy = false, error = issued.exceptionOrNull()!!.toPair()) }
                    return@launch
                }
                _state.update { it.copy(line = it.line?.copy(labelIssued = true)) }
            }
            confirmNow(line, qty!!, s, printed = print)
        }
    }

    private suspend fun confirmNow(line: ReceivingPlan, qty: Double, s: ReceivingDetailState, printed: Boolean) {
        runCatching { repo.confirmLine(line, qty, s.reasonCode, s.lotNo, s.locationCode) }
            .onSuccess { row ->
                val msg = when {
                    printed -> HtMsg("htLabelAndConfirmed", "Đã in nhãn và chốt: thực nhận {0}", fmt(row.qty))
                    row.status == STATUS_ARRIVED -> HtMsg("htReceivedFull", "Đã chốt: nhận đủ {0}", fmt(row.qty))
                    else -> HtMsg("htReceivedShort", "Đã ghi thiếu hàng: thực nhận {0}", fmt(row.qty))
                }
                _state.update { it.copy(busy = false, toast = msg, done = true) }
            }
            .onFailure { e ->
                val (code, msg) = e.toPair()
                _state.update {
                    it.copy(busy = false, error = if (printed) code to "Đã in nhãn nhưng CHƯA chốt được dòng. $msg" else code to msg)
                }
            }
    }

    fun clearToast() = _state.update { it.copy(toast = null) }

    fun clearError() = _state.update { it.copy(error = null) }
}


data class SettingsState(
    val loading: Boolean = true,
    val serverIp: String = "",
    val serverUrl: String? = null,
    val printerDeviceCode: String = "",
    val zoneCode: String = "",
    val printers: List<Pair<String, String>> = emptyList(),
    val zones: List<Pair<String, String>> = emptyList(),
    val langDataAt: String? = null,
    val deviceId: String = "",
    val downloading: Boolean = false,
    val error: Pair<String, String>? = null,
    val toast: HtMsg? = null,
)

class SettingsViewModel : ViewModel() {
    private val repo = ReceivingRepository()
    private val _state = MutableStateFlow(SettingsState())
    val state: StateFlow<SettingsState> = _state.asStateFlow()

    init { load() }

    private val deviceIp = ServerAddress.deviceIpv4()

    fun setServerIp(v: String) = _state.update {
        it.copy(serverIp = v, serverUrl = ServerAddress.toBaseUrl(v, deviceIp), error = null)
    }
    fun setPrinterDeviceCode(v: String) = _state.update { it.copy(printerDeviceCode = v) }
    fun setZone(v: String) = _state.update { it.copy(zoneCode = v) }
    fun setDeviceId(v: String) = _state.update { it.copy(deviceId = v, error = null) }

    fun load() {
        viewModelScope.launch {
            val store = ApiClient.tokens
            val prefs = store.htSettingsBlocking()
            val deviceId = ApiClient.updates.current().deviceId
            val printers = runCatching { repo.printers() }
            val zones = runCatching { repo.zones() }
            val p = printers.getOrDefault(emptyList())
            val z = zones.getOrDefault(emptyList())
            _state.update {
                it.copy(
                    loading = false,
                    serverIp = ServerAddress.toDisplay(prefs.serverIp.ifBlank { store.baseUrlBlocking() }),
                    serverUrl = prefs.serverIp.ifBlank { store.baseUrlBlocking() },
                    printerDeviceCode = when {
                        printers.isFailure -> prefs.printerDeviceCode
                        else -> prefs.printerDeviceCode.takeIf { c -> p.any { it.first == c } }
                            ?: p.firstOrNull()?.first.orEmpty()
                    },
                    zoneCode = prefs.zoneCode.ifBlank { z.firstOrNull()?.first.orEmpty() },
                    printers = p,
                    zones = z,
                    langDataAt = prefs.langDataAt,
                    deviceId = deviceId,
                    error = (printers.exceptionOrNull() ?: zones.exceptionOrNull())?.toPair(),
                )
            }
        }
    }

    fun downloadLanguageData(download: suspend () -> Int) {
        if (_state.value.downloading) return
        _state.update { it.copy(downloading = true, error = null) }
        viewModelScope.launch {
            runCatching { download() }
                .onSuccess { count ->
                    val at = nowStamp()
                    ApiClient.tokens.saveLangDataAt(at)
                    _state.update { it.copy(downloading = false, langDataAt = at, toast = HtMsg("htDataDownloadDoneN", "Đã tải {0} nhãn ngôn ngữ", count)) }
                }
                .onFailure { e -> _state.update { it.copy(downloading = false, error = e.toPair()) } }
        }
    }

    fun clearLanguageData(clear: suspend () -> Unit) {
        viewModelScope.launch {
            clear()
            ApiClient.tokens.saveLangDataAt(null)
            _state.update { it.copy(langDataAt = null, toast = HtMsg("htLangDataClearDone", "Đã xoá dữ liệu ngôn ngữ")) }
        }
    }

    fun save(then: (() -> Unit)? = null) {
        val s = _state.value
        val url = ServerAddress.toBaseUrl(s.serverIp, deviceIp)
        if (url == null) {
            _state.update { it.copy(error = "VAL-004" to "Địa chỉ máy chủ không hợp lệ.") }
            return
        }
        val deviceId = if (s.deviceId.isBlank()) "" else UpdateRules.deviceIdOf(s.deviceId)
        if (deviceId == null) {
            _state.update { it.copy(error = "VAL-005" to "Mã thiết bị phải từ 1 đến 40 ký tự.") }
            return
        }
        viewModelScope.launch {
            ApiClient.tokens.saveBaseUrl(url)
            ApiClient.tokens.saveHtSettings(url, s.printerDeviceCode, s.zoneCode)
            ApiClient.updates.saveDeviceId(deviceId)
            _state.update {
                it.copy(
                    serverIp = ServerAddress.toDisplay(url), serverUrl = url, deviceId = deviceId,
                    toast = if (then == null) HtMsg("htSettingsSaved", "Đã lưu") else null,
                )
            }
            then?.invoke()
        }
    }

    fun clearToast() = _state.update { it.copy(toast = null) }
}

private fun nowStamp(): String {
    val f = java.text.SimpleDateFormat("yyyy-MM-dd HH:mm", java.util.Locale.US)
    return f.format(java.util.Date())
}

fun fmt(v: Double): String =
    if (v % 1.0 == 0.0) v.toLong().toString() else v.toString()
