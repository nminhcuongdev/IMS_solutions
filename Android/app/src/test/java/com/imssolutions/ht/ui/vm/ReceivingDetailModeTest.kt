package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import com.imssolutions.ht.data.HtConfig
import com.imssolutions.ht.data.ReceivingPlan

class ReceivingDetailModeTest {

    private fun state(
        fromScan: Boolean = false,
        labelIssued: Boolean = false,
        labelMode: String = "onReceive",
        planned: Double = 120.0,
        packingQty: Double? = 12.0,
        actual: String = "",
    ) = ReceivingDetailState(
        loading = false,
        line = ReceivingPlan(
            poNo = "PO-1", serialNo = "01", itemCode = "IT-01",
            plannedQty = planned, packingQty = packingQty, labelIssued = labelIssued,
        ),
        config = HtConfig(labelModeRcv = labelMode),
        fromScan = fromScan,
        actualQty = actual,
    )


    @Test
    fun `scanned line locks the quantity and offers only Confirm`() {
        val s = state(fromScan = true)
        assertFalse(s.canEditActual)
        assertTrue(s.showConfirm)
        assertFalse(s.showLabel)
    }


    @Test
    fun `hand picked line is editable and offers only Label`() {
        val s = state(fromScan = false)
        assertTrue(s.canEditActual)
        assertFalse(s.showConfirm)
        assertTrue(s.showLabel)
    }

    @Test
    fun `hand picked line falls back to Confirm once the label is issued`() {
        val s = state(fromScan = false, labelIssued = true)
        assertTrue(s.showConfirm)
        assertFalse(s.showLabel)
    }

    @Test
    fun `hand picked line shows Confirm when labels are pre-issued`() {
        val s = state(fromScan = false, labelMode = "preIssue")
        assertTrue(s.canEditActual)
        assertTrue(s.showConfirm)
        assertFalse(s.showLabel)
    }

    @Test
    fun `scanned line never shows the label button even before pre-issue`() {
        assertFalse(state(fromScan = true, labelMode = "preIssue").showLabel)
        assertFalse(state(fromScan = true, labelMode = "onReceive").showLabel)
    }


    @Test
    fun `expected splits the planned quantity across the packages`() {
        assertEquals(10.0, state().expected, 0.0001)
    }

    @Test
    fun `expected falls back to the whole line when packing qty is missing or zero`() {
        assertEquals(120.0, state(packingQty = null).expected, 0.0001)
        assertEquals(120.0, state(packingQty = 0.0).expected, 0.0001)
    }

    @Test
    fun `a full package is not a shortage even though the line is far from complete`() {
        val s = state(actual = "10")
        assertEquals(0.0, s.diff!!, 0.0001)
        assertFalse(s.needsReason)
    }


    @Test
    fun `a package with goods and no scanned location is blocked`() {
        assertTrue(state(actual = "10").missingLocation)
        assertFalse(state(actual = "10").copy(locationCode = "X01_Y01_Z01").missingLocation)
    }

    @Test
    fun `declaring zero needs no location`() {
        assertFalse(state(actual = "0").missingLocation)
    }

    @Test
    fun `location label fills Location, product label fills Lot and its lc_cd is ignored`() {
        assertEquals(
            ReceivingScanTarget.Location("X01_Y01_Z01"),
            receivingScanTarget("?lc_cd=X01_Y01_Z01&lc_name=Cell%20Z%3D1"),
        )
        assertEquals(
            ReceivingScanTarget.Lot("LOT260809B"),
            receivingScanTarget("/10/LOT260809B?30=10&item_cd=HL-HND210&lc_cd=X01_Y01_Z02"),
        )
        assertEquals(ReceivingScanTarget.Lot("LOT-RAW"), receivingScanTarget("LOT-RAW"))
    }

    @Test
    fun `a package short of expected does need a reason`() {
        val s = state(actual = "7")
        assertEquals(-3.0, s.diff!!, 0.0001)
        assertTrue(s.needsReason)
    }
}
