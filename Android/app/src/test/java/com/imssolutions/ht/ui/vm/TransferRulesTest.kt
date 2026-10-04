package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import com.imssolutions.ht.data.Location
import com.imssolutions.ht.data.TransferLine

class TransferRulesTest {

    private fun line(loc: String? = "X01_Y01_Z01", qty: Double = 5.0) = TransferLine(
        itemCode = "IT-01", serialNo = "0000000001", itemName = "Item",
        lotNo = "L-A", unit = "pcs", qty = qty, locationCode = loc,
    )


    @Test
    fun `chua chon o ke thi khong duoc bam Next`() {
        assertFalse(TransferState().canProceed)
        assertFalse(TransferState(toLocationCode = "   ").canProceed)
        assertTrue(TransferState(toLocationCode = "X01_Y01_Z01").canProceed)
    }


    @Test
    fun `khoa dong la CAP ma hang va serial`() {
        val a = line().copy(serialNo = "0000000001")
        val b = line().copy(serialNo = "0000000002")
        assertEquals("IT-01/0000000001", a.key)
        assertTrue(a.key != b.key)
    }


    @Test
    fun `tong so luong cong het cac dong`() {
        val s = TransferState(lines = listOf(line(qty = 5.0), line(qty = 2.5).copy(serialNo = "2")))
        assertEquals(7.5, s.totalQty, 0.0001)
        assertFalse(s.empty)
    }

    @Test
    fun `danh sach rong thi khong co gi de chuyen`() {
        assertTrue(TransferState().empty)
    }


    @Test
    fun `dong dang o chinh o dich thi khong con gi de lam`() {
        val state = TransferState(toLocationCode = "X01_Y01_Z01")
        val scanned = line(loc = "X01_Y01_Z01")
        assertTrue(scanned.locationCode.equals(state.toLocationCode, ignoreCase = true))
    }

    @Test
    fun `dong o o khac thi chuyen duoc`() {
        val state = TransferState(toLocationCode = "X02_Y01_Z01")
        assertFalse(line(loc = "X01_Y01_Z01").locationCode.equals(state.toLocationCode, ignoreCase = true))
    }

    @Test
    fun `dong chua biet dang o dau van cho chuyen`() {
        val state = TransferState(toLocationCode = "X02_Y01_Z01")
        assertFalse(line(loc = null).locationCode.equals(state.toLocationCode, ignoreCase = true))
    }


    @Test
    fun `doi zone phai bo o ke dang chon`() {
        val before = TransferState(
            zoneCode = "ZONE-A",
            toLocationCode = "X01_Y01_Z01",
            locations = listOf(Location("X01_Y01_Z01", "Shelf")),
        )
        val after = before.copy(zoneCode = "ZONE-B", toLocationCode = null, locations = emptyList())
        assertFalse(after.canProceed)
        assertTrue(after.locations.isEmpty())
    }
}
