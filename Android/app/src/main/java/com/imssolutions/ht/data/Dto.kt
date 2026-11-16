package com.imssolutions.ht.data

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable


@Serializable
data class ApiSuccess<T>(
    val success: Boolean = true,
    val data: T? = null,
)

@Serializable
data class LanguageOption(
    val languageCode: String,
    val languageName: String,
    val iconImage: String? = null,
)

@Serializable
data class ApiListSuccess<T>(
    val success: Boolean = true,
    val data: List<T> = emptyList(),
    val meta: PageMeta = PageMeta(),
)

@Serializable
data class PageMeta(
    val total: Int = 0,
    val page: Int = 1,
    val pageSize: Int = 50,
)

@Serializable
data class ApiErrorBody(
    val code: String = "",
    val httpStatus: Int = 0,
    val severity: String = "",
    val message: String = "",
    val args: List<String>? = null,
    val messageKey: String? = null,
)

@Serializable
data class ApiErrorEnvelope(
    val success: Boolean = false,
    val error: ApiErrorBody = ApiErrorBody(),
)


@Serializable
data class LoginRequest(
    val userId: String,
    val password: String,
)

@Serializable
data class LoginResponse(
    val token: String = "",
    val expiresIn: Int = 0,
    val tokenType: String = "Bearer",
    val userCode: String = "",
    val userName: String = "",
    val roleCode: String = "",
    val isSuperAdmin: Boolean = false,
    val defaultLanguage: String? = null,
)


@Serializable
data class ReceivingPlan(
    val poNo: String = "",
    val serialNo: String = "",
    val supplierCode: String? = null,
    val supplierName: String? = null,
    val itemCode: String = "",
    val itemName: String? = null,
    val plannedQty: Double = 0.0,
    val unit: String? = null,
    val actualQty: Double = 0.0,
    val packingTypeCode: String? = null,
    val packingQty: Double? = null,
    val scheduledDate: String? = null,
    val invoiceNo: String? = null,
    val status: String = STATUS_NOT_ARRIVED,
    val shortageReason: String? = null,
    val lotNo: String? = null,
    val locationCode: String? = null,
    val labelIssued: Boolean = false,
    val createdAt: String = "",
    val createdBy: String? = null,
) {
    val lineKey: String get() = "$poNo/$serialNo"

    val expectedQty: Double
        get() {
            val remaining = (plannedQty - actualQty).coerceAtLeast(0.0)
            val pack = packingQty ?: 0.0
            return if (pack > 1) minOf(pack, remaining) else remaining
        }

    val caseCount: Double?
        get() = packingQty?.takeIf { it > 1 }?.let { kotlin.math.ceil(plannedQty / it) }

    val isOpen: Boolean get() = status == STATUS_NOT_ARRIVED || status == STATUS_PARTIAL
}

@Serializable
data class ReceivingResult(
    val no: Long = 0,
    val createdAt: String = "",
    val itemCode: String = "",
    val itemName: String? = null,
    val qty: Double = 0.0,
    val unit: String? = null,
    val poNo: String? = null,
    val serialNo: String? = null,
    val invoiceNo: String? = null,
    val locationCode: String? = null,
    val worker: String? = null,
    val status: String = STATUS_ARRIVED,
    val shortageReason: String? = null,
    val expiryDate: String? = null,
)

@Serializable
data class StatusChangeRequest(
    val poNos: List<String>,
    val status: String,
)

@Serializable
data class StatusChangeResult(
    val updated: Int = 0,
    val skipped: List<String> = emptyList(),
)

@Serializable
data class ShortageRequest(
    val shortageQty: Double,
    val reasonCode: String,
    val serialNo: String? = null,
)

@Serializable
data class ReceivingResultRequest(
    val itemCode: String,
    val qty: Double,
    val poNo: String? = null,
    val serialNo: String? = null,
    val locationCode: String? = null,
    val lotNo: String? = null,
    val expiryDate: String? = null,
    val reasonCode: String? = null,
    val invoiceNo: String? = null,
    val isOnHold: Boolean = false,
    val requestId: String? = null,
)

@Serializable
data class Reason(
    val reasonCode: String = "",
    val reasonName: String = "",
    @SerialName("reasonCategory") val category: String? = null,
)

@Serializable
data class HtConfig(
    val labelModeRcv: String = "printNow",
    val labelModeShp: String = "printNow",
    val labelModeRtn: String = "printNow",
    val reasonScope: String = "byFunction",
    val apkVersion: String? = null,
    val apkDownloadUrl: String? = null,
    val printerDrivers: List<String> = emptyList(),
)


@Serializable
data class ReturnPlan(
    val returnNo: String = "",
    val serialNo: String = "",
    val returnedByCode: String? = null,
    val returnedByName: String? = null,
    val itemCode: String = "",
    val itemName: String? = null,
    val expectedQty: Double = 0.0,
    val unit: String? = null,
    val actualQty: Double = 0.0,
    val returnDate: String? = null,
    val invoiceNo: String? = null,
    val status: String = STATUS_PENDING_RETURN,
    val shortageReason: String? = null,
    val lotNo: String? = null,
    val labelIssued: Boolean = false,
    val createdAt: String = "",
    val createdBy: String? = null,
) {
    val lineKey: String get() = "$returnNo/$serialNo"

    val isOpen: Boolean get() = status == STATUS_PENDING_RETURN || status == STATUS_PARTIAL_RETURN
}

@Serializable
data class ReturnResult(
    val no: Long = 0,
    val createdAt: String = "",
    val itemCode: String = "",
    val itemName: String? = null,
    val qty: Double = 0.0,
    val unit: String? = null,
    val returnNo: String? = null,
    val serialNo: String? = null,
    val returnedByCode: String? = null,
    val returnedByName: String? = null,
    val locationCode: String? = null,
    val lotNo: String? = null,
    val worker: String? = null,
    val status: String = STATUS_RETURNED,
    val shortageReason: String? = null,
)

@Serializable
data class ReturnResultRequest(
    val itemCode: String,
    val qty: Double,
    val returnNo: String? = null,
    val serialNo: String? = null,
    val locationCode: String? = null,
    val lotNo: String? = null,
    val requestId: String? = null,
)

@Serializable
data class Location(
    val locationCode: String = "",
    val locationName: String? = null,
)

@Serializable
data class Zone(
    val zoneCode: String = "",
    val zoneName: String = "",
)

@Serializable
data class Device(
    val deviceCode: String = "",
    val deviceName: String = "",
    val ipAddress: String? = null,
)

@Serializable
data class ItemBrief(
    val itemCode: String = "",
    val itemName: String? = null,
    val barcode: String? = null,
    val packageQty: Double = 1.0,
    val uomCode: String? = null,
    val expirationPeriod: Double? = null,
    val isLotManaged: Boolean = false,
)

@Serializable
data class HtLot(val lotNo: String = "", val itemCode: String = "")

@Serializable
data class PrintPreviewRequest(
    val layoutKey: String,
    val deviceCode: String,
    val fields: Map<String, String>,
)

@Serializable
data class PrintPreviewResponse(val image: String = "")

@Serializable
data class PrintHistoryRequest(
    val category: String,
    val docNo: String? = null,
    val itemCode: String? = null,
    val qty: Double,
    val uomCode: String? = null,
    val packageCount: Int = 1,
    val spec: String? = null,
)

@Serializable
data class PrintLabelRequest(
    val layoutKey: String,
    val deviceCode: String,
    val fields: Map<String, String>,
    val history: PrintHistoryRequest,
    val copies: Int = 1,
)

@Serializable
data class PrintHistoryRow(
    val no: Long = 0,
    val category: String = "",
    val docNo: String? = null,
    val itemCode: String? = null,
    val qty: Double? = null,
)


@Serializable
data class ShippingPlan(
    val slipNo: String = "",
    val serialNo: String = "",
    val customerCode: String? = null,
    val customerName: String? = null,
    val itemCode: String = "",
    val itemName: String? = null,
    val plannedQty: Double = 0.0,
    val unit: String? = null,
    val actualQty: Double = 0.0,
    val packingTypeCode: String? = null,
    val packingQty: Double? = null,
    val scheduledDate: String? = null,
    val gateCode: String? = null,
    val invoiceNo: String? = null,
    val status: String = STATUS_NOT_SHIPPED,
    val shortageReason: String? = null,
    val labelIssued: Boolean = false,
    val createdAt: String = "",
    val createdBy: String? = null,
) {
    val lineKey: String get() = "$slipNo/$serialNo"

    val isOpen: Boolean get() = status == STATUS_NOT_SHIPPED || status == STATUS_PARTIAL_SHIP
}

@Serializable
data class ShippingResult(
    val no: Long = 0,
    val createdAt: String = "",
    val itemCode: String = "",
    val itemName: String? = null,
    val qty: Double = 0.0,
    val unit: String? = null,
    val slipNo: String? = null,
    val serialNo: String? = null,
    val customerCode: String? = null,
    val customerName: String? = null,
    val locationCode: String? = null,
    val lotNo: String? = null,
    val expiryDate: String? = null,
    val scheduledDate: String? = null,
    val invoiceNo: String? = null,
    val worker: String? = null,
    val status: String = STATUS_SHIPPED,
    val shortageReason: String? = null,
)

@Serializable
data class ShippingResultRequest(
    val itemCode: String,
    val qty: Double,
    val slipNo: String? = null,
    val serialNo: String? = null,
    val locationCode: String? = null,
    val lotNo: String? = null,
    val expiryDate: String? = null,
    val reasonCode: String? = null,
    val requestId: String? = null,
)

@Serializable
data class PickingPlan(
    val soNo: String = "",
    val serialNo: String = "",
    val itemCode: String = "",
    val itemName: String? = null,
    val locationCode: String? = null,
    val locationName: String? = null,
    val plannedQty: Double = 0.0,
    val unit: String? = null,
    val pickedQty: Double = 0.0,
    val scheduledDate: String? = null,
    val status: String = STATUS_NOT_PICKED,
    val createdAt: String = "",
    val createdBy: String? = null,
) {
    val lineKey: String get() = "$soNo/$serialNo"

    val isOpen: Boolean get() = status == STATUS_NOT_PICKED

    val isPartial: Boolean get() = isOpen && pickedQty > 0
}

@Serializable
data class PickingResult(
    val no: Long = 0,
    val createdAt: String = "",
    val itemCode: String = "",
    val itemName: String? = null,
    val qty: Double = 0.0,
    val unit: String? = null,
    val soNo: String = "",
    val serialNo: String = "",
    val scheduledDate: String? = null,
    val locationCode: String = "",
    val locationName: String? = null,
    val lotNo: String? = null,
    val worker: String? = null,
)

@Serializable
data class PickingResultRequest(
    val itemCode: String,
    val qty: Double,
    val soNo: String,
    val serialNo: String? = null,
    val locationCode: String? = null,
    val lotNo: String? = null,
    val requestId: String? = null,
)


const val STATUS_NOT_ARRIVED = "statusNotArrived"
const val STATUS_PARTIAL = "statusPartial"
const val STATUS_ARRIVED = "statusArrived"
const val STATUS_CANCELLED = "statusCancelled"

const val STATUS_PENDING_RETURN = "statusPendingReturn"
const val STATUS_PARTIAL_RETURN = "statusPartialReturn"
const val STATUS_RETURNED = "statusReturned"

const val STATUS_NOT_SHIPPED = "statusNotShipped"
const val STATUS_PARTIAL_SHIP = "statusPartialShip"
const val STATUS_SHIPPED = "statusShipped"

const val STATUS_NOT_PICKED = "statusNotPicked"
const val STATUS_PICKED = "statusPicked"

const val ACTION_DONE = "done"
const val ACTION_NOT_DONE = "notDone"
const val ACTION_CANCELLED = "cancelled"
