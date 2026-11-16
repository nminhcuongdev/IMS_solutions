package com.imssolutions.ht.data

class StocktakingRepository {

    private val retryKey = RetryKey()

    suspend fun session(): StocktakingSession? = ApiClient.call { stocktakingSession() }.data

    suspend fun itemsAt(locationCode: String): List<StocktakingGroup> =
        ApiClient.call { stocktakingItems(locationCode) }.data.orEmpty()

    suspend fun save(
        locationCode: String,
        itemCode: String,
        lotNo: String?,
        countedLabels: Int,
    ): StocktakingGroup {
        val request = StocktakingResultRequest(
            locationCode = locationCode,
            itemCode = itemCode,
            lotNo = lotNo?.trim()?.takeIf { it.isNotBlank() },
            countedLabels = countedLabels,
        )
        return retryKey.send(request, { r, id -> r.copy(requestId = id) }) { sent ->
            ApiClient.call { saveStocktakingResult(sent) }
        }.data ?: throw ApiException("SYS-004", 500, "The server did not return the saved row.")
    }

    suspend fun zones(): List<Zone> = ApiClient.call { listZones() }.data

    suspend fun locationsIn(zoneCode: String): List<Location> =
        ApiClient.call { listLocations(zoneCode = zoneCode, type = "Small") }.data
}
