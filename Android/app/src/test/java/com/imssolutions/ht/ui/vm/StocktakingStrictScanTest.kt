package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import com.imssolutions.ht.data.StocktakingGroup

class StocktakingStrictScanTest {

    private fun group(planned: Int = 2, scanned: Int = 0) = StocktakingGroup(
        itemCode = "IT-01", lotNo = "L-A", plannedLabels = planned, scannedLabels = scanned,
    )

    @Test
    fun `nhan dung dong va con cho trong thi nhan`() {
        assertNull(stocktakingScanRefusal(group(planned = 2, scanned = 0), 1, "IT-01"))
    }

    @Test
    fun `quet du so so sach van nhan - ranh gioi la nhieu hon, khong phai bang`() {
        assertNull(stocktakingScanRefusal(group(planned = 2), 2, "IT-01"))
    }

    @Test
    fun `khong khop dong nao thi LOC-003`() {
        val why = stocktakingScanRefusal(null, 1, "IT-99")
        assertEquals("LOC-003", why?.first)
        assertEquals(true, why?.second?.contains("IT-99"))
    }

    @Test
    fun `quet thua thi QTY-002`() {
        assertEquals("QTY-002", stocktakingScanRefusal(group(planned = 2), 3, "IT-01")?.first)
    }

    @Test
    fun `dong da dem du thi lan quet sau bi chan`() {
        val g = group(planned = 1, scanned = 1)
        assertEquals("QTY-002", stocktakingScanRefusal(g, g.scannedLabels + 1, "IT-01")?.first)
    }

    @Test
    fun `nhom khong co trong anh chup uu tien LOC-003`() {
        assertEquals("LOC-003", stocktakingScanRefusal(null, 1, "IT-01")?.first)
    }
}
