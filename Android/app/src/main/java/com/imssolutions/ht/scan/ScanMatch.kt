package com.imssolutions.ht.scan

enum class ScanError(val code: String, val message: String) {
    SCN_001("SCN-001", "The scanned barcode is not valid."),

    SCN_005("SCN-005", "Does not match the specified item."),

    SCN_006("SCN-006", "Does not match the specified lot."),
}

data class ScanLine(val itemCode: String, val lotNo: String?)

sealed interface ScanMatch {
    data class Line(val index: Int) : ScanMatch
    data class Failed(val error: ScanError) : ScanMatch
}

fun matchScannedItem(payload: QrPayload, lines: List<ScanLine>): ScanMatch {
    val code = payload.itemCode?.trim().orEmpty()
    if (code.isEmpty()) return ScanMatch.Failed(ScanError.SCN_005)

    val candidates = lines.withIndex().filter { it.value.itemCode.equals(code, ignoreCase = true) }
    if (candidates.isEmpty()) return ScanMatch.Failed(ScanError.SCN_005)

    val lot = payload.lotNo?.trim()
    if (lot.isNullOrEmpty()) return ScanMatch.Line(candidates.first().index)

    candidates.firstOrNull { it.value.lotNo?.trim().equals(lot, ignoreCase = true) }
        ?.let { return ScanMatch.Line(it.index) }

    val unassigned = candidates.firstOrNull { it.value.lotNo.isNullOrBlank() }
    return if (unassigned != null) ScanMatch.Line(unassigned.index)
    else ScanMatch.Failed(ScanError.SCN_006)
}
