package com.imssolutions.rfid.logic

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class StockPartsTest {

    private val board = Board(listOf(
        ScanTag("A01", "7100001", "Generic item", isScanned = true),
        ScanTag("A02", "7100001", "Generic item", isScanned = false),
        ScanTag("B01", "M47903000", "Printer head", isScanned = false),
        ScanTag("C01", null, null, isScanned = false),
    ))

    @Test
    fun `choices list one row per part with its counts, a part without code keyed as empty`() {
        val c = partChoices(board)

        assertEquals(listOf("", "7100001", "M47903000"), c.map { it.key })
        assertEquals(PartChoice("7100001", "7100001", "Generic item", scanned = 1, plan = 2), c[1])
    }

    @Test
    fun `a board limited to chosen parts counts and accepts only their tags`() {
        val only = board.onlyParts(setOf("M47903000"))

        assertEquals(listOf("Printer head"), only.stockLines(emptySet()).map { it.itemName })
        assertEquals(null, only.acceptStockRead(emptySet(), "A02"))
        assertEquals("B01", only.acceptStockRead(emptySet(), "b01"))
    }

    @Test
    fun `changing the choice drops pending reads of parts no longer chosen`() {
        assertEquals(setOf("B01"), keepChosen(board, setOf("A02", "B01"), setOf("M47903000")))
    }

    @Test
    fun `the first choice is every part and a later one keeps what still exists`() {
        val c = partChoices(board)

        assertEquals(setOf("", "7100001", "M47903000"), initialChoice(c, null))
        assertEquals(setOf("7100001"), initialChoice(c, setOf("7100001", "GONE")))
        assertTrue(initialChoice(c, setOf("GONE")).size == 3)
    }
}
