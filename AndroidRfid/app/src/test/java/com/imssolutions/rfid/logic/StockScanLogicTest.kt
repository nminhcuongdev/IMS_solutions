package com.imssolutions.rfid.logic

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class StockScanLogicTest {

    private val tags = listOf(
        ScanTag("A01", "7100001", "IND4", isScanned = true),
        ScanTag("A02", "7100001", "IND4", isScanned = false),
        ScanTag("B01", "M47903000", "Printer head", isScanned = false),
        ScanTag("B02", "M47903000", "Printer head", isScanned = false),
        ScanTag("B03", "M47903000", "Printer head", isScanned = true),
    )
    private val board = Board(tags)

    @Test
    fun a_planned_unscanned_tag_is_accepted_once() {
        assertEquals("A02", board.acceptStockRead(emptySet(), " a02 "))
        assertNull(board.acceptStockRead(setOf("A02"), "A02"))
    }

    @Test
    fun a_tag_already_scanned_on_the_server_is_not_pending_again() {
        assertNull(board.acceptStockRead(emptySet(), "A01"))
    }

    @Test
    fun a_tag_outside_the_plan_is_ignored() {
        assertNull(board.acceptStockRead(emptySet(), "ZZZ"))
        assertNull(board.acceptStockRead(emptySet(), "   "))
        assertNull(board.acceptStockRead(emptySet(), null))
    }

    @Test
    fun lines_count_server_scans_plus_pending_per_item_in_list_order() {
        val lines = board.stockLines(setOf("B01", "B02"))
        assertEquals(listOf("IND4", "Printer head"), lines.map { it.itemName })
        assertEquals(StockLine("7100001", "IND4", qty = 1, plan = 2), lines[0])
        assertEquals(StockLine("M47903000", "Printer head", qty = 3, plan = 3), lines[1])
        assertFalse(lines[0].isComplete)
        assertTrue(lines[1].isComplete)
        assertEquals(4, board.totalCount(setOf("B01", "B02")))
    }

    @Test
    fun a_line_is_not_scanned_short_or_matched_against_its_plan() {
        assertEquals(LineStatus.NotScanned, StockLine("X", "X", qty = 0, plan = 3).status)
        assertEquals(LineStatus.Short, StockLine("X", "X", qty = 1, plan = 3).status)
        assertEquals(LineStatus.Match, StockLine("X", "X", qty = 3, plan = 3).status)
        assertEquals(LineStatus.NotScanned, StockLine("X", "X", qty = 0, plan = 0).status)
    }

    @Test
    fun an_item_that_reached_its_plan_counts_no_further_reads() {
        val full = board.stockLines(setOf("B01", "B02")).single { it.itemName == "Printer head" }
        assertEquals(LineStatus.Match, full.status)
        assertNull(board.acceptStockRead(setOf("B01", "B02"), "B03"))
        assertEquals(3, board.stockLines(setOf("B01", "B02")).single { it.itemName == "Printer head" }.qty)
    }

    @Test
    fun item_names_are_distinct_in_list_order() {
        assertEquals(listOf("IND4", "Printer head"), board.itemNames())
    }

    @Test
    fun a_search_read_counts_only_unscanned_tags_of_the_selected_item() {
        assertEquals("B01", board.acceptSearchRead("Printer head", "b01"))
        assertNull(board.acceptSearchRead("Printer head", "B03"))
        assertNull(board.acceptSearchRead("Printer head", "A02"))
        assertNull(board.acceptSearchRead(null, "B01"))
    }

    @Test
    fun manual_serial_lookup_tells_apart_unknown_and_already_scanned() {
        assertEquals(ManualSerial.Read("B02"), board.manualSerial("Printer head", "b02"))
        assertEquals(ManualSerial.AlreadyScanned, board.manualSerial("Printer head", "B03"))
        assertEquals(ManualSerial.NotFound, board.manualSerial("Printer head", "A02"))
        assertEquals(ManualSerial.Blank, board.manualSerial("Printer head", " "))
    }

    @Test
    fun search_bars_put_confirmed_first_then_strongest_reads_then_unread() {
        val bars = board.searchBars("Printer head", mapOf("B02" to SearchHit(40, 0L), "B01" to SearchHit(80, 0L)), 0L)
        assertEquals(listOf("B03", "B01", "B02"), bars.map { it.epc })
        assertEquals(listOf(BarKind.Confirmed, BarKind.Read, BarKind.Read), bars.map { it.kind })
        assertEquals(listOf(100, 80, 40), bars.map { it.percent })

        val none = board.searchBars("Printer head", emptyMap(), 0L)
        assertEquals(listOf("B03", "B01", "B02"), none.map { it.epc })
        assertEquals(BarKind.Unread, none[1].kind)
        assertEquals(UNREAD_PERCENT, none[1].percent)
    }

    @Test
    fun rssi_maps_onto_a_bar_length_within_bounds() {
        assertEquals(100, rssiToPercent(-20.0))
        assertEquals(MIN_READ_PERCENT, rssiToPercent(-120.0))
        assertTrue(rssiToPercent(-45.0) > rssiToPercent(-70.0))
        assertEquals(55, rssiToPercent(55.0))
    }

    @Test
    fun a_tag_keeps_its_signal_while_it_is_still_being_read() {
        val hit = SearchHit(70, seenAt = 10_000L)
        assertEquals(70, liveSignal(hit, 10_000L))
        assertEquals(70, liveSignal(hit, 10_000L + SIGNAL_HOLD_MS))
    }

    @Test
    fun a_tag_that_stops_answering_fades_down_to_the_floor_and_is_marked_lost() {
        val hit = SearchHit(90, seenAt = 0L)
        val halfway = liveSignal(hit, SIGNAL_HOLD_MS + SIGNAL_FADE_MS / 2)
        assertTrue(halfway in (MIN_READ_PERCENT + 1) until 90)
        assertEquals(MIN_READ_PERCENT, liveSignal(hit, SIGNAL_HOLD_MS + SIGNAL_FADE_MS))
        assertEquals(MIN_READ_PERCENT, liveSignal(hit, 60_000L))

        val bars = board.searchBars("Printer head", mapOf("B01" to hit), 60_000L)
        assertEquals(BarKind.Lost, bars.single { it.epc == "B01" }.kind)
        assertEquals(BarKind.Read, board.searchBars("Printer head", mapOf("B01" to hit), 0L).single { it.epc == "B01" }.kind)
    }

    @Test
    fun a_serial_typed_by_hand_never_fades() {
        val hit = SearchHit(100, seenAt = 0L, manual = true)
        assertEquals(100, liveSignal(hit, 60_000L))
        assertEquals(BarKind.Read, board.searchBars("Printer head", mapOf("B01" to hit), 60_000L).single { it.epc == "B01" }.kind)
    }

    @Test
    fun a_fresh_read_ranks_above_a_lost_tag_that_was_once_stronger() {
        val bars = board.searchBars(
            "Printer head",
            mapOf("B01" to SearchHit(95, seenAt = 0L), "B02" to SearchHit(30, seenAt = 60_000L)),
            60_000L,
        )
        assertEquals(listOf("B03", "B02", "B01"), bars.map { it.epc })
    }

    @Test
    fun a_barcode_tells_the_operator_why_it_was_not_counted() {
        assertEquals(StockScan.Counted("A02"), board.stockScan(emptySet(), " a02\r\n"))
        assertEquals(StockScan.AlreadyCounted, board.stockScan(setOf("A02"), "A02"))
        assertEquals(StockScan.AlreadyCounted, board.stockScan(emptySet(), "A01"))
        assertEquals(StockScan.Unknown, board.stockScan(emptySet(), "ZZZ"))
        assertEquals(StockScan.Unknown, board.stockScan(emptySet(), "  "))
    }

    @Test
    fun a_barcode_for_a_product_already_at_plan_is_refused() {
        val full = Board(
            listOf(
                ScanTag("C01", "X", "Box", isScanned = false),
                ScanTag("C02", "X", "Box", isScanned = false),
            ),
        )
        assertEquals(StockScan.Counted("C01"), full.stockScan(emptySet(), "C01"))
        assertEquals(StockScan.AlreadyCounted, full.stockScan(setOf("C01", "C02"), "C01"))
    }

    @Test
    fun rfid_reads_and_barcodes_share_one_rule() {
        assertEquals("A02", board.acceptStockRead(emptySet(), "a02"))
        assertNull(board.acceptStockRead(setOf("A02"), "A02"))
    }
}
