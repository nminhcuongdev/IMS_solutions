package com.imssolutions.ht.data

import kotlinx.serialization.Serializable


const val ST_IN_PROGRESS = 1
const val ST_COMPLETED = 2

@Serializable
data class StocktakingSession(
    val no: Long = 0,
    val sessionName: String = "",
    val status: Int = 0,
    val statusLabelKey: String = "",
    val startedAt: String? = null,
    val completedAt: String? = null,
    val acceptsResults: Boolean = false,
    val labelCount: Int = 0,
    val scannedCount: Int = 0,
)

@Serializable
data class StocktakingGroup(
    val itemCode: String = "",
    val itemName: String? = null,
    val locationText: String = "",
    val lotNo: String? = null,
    val plannedLabels: Int = 0,
    val scannedLabels: Int = 0,
    val plannedQty: Double = 0.0,
    val resultQty: Double = 0.0,
    val unit: String? = null,
    val updatedAt: String? = null,
) {
    val key: String get() = "$itemCode|${lotNo.orEmpty()}"

    val counted: Boolean get() = scannedLabels > 0
}

fun List<StocktakingGroup>.matching(itemCode: String, lotNo: String?): StocktakingGroup? {
    val item = itemCode.trim()
    val lot = lotNo?.trim().orEmpty()
    val ofItem = filter { it.itemCode.trim().equals(item, ignoreCase = true) }
    return ofItem.firstOrNull { it.lotNo?.trim().orEmpty().equals(lot, ignoreCase = true) }
        ?: ofItem.firstOrNull { it.lotNo.isNullOrBlank() }
}

@Serializable
data class StocktakingResultRequest(
    val locationCode: String,
    val itemCode: String,
    val lotNo: String? = null,
    val countedLabels: Int,
)
