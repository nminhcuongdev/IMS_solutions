package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import com.imssolutions.ht.data.HtConfig
import com.imssolutions.ht.data.ReturnPlan

class ReturnDetailModeTest {

    private fun state(
        fromScan: Boolean = false,
        labelIssued: Boolean = false,
        labelMode: String = "onReturn",
        expected: Double = 10.0,
        actual: String = "",
        planLot: String? = null,
        lotInput: String = "",
    ) = ReturnDetailState(
        loading = false,
        line = ReturnPlan(
            returnNo = "RTN-1", serialNo = "01", itemCode = "IT-01",
            expectedQty = expected, labelIssued = labelIssued, lotNo = planLot,
        ),
        config = HtConfig(labelModeRtn = labelMode),
        fromScan = fromScan,
        actualQty = actual,
        lotInput = lotInput,
    )


    @Test
    fun `scanned line locks the quantity`() {
        assertFalse(state(fromScan = true).canEditActual)
    }

    @Test
    fun `scanned line never offers the label button`() {
        assertFalse(state(fromScan = true).showLabel)
        assertFalse(state(fromScan = true, labelMode = "onReturn").showLabel)
    }


    @Test
    fun `hand picked line is editable`() {
        assertTrue(state(fromScan = false).canEditActual)
    }

    @Test
    fun `hand picked line offers the label button`() {
        assertTrue(state(fromScan = false).showLabel)
    }

    @Test
    fun `a line whose label is already issued does not offer it again`() {
        assertFalse(state(fromScan = false, labelIssued = true).showLabel)
    }

    @Test
    fun `preIssue mode hides the label button entirely`() {
        assertFalse(state(fromScan = false, labelMode = "preIssue").showLabel)
    }


    @Test
    fun `a line that already knows its lot keeps the field read only`() {
        val s = state(planLot = "LOT260816")
        assertFalse(s.lotEditable)
        assertEquals("LOT260816", s.lotNo)
    }

    @Test
    fun `a line with no lot opens the field so Confirm is reachable`() {
        assertTrue(state(planLot = null).lotEditable)
        assertTrue(state(planLot = "   ").lotEditable)
    }

    @Test
    fun `the typed lot is what gets sent when the plan has none`() {
        assertEquals("LOT-NEW", state(planLot = null, lotInput = " LOT-NEW ").lotNo)
    }

    @Test
    fun `the plan lot wins over anything read from the label`() {
        assertEquals("LOT260816", state(planLot = "LOT260816", lotInput = "LOT-FROM-QR").lotNo)
    }


    @Test
    fun `diff is null until a quantity is typed`() {
        assertEquals(null, state().diff)
    }

    @Test
    fun `diff counts down from the expected quantity`() {
        assertEquals(-3.0, state(expected = 10.0, actual = "7").diff!!, 1e-9)
    }

    @Test
    fun `a scanned quantity equal to expected leaves no difference`() {
        assertEquals(0.0, state(fromScan = true, expected = 10.0, actual = "10").diff!!, 1e-9)
    }
}
