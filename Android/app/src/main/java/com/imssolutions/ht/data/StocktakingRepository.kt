package com.imssolutions.ht.data

class StocktakingRepository {

    suspend fun session(): StocktakingSession? = ApiClient.call { stocktakingSession() }.data

    suspend fun itemsAt(locationCode: String): List<StocktakingGroup> =
        ApiClient.call { stocktakingItems(locationCode) }.data.orEmpty()

    suspend fun save(
        locationCode: String,
        itemCode: String,
        lotNo: String?,
        countedLabels: Int,
    ): StocktakingGroup = ApiClient.call {
        saveStocktakingResult(
            StocktakingResultRequest(
                locationCode = locationCode,
                itemCode = itemCode,
                lotNo = lotNo?.trim()?.takeIf { it.isNotBlank() },
                countedLabels = countedLabels,
            ),
        )
    }.data!!

    suspend fun zones(): List<Zone> = ApiClient.call { listZones() }.data

    suspend fun locationsIn(zoneCode: String): List<Location> =
        ApiClient.call { listLocations(zoneCode = zoneCode, type = "Small") }.data
}
