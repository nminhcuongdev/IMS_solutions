package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import com.imssolutions.ht.data.HtConfig
import com.imssolutions.ht.data.STATUS_NOT_SHIPPED
import com.imssolutions.ht.data.STATUS_PARTIAL_SHIP
import com.imssolutions.ht.data.STATUS_SHIPPED
import com.imssolutions.ht.data.ShippingPlan

class ShippingDetailModeTest {

    private fun state(
        fromScan: Boolean = false,
        labelIssued: Boolean = false,
        labelMode: String = "printNow",
        planned: Double = 10.0,
        actual: String = "",
        reason: String? = null,
    ) = ShippingDetailState(
        loading = false,
        line = ShippingPlan(
            slipNo = "SO-1", serialNo = "01", itemCode = "IT-01",
            plannedQty = planned, labelIssued = labelIssued,
            packingQty = 5.0, packingTypeCode = "packingCase",
        ),
        config = HtConfig(labelModeShp = labelMode),
        fromScan = fromScan,
        actualQty = actual,
        reasonCode = reason,
    )


    @Test
    fun `the yardstick is the whole line, never the package size`() {
        val s = state(planned = 10.0, actual = "10")
        assertEquals(10.0, s.planned, 0.0)
        assertEquals(0.0, s.diff!!, 0.0)
        assertFalse(s.needsReason)
        assertFalse(s.overShipped)
    }

    @Test
    fun `shipping less than planned asks for a reason`() {
        val s = state(planned = 10.0, actual = "7")
        assertEquals(-3.0, s.diff!!, 0.0)
        assertTrue(s.needsReason)
    }

    @Test
    fun `shipping more than planned is flagged, not accepted`() {
        val s = state(planned = 10.0, actual = "12")
        assertTrue(s.overShipped)
        assertFalse(s.needsReason)
    }

    @Test
    fun `an empty quantity has no diff at all`() {
        val s = state(actual = "")
        assertNull(s.diff)
        assertFalse(s.needsReason)
        assertFalse(s.overShipped)
    }

    @Test
    fun `zero is a real quantity and still needs a reason`() {
        val s = state(planned = 10.0, actual = "0")
        assertEquals(-10.0, s.diff!!, 0.0)
        assertTrue(s.needsReason)
    }


    @Test
    fun `scanned line still lets the worker type the shipped quantity`() {
        assertTrue(state(fromScan = true).canEditActual)
    }

    @Test
    fun `scanned line never offers the label button`() {
        assertFalse(state(fromScan = true).showLabel)
        assertFalse(state(fromScan = true, labelMode = "printNow").showLabel)
    }


    @Test
    fun `hand picked line is editable and can issue a label`() {
        val s = state(fromScan = false)
        assertTrue(s.canEditActual)
        assertTrue(s.showLabel)
    }

    @Test
    fun `a line that already has a label does not offer another`() {
        assertFalse(state(labelIssued = true).showLabel)
    }

    @Test
    fun `preIssue mode hides the label button entirely`() {
        assertFalse(state(labelMode = "preIssue").showLabel)
    }


    @Test
    fun `slip status takes the least complete line`() {
        val shipped = ShippingPlan(slipNo = "SO-1", serialNo = "01", status = STATUS_SHIPPED)
        val partial = ShippingPlan(slipNo = "SO-1", serialNo = "02", status = STATUS_PARTIAL_SHIP)
        val notYet = ShippingPlan(slipNo = "SO-1", serialNo = "03", status = STATUS_NOT_SHIPPED)

        assertEquals(STATUS_PARTIAL_SHIP, worstShippingStatus(listOf(shipped, partial, notYet)))
        assertEquals(STATUS_NOT_SHIPPED, worstShippingStatus(listOf(shipped, notYet)))
        assertEquals(STATUS_SHIPPED, worstShippingStatus(listOf(shipped)))
    }

    @Test
    fun `an open line is anything not yet fully shipped`() {
        assertTrue(ShippingPlan(status = STATUS_NOT_SHIPPED).isOpen)
        assertTrue(ShippingPlan(status = STATUS_PARTIAL_SHIP).isOpen)
        assertFalse(ShippingPlan(status = STATUS_SHIPPED).isOpen)
    }
}
