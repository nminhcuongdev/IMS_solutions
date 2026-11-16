package com.imssolutions.ht.data

class PickingRepository {

    private val retryKey = RetryKey()

    suspend fun listPlans(
        query: String? = null,
        date: String? = null,
        page: Int = 1,
        pageSize: Int = Paging.PAGE_SIZE,
    ): ApiListSuccess<PickingPlan> = ApiClient.call {
        listPickingSchedule(
            page = page,
            pageSize = pageSize,
            sort = "scheduled_date",
            q = query?.takeIf { it.isNotBlank() },
            dateFrom = date,
            dateTo = date,
        )
    }

    suspend fun listAllPlans(query: String? = null, date: String? = null): ApiListSuccess<PickingPlan> =
        Paging.all { page, size -> listPlans(query, date, page, size) }

    suspend fun line(soNo: String, serialNo: String): PickingPlan? =
        Paging.all { page, size ->
            ApiClient.call { listPickingSchedule(soNo = soNo, page = page, pageSize = size, sort = "so_branch_no") }
        }.data.firstOrNull { it.soNo.equals(soNo, ignoreCase = true) && it.serialNo == serialNo }

    suspend fun confirmLine(
        line: PickingPlan,
        pickedQty: Double,
        locationCode: String? = null,
        lotNo: String? = null,
    ): PickingResult {
        if (pickedQty < 0) throw ApiException("QTY-001", 400, "Please enter a valid quantity (0 or more).")
        if (pickedQty > line.plannedQty) {
            throw ApiException("QTY-002", 400, "The scanned quantity exceeds the planned quantity.")
        }
        val request = PickingResultRequest(
            itemCode = line.itemCode,
            qty = pickedQty,
            soNo = line.soNo,
            serialNo = line.serialNo,
            locationCode = locationCode?.takeIf { it.isNotBlank() } ?: line.locationCode,
            lotNo = lotNo?.trim()?.takeIf { it.isNotBlank() },
        )
        return retryKey.send(request, { r, id -> r.copy(requestId = id) }) { sent ->
            ApiClient.call { createPickingResult(sent) }
        }.data ?: throw ApiException("SYS-004", 500, "The server did not return the saved row.")
    }
}
