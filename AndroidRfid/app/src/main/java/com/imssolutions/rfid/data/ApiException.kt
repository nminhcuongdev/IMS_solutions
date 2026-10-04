package com.imssolutions.rfid.data

class ApiException(
    val code: String,
    val httpStatus: Int,
    override val message: String,
) : Exception(message) {
    val isUnauthorized: Boolean get() = httpStatus == 401
}
