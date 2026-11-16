package com.imssolutions.ht.data

class InventoryRepository {

    private val disposalKeys = RetryKeys()
    private val transferKeys = RetryKeys()

    suspend fun listItems(
        query: String? = null,
        page: Int = 1,
        pageSize: Int = Paging.PAGE_SIZE,
    ): ApiListSuccess<StockGroup> = ApiClient.call {
        listStockByItem(
            page = page,
            pageSize = pageSize,
            q = query?.trim()?.takeIf { it.isNotBlank() },
        )
    }

    suspend fun listAllItems(query: String? = null): ApiListSuccess<StockGroup> =
        Paging.all { page, size -> listItems(query, page, size) }

    suspend fun linesOf(itemCode: String): List<StockLine> =
        Paging.all { page, size ->
            ApiClient.call { listStockLines(itemCode = itemCode, page = page, pageSize = size, sort = "location_code") }
        }.data.filter { it.itemCode.equals(itemCode, ignoreCase = true) }

    suspend fun disposalReasons(): List<Reason> =
        ApiClient.call { listReasons(category = "Defect") }.data

    suspend fun candidates(itemCode: String, lotNo: String?, serialNo: String?): List<StockLine> =
        StockScanRules.candidates(linesOf(itemCode), lotNo, serialNo)

    suspend fun disposeAll(lines: List<DisposalLine>, reasonCode: String): StockBulkResult {
        var done = 0
        val failed = mutableListOf<String>()
        for (l in lines) {
            val request = StockDisposalRequest(
                itemCode = l.itemCode, serialNo = l.serialNo, reasonCode = reasonCode, qty = l.qty,
            )
            runCatching {
                disposalKeys.of(l.key).send(request, { r, id -> r.copy(requestId = id) }) { sent ->
                    ApiClient.call { disposeStock(sent) }
                }
            }.onSuccess { done++ }.onFailure { failed += l.key }
        }
        return StockBulkResult(done, failed)
    }

    suspend fun zones(): List<Zone> = ApiClient.call { listZones() }.data

    suspend fun locationsIn(zoneCode: String): List<Location> =
        ApiClient.call { listLocations(zoneCode = zoneCode, type = "Small") }.data

    suspend fun transferAll(lines: List<TransferLine>, toLocationCode: String): StockBulkResult {
        var done = 0
        val failed = mutableListOf<String>()
        for (l in lines) {
            val request = StockTransferRequest(
                itemCode = l.itemCode, serialNo = l.serialNo, toLocationCode = toLocationCode,
            )
            runCatching {
                transferKeys.of(l.key + ">" + toLocationCode).send(request, { r, id -> r.copy(requestId = id) }) { sent ->
                    ApiClient.call { transferStock(sent) }
                }
            }.onSuccess { done++ }.onFailure { failed += l.key }
        }
        return StockBulkResult(done, failed)
    }
}
