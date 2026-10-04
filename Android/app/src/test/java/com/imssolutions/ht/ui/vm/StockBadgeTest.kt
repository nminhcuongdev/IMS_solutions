package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import com.imssolutions.ht.data.ExpiryState
import com.imssolutions.ht.data.StockGroup
import com.imssolutions.ht.data.expiryStateOf

class StockBadgeTest {

    private fun g(
        qty: Double = 30.0,
        reorder: Double? = 20.0,
        hold: Boolean = false,
        remainingDays: Int? = null,
    ) = StockGroup(
        key = "IT-01", name = "Item", stockQty = qty,
        reorderPoint = reorder, anyOnHold = hold, remainingDays = remainingDays,
    )


    @Test
    fun `bien 30 ngay van la sap het han`() {
        assertEquals(ExpiryState.SOON, expiryStateOf(30))
        assertEquals(ExpiryState.NONE, expiryStateOf(31))
    }

    @Test
    fun `so ngay am la da qua han`() {
        assertEquals(ExpiryState.EXPIRED, expiryStateOf(-1))
        assertEquals(ExpiryState.SOON, expiryStateOf(0))
    }

    @Test
    fun `khong co han thi khong canh bao`() {
        assertEquals(ExpiryState.NONE, expiryStateOf(null))
    }


    @Test
    fun `ton duoi nguong dat hang lai thi vien do`() {
        assertTrue(g(qty = 15.0, reorder = 20.0).isLow)
        assertTrue(g(qty = 15.0, reorder = 20.0).isAlert)
    }

    @Test
    fun `dung bang nguong thi CHUA phai ton thap`() {
        assertFalse(g(qty = 20.0, reorder = 20.0).isLow)
    }

    @Test
    fun `master chua khai nguong thi khong bao gio la ton thap`() {
        assertFalse(g(qty = 0.0, reorder = null).isLow)
    }

    @Test
    fun `bi giu thi vien do du ton con nhieu`() {
        val row = g(qty = 200.0, reorder = 20.0, hold = true)
        assertFalse(row.isLow)
        assertTrue(row.isAlert)
    }

    @Test
    fun `sap het han KHONG bat vien do`() {
        val row = g(qty = 500.0, reorder = 20.0, remainingDays = 5)
        assertEquals(ExpiryState.SOON, row.expiry)
        assertFalse(row.isAlert)
    }
}
