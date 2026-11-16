package com.imssolutions.ht.data

import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.jsonPrimitive
import com.imssolutions.ht.scan.LabelQr

class ProductionRepository {
    private val receiptKey = RetryKey()
    private val consumptionKey = RetryKey()
    private val returnKey = RetryKey()
    private val outputKey = RetryKey()
    private val labelKey = RetryKey()
    var outputRequestId: String? = null
        private set

    suspend fun orders(): List<ProductionOrder> {
        val rows = mutableListOf<ProductionOrder>()
        var page = 1
        do {
            val result = ApiClient.call { listProductionOrders(pageSize = 200, page = page) }
            rows.addAll(result.data)
            page++
        } while (rows.size < result.meta.total && result.data.isNotEmpty())
        return rows
    }

    suspend fun allLines(): List<ProductionLine> {
        val rows = mutableListOf<ProductionLine>()
        var page = 1
        do {
            val result = ApiClient.call { listProductionLines(pageSize = 200, page = page) }
            rows.addAll(result.data)
            page++
        } while (rows.size < result.meta.total && result.data.isNotEmpty())
        return rows
    }

    suspend fun orderRow(no: String): ProductionOrder =
        ApiClient.call { listProductionOrders(pageSize = 20, orderNo = no) }.data
            .firstOrNull { it.productionOrderNo == no }
            ?: throw ApiException("SYS-404", 404, no)

    suspend fun item(code: String): ItemBrief = ApiClient.call { getItem(code) }.data
        ?: throw ApiException("SYS-404", 404, code)

    suspend fun printLabels(
        order: ProductionOrder,
        item: ItemBrief,
        outputs: List<ProductionWork>,
        plan: LabelPlan,
        printerDeviceCode: String,
    ) {
        val serials = outputs.sortedBy { it.no }
        serials.forEachIndexed { i, out ->
            val qty = out.qty ?: if (plan.std > 1 && i < plan.full) plan.std else plan.remainder
            val lot = out.lotNo ?: order.outputLotNo
            ApiClient.call {
                printLabel(
                    PrintLabelRequest(
                        layoutKey = "ProductionQR",
                        deviceCode = printerDeviceCode,
                        history = PrintHistoryRequest(
                            category = "Production",
                            docNo = out.serialNo ?: "${order.productionOrderNo}-${i + 1}",
                            itemCode = item.itemCode,
                            qty = qty,
                            uomCode = item.uomCode ?: order.uomCode,
                            spec = printerDeviceCode,
                        ),
                        fields = mapOf(
                            "ProductionOrderNo" to order.productionOrderNo,
                            "ProductionLotNo" to (order.productionLotNo ?: ""),
                            "Item Code" to item.itemCode,
                            "Item Name" to (item.itemName ?: item.itemCode),
                            "Unit" to (item.uomCode ?: order.uomCode),
                            "ItemQty" to fmtQty(qty),
                            "Lot" to (lot ?: ""),
                            "ProductionDate" to (out.createdAt.take(10)),
                            "ExpiryDate" to (out.expiryDate?.take(10) ?: ""),
                            "SerialNo" to (out.serialNo ?: ""),
                            "StorageLocation" to (out.locationCode ?: order.storageLocationCode),
                            "Factory" to (order.factoryCode ?: ""),
                            "ReprintFlag" to "",
                            "QR" to LabelQr.product(
                                gtin = item.barcode,
                                lotNo = lot,
                                qty = qty,
                                itemCode = item.itemCode,
                                itemName = item.itemName,
                                productionDate = LabelQr.date(out.createdAt),
                                expiryDate = LabelQr.date(out.expiryDate),
                                userCode = ApiClient.userCode(),
                            ),
                        ),
                    ),
                )
            }
        }
    }

    private fun fmtQty(qty: Double): String =
        if (qty == qty.toLong().toDouble()) qty.toLong().toString() else qty.toString()

    suspend fun order(no: String): ProductionOrderDetail = ApiClient.call { productionOrder(no) }.data
        ?: throw ApiException("SYS-004", 500, "Empty production order")

    suspend fun lines(no: String): List<ProductionLine> = ApiClient.call { productionOrder(no) }.data?.lines.orEmpty()

    suspend fun works(no: String): ProductionWorks =
        productionWorksOf(ApiClient.call { productionOrder(no) }.data?.works.orEmpty())

    suspend fun stock(itemCode: String): List<StockLine> = ApiClient.call {
        listStockLines(itemCode = itemCode, pageSize = 200)
    }.data

    private suspend fun <T> write(key: RetryKey, identity: JsonObject, body: suspend (String) -> T): T {
        val id = key.idFor(identity)
        return try {
            body(id).also { key.succeeded() }
        } catch (e: ApiException) {
            key.failed(e)
            throw e
        }
    }

    suspend fun receipt(no: String) {
        val identity = buildJsonObject { put("orderNo", no) }
        write(receiptKey, identity) { id ->
            ApiClient.call { productionReceipt(no, buildJsonObject { put("type", "receipt"); put("requestId", id) }) }
        }
    }

    suspend fun consume(no: String, process: String, item: String, lot: String?, qty: Double, lineNo: Int?): List<Pair<String, Double>> {
        val identity = buildJsonObject {
            put("orderNo", no); put("processCode", process); put("itemCode", item)
            put("lotNo", lot); put("qty", qty); put("lineNo", lineNo)
        }
        val saved = write(consumptionKey, identity) { id ->
            ApiClient.call { productionConsumption(no, buildJsonObject {
                put("type", "consume"); put("requestId", id); put("processCode", process); put("itemCode", item)
                put("lotNo", lot); put("qty", qty); put("lineNo", lineNo)
            }) }
        }
        return saved.data.orEmpty().map { row ->
            val text = { k: String -> row[k]?.jsonPrimitive?.contentOrNull.orEmpty() }
            listOf(text("lotNo"), text("serialNo")).filter { it.isNotEmpty() }.joinToString(" / ") to (text("qty").toDoubleOrNull() ?: 0.0)
        }
    }

    suspend fun returnMaterial(no: String, item: String, lot: String?, qty: Double) {
        val identity = buildJsonObject { put("orderNo", no); put("itemCode", item); put("lotNo", lot); put("qty", qty) }
        write(returnKey, identity) { id ->
            ApiClient.call { productionReturn(no, buildJsonObject {
                put("type", "return"); put("requestId", id); put("itemCode", item); put("lotNo", lot); put("qty", qty)
            }) }
        }
    }

    suspend fun output(no: String, qty: Double, complete: Boolean, expiryDate: String?, lotNo: String?): ProductionOutputResult {
        val identity = buildJsonObject {
            put("orderNo", no); put("qty", qty); put("complete", complete)
            put("expiryDate", expiryDate); put("lotNo", lotNo)
        }
        val id = outputKey.idFor(identity)
        return try {
            val result = ApiClient.call { productionOutput(no, buildJsonObject {
                put("type", "output"); put("requestId", id); put("qty", qty); put("complete", complete)
                put("expiryDate", expiryDate); put("lotNo", lotNo)
            }) }.data ?: throw ApiException("SYS-004", 500, "Empty production result")
            outputRequestId = id
            outputKey.succeeded()
            result
        } catch (e: ApiException) {
            outputKey.failed(e)
            throw e
        }
    }

    suspend fun label(no: String, outputId: String) {
        val identity = buildJsonObject { put("orderNo", no); put("outputRequestId", outputId) }
        write(labelKey, identity) { id ->
            ApiClient.call { productionLabel(no, buildJsonObject {
                put("type", "label"); put("requestId", id); put("outputRequestId", outputId)
            }) }
        }
    }
}
