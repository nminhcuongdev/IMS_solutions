package com.imssolutions.ht.data

import org.junit.Assert.assertEquals
import org.junit.Test

class ReceivingExpectedQtyTest {

    private fun line(planned: Double, packing: Double?, actual: Double = 0.0) =
        ReceivingPlan(poNo = "PO-1", plannedQty = planned, packingQty = packing, actualQty = actual)

    @Test
    fun `one case holds packing qty units`() {
        assertEquals(10.0, line(200.0, 10.0).expectedQty, 0.0)
    }

    @Test
    fun `the last case only holds what is left of the line`() {
        assertEquals(5.0, line(30.0, 10.0, actual = 25.0).expectedQty, 0.0)
    }

    @Test
    fun `a loose item is received against the rest of the line`() {
        assertEquals(50.0, line(50.0, 1.0).expectedQty, 0.0)
        assertEquals(20.0, line(50.0, 1.0, actual = 30.0).expectedQty, 0.0)
    }

    @Test
    fun `no packing spec falls back to the rest of the line`() {
        assertEquals(100.0, line(100.0, null).expectedQty, 0.0)
        assertEquals(100.0, line(100.0, 0.0).expectedQty, 0.0)
    }

    @Test
    fun `the case count is the line divided by the units in one case`() {
        assertEquals(20.0, line(200.0, 10.0).caseCount!!, 0.0)
        assertEquals(null, line(50.0, 1.0).caseCount)
    }
}
