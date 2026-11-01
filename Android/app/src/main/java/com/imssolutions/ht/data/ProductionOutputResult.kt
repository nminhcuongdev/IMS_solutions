package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class ProductionOutputResult(
    val orderNo: String = "",
    val itemCode: String = "",
    val lotNo: String = "",
    val qty: Double = 0.0,
    val serials: List<String> = emptyList(),
    val overPlan: Boolean = false,
    val labelCount: Int = 0,
    val stdQty: Double = 1.0,
    val remainderQty: Double = 0.0,
)
