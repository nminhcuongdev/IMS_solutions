package com.imssolutions.ht.data

import com.imssolutions.ht.scan.LabelQr

class ReceivingRepository {

    private val retryKey = RetryKey()

    suspend fun listPlans(
        query: String? = null,
        date: String? = null,
        page: Int = 1,
        pageSize: Int = 200,
    ): ApiListSuccess<ReceivingPlan> = ApiClient.call {
        listReceivingSchedule(
            page = page,
            pageSize = pageSize,
            sort = "scheduled_date",
            q = query?.takeIf { it.isNotBlank() },
            dateFrom = date,
            dateTo = date,
        )
    }

    suspend fun config(): HtConfig =
        ApiClient.call { htConfig() }.data ?: HtConfig()

    suspend fun knownLots(itemCode: String? = null): List<HtLot> =
        ApiClient.call { htLots(itemCode = itemCode) }.data

    suspend fun zones(): List<Pair<String, String>> =
        ApiClient.call { listZones() }.data.map { it.zoneCode to "${it.zoneCode} - ${it.zoneName}" }

    suspend fun printers(): List<Pair<String, String>> =
        ApiClient.call { listDevices() }.data
            .filter { !it.ipAddress.isNullOrBlank() }
            .map { it.deviceCode to "${it.deviceName} (${it.ipAddress!!.trim()})" }

    suspend fun languageLabels(lang: String = "vi"): Int =
        ApiClient.call { languageLabels(lang = lang) }.data?.size ?: 0

    suspend fun issueLabel(line: ReceivingPlan, qty: Double, printerDeviceCode: String?): PrintHistoryRow {
        val printer = printerDeviceCode?.takeIf { it.isNotBlank() }
            ?: throw ApiException("htNoPrinter", 0, "Select a printer in Settings.")
        val history = PrintHistoryRequest(
            category = "Receiving",
            docNo = line.poNo,
            itemCode = line.itemCode,
            qty = qty,
            uomCode = line.unit,
            spec = printer,
        )
        run {
            val qr = buildReceivingQr(line, qty)
            return ApiClient.call {
                printLabel(
                    PrintLabelRequest(
                        layoutKey = "ReceivingQR",
                        deviceCode = printer,
                        history = history,
                        fields = mapOf(
                            "PONo" to line.poNo,
                            "SupplierCode" to (line.supplierCode ?: ""),
                            "SupplierName" to (line.supplierName ?: ""),
                            "Item Code" to line.itemCode,
                            "Item Name" to (line.itemName ?: line.itemCode),
                            "Unit" to (line.unit ?: ""),
                            "ItemQty" to formatQty(qty),
                            "Lot" to (line.lotNo ?: ""),
                            "ScheduledDate" to (line.scheduledDate ?: ""),
                            "ReprintFlag" to "",
                            "QR" to qr,
                        ),
                    ),
                )
            }.data ?: throw ApiException("SYS-004", 500, "The server did not return the print record.")
        }
    }

    private fun formatQty(qty: Double): String =
        if (qty == qty.toLong().toDouble()) qty.toLong().toString() else qty.toString()

    private suspend fun buildReceivingQr(line: ReceivingPlan, qty: Double): String {
        val gtin = runCatching { ApiClient.call { getItem(line.itemCode) }.data?.barcode }.getOrNull()
        return LabelQr.product(
            gtin = gtin,
            lotNo = line.lotNo,
            qty = qty,
            itemCode = line.itemCode,
            itemName = line.itemName,
            productionDate = LabelQr.date(line.scheduledDate),
            userCode = ApiClient.userCode(),
        )
    }

    suspend fun listAllPlans(query: String? = null, date: String? = null): ApiListSuccess<ReceivingPlan> =
        Paging.all { page, size -> listPlans(query, date, page, size) }

    suspend fun linesOf(poNo: String): List<ReceivingPlan> =
        Paging.all { page, size ->
            ApiClient.call { listReceivingSchedule(poNo = poNo, page = page, pageSize = size, sort = "po_branch_no") }
        }.data.filter { it.poNo.equals(poNo, ignoreCase = true) }

    suspend fun listResults(page: Int = 1, pageSize: Int = 50): ApiListSuccess<ReceivingResult> =
        ApiClient.call { listReceivingResult(page = page, pageSize = pageSize, sort = "-created_at") }

    suspend fun shortageReasons(): List<Reason> =
        ApiClient.call { listReasons() }.data

    suspend fun confirmLine(
        line: ReceivingPlan,
        actualQty: Double,
        reasonCode: String?,
        lotNo: String?,
        locationCode: String?,
    ): ReceivingResult {
        if (actualQty < 0) throw ApiException("QTY-001", 400, "Please enter a valid quantity (0 or more).")

        val lot = lotNo?.trim()?.takeIf { it.isNotBlank() }
        val reason = reasonCode?.takeIf { it.isNotBlank() }

        val request = ReceivingResultRequest(
            itemCode = line.itemCode,
            qty = actualQty,
            poNo = line.poNo,
            serialNo = line.serialNo,
            locationCode = locationCode?.trim()?.takeIf { it.isNotEmpty() },
            lotNo = lot,
            reasonCode = reason,
        )
        return retryKey.send(request, { r, id -> r.copy(requestId = id) }) { sent ->
            ApiClient.call { createReceivingResult(sent) }
        }.data ?: throw ApiException("SYS-004", 500, "The server did not return the saved row.")
    }
}

