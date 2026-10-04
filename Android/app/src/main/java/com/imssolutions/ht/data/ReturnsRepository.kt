package com.imssolutions.ht.data

class ReturnsRepository {

    suspend fun listPlans(
        query: String? = null,
        date: String? = null,
        page: Int = 1,
        pageSize: Int = 200,
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

    suspend fun linesOf(returnNo: String): List<ReturnPlan> = ApiClient.call {
        listReturnsSchedule(returnNo = returnNo, pageSize = 200, sort = "return_branch_no")
    }.data.filter { it.returnNo.equals(returnNo, ignoreCase = true) }

    suspend fun locations(q: String? = null): List<Location> =
        ApiClient.call { listLocations(q = q?.takeIf { it.isNotBlank() }) }.data

    suspend fun confirmLine(
        line: ReturnPlan,
        actualQty: Double,
        locationCode: String?,
        lotNo: String?,
    ): ReturnResult {
        require(actualQty >= 0) { "actualQty không được âm" }

        val lot = lotNo?.trim()?.takeIf { it.isNotBlank() }
            ?: throw ApiException("LOT-001", 400, "Vui lòng nhập số lô.")
        val loc = locationCode?.trim()?.takeIf { it.isNotBlank() }
            ?: throw ApiException("LOC-001", 400, "Vui lòng nhập vị trí.")

        return ApiClient.call {
            createReturnResult(
                ReturnResultRequest(
                    itemCode = line.itemCode,
                    qty = actualQty,
                    returnNo = line.returnNo,
                    serialNo = line.serialNo,
                    locationCode = loc,
                    lotNo = lot,
                ),
            )
        }.data ?: throw ApiException("SYS-004", 500, "Máy chủ không trả về dòng vừa ghi.")
    }

    suspend fun issueLabel(line: ReturnPlan, qty: Double, printerDeviceCode: String?): PrintHistoryRow =
        ApiClient.call {
            createPrintHistory(
                PrintHistoryRequest(
                    category = "Returns",
                    docNo = line.returnNo,
                    itemCode = line.itemCode,
                    qty = qty,
                    uomCode = line.unit,
                    spec = printerDeviceCode,
                ),
            )
        }.data ?: throw ApiException("SYS-004", 500, "Máy chủ không trả về bản ghi in.")
}
