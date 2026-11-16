package com.imssolutions.ht.data

import kotlin.math.floor

data class LabelPlan(val count: Int, val full: Int, val remainder: Double, val std: Double)

data class MaterialPreview(val code: String?, val available: Double = 0.0, val nearExpiry: Boolean = false)

private const val NEAR_EXPIRY_DAYS = 30
private const val EPSILON = 1e-9

fun productionLabelPlan(qty: Double, packageQty: Double): LabelPlan {
    if (qty <= 0) return LabelPlan(0, 0, 0.0, packageQty)
    if (packageQty <= 1) return LabelPlan(1, 0, qty, packageQty)
    val full = floor(qty / packageQty + EPSILON).toInt()
    val rest = round4(qty - full * packageQty)
    return LabelPlan(full + if (rest > EPSILON) 1 else 0, full, if (rest > EPSILON) rest else 0.0, packageQty)
}

fun productionLabelCount(qty: Double, packageQty: Double): Int = productionLabelPlan(qty, packageQty).count

fun round4(v: Double): Double = Math.round(v * 10000.0) / 10000.0

fun materialPreview(stocks: List<StockLine>, lotManaged: Boolean, lot: String?, qty: Double? = null): MaterialPreview {
    val wanted = lot?.trim()?.takeIf { it.isNotEmpty() }
    val rows = if (lotManaged) {
        if (wanted == null) return MaterialPreview("LOT-001")
        stocks.filter { it.lotNo == wanted && it.stockQty + it.availableQty > 0 }
            .ifEmpty { return MaterialPreview("SCN-006") }
    } else {
        stocks.filter { (wanted == null || it.lotNo == wanted) && it.stockQty + it.availableQty > 0 }
            .ifEmpty { return MaterialPreview("INV-009") }
    }
    val free = rows.filterNot { it.isOnHold }.ifEmpty { return MaterialPreview("HLD-001") }
    val valid = free.filter { it.remainingDays?.let { d -> d >= 0 } ?: true }
        .ifEmpty { return MaterialPreview("INV-001") }
        .sortedWith(compareBy<StockLine> { it.expiryDate == null }.thenBy { it.expiryDate })
    val available = round4(valid.sumOf { it.availableQty })
    var need = qty ?: 0.0
    var near = false
    for (row in valid) {
        if (need <= EPSILON) break
        if ((row.remainingDays ?: Int.MAX_VALUE) <= NEAR_EXPIRY_DAYS) near = true
        need -= minOf(row.availableQty, need)
    }
    if (qty != null && available + EPSILON < qty) return MaterialPreview("INV-009", available, near)
    return MaterialPreview(null, available, near)
}

fun processScanError(lines: List<ProductionLine>, processCode: String): String? {
    val htLines = lines.filter { it.processCode == processCode && it.htScanRequired }
    return when {
        htLines.isEmpty() -> "SCN-010"
        htLines.none { it.processActive } -> "SYS-404"
        else -> null
    }
}

fun materialScanError(lines: List<ProductionLine>, processCode: String, itemCode: String): String? =
    if (lines.any { it.processCode == processCode && it.htScanRequired && it.itemCode == itemCode }) null else "SCN-005"

fun productionOrdersFor(
    kind: String,
    orders: List<ProductionOrder>,
    lines: List<ProductionLine>,
    filter: String,
    query: String,
): List<ProductionOrder> = orders.filter { o ->
    o.statusCode != 3 && when (filter) {
        "active" -> o.statusCode != 2
        "completed" -> o.statusCode == 2
        else -> true
    } && o.productionOrderNo.contains(query.trim(), ignoreCase = true) && when (kind) {
        "input" -> lines.any { it.productionOrderNo == o.productionOrderNo && it.htScanRequired }
        "return" -> lines.any { it.productionOrderNo == o.productionOrderNo && it.consumedQty > 0 }
        else -> true
    }
}.sortedByDescending { it.productionOrderNo }

fun returnableQty(consumed: Double, returned: Double): Double =
    (consumed - returned).coerceAtLeast(0.0)

fun productionWorksOf(timeline: List<ProductionWork>): ProductionWorks = ProductionWorks(
    receipts = timeline.filter { it.type == "receipt" },
    consumptions = timeline.filter { it.type == "consumption" },
    outputs = timeline.filter { it.type == "output" },
    labels = timeline.filter { it.type == "label" },
    returns = timeline.filter { it.type == "return" },
)

fun activeProductionWorks(rows: List<ProductionWork>): List<ProductionWork> {
    val reversed = rows.mapNotNull { it.reversesNo }.toSet()
    return rows.filter { it.reversesNo == null && it.no !in reversed }
}

fun nearExpiryFefo(stocks: List<StockLine>, qty: Double): Boolean {
    var remaining = qty
    for (stock in stocks.sortedWith(compareBy<StockLine> { it.expiryDate == null }.thenBy { it.expiryDate })) {
        if (remaining <= 0) break
        if (stock.availableQty <= 0 || stock.isOnHold || stock.remainingDays?.let { it < 0 } == true) continue
        if (stock.remainingDays?.let { it <= 30 } == true) return true
        remaining -= stock.availableQty
    }
    return false
}

fun productionMenuKeys(permissions: MyPermissions?): Set<String> {
    if (permissions == null) return emptySet()
    val codes = setOf("ht_prd_receipt", "ht_prd_input", "ht_prd_return", "ht_prd_output")
    if (permissions.isSuperAdmin) return codes
    return permissions.functions.filter { it.functionCode in codes && "Open" in it.actions }.map { it.functionCode }.toSet()
}
