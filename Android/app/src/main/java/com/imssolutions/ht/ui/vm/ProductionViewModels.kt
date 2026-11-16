package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.data.ApiException
import com.imssolutions.ht.data.ItemBrief
import com.imssolutions.ht.data.LabelPlan
import com.imssolutions.ht.data.ProductionLine
import com.imssolutions.ht.data.ProductionOrder
import com.imssolutions.ht.data.ProductionRepository
import com.imssolutions.ht.data.ProductionWork
import com.imssolutions.ht.data.ProductionWorks
import com.imssolutions.ht.data.StockLine
import com.imssolutions.ht.data.activeProductionWorks
import com.imssolutions.ht.data.materialPreview
import com.imssolutions.ht.data.materialScanError
import com.imssolutions.ht.data.processScanError
import com.imssolutions.ht.data.productionLabelPlan
import com.imssolutions.ht.data.productionOrdersFor
import com.imssolutions.ht.data.round4
import com.imssolutions.ht.scan.QrLabelKind
import com.imssolutions.ht.scan.QrPayload
import com.imssolutions.ht.scan.QrScan

private fun Throwable.toPrdError(): Pair<String, String> = when (this) {
    is ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}

private fun parseQty(raw: String): Double? {
    val text = raw.trim()
    if (text.isEmpty() || Regex("""\.\d{5,}$""").containsMatchIn(text)) return null
    return text.toDoubleOrNull()?.takeIf { it > 0 }
}

data class PrdDone(val extra: List<String> = emptyList())

data class PrdListState(
    val loading: Boolean = true,
    val orders: List<ProductionOrder> = emptyList(),
    val lines: List<ProductionLine> = emptyList(),
    val filter: String = "active",
    val query: String = "",
    val error: Pair<String, String>? = null,
)

class PrdListViewModel : ViewModel() {
    private val repo = ProductionRepository()
    private val _state = MutableStateFlow(PrdListState())
    val state: StateFlow<PrdListState> = _state.asStateFlow()

    fun start(kind: String) {
        if (kind == "return" && _state.value.orders.isEmpty()) _state.update { it.copy(filter = "all") }
        load()
    }

    fun setFilter(v: String) = _state.update { it.copy(filter = v) }
    fun setQuery(v: String) = _state.update { it.copy(query = v) }

    fun visible(kind: String): List<ProductionOrder> =
        _state.value.let { productionOrdersFor(kind, it.orders, it.lines, it.filter, it.query) }

    fun load() {
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.orders() to repo.allLines() }
                .onSuccess { (orders, lines) -> _state.update { it.copy(loading = false, orders = orders, lines = lines) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPrdError()) } }
        }
    }
}

data class PrdOrderState(
    val loading: Boolean = true,
    val busy: Boolean = false,
    val order: ProductionOrder? = null,
    val lines: List<ProductionLine> = emptyList(),
    val works: ProductionWorks = ProductionWorks(),
    val error: Pair<String, String>? = null,
    val done: PrdDone? = null,
) {
    val htLines: List<ProductionLine> get() = lines.filter { it.htScanRequired }
    val receivedAt: String? get() = works.receipts.minByOrNull { it.createdAt }?.createdAt
    val processes: List<ProductionLine> get() = htLines.distinctBy { it.processCode }
    fun processName(code: String): String = lines.firstOrNull { it.processCode == code }?.processName ?: code
}

class PrdOrderViewModel : ViewModel() {
    private val repo = ProductionRepository()
    private val _state = MutableStateFlow(PrdOrderState())
    val state: StateFlow<PrdOrderState> = _state.asStateFlow()

    fun load(no: String) {
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching { Triple(repo.orderRow(no), repo.lines(no), repo.works(no)) }
                .onSuccess { (o, l, w) -> _state.update { it.copy(loading = false, order = o, lines = l, works = w) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPrdError()) } }
        }
    }

    fun confirmReceipt() {
        val order = _state.value.order ?: return
        if (_state.value.busy) return
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.receipt(order.productionOrderNo) }
                .onSuccess { _state.update { it.copy(busy = false, done = PrdDone()) } }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPrdError()) } }
        }
    }

    fun processError(code: String): String? = processScanError(_state.value.lines, code)

    fun clearError() = _state.update { it.copy(error = null) }
    fun showError(code: String, message: String) = _state.update { it.copy(error = code to message) }
}

data class PrdMaterial(val itemCode: String, val lotNo: String)

data class PrdScanMatState(
    val busy: Boolean = false,
    val error: Pair<String, String>? = null,
    val accepted: PrdMaterial? = null,
)

class PrdScanMatViewModel : ViewModel() {
    private val repo = ProductionRepository()
    private val _state = MutableStateFlow(PrdScanMatState())
    val state: StateFlow<PrdScanMatState> = _state.asStateFlow()

    fun submit(raw: String, lines: List<ProductionLine>, processCode: String) {
        if (_state.value.busy) return
        val scan = QrPayload.parse(raw)
        val payload = (scan as? QrScan.Ok)?.payload
        val itemCode = payload?.itemCode
        if (payload == null || payload.kind != QrLabelKind.Product || itemCode.isNullOrBlank()) {
            _state.update { it.copy(error = "SCN-001" to "The scanned barcode is invalid.") }
            return
        }
        materialScanError(lines, processCode, itemCode)?.let { code ->
            _state.update { it.copy(error = code to "Does not match the specified item.") }
            return
        }
        val lot = payload.lotNo.orEmpty()
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching {
                val item = repo.item(itemCode)
                materialPreview(repo.stock(itemCode), item.isLotManaged, lot).code
            }.onSuccess { code ->
                _state.update {
                    if (code != null) it.copy(busy = false, error = code to code)
                    else it.copy(busy = false, accepted = PrdMaterial(itemCode, lot))
                }
            }.onFailure { e -> _state.update { it.copy(busy = false, error = e.toPrdError()) } }
        }
    }

    fun consumed() = _state.update { it.copy(accepted = null) }
    fun clearError() = _state.update { it.copy(error = null) }
}

data class PrdConsState(
    val loading: Boolean = true,
    val busy: Boolean = false,
    val order: ProductionOrder? = null,
    val lines: List<ProductionLine> = emptyList(),
    val works: ProductionWorks = ProductionWorks(),
    val item: ItemBrief? = null,
    val stocks: List<StockLine> = emptyList(),
    val qty: String = "",
    val askNearExpiry: Boolean = false,
    val error: Pair<String, String>? = null,
    val done: PrdDone? = null,
) {
    fun processLines(process: String, item: String) =
        lines.filter { it.processCode == process && it.itemCode == item && it.htScanRequired }
}

class PrdConsViewModel : ViewModel() {
    private val repo = ProductionRepository()
    private val _state = MutableStateFlow(PrdConsState())
    val state: StateFlow<PrdConsState> = _state.asStateFlow()
    private var key: Triple<String, String, PrdMaterial>? = null

    fun load(no: String, process: String, material: PrdMaterial) {
        key = Triple(no, process, material)
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching {
                PrdConsState(
                    loading = false,
                    order = repo.orderRow(no),
                    lines = repo.lines(no),
                    works = repo.works(no),
                    item = repo.item(material.itemCode),
                    stocks = repo.stock(material.itemCode),
                )
            }.onSuccess { loaded -> _state.update { loaded.copy(qty = it.qty) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPrdError()) } }
        }
    }

    fun setQty(v: String) = _state.update { it.copy(qty = v) }

    fun preview() = key?.let { (_, _, m) ->
        val s = _state.value
        materialPreview(s.stocks, s.item?.isLotManaged ?: false, m.lotNo.ifEmpty { null })
    }

    fun confirm() {
        val (no, process, m) = key ?: return
        val s = _state.value
        if (s.busy) return
        val qty = parseQty(s.qty) ?: return _state.update { it.copy(error = "QTY-003" to "Enter a quantity greater than 0.") }
        val check = materialPreview(s.stocks, s.item?.isLotManaged ?: false, m.lotNo.ifEmpty { null }, qty)
        if (check.code != null) return _state.update { it.copy(error = check.code to check.code) }
        if (check.nearExpiry) return _state.update { it.copy(askNearExpiry = true) }
        send(no, process, m, qty)
    }

    fun acceptNearExpiry() {
        val (no, process, m) = key ?: return
        _state.update { it.copy(askNearExpiry = false) }
        parseQty(_state.value.qty)?.let { send(no, process, m, it) }
    }

    fun dismissNearExpiry() = _state.update { it.copy(askNearExpiry = false) }

    private fun send(no: String, process: String, m: PrdMaterial, qty: Double) {
        val lineNo = _state.value.processLines(process, m.itemCode)
            .sortedBy { it.lineNo }
            .firstOrNull { it.consumedQty < it.plannedQty }?.lineNo
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.consume(no, process, m.itemCode, m.lotNo.ifEmpty { null }, qty, lineNo) }
                .onSuccess { rows ->
                    val unit = _state.value.item?.uomCode.orEmpty()
                    _state.update { it.copy(busy = false, done = PrdDone(rows.map { (label, q) -> "$label : ${fmt(q)} $unit".trimEnd() })) }
                }
                .onFailure { e -> _state.update { it.copy(busy = false, error = e.toPrdError()) } }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }
}

data class PrdReturnRow(
    val itemCode: String,
    val itemName: String?,
    val lotNo: String?,
    val unit: String,
    val expiryDate: String?,
    val net: Double,
)

data class PrdReturnState(
    val loading: Boolean = true,
    val busy: Boolean = false,
    val order: ProductionOrder? = null,
    val rows: List<PrdReturnRow> = emptyList(),
    val inputs: Map<Int, String> = emptyMap(),
    val error: Pair<String, String>? = null,
    val done: PrdDone? = null,
)

class PrdReturnViewModel : ViewModel() {
    private val repo = ProductionRepository()
    private val _state = MutableStateFlow(PrdReturnState())
    val state: StateFlow<PrdReturnState> = _state.asStateFlow()

    fun load(no: String) {
        _state.update { it.copy(loading = true, error = null) }
        refresh(no)
    }

    private fun refresh(no: String) {
        viewModelScope.launch {
            runCatching {
                val order = repo.orderRow(no)
                val lines = repo.lines(no)
                val works = repo.works(no)
                order to returnRows(lines, works)
            }.onSuccess { (o, rows) -> _state.update { it.copy(loading = false, order = o, rows = rows, inputs = emptyMap()) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = it.error ?: e.toPrdError()) } }
        }
    }

    private fun returnRows(lines: List<ProductionLine>, works: ProductionWorks): List<PrdReturnRow> {
        val consumed = activeProductionWorks(works.consumptions)
        val returned = activeProductionWorks(works.returns)
        return consumed.groupBy { it.itemCode.orEmpty() to it.lotNo }.map { (key, rows) ->
            val line = lines.firstOrNull { it.itemCode == key.first }
            val back = returned.filter { it.itemCode == key.first && it.lotNo == key.second }.sumOf { it.qty ?: 0.0 }
            PrdReturnRow(
                itemCode = key.first,
                itemName = line?.itemName,
                lotNo = key.second,
                unit = line?.uomCode.orEmpty(),
                expiryDate = rows.firstNotNullOfOrNull { it.expiryDate },
                net = round4(rows.sumOf { it.qty ?: 0.0 } - back),
            )
        }
    }

    fun setInput(i: Int, v: String) = _state.update { it.copy(inputs = it.inputs + (i to v)) }

    fun confirm() {
        val s = _state.value
        val order = s.order ?: return
        if (s.busy) return
        val entries = s.inputs.filterValues { it.isNotBlank() }
        if (entries.isEmpty()) return _state.update { it.copy(error = "QTY-003" to "Enter a quantity greater than 0.") }
        val parsed = entries.mapValues { parseQty(it.value) }
        if (parsed.values.any { it == null }) return _state.update { it.copy(error = "QTY-003" to "Enter a quantity greater than 0.") }
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            try {
                for ((i, qty) in parsed.toSortedMap()) {
                    val row = s.rows[i]
                    repo.returnMaterial(order.productionOrderNo, row.itemCode, row.lotNo, qty!!)
                }
                _state.update { it.copy(busy = false, done = PrdDone()) }
            } catch (e: Exception) {
                _state.update { it.copy(busy = false, error = e.toPrdError()) }
                refresh(order.productionOrderNo)
            }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }
}

data class PrdOutputState(
    val loading: Boolean = true,
    val busy: Boolean = false,
    val order: ProductionOrder? = null,
    val item: ItemBrief? = null,
    val qty: String = "",
    val lot: String = "",
    val expiry: String = "",
    val finalize: Boolean = true,
    val askOverPlan: Boolean = false,
    val error: Pair<String, String>? = null,
    val doneRequestId: String? = null,
    val doneLines: List<String> = emptyList(),
) {
    val lotEditable: Boolean get() = order?.outputLotNo.isNullOrBlank()
    val qtyValue: Double? get() = parseQty(qty)
    val diffToPlan: Double? get() = order?.let { o -> qtyValue?.let { round4(o.outputQty + it - o.plannedQty) } }
    val plan: LabelPlan? get() = qtyValue?.let { productionLabelPlan(it, item?.packageQty ?: 1.0) }
}

class PrdOutputViewModel : ViewModel() {
    private val repo = ProductionRepository()
    private val _state = MutableStateFlow(PrdOutputState())
    val state: StateFlow<PrdOutputState> = _state.asStateFlow()

    fun load(no: String) {
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching {
                val order = repo.orderRow(no)
                order to repo.item(order.outputItemCode)
            }.onSuccess { (o, item) ->
                val remaining = round4(o.plannedQty - o.outputQty)
                val qty = if (remaining > 0) remaining else o.plannedQty
                _state.update {
                    it.copy(
                        loading = false, order = o, item = item,
                        qty = fmt(qty), lot = o.outputLotNo.orEmpty(), expiry = "",
                    )
                }
            }.onFailure { e -> _state.update { it.copy(loading = false, error = e.toPrdError()) } }
        }
    }

    fun setQty(v: String) = _state.update { it.copy(qty = v) }
    fun setLot(v: String) = _state.update { it.copy(lot = v) }
    fun setExpiry(v: String) = _state.update { it.copy(expiry = v) }
    fun setFinalize(v: Boolean) = _state.update { it.copy(finalize = v) }

    fun confirm() {
        val s = _state.value
        val order = s.order ?: return
        if (s.busy) return
        val qty = s.qtyValue ?: return _state.update { it.copy(error = "QTY-003" to "Enter a quantity greater than 0.") }
        if (order.storageLocationCode.isBlank()) return _state.update { it.copy(error = "LOC-001" to "LOC-001") }
        if (order.outputQty + qty > order.plannedQty + 1e-9) return _state.update { it.copy(askOverPlan = true) }
        send(order, qty)
    }

    fun acceptOverPlan() {
        val order = _state.value.order ?: return
        _state.update { it.copy(askOverPlan = false) }
        _state.value.qtyValue?.let { send(order, it) }
    }

    fun dismissOverPlan() = _state.update { it.copy(askOverPlan = false) }

    private fun send(order: ProductionOrder, qty: Double) {
        val s = _state.value
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            runCatching {
                repo.output(
                    order.productionOrderNo, qty, s.finalize,
                    s.expiry.trim().ifEmpty { null }, s.lot.trim().ifEmpty { null },
                ) to repo.outputRequestId
            }.onSuccess { (result, requestId) ->
                val serials = result.serials
                val range = when {
                    serials.isEmpty() -> ""
                    serials.size == 1 -> serials.first()
                    else -> "${serials.first()} … ${serials.last()}"
                }
                _state.update {
                    it.copy(busy = false, doneRequestId = requestId, doneLines = listOf(result.lotNo, range))
                }
            }.onFailure { e -> _state.update { it.copy(busy = false, error = e.toPrdError()) } }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }
}

data class PrdLabelState(
    val loading: Boolean = true,
    val busy: Boolean = false,
    val order: ProductionOrder? = null,
    val item: ItemBrief? = null,
    val outputs: List<ProductionWork> = emptyList(),
    val issued: ProductionWork? = null,
    val error: Pair<String, String>? = null,
    val printed: Boolean = false,
) {
    val qty: Double get() = round4(outputs.sumOf { it.qty ?: 0.0 })
    val lot: String get() = outputs.firstNotNullOfOrNull { it.lotNo } ?: order?.outputLotNo.orEmpty()
    val plan: LabelPlan get() = productionLabelPlan(qty, item?.packageQty ?: 1.0)
}

class PrdLabelViewModel : ViewModel() {
    private val repo = ProductionRepository()
    private val _state = MutableStateFlow(PrdLabelState())
    val state: StateFlow<PrdLabelState> = _state.asStateFlow()
    private var outputRequestId: String = ""

    fun load(no: String, requestId: String) {
        outputRequestId = requestId
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching {
                val order = repo.orderRow(no)
                val works = repo.works(no)
                PrdLabelState(
                    loading = false,
                    order = order,
                    item = repo.item(order.outputItemCode),
                    outputs = activeProductionWorks(works.outputs).filter { it.requestId == requestId },
                    issued = works.labels.firstOrNull { it.outputRequestId == requestId },
                )
            }.onSuccess { loaded -> _state.value = loaded }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPrdError()) } }
        }
    }

    fun issue(printerDeviceCode: String) {
        val s = _state.value
        val order = s.order ?: return
        val item = s.item ?: return
        if (s.busy || s.issued != null || s.outputs.isEmpty()) return
        if (printerDeviceCode.isBlank()) return _state.update { it.copy(error = "PRN-001" to "PRN-001") }
        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            try {
                repo.printLabels(order, item, s.outputs, s.plan, printerDeviceCode)
                repo.label(order.productionOrderNo, outputRequestId)
                _state.update { it.copy(busy = false, printed = true) }
            } catch (e: Exception) {
                _state.update { it.copy(busy = false, error = e.toPrdError()) }
            }
            runCatching { repo.works(order.productionOrderNo) }.onSuccess { w ->
                _state.update { st -> st.copy(issued = w.labels.firstOrNull { it.outputRequestId == outputRequestId }) }
            }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }
}


fun fmtDateTime(iso: String?): String {
    val v = iso?.trim().orEmpty()
    if (v.length < 16) return v.ifEmpty { "—" }
    return v.substring(0, 10).replace('-', '/') + " " + v.substring(11, 16)
}
