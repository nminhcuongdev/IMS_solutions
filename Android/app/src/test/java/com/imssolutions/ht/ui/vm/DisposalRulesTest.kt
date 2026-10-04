package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import com.imssolutions.ht.data.DisposalLine

class DisposalRulesTest {

    private fun line(qty: Double = 1.0, stock: Double = 10.0) = DisposalLine(
        itemCode = "IT-01", serialNo = "0000000001", itemName = "Item",
        lotNo = "L-A", unit = "pcs", stockQty = stock, qty = qty,
    )


    @Test
    fun `go dung bang ton la hop le`() {
        assertFalse(line(qty = 10.0, stock = 10.0).overStock)
    }

    @Test
    fun `go qua ton la loi`() {
        assertTrue(line(qty = 11.0, stock = 10.0).overStock)
    }

    @Test
    fun `state co mot dong sai thi chan ca lo`() {
        val s = DisposalState(lines = listOf(line(qty = 1.0), line(qty = 99.0, stock = 5.0)))
        assertTrue(s.hasBadQty)
    }

    @Test
    fun `so luong bang khong bi coi la sai`() {
        assertTrue(DisposalState(lines = listOf(line(qty = 0.0))).hasBadQty)
    }

    @Test
    fun `lo hop le thi khong bao gi`() {
        val s = DisposalState(lines = listOf(line(qty = 2.0, stock = 10.0), line(qty = 10.0, stock = 10.0)))
        assertFalse(s.hasBadQty)
        assertEquals(12.0, s.totalQty, 0.0001)
    }


    @Test
    fun `khoa dong la CAP ma hang va serial`() {
        val a = line().copy(serialNo = "0000000001")
        val b = line().copy(serialNo = "0000000002")
        assertEquals("IT-01/0000000001", a.key)
        assertTrue(a.key != b.key)
    }

    @Test
    fun `danh sach rong thi khong con gi de huy`() {
        assertTrue(DisposalState().empty)
        assertFalse(DisposalState(lines = listOf(line())).empty)
    }
}
