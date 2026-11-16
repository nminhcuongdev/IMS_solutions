package com.imssolutions.rfid.data

import kotlinx.serialization.Serializable

@Serializable
data class ApiSuccess<T>(
    val success: Boolean = true,
    val data: T? = null,
)

@Serializable
data class ApiErrorBody(
    val code: String = "",
    val httpStatus: Int = 0,
    val severity: String = "",
    val message: String = "",
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
    val userCode: String = "",
    val userName: String = "",
    val defaultLanguage: String? = null,
)

@Serializable
data class StockScanRowDto(
    val epc: String = "",
    val itemCode: String? = null,
    val itemName: String? = null,
    val itemCategory: String? = null,
    val isScanned: Boolean = false,
    val workerCode: String? = null,
    val workerName: String? = null,
)

@Serializable
data class StockScanBoardDto(
    val scanned: Int = 0,
    val total: Int = 0,
    val rows: List<StockScanRowDto> = emptyList(),
)

@Serializable
data class StockScanReadRequest(
    val epcs: List<String>,
)

@Serializable
data class StockScanReadResultDto(
    val accepted: Int = 0,
    val alreadyScanned: Int = 0,
    val ignored: Int = 0,
    val scanned: Int = 0,
    val total: Int = 0,
)

@Serializable
data class EpcLookupRequest(
    val epcs: List<String>,
)

@Serializable
data class RegisteredEpcDto(
    val epc: String = "",
    val itemCode: String? = null,
    val itemName: String? = null,
)

@Serializable
data class EpcPrefixDto(
    val prefix: String = "",
    val itemCode: String? = null,
    val itemName: String? = null,
)

@Serializable
data class EpcLookupResultDto(
    val registered: List<RegisteredEpcDto> = emptyList(),
    val prefixes: List<EpcPrefixDto> = emptyList(),
)

@Serializable
data class EpcRegisterGroup(
    val itemCode: String,
    val itemName: String,
    val epcs: List<String>,
)

@Serializable
data class EpcRegisterRequest(
    val groups: List<EpcRegisterGroup>,
)

@Serializable
data class EpcRegisterResultDto(
    val registered: Int = 0,
    val parts: Int = 0,
)
