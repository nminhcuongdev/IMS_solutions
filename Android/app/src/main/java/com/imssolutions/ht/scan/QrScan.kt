package com.imssolutions.ht.scan

sealed interface QrScan {
    data class Ok(val payload: QrPayload) : QrScan
    data class Invalid(val error: ScanError, val raw: String) : QrScan
}
