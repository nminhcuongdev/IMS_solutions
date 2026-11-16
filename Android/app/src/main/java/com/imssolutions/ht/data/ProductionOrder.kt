package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class ProductionOrder(
    val no: Long = 0,
    val productionOrderNo: String = "",
    val outputItemCode: String = "",
    val outputItemName: String? = null,
    val plannedQty: Double = 0.0,
    val outputQty: Double = 0.0,
    val uomCode: String = "",
    val productionLotNo: String? = null,
    val outputLotNo: String? = null,
    val storageLocationCode: String = "",
    val factoryCode: String? = null,
    val plannedStartDate: String = "",
    val statusCode: Int = 0,
)
