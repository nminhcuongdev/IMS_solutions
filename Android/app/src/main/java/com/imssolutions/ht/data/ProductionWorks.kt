package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class ProductionWorks(
    val receipts: List<ProductionWork> = emptyList(),
    val consumptions: List<ProductionWork> = emptyList(),
    val outputs: List<ProductionWork> = emptyList(),
    val labels: List<ProductionWork> = emptyList(),
    val returns: List<ProductionWork> = emptyList(),
)
