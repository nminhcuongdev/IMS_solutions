package com.imssolutions.rfid.logic

const val UNREAD_PERCENT = 6
const val MIN_READ_PERCENT = 10

data class ScanTag(
    val epc: String,
    val itemCode: String?,
    val itemName: String?,
    val isScanned: Boolean,
)

data class StockLine(
    val itemCode: String?,
    val itemName: String,
    val qty: Int,
    val plan: Int,
) {
    val isComplete: Boolean get() = qty >= plan
}

enum class BarKind { Confirmed, Read, Unread }

data class SearchBar(val epc: String, val percent: Int, val kind: BarKind)

sealed interface ManualSerial {
    data object Blank : ManualSerial
    data object NotFound : ManualSerial
    data object AlreadyScanned : ManualSerial
    data class Read(val epc: String) : ManualSerial
}

fun normalizeEpc(raw: String?): String? = raw?.trim()?.uppercase()?.takeIf { it.isNotEmpty() }

fun rssiToPercent(rssi: Double): Int {
    if (rssi >= 0) return rssi.toInt().coerceIn(MIN_READ_PERCENT, 100)
    val pct = ((rssi + 85.0) / 50.0 * 100.0).toInt()
    return pct.coerceIn(MIN_READ_PERCENT, 100)
}

class Board(val tags: List<ScanTag>) {

    private val byEpc: Map<String, ScanTag> =
        tags.associateBy { it.epc.uppercase() }

    private fun nameOf(tag: ScanTag): String = tag.itemName ?: tag.itemCode ?: "-"

    fun acceptStockRead(pending: Set<String>, raw: String?): String? {
        val epc = normalizeEpc(raw) ?: return null
        val tag = byEpc[epc] ?: return null
        if (tag.isScanned || epc in pending) return null
        return epc
    }

    fun stockLines(pending: Set<String>): List<StockLine> =
        tags.groupBy { it.itemCode to nameOf(it) }
            .map { (key, group) ->
                StockLine(
                    itemCode = key.first,
                    itemName = key.second,
                    qty = group.count { it.isScanned || it.epc.uppercase() in pending },
                    plan = group.size,
                )
            }

    fun totalCount(pending: Set<String>): Int = stockLines(pending).sumOf { it.qty }

    fun itemNames(): List<String> = tags.map(::nameOf).distinct()

    fun acceptSearchRead(itemName: String?, raw: String?): String? {
        if (itemName == null) return null
        val epc = normalizeEpc(raw) ?: return null
        val tag = byEpc[epc] ?: return null
        if (nameOf(tag) != itemName || tag.isScanned) return null
        return epc
    }

    fun manualSerial(itemName: String?, raw: String?): ManualSerial {
        val epc = normalizeEpc(raw) ?: return ManualSerial.Blank
        val tag = byEpc[epc]
        if (tag == null || itemName == null || nameOf(tag) != itemName) return ManualSerial.NotFound
        if (tag.isScanned) return ManualSerial.AlreadyScanned
        return ManualSerial.Read(epc)
    }

    fun searchBars(itemName: String?, read: Map<String, Int>): List<SearchBar> {
        if (itemName == null) return emptyList()
        val group = tags.filter { nameOf(it) == itemName }
        val confirmed = group.filter { it.isScanned }
            .sortedBy { it.epc }
            .map { SearchBar(it.epc, 100, BarKind.Confirmed) }
        val reading = group.filter { !it.isScanned && it.epc.uppercase() in read }
            .map { SearchBar(it.epc, read.getValue(it.epc.uppercase()), BarKind.Read) }
            .sortedByDescending { it.percent }
        val unread = group.filter { !it.isScanned && it.epc.uppercase() !in read }
            .sortedBy { it.epc }
            .map { SearchBar(it.epc, UNREAD_PERCENT, BarKind.Unread) }
        return confirmed + reading + unread
    }
}
