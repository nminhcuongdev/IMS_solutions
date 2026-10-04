package com.imssolutions.ht.data

class ReceivingRepository {

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
        val history = PrintHistoryRequest(
            category = "Receiving",
            docNo = line.poNo,
            itemCode = line.itemCode,
            qty = qty,
            uomCode = line.unit,
            spec = printerDeviceCode,
        )
        if (!printerDeviceCode.isNullOrBlank()) {
            return ApiClient.call {
                printLabel(
                    PrintLabelRequest(
                        layoutKey = "ReceivingQR",
                        deviceCode = printerDeviceCode,
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
                            "QR" to buildReceivingQr(line, qty),
                        ),
                    ),
                )
            }.data ?: throw ApiException("SYS-004", 500, "Máy chủ không trả về bản ghi in.")
        }
        return ApiClient.call { createPrintHistory(history) }.data
            ?: throw ApiException("SYS-004", 500, "Máy chủ không trả về bản ghi in.")
    }

    private fun formatQty(qty: Double): String =
        if (qty == qty.toLong().toDouble()) qty.toLong().toString() else qty.toString()

    private fun buildReceivingQr(line: ReceivingPlan, qty: Double): String {
        fun enc(v: String) = java.net.URLEncoder.encode(v, "UTF-8")
        val path = line.lotNo?.trim()?.takeIf { it.isNotEmpty() }?.let { "/10/${enc(it)}" } ?: ""
        val query = buildString {
            append("30=").append(enc(formatQty(qty)))
            append("&item_cd=").append(enc(line.itemCode))
        }
        return "$path?$query"
    }

    suspend fun linesOf(poNo: String): List<ReceivingPlan> = ApiClient.call {
        listReceivingSchedule(poNo = poNo, pageSize = 200, sort = "po_branch_no")
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
        require(actualQty >= 0) { "actualQty không được âm" }

        val lot = lotNo?.trim()?.takeIf { it.isNotBlank() }
            ?: throw ApiException("LOT-001", 400, "Vui lòng nhập số lô.")
        val reason = reasonCode?.takeIf { it.isNotBlank() }
        if (actualQty < line.expectedQty && reason == null) {
            throw ApiException("RSN-001", 400, "Vui lòng chọn lý do.")
        }

        return ApiClient.call {
            createReceivingResult(
                ReceivingResultRequest(
                    itemCode = line.itemCode,
                    qty = actualQty,
                    poNo = line.poNo,
                    serialNo = line.serialNo,
                    locationCode = locationCode?.trim()?.takeIf { it.isNotEmpty() },
                    lotNo = lot,
                    reasonCode = reason,
                ),
            )
        }.data ?: throw ApiException("SYS-004", 500, "Máy chủ không trả về dòng vừa ghi.")
    }
}

