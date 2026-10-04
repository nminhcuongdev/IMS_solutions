package com.imssolutions.ht.scan

import org.junit.Assert.assertEquals
import org.junit.Test

class ScanMatchTest {

    private fun qr(raw: String) = (QrPayload.parse(raw) as QrScan.Ok).payload

    private fun matchOf(raw: String, vararg lines: ScanLine): ScanMatch =
        matchScannedItem(qr(raw), lines.toList())

    private fun indexOf(m: ScanMatch) = (m as ScanMatch.Line).index
    private fun errorOf(m: ScanMatch) = (m as ScanMatch.Failed).error

    @Test
    fun `matches the line carrying the scanned item code`() {
        val m = matchOf(
            "?item_cd=RM002",
            ScanLine("RM001", null), ScanLine("RM002", null),
        )
        assertEquals(1, indexOf(m))
    }

    @Test
    fun `item code comparison ignores case`() {
        assertEquals(0, indexOf(matchOf("?item_cd=rm001", ScanLine("RM001", null))))
    }

    @Test
    fun `unknown item code reports SCN-005`() {
        assertEquals(ScanError.SCN_005, errorOf(matchOf("?item_cd=RM999", ScanLine("RM001", null))))
    }

    @Test
    fun `location label reports SCN-005 not a format error`() {
        assertEquals(
            ScanError.SCN_005,
            errorOf(matchOf("?lc_cd=Shelf2_middle_left&lc_name=WarehouseA", ScanLine("RM001", null))),
        )
    }

    @Test
    fun `user label reports SCN-005`() {
        assertEquals(ScanError.SCN_005, errorOf(matchOf("?user_cd=5936", ScanLine("RM001", null))))
    }


    @Test
    fun `lot in the QR picks the matching line among duplicates of the same item`() {
        val m = matchOf(
            "/10/LOT-B?item_cd=RM001",
            ScanLine("RM001", "LOT-A"), ScanLine("RM001", "LOT-B"),
        )
        assertEquals(1, indexOf(m))
    }

    @Test
    fun `lot that matches no assigned line reports SCN-006`() {
        val m = matchOf(
            "/10/LOT-Z?item_cd=RM001",
            ScanLine("RM001", "LOT-A"), ScanLine("RM001", "LOT-B"),
        )
        assertEquals(ScanError.SCN_006, errorOf(m))
    }

    @Test
    fun `line without an assigned lot still opens`() {
        val m = matchOf("/10/LOT-Z?item_cd=RM001", ScanLine("RM001", null))
        assertEquals(0, indexOf(m))
    }

    @Test
    fun `assigned line wins over an unassigned one when the lot matches`() {
        val m = matchOf(
            "/10/LOT-A?item_cd=RM001",
            ScanLine("RM001", ""), ScanLine("RM001", "LOT-A"),
        )
        assertEquals(1, indexOf(m))
    }

    @Test
    fun `QR without a lot opens the first line of that item`() {
        val m = matchOf("?item_cd=RM001", ScanLine("RM001", "LOT-A"), ScanLine("RM001", "LOT-B"))
        assertEquals(0, indexOf(m))
    }
}
