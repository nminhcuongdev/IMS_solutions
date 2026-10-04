package com.imssolutions.ht.data

class ShippingRepository {

    suspend fun listPlans(
        query: String? = null,
        date: String? = null,
        page: Int = 1,
        pageSize: Int = 200,
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

    suspend fun linesOf(slipNo: String): List<ShippingPlan> = ApiClient.call {
        listShippingSchedule(slipNo = slipNo, pageSize = 200, sort = "so_branch_no")
    }.data.filter { it.slipNo.equals(slipNo, ignoreCase = true) }

    suspend fun shortageReasons(): List<Reason> =
        ApiClient.call { listReasons() }.data

    suspend fun confirmLine(
        line: ShippingPlan,
        actualQty: Double,
        lotNo: String?,
        reasonCode: String?,
    ): ShippingResult {
        require(actualQty >= 0) { "actualQty không được âm" }

        val lot = lotNo?.trim()?.takeIf { it.isNotBlank() }
            ?: throw ApiException("LOT-001", 400, "Vui lòng nhập số lô.")
        if (actualQty > line.plannedQty) {
            throw ApiException("QTY-001", 400, "Không xuất được nhiều hơn số kế hoạch.")
        }
        val reason = reasonCode?.takeIf { it.isNotBlank() }
        if (actualQty < line.plannedQty && reason == null) {
            throw ApiException("RSN-001", 400, "Vui lòng chọn lý do.")
        }

        return ApiClient.call {
            createShippingResult(
                ShippingResultRequest(
                    itemCode = line.itemCode,
                    qty = actualQty,
                    slipNo = line.slipNo,
                    serialNo = line.serialNo,
                    lotNo = lot,
                    reasonCode = reason,
                ),
            )
        }.data ?: throw ApiException("SYS-004", 500, "Máy chủ không trả về dòng vừa ghi.")
    }

    suspend fun issueLabel(line: ShippingPlan, qty: Double, printerDeviceCode: String?): PrintHistoryRow =
        ApiClient.call {
            createPrintHistory(
                PrintHistoryRequest(
                    category = "Shipping",
                    docNo = line.slipNo,
                    itemCode = line.itemCode,
                    qty = qty,
                    uomCode = line.unit,
                    spec = printerDeviceCode,
                ),
            )
        }.data ?: throw ApiException("SYS-004", 500, "Máy chủ không trả về bản ghi in.")
}
