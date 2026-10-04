package com.imssolutions.ht.ui.vm

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class StocktakingShelfScanTest {

    @Test
    fun `tem vi tri cho ra ma o ke`() {
        assertEquals("X01_Y02_Z05", stocktakingShelfOf("?lc_cd=X01_Y02_Z05&lc_name=Shelf%205"))
    }

    @Test
    fun `tem hang khong phai tem vi tri`() {
        assertNull(stocktakingShelfOf("/10/LOT260809?item_cd=HL-HND208"))
    }

    @Test
    fun `tem hang in truoc 15-09 con mang lc_cd van khong doi ke`() {
        assertNull(stocktakingShelfOf("/10/LOT260809?item_cd=HL-HND208&lc_cd=X01_Y01_Z01"))
    }

    @Test
    fun `ma sai dinh dang thi khong ra ke nao`() {
        assertNull(stocktakingShelfOf("abc"))
    }
}
