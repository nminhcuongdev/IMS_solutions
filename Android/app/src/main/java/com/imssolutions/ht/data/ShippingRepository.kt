package com.imssolutions.ht.data

import com.imssolutions.ht.scan.LabelQr

class ShippingRepository {

    private val retryKey = RetryKey()

    suspend fun listPlans(
        query: String? = null,
        date: String? = null,
        page: Int = 1,
        pageSize: Int = Paging.PAGE_SIZE,
    ): ApiListSuccess<ShippingPlan> = ApiClient.call {
        listShippingSchedule(
            page = page,
            pageSize = pageSize,
            sort = "scheduled_date",
            q = query?.takeIf { it.isNotBlank() },
            dateFrom = date,
            dateTo = date,
        )
    }

    suspend fun listAllPlans(query: String? = null, date: String? = null): ApiListSuccess<ShippingPlan> =
        Paging.all { page, size -> listPlans(query, date, page, size) }

    suspend fun linesOf(slipNo: String): List<ShippingPlan> =
        Paging.all { page, size ->
            ApiClient.call { listShippingSchedule(slipNo = slipNo, page = page, pageSize = size, sort = "so_branch_no") }
        }.data.filter { it.slipNo.equals(slipNo, ignoreCase = true) }

    suspend fun shortageReasons(): List<Reason> =
        ApiClient.call { listReasons() }.data

    suspend fun confirmLine(
        line: ShippingPlan,
        actualQty: Double,
        lotNo: String?,
        reasonCode: String?,
    ): ShippingResult {
        if (actualQty < 0) throw ApiException("QTY-001", 400, "Please enter a valid quantity (0 or more).")
        if (actualQty > line.plannedQty) {
            throw ApiException("QTY-002", 400, "The scanned quantity exceeds the planned quantity.")
        }
        val request = ShippingResultRequest(
            itemCode = line.itemCode,
            qty = actualQty,
            slipNo = line.slipNo,
            serialNo = line.serialNo,
            lotNo = lotNo?.trim()?.takeIf { it.isNotBlank() },
            reasonCode = reasonCode?.takeIf { it.isNotBlank() },
        )
        return retryKey.send(request, { r, id -> r.copy(requestId = id) }) { sent ->
            ApiClient.call { createShippingResult(sent) }
        }.data ?: throw ApiException("SYS-004", 500, "The server did not return the saved row.")
    }

    suspend fun issueLabel(line: ShippingPlan, qty: Double, printerDeviceCode: String?, lotNo: String? = null): PrintHistoryRow {
        val printer = printerDeviceCode?.takeIf { it.isNotBlank() }
            ?: throw ApiException("htNoPrinter", 0, "Select a printer in Settings.")
        val gtin = runCatching { ApiClient.call { getItem(line.itemCode) }.data?.barcode }.getOrNull()
        val history = PrintHistoryRequest(
            category = "Shipping", docNo = line.slipNo, itemCode = line.itemCode,
            qty = qty, uomCode = line.unit, spec = printer,
        )
        return ApiClient.call {
            printLabel(
                PrintLabelRequest(
                    layoutKey = "ShippingQR",
                    deviceCode = printer,
                    history = history,
                    fields = mapOf(
                        "PONo" to line.slipNo,
                        "SupplierCode" to (line.customerCode ?: ""),
                        "SupplierName" to (line.customerName ?: ""),
                        "Item Code" to line.itemCode,
                        "Item Name" to (line.itemName ?: line.itemCode),
                        "Unit" to (line.unit ?: ""),
                        "TotalQty" to LabelFormat.qty(qty),
                        "Lot" to (lotNo ?: ""),
                        "ScheduledDate" to (line.scheduledDate ?: ""),
                        "ReprintFlag" to "",
                        "QR" to LabelQr.shipping(gtin, lotNo, qty, line.itemCode, line.itemName),
                    ),
                ),
            )
        }.data ?: throw ApiException("SYS-004", 500, "The server did not return the print record.")
    }
}
