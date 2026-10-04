package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import com.imssolutions.ht.data.PickingPlan
import com.imssolutions.ht.data.STATUS_NOT_PICKED
import com.imssolutions.ht.data.STATUS_PICKED

class PickingDetailModeTest {

    private fun line(
        planned: Double = 8.0,
        picked: Double = 0.0,
        status: String = STATUS_NOT_PICKED,
        location: String? = "X03_Y01_Z01",
    ) = PickingPlan(
        soNo = "SO-20260811-003",
        serialNo = "01",
        itemCode = "PR-CMP-06",
        itemName = "Compact Desktop Printer",
        locationCode = location,
        plannedQty = planned,
        unit = "pcs",
        pickedQty = picked,
        status = status,
    )


    @Test
    fun `The yardstick is the whole line, never a package`() {
        val s = PickingDetailState(loading = false, line = line(planned = 8.0))
        assertEquals(8.0, s.planned, 0.0)
    }

    @Test
    fun `Picking the full line leaves Diff at zero`() {
        val s = PickingDetailState(loading = false, line = line(planned = 8.0), actualQty = "8")
        assertEquals(0.0, s.diff!!, 0.0)
        assertFalse(s.short)
        assertFalse(s.overPicked)
    }

    @Test
    fun `An empty box has no Diff at all, which is not the same as zero`() {
        val s = PickingDetailState(loading = false, line = line())
        assertNull(s.diff)
        assertFalse(s.short)
    }


    @Test
    fun `A short pick is flagged but asks for no reason`() {
        val s = PickingDetailState(loading = false, line = line(planned = 8.0), actualQty = "3")
        assertEquals(-5.0, s.diff!!, 0.0)
        assertTrue(s.short)
        assertFalse(s.overPicked)
    }

    @Test
    fun `A short pick keeps the line open, so there are only two outcomes to report`() {
        val short = line(planned = 8.0, picked = 3.0, status = STATUS_NOT_PICKED)
        assertTrue(short.isOpen)
        assertTrue(short.isPartial)

        val full = line(planned = 8.0, picked = 8.0, status = STATUS_PICKED)
        assertFalse(full.isOpen)
        assertFalse(full.isPartial)
    }

    @Test
    fun `An untouched line is open but not partial`() {
        val fresh = line(planned = 8.0, picked = 0.0)
        assertTrue(fresh.isOpen)
        assertFalse(fresh.isPartial)
    }


    @Test
    fun `Picking more than planned is flagged, unlike receiving where extra is normal`() {
        val s = PickingDetailState(loading = false, line = line(planned = 8.0), actualQty = "9")
        assertTrue(s.overPicked)
        assertFalse(s.short)
    }


    @Test
    fun `A plan that knows its location keeps the field read-only`() {
        val s = PickingDetailState(loading = false, line = line(location = "X03_Y01_Z01"))
        assertFalse(s.locationEditable)
    }

    @Test
    fun `A plan with no location opens the field, or Confirm would dead-end on LOC-001`() {
        assertTrue(PickingDetailState(loading = false, line = line(location = null)).locationEditable)
        assertTrue(PickingDetailState(loading = false, line = line(location = " ")).locationEditable)
    }


    @Test
    fun `The Shortage filter reads picked quantity, because no status carries it`() {
        val rows = listOf(
            line(planned = 5.0, picked = 0.0),
            line(planned = 8.0, picked = 3.0),
            line(planned = 10.0, picked = 10.0, status = STATUS_PICKED),
        )
        fun visible(f: PickFilter) =
            PickingListState(loading = false, lines = rows, filter = f).visible

        assertEquals(3, visible(PickFilter.ALL).size)
        assertEquals(1, visible(PickFilter.COMPLETED).size)
        assertEquals(1, visible(PickFilter.SHORTAGE).size)
        assertEquals(1, visible(PickFilter.NOT_COMPLETED).size)
        assertEquals(2, visible(PickFilter.ACTIVE).size)
    }

    @Test
    fun `The search box matches the SO number`() {
        val rows = listOf(line(), line().copy(soNo = "SO-20260809-001", serialNo = "02"))
        val s = PickingListState(loading = false, lines = rows, filter = PickFilter.ALL, query = "0809")
        assertEquals(1, s.visible.size)
        assertEquals("SO-20260809-001", s.visible[0].soNo)
    }
}
