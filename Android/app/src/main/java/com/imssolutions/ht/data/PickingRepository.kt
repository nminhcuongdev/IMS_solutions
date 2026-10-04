package com.imssolutions.ht.data

class PickingRepository {

    suspend fun listPlans(
        query: String? = null,
        date: String? = null,
        page: Int = 1,
        pageSize: Int = 200,
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

    suspend fun line(soNo: String, serialNo: String): PickingPlan? = ApiClient.call {
        listPickingSchedule(soNo = soNo, pageSize = 200, sort = "so_branch_no")
    }.data.firstOrNull { it.soNo.equals(soNo, ignoreCase = true) && it.serialNo == serialNo }

    suspend fun confirmLine(
        line: PickingPlan,
        pickedQty: Double,
        locationCode: String? = null,
        lotNo: String? = null,
    ): PickingResult {
        require(pickedQty >= 0) { "pickedQty không được âm" }

        if (pickedQty > line.plannedQty) {
            throw ApiException("QTY-001", 400, "Không nhặt được nhiều hơn số kế hoạch.")
        }

        return ApiClient.call {
            createPickingResult(
                PickingResultRequest(
                    itemCode = line.itemCode,
                    qty = pickedQty,
                    soNo = line.soNo,
                    serialNo = line.serialNo,
                    locationCode = locationCode?.takeIf { it.isNotBlank() } ?: line.locationCode,
                    lotNo = lotNo?.trim()?.takeIf { it.isNotBlank() },
                ),
            )
        }.data ?: throw ApiException("SYS-004", 500, "Máy chủ không trả về dòng vừa ghi.")
    }
}
