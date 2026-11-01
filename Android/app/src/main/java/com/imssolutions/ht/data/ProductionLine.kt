package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class ProductionLine(
    val no: Long = 0,
    val productionOrderNo: String = "",
    val lineNo: Int = 0,
    val processCode: String = "",
    val itemCode: String = "",
    val itemName: String? = null,
    val plannedQty: Double = 0.0,
    val consumedQty: Double = 0.0,
    val uomCode: String = "",
    val received: Boolean = false,
    val htScanRequired: Boolean = false,
    val processName: String? = null,
    val processActive: Boolean = true,
    val category: String? = null,
    val htInputCount: Int = 0,
    val scheduledDate: String? = null,
    val statusCode: Int = 0,
)
