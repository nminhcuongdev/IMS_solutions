package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class ProductionWork(
    val no: Long = 0,
    val requestId: String? = null,
    val outputRequestId: String? = null,
    val type: String = "",
    val itemCode: String? = null,
    val lotNo: String? = null,
    val qty: Double? = null,
    val createdAt: String = "",
    val isReversed: Boolean = false,
    val reversesNo: Long? = null,
    val productionOrderNo: String? = null,
    val lineNo: Int? = null,
    val serialNo: String? = null,
    val expiryDate: String? = null,
    val locationCode: String? = null,
    val labelCount: Int? = null,
    val totalQty: Double? = null,
    val stdQty: Double? = null,
    val remainderQty: Double? = null,
    val createdBy: String? = null,
)
