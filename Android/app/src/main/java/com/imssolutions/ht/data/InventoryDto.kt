package com.imssolutions.ht.data

import kotlinx.serialization.Serializable


const val EXPIRING_SOON_DAYS = 30

@Serializable
data class StockGroup(
    val key: String = "",
    val name: String? = null,
    val category: String? = null,
    val warehouse: String? = null,
    val unit: String? = null,
    val stockQty: Double = 0.0,
    val lotCount: Int = 0,
    val itemCount: Int = 0,
    val anyOnHold: Boolean = false,
    val earliestExpiry: String? = null,
    val remainingDays: Int? = null,
    val reorderPoint: Double? = null,
) {
    val expiry: ExpiryState get() = expiryStateOf(remainingDays)

    val isLow: Boolean get() = reorderPoint?.let { stockQty < it } == true

    val isAlert: Boolean get() = isLow || anyOnHold
}

@Serializable
data class StockLine(
    val itemCode: String = "",
    val serialNo: String = "",
    val itemName: String? = null,
    val category: String? = null,
    val warehouse: String? = null,
    val locationCode: String? = null,
    val locationName: String? = null,
    val lotNo: String? = null,
    val isOnHold: Boolean = false,
    val holdReason: String? = null,
    val stockQty: Double = 0.0,
    val availableQty: Double = 0.0,
    val unit: String? = null,
    val expiryDate: String? = null,
    val remainingDays: Int? = null,
)

enum class ExpiryState { NONE, SOON, EXPIRED }

fun expiryStateOf(remainingDays: Int?): ExpiryState = when {
    remainingDays == null -> ExpiryState.NONE
    remainingDays < 0 -> ExpiryState.EXPIRED
    remainingDays <= EXPIRING_SOON_DAYS -> ExpiryState.SOON
    else -> ExpiryState.NONE
}

data class StockBulkResult(val updated: Int, val skipped: List<String>)

@Serializable
data class StockDisposalRequest(
    val itemCode: String,
    val serialNo: String,
    val reasonCode: String,
    val qty: Double,
)

data class DisposalLine(
    val itemCode: String,
    val serialNo: String,
    val itemName: String?,
    val lotNo: String?,
    val unit: String?,
    val stockQty: Double,
    val qty: Double = 1.0,
) {
    val key: String get() = "$itemCode/$serialNo"

    val overStock: Boolean get() = qty > stockQty
}

@Serializable
data class StockTransferRequest(
    val itemCode: String,
    val serialNo: String,
    val toLocationCode: String,
)

data class TransferLine(
    val itemCode: String,
    val serialNo: String,
    val itemName: String?,
    val lotNo: String?,
    val unit: String?,
    val qty: Double,
    val locationCode: String?,
) {
    val key: String get() = "$itemCode/$serialNo"
}
