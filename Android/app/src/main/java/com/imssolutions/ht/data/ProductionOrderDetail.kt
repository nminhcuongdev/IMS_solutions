package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class ProductionOrderDetail(
    val order: ProductionOrder = ProductionOrder(),
    val lines: List<ProductionLine> = emptyList(),
    val outputQty: Double = 0.0,
)
