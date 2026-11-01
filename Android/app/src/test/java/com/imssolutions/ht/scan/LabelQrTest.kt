package com.imssolutions.ht.scan

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Test
import com.imssolutions.ht.data.ApiException
import java.util.Calendar

class LabelQrTest {

    private val at = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 9, 14, 5, 7) }.time

    private fun ok(raw: String): QrPayload = (QrPayload.parse(raw) as QrScan.Ok).payload

    @Test
    fun `encoding matches encodeURIComponent byte for byte`() {
        assertEquals("A%20B%2BC%26D%3DE%25F%2FG%3FH", LabelQr.encode("A B+C&D=E%F/G?H"))
        assertEquals("%E7%BE%8E", LabelQr.encode("美"))
        assertEquals("a-b_c.d!e~f*g'h(i)", LabelQr.encode("a-b_c.d!e~f*g'h(i)"))
    }

    @Test
    fun `a plus sign in a lot survives a round trip`() {
        for (lot in listOf("A+B", "A B", "A%B", "A&B=C", "LOT/01?x", "Lô 美 +%")) {
            val qr = LabelQr.product(gtin = null, lotNo = lot, qty = 1.0, itemCode = "IT-1", printedAt = at)
            assertEquals(lot, ok(qr).lotNo)
        }
    }

    @Test
    fun `a raw plus sign from another printer is not read as a space`() {
        assertEquals("A+B", ok("/10/A+B?30=1&item_cd=X").lotNo)
        assertEquals("A B+C", ok("/10/A%20B+C?30=1&item_cd=X").lotNo)
    }

    @Test
    fun `the receiving label carries the same fields as the PC label`() {
        val qr = LabelQr.product(
            gtin = "4912345678905", lotNo = "LOT1", qty = 25.0, itemCode = "HL-HND208",
            itemName = "Handheld Labeler", productionDate = LabelQr.date("2026-10-09"),
            userCode = "admin", printedAt = at,
        )
        assertEquals(
            "/01/4912345678905/10/LOT1?11=261009&30=25&item_cd=HL-HND208" +
                "&item_name=Handheld%20Labeler&print_at=261009140507&user_cd=admin",
            qr,
        )
        assertEquals(QrLabelKind.Product, ok(qr).kind)
    }

    @Test
    fun `the shipping label follows the shipping sample of the QR spec`() {
        val qr = LabelQr.shipping(
            gtin = "4912345678905", lotNo = null, qty = 10.0, itemCode = "FG001",
            itemName = "Tea", printedAt = at,
        )
        assertEquals("/01/4912345678905?13=261009&37=10&item_cd=FG001&item_name=Tea&print_at=261009140507", qr)
        val p = ok(qr)
        assertEquals(QrLabelKind.Shipping, p.kind)
        assertEquals("10", p.itemsPerPack)
    }

    @Test
    fun `optional fields are dropped before identity fields when the QR is too long`() {
        val qr = LabelQr.product(
            gtin = "4912345678905", lotNo = "LOT1", qty = 3.0, itemCode = "IT-1",
            itemName = "x".repeat(300), userCode = "admin", printedAt = at,
        )
        assertTrue(qr.length <= LabelQr.MAX_LENGTH)
        val p = ok(qr)
        assertEquals("LOT1", p.lotNo)
        assertEquals("IT-1", p.itemCode)
        assertEquals("3", p.variableCount)
        assertFalse(qr.contains("item_name"))
    }

    @Test
    fun `a QR that cannot fit even without optional fields is refused, never cut`() {
        try {
            LabelQr.product(gtin = null, lotNo = "L".repeat(300), qty = 1.0, itemCode = "IT-1", printedAt = at)
            fail("expected VAL-005")
        } catch (e: ApiException) {
            assertEquals("VAL-005", e.code)
        }
    }
}
