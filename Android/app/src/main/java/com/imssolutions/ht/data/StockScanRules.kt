package com.imssolutions.ht.data

object StockScanRules {
    fun candidates(lines: List<StockLine>, lotNo: String?, serialNo: String?): List<StockLine> {
        val serial = serialNo?.trim()?.takeIf { it.isNotEmpty() }
        if (serial != null) return lines.filter { it.serialNo.equals(serial, ignoreCase = true) }
        val lot = lotNo?.trim()?.takeIf { it.isNotEmpty() }
        return lines.filter { it.stockQty > 0 && (lot == null || it.lotNo?.trim().equals(lot, ignoreCase = true)) }
    }

    fun toDisposal(line: StockLine): DisposalLine = DisposalLine(
        itemCode = line.itemCode, serialNo = line.serialNo, itemName = line.itemName,
        lotNo = line.lotNo, unit = line.unit, stockQty = line.stockQty, locationCode = line.locationCode,
    )

    fun toTransfer(line: StockLine): TransferLine = TransferLine(
        itemCode = line.itemCode, serialNo = line.serialNo, itemName = line.itemName,
        lotNo = line.lotNo, unit = line.unit, qty = line.stockQty, locationCode = line.locationCode,
    )
}
