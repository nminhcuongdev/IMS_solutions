package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import com.imssolutions.ht.data.ST_COMPLETED
import com.imssolutions.ht.data.ST_IN_PROGRESS
import com.imssolutions.ht.data.StocktakingGroup
import com.imssolutions.ht.data.StocktakingSession
import com.imssolutions.ht.data.matching

class StocktakingRulesTest {

    private fun group(
        item: String = "IT-01",
        lot: String? = "L-A",
        planned: Int = 3,
        scanned: Int = 0,
    ) = StocktakingGroup(
        itemCode = item, itemName = "Item", locationText = "X01_Y01_Z01", lotNo = lot,
        plannedLabels = planned, scannedLabels = scanned,
        plannedQty = planned * 10.0, resultQty = scanned * 10.0, unit = "pcs",
    )

    private fun session(status: Int) = StocktakingSession(
        no = 1, sessionName = "Dot 2026-H2", status = status,
        acceptsResults = status == ST_IN_PROGRESS, labelCount = 3,
    )


    @Test
    fun `chua co dot nao thi khong dem duoc`() {
        assertFalse(StocktakingState().open)
        assertFalse(StocktakingState(locationCode = "X01_Y01_Z01").canProceed)
    }

    @Test
    fun `dot da khoa thi cung khong dem duoc`() {
        val s = StocktakingState(session = session(ST_COMPLETED), locationCode = "X01_Y01_Z01")
        assertFalse(s.open)
        assertFalse(s.canProceed)
    }

    @Test
    fun `dot dang mo va da chon o ke thi di tiep duoc`() {
        val s = StocktakingState(session = session(ST_IN_PROGRESS), locationCode = "X01_Y01_Z01")
        assertTrue(s.open)
        assertTrue(s.canProceed)
    }

    @Test
    fun `dot dang mo nhung chua chon o ke thi chua di duoc`() {
        val s = StocktakingState(session = session(ST_IN_PROGRESS))
        assertFalse(s.canProceed)
        assertFalse(s.copy(locationCode = "   ").canProceed)
    }


    @Test
    fun `khoa dong la CAP ma hang va lo`() {
        assertEquals("IT-01|L-A", group(lot = "L-A").key)
        assertTrue(group(lot = "L-A").key != group(lot = "L-B").key)
    }

    @Test
    fun `hang khong quan lo van co khoa on dinh`() {
        assertEquals("IT-01|", group(lot = null).key)
    }


    @Test
    fun `dem duoc it nhat mot kien thi the doi mau`() {
        assertFalse(group(scanned = 0).counted)
        assertTrue(group(scanned = 1).counted)
    }

    @Test
    fun `ghi 0 thi the van trong nhu chua dem`() {
        assertFalse(group(scanned = 0).counted)
    }


    @Test
    fun `tien do doc theo con so dang go, khong phai con so da luu`() {
        val s = StocktakingState(
            groups = listOf(group(planned = 3, scanned = 1)),
            selectedKey = "IT-01|L-A",
            counted = "2",
        )
        assertEquals(2 to 3, s.progress)
    }

    @Test
    fun `o Actual trong doc la 0 chu khong lam sap man hinh`() {
        val s = StocktakingState(
            groups = listOf(group(planned = 3)), selectedKey = "IT-01|L-A", counted = "",
        )
        assertEquals(0 to 3, s.progress)
    }

    @Test
    fun `chua chon dong nao thi tien do la 0 tren 0`() {
        assertEquals(0 to 0, StocktakingState().progress)
        assertNull(StocktakingState().selected)
    }

    @Test
    fun `tien do hien dung con so dang go, ke ca khi vuot so ke hoach`() {
        val s = StocktakingState(
            groups = listOf(group(planned = 2)), selectedKey = "IT-01|L-A", counted = "5",
        )
        assertEquals(5 to 2, s.progress)
    }


    @Test
    fun `khop dung cap ma hang va lo`() {
        val gs = listOf(group(item = "IT-01", lot = "L-A"), group(item = "IT-01", lot = "L-B"))
        assertEquals("L-B", gs.matching("IT-01", "L-B")?.lotNo)
    }

    @Test
    fun `hoa thuong khong lam lech dong`() {
        val gs = listOf(group(item = "IT-01", lot = "L-A"))
        assertEquals("IT-01|L-A", gs.matching("it-01", "l-a")?.key)
    }

    @Test
    fun `dong khong co lo nhan duoc nhan co lo`() {
        val gs = listOf(group(item = "IT-01", lot = null))
        assertEquals("IT-01|", gs.matching("IT-01", "L-A")?.key)
    }

    @Test
    fun `lo la cua mat hang da biet van la dong moi`() {
        val gs = listOf(group(item = "IT-01", lot = "L-A"))
        assertNull(gs.matching("IT-01", "L-9"))
    }

    @Test
    fun `mat hang khong co trong danh sach thi khong khop gi`() {
        val gs = listOf(group(item = "IT-01", lot = "L-A"))
        assertNull(gs.matching("IT-99", "L-A"))
    }

    @Test
    fun `ke trong thi khong nhan nao khop - moi lan quet deu bi tu choi`() {
        assertNull(emptyList<StocktakingGroup>().matching("IT-01", "L-A"))
    }

    @Test
    fun `khop dung uu tien hon dong khong lo`() {
        val gs = listOf(group(item = "IT-01", lot = null), group(item = "IT-01", lot = "L-A"))
        assertEquals("IT-01|L-A", gs.matching("IT-01", "L-A")?.key)
    }
}
