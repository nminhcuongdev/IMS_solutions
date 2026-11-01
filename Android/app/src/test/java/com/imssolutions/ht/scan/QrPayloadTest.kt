package com.imssolutions.ht.scan

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class QrPayloadTest {

    private val product =
        "/01/04912345678905/10/LOT20260801?11=260807&17=270630&item_cd=RM001" +
            "&item_name=美味しいお茶&print_at=260807100910&lc_cd=RM001" +
            "&lc_name=RawMaterialWarehouse&user_cd=USER001"

    private val shipping =
        "/01/04912345678905/10/LOT20260801?13=260807&37=10&3102=002555&item_cd=FG001" +
            "&item_name=美味しいお茶&print_at=260807110000&lc_cd=CASE001" +
            "&lc_name=FinishedGoodsWarehouse"

    private fun ok(raw: String): QrPayload =
        (QrPayload.parse(raw) as QrScan.Ok).payload


    @Test
    fun `path splits into odd AI and even value`() {
        val p = ok(product)
        assertEquals("04912345678905", p.gtin)
        assertEquals("LOT20260801", p.lotNo)
    }

    @Test
    fun `serial number qualifier is read`() {
        assertEquals("SN12345678", ok("/01/04912345678905/21/SN12345678").serialNo)
    }


    @Test
    fun `query attributes are read`() {
        val p = ok(product)
        assertEquals("260807", p.productionDate)
        assertEquals("270630", p.expiryDate)
        assertEquals("RM001", p.itemCode)
        assertEquals("美味しいお茶", p.itemName)
        assertEquals("260807100910", p.printedAt)
        assertEquals("RM001", p.locationCode)
        assertEquals("RawMaterialWarehouse", p.locationName)
        assertEquals("USER001", p.userCode)
    }

    @Test
    fun `percent encoded value is decoded`() {
        assertEquals("Trà xanh", ok("?item_name=Tr%C3%A0%20xanh").itemName)
    }

    @Test
    fun `plus sign in a value is not turned into a space`() {
        assertEquals("RM+001", ok("?item_cd=RM+001").itemCode)
    }

    @Test
    fun `value containing an equals sign keeps it`() {
        assertEquals("a=b", ok("?item_cd=a=b").itemCode)
    }


    @Test
    fun `payload without any delimiter is rejected`() {
        val r = QrPayload.parse("RM001") as QrScan.Invalid
        assertEquals(ScanError.SCN_001, r.error)
    }

    @Test
    fun `blank payload is rejected`() {
        assertTrue(QrPayload.parse("   ") is QrScan.Invalid)
    }

    @Test
    fun `delimiters alone are rejected`() {
        assertTrue(QrPayload.parse("???") is QrScan.Invalid)
    }

    @Test
    fun `dangling path token is ignored`() {
        val p = ok("/01/04912345678905/10")
        assertEquals("04912345678905", p.gtin)
        assertNull(p.lotNo)
    }


    @Test
    fun `product label is recognised`() {
        assertEquals(QrLabelKind.Product, ok(product).kind)
    }

    @Test
    fun `shipping label is recognised by pack count and measurement AI`() {
        val p = ok(shipping)
        assertEquals(QrLabelKind.Shipping, p.kind)
        assertEquals("10", p.itemsPerPack)
        assertEquals("002555", p.ais["3102"])
    }

    @Test
    fun `user label is recognised`() {
        val p = ok("?user_cd=5936")
        assertEquals(QrLabelKind.User, p.kind)
        assertEquals("5936", p.userCode)
    }


    @Test
    fun `payload produced by the web app and printed on a real label parses back`() {
        val p = ok(
            "/01/04912345678905/10/LOT20260801?11=260908&30=10&item_cd=IT-01" +
                "&item_name=Tr%C3%A0%20xanh&print_at=260908110000" +
                "&lc_cd=W1_ZoneA_Shelf01&user_cd=U001",
        )
        assertEquals("IT-01", p.itemCode)
        assertEquals("LOT20260801", p.lotNo)
        assertEquals("10", p.variableCount)
        assertEquals("Trà xanh", p.itemName)
        assertEquals("W1_ZoneA_Shelf01", p.locationCode)
        assertEquals(QrLabelKind.Product, p.kind)
    }

    @Test
    fun `receiving label without GTIN but with lc_cd is still a product label`() {
        val p = ok("/10/LOT260809B?11=260809&30=10&item_cd=HL-HND210&lc_cd=X01_Y01_Z02&user_cd=admin")
        assertEquals(QrLabelKind.Product, p.kind)
    }

    @Test
    fun `location label is recognised`() {
        val p = ok("?lc_cd=Shelf2_middle_left&lc_name=WarehouseA")
        assertEquals(QrLabelKind.Location, p.kind)
        assertEquals("Shelf2_middle_left", p.locationCode)
    }

    @Test
    fun `process label is recognised`() {
        val p = ok("?pr_cd=INPUT_PAPER&pr_name=Paper%20input")
        assertEquals(QrLabelKind.Process, p.kind)
        assertEquals("INPUT_PAPER", p.processCode)
        assertEquals("Paper input", p.processName)
    }

    @Test
    fun `process and location in one label is invalid`() {
        assertEquals(QrLabelKind.Unknown, ok("?pr_cd=INPUT_PAPER&lc_cd=L1").kind)
    }
}
