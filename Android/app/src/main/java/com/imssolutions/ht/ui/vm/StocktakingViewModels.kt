package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.data.ApiException
import com.imssolutions.ht.data.Location
import com.imssolutions.ht.data.ST_IN_PROGRESS
import com.imssolutions.ht.data.StocktakingGroup
import com.imssolutions.ht.data.StocktakingRepository
import com.imssolutions.ht.data.StocktakingSession
import com.imssolutions.ht.data.matching
import com.imssolutions.ht.data.Zone
import com.imssolutions.ht.scan.QrLabelKind
import com.imssolutions.ht.scan.QrPayload
import com.imssolutions.ht.scan.QrScan
import com.imssolutions.ht.ui.HtMsg

internal fun stocktakingShelfOf(raw: String): String? {
    val p = (QrPayload.parse(raw) as? QrScan.Ok)?.payload ?: return null
    if (p.kind != QrLabelKind.Location) return null
    return p.locationCode?.trim()?.takeIf { it.isNotEmpty() }
}

internal fun stocktakingScanRefusal(
    group: StocktakingGroup?,
    next: Int,
    itemCode: String,
): Pair<String, String>? = when {
    group == null -> "LOC-003" to "Sổ sách không ghi $itemCode ở ô kệ này."
    next > group.plannedLabels -> "QTY-002" to "Sổ sách chỉ ghi ${group.plannedLabels} kiện ở ô kệ này."
    else -> null
}

class StocktakingViewModel : ViewModel() {

    private val repo = StocktakingRepository()
    private val _state = MutableStateFlow(StocktakingState())
    val state: StateFlow<StocktakingState> = _state.asStateFlow()

    init { load() }

    fun load() {
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.session() }
                .onSuccess { s ->
                    if (s == null || s.status != ST_IN_PROGRESS) {
                        _state.update {
                            it.copy(
                                busy = false, session = s, sessionChecked = true,
                                error = if (s == null) {
                                    "STK-001" to "Chưa bắt đầu kiểm kê. Hãy bắt đầu trên PC."
                                } else {
                                    "STK-004" to "Kiểm kê đã hoàn thành. Không thể gửi kết quả."
                                },
                            )
                        }
                        return@onSuccess
                    }
                    _state.update { it.copy(session = s, sessionChecked = true) }
                    loadZones()
                }
                .onFailure { e ->
                    _state.update { it.copy(busy = false, sessionChecked = true, error = e.toPair()) }
                }
        }
    }

    private fun loadZones() {
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
            it.copy(zoneCode = zoneCode, locationCode = null, locations = emptyList(), error = null)
        }
        viewModelScope.launch {
            runCatching { repo.locationsIn(zoneCode) }
                .onSuccess { ls ->
                    _state.update {
                        if (it.zoneCode != zoneCode) it
                        else it.copy(locations = ls, locationCode = ls.firstOrNull()?.locationCode)
                    }
                }
                .onFailure { e -> _state.update { it.copy(error = e.toPair()) } }
        }
    }

    fun selectLocation(code: String) = _state.update { it.copy(locationCode = code, error = null) }

    fun onScanLocation(raw: String, onReady: () -> Unit) {
        val s = _state.value
        if (!s.open || s.busy) return
        val shelf = stocktakingShelfOf(raw)
        if (shelf == null) {
            _state.update { it.copy(error = "LOC-001" to "Vui lòng quét tem vị trí.") }
            return
        }
        selectLocation(shelf)
        openLocation(onReady)
    }

    fun openLocation(onReady: () -> Unit) {
        if (_state.value.busy) return
        val loc = _state.value.locationCode
        if (loc.isNullOrBlank()) {
            _state.update { it.copy(error = "LOC-001" to "Chưa chọn vị trí.") }
            return
        }
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.itemsAt(loc) }
                .onSuccess { gs ->
                    _state.update { it.copy(busy = false, groups = gs, error = null) }
                    onReady()
                }
                .onFailure { e ->
                    _state.update { it.copy(busy = false, error = e.toPair()) }
                }
        }
    }

    fun refreshGroups() {
        val loc = _state.value.locationCode ?: return
        viewModelScope.launch {
            runCatching { repo.itemsAt(loc) }
                .onSuccess { gs -> _state.update { it.copy(groups = gs) } }
                .onFailure { e ->
                    val p = e.toPair()
                    if (p.first == "LOC-003") _state.update { it.copy(groups = emptyList()) }
                    else _state.update { it.copy(error = p) }
                }
        }
    }

    fun open(key: String) = _state.update { s ->
        val g = s.groups.firstOrNull { it.key == key }
        s.copy(
            selectedKey = if (g == null) null else key,
            counted = g?.scannedLabels?.toString().orEmpty(),
            error = null,
        )
    }

    fun setCounted(v: String) = _state.update { it.copy(counted = v.filter { c -> c.isDigit() }) }

    fun onScan(raw: String) {
        val g = _state.value.selected ?: return
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
        if (!itemCode.equals(g.itemCode, ignoreCase = true)) {
            _state.update { it.copy(error = "SCN-006" to "Nhãn $itemCode không thuộc dòng đang đếm.") }
            return
        }
        val lot = payload.lotNo?.trim().orEmpty()
        val want = g.lotNo?.trim().orEmpty()
        if (want.isNotEmpty() && lot.isNotEmpty() && !lot.equals(want, ignoreCase = true)) {
            _state.update { it.copy(error = "SCN-006" to "Lô $lot không khớp lô $want.") }
            return
        }

        val next = (_state.value.counted.toIntOrNull() ?: 0) + 1
        stocktakingScanRefusal(g, next, itemCode)?.let { why ->
            _state.update { it.copy(error = why) }
            return
        }

        _state.update { s -> s.copy(counted = next.toString(), error = null) }
    }

    fun onScanList(raw: String) {
        val s = _state.value
        if (s.busy) return
        val loc = s.locationCode ?: return

        val payload = when (val r = QrPayload.parse(raw)) {
            is QrScan.Ok -> r.payload
            is QrScan.Invalid -> {
                _state.update { it.copy(error = "SCN-001" to "Mã quét không đúng định dạng.") }
                return
            }
        }
        stocktakingShelfOf(raw)?.let { shelf ->
            if (shelf != loc) { selectLocation(shelf); openLocation {} }
            return
        }
        val itemCode = payload.itemCode?.trim().orEmpty()
        if (itemCode.isEmpty()) {
            _state.update { it.copy(error = "SCN-006" to "Nhãn không có mã hàng.") }
            return
        }
        val lot = payload.lotNo?.trim()?.takeIf { it.isNotBlank() }

        val hit = s.groups.matching(itemCode, lot)
        val next = (hit?.scannedLabels ?: 0) + 1
        stocktakingScanRefusal(hit, next, itemCode)?.let { why ->
            _state.update { it.copy(error = why) }
            return
        }
        val g = hit ?: return
        val sendItem = g.itemCode
        val sendLot = g.lotNo

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.save(loc, sendItem, sendLot, next) }
                .onSuccess { updated ->
                    _state.update { st ->
                        st.copy(
                            busy = false,
                            groups = st.groups.map { if (it.key == updated.key) updated else it },
                            toast = HtMsg(
                                "htStSavedN", "Đã ghi {0}/{1} kiện cho {2}.",
                                updated.scannedLabels, updated.plannedLabels, updated.itemCode,
                            ),
                        )
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }

    fun clearToast() = _state.update { it.copy(toast = null) }

    fun closeDetail() = _state.update { it.copy(selectedKey = null, counted = "", saved = false) }

    fun save() {
        val s = _state.value
        if (s.busy) return
        val g = s.selected ?: return
        val loc = s.locationCode ?: return
        val n = s.counted.trim().toIntOrNull()
        if (n == null || n < 0) {
            _state.update { it.copy(error = "QTY-001" to "Vui lòng nhập số lượng hợp lệ (từ 0 trở lên).") }
            return
        }
        if (n > g.plannedLabels) {
            _state.update {
                it.copy(error = "QTY-002" to "Sổ sách chỉ ghi ${g.plannedLabels} kiện ở ô kệ này.")
            }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.save(loc, g.itemCode, g.lotNo, n) }
                .onSuccess { updated ->
                    _state.update { st ->
                        st.copy(
                            busy = false,
                            saved = true,
                            groups = st.groups.map { if (it.key == updated.key) updated else it },
                            toast = HtMsg(
                                "htStSavedN", "Đã ghi {0}/{1} kiện cho {2}.",
                                updated.scannedLabels, updated.plannedLabels, updated.itemCode,
                            ),
                        )
                    }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPair()) } }
        }
    }
}

data class StocktakingState(
    val session: StocktakingSession? = null,
    val sessionChecked: Boolean = false,
    val zones: List<Zone> = emptyList(),
    val locations: List<Location> = emptyList(),
    val zoneCode: String? = null,
    val locationCode: String? = null,
    val groups: List<StocktakingGroup> = emptyList(),
    val selectedKey: String? = null,
    val counted: String = "",
    val busy: Boolean = false,
    val error: Pair<String, String>? = null,
    val toast: HtMsg? = null,
    val saved: Boolean = false,
) {
    val selected: StocktakingGroup? get() = groups.firstOrNull { it.key == selectedKey }

    val open: Boolean get() = session?.acceptsResults == true

    val canProceed: Boolean get() = open && !locationCode.isNullOrBlank()

    val progress: Pair<Int, Int>
        get() = (counted.toIntOrNull() ?: 0) to (selected?.plannedLabels ?: 0)
}

private fun Throwable.toPair(): Pair<String, String> = when (this) {
    is ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}
