package com.imssolutions.ht.data

import com.imssolutions.ht.scan.LabelQr

class ReturnsRepository {

    private val retryKey = RetryKey()

    suspend fun listPlans(
        query: String? = null,
        date: String? = null,
        page: Int = 1,
        pageSize: Int = Paging.PAGE_SIZE,
    ): ApiListSuccess<ReturnPlan> = ApiClient.call {
        listReturnsSchedule(
            page = page,
            pageSize = pageSize,
            sort = "scheduled_date",
            q = query?.takeIf { it.isNotBlank() },
            dateFrom = date,
            dateTo = date,
        )
    }

    suspend fun listAllPlans(query: String? = null, date: String? = null): ApiListSuccess<ReturnPlan> =
        Paging.all { page, size -> listPlans(query, date, page, size) }

    suspend fun linesOf(returnNo: String): List<ReturnPlan> =
        Paging.all { page, size ->
            ApiClient.call { listReturnsSchedule(returnNo = returnNo, page = page, pageSize = size, sort = "return_branch_no") }
        }.data.filter { it.returnNo.equals(returnNo, ignoreCase = true) }

    suspend fun locations(q: String? = null): List<Location> =
        ApiClient.call { listLocations(q = q?.takeIf { it.isNotBlank() }) }.data

    suspend fun confirmLine(
        line: ReturnPlan,
        actualQty: Double,
        locationCode: String?,
        lotNo: String?,
    ): ReturnResult {
        if (actualQty < 0) throw ApiException("QTY-001", 400, "Please enter a valid quantity (0 or more).")
        val loc = locationCode?.trim()?.takeIf { it.isNotBlank() }
            ?: throw ApiException("LOC-001", 400, "Please enter a Location.")
        val request = ReturnResultRequest(
            itemCode = line.itemCode,
            qty = actualQty,
            returnNo = line.returnNo,
            serialNo = line.serialNo,
            locationCode = loc,
            lotNo = lotNo?.trim()?.takeIf { it.isNotBlank() },
        )
        return retryKey.send(request, { r, id -> r.copy(requestId = id) }) { sent ->
            ApiClient.call { createReturnResult(sent) }
        }.data ?: throw ApiException("SYS-004", 500, "The server did not return the saved row.")
    }

    suspend fun issueLabel(line: ReturnPlan, qty: Double, printerDeviceCode: String?, lotNo: String? = null): PrintHistoryRow {
        val printer = printerDeviceCode?.takeIf { it.isNotBlank() }
            ?: throw ApiException("htNoPrinter", 0, "Select a printer in Settings.")
        val lot = lotNo ?: line.lotNo
        val gtin = runCatching { ApiClient.call { getItem(line.itemCode) }.data?.barcode }.getOrNull()
        val history = PrintHistoryRequest(
            category = "Returns", docNo = line.returnNo, itemCode = line.itemCode,
            qty = qty, uomCode = line.unit, spec = printer,
        )
        val returnedBy = listOfNotNull(line.returnedByName, line.returnedByCode?.let { "($it)" }).joinToString(" ")
        return ApiClient.call {
            printLabel(
                PrintLabelRequest(
                    layoutKey = "ReturnsQR",
                    deviceCode = printer,
                    history = history,
                    fields = mapOf(
                        "PONo" to line.returnNo,
                        "SupplierCode" to (line.returnedByCode ?: ""),
                        "SupplierName" to returnedBy,
                        "Item Code" to line.itemCode,
                        "Item Name" to (line.itemName ?: line.itemCode),
                        "Unit" to (line.unit ?: ""),
                        "ItemQty" to LabelFormat.qty(qty),
                        "Lot" to (lot ?: ""),
                        "ScheduledDate" to (line.returnDate ?: ""),
                        "ReprintFlag" to "",
                        "QR" to LabelQr.product(
                            gtin = gtin, lotNo = lot, qty = qty, itemCode = line.itemCode, itemName = line.itemName,
                            productionDate = LabelQr.date(line.returnDate), userCode = ApiClient.userCode(),
                        ),
                    ),
                ),
            )
        }.data ?: throw ApiException("SYS-004", 500, "The server did not return the print record.")
    }
}
