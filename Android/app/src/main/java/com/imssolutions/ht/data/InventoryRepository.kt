package com.imssolutions.ht.data

class InventoryRepository {

    suspend fun listItems(
        query: String? = null,
        page: Int = 1,
        pageSize: Int = 200,
    ): ApiListSuccess<StockGroup> = ApiClient.call {
        listStockByItem(
            page = page,
            pageSize = pageSize,
            q = query?.trim()?.takeIf { it.isNotBlank() },
        )
    }

    suspend fun linesOf(itemCode: String): List<StockLine> = ApiClient.call {
        listStockLines(itemCode = itemCode, pageSize = 200, sort = "location_code")
    }.data.filter { it.itemCode.equals(itemCode, ignoreCase = true) }


    suspend fun disposalReasons(): List<Reason> =
        ApiClient.call { listReasons(category = "Defect") }.data

    suspend fun resolveScan(itemCode: String, lotNo: String?): DisposalLine? {
        val lines = linesOf(itemCode).filter { it.stockQty > 0 }
        val lot = lotNo?.trim()?.takeIf { it.isNotBlank() }
        val hit = if (lot == null) lines.firstOrNull()
        else lines.firstOrNull { it.lotNo?.trim().equals(lot, ignoreCase = true) }
        return hit?.let {
            DisposalLine(
                itemCode = it.itemCode, serialNo = it.serialNo, itemName = it.itemName,
                lotNo = it.lotNo, unit = it.unit, stockQty = it.stockQty,
            )
        }
    }

    suspend fun disposeAll(lines: List<DisposalLine>, reasonCode: String): StockBulkResult {
        var done = 0
        val failed = mutableListOf<String>()
        for (l in lines) {
            runCatching {
                ApiClient.call {
                    disposeStock(
                        StockDisposalRequest(
                            itemCode = l.itemCode, serialNo = l.serialNo,
                            reasonCode = reasonCode, qty = l.qty,
                        ),
                    )
                }
            }.onSuccess { done++ }.onFailure { failed += l.key }
        }
        return StockBulkResult(done, failed)
    }


    suspend fun zones(): List<Zone> = ApiClient.call { listZones() }.data

    suspend fun locationsIn(zoneCode: String): List<Location> =
        ApiClient.call { listLocations(zoneCode = zoneCode, type = "Small") }.data

    suspend fun resolveTransferScan(itemCode: String, lotNo: String?): TransferLine? {
        val lines = linesOf(itemCode)
        val lot = lotNo?.trim()?.takeIf { it.isNotBlank() }
        val hit = if (lot == null) lines.firstOrNull { it.stockQty > 0 } ?: lines.firstOrNull()
        else lines.firstOrNull { it.lotNo?.trim().equals(lot, ignoreCase = true) }
        return hit?.let {
            TransferLine(
                itemCode = it.itemCode, serialNo = it.serialNo, itemName = it.itemName,
                lotNo = it.lotNo, unit = it.unit, qty = it.stockQty, locationCode = it.locationCode,
            )
        }
    }

    suspend fun transferAll(lines: List<TransferLine>, toLocationCode: String): StockBulkResult {
        var done = 0
        val failed = mutableListOf<String>()
        for (l in lines) {
            runCatching {
                ApiClient.call {
                    transferStock(
                        StockTransferRequest(
                            itemCode = l.itemCode, serialNo = l.serialNo,
                            toLocationCode = toLocationCode,
                        ),
                    )
                }
            }.onSuccess { done++ }.onFailure { failed += l.key }
        }
        return StockBulkResult(done, failed)
    }
}
