package com.imssolutions.rfid.logic

const val UNREAD_PERCENT = 6
const val MIN_READ_PERCENT = 10
const val SIGNAL_HOLD_MS = 1_000L
const val SIGNAL_FADE_MS = 3_000L

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

    val status: LineStatus
        get() = when {
            qty <= 0 -> LineStatus.NotScanned
            qty >= plan -> LineStatus.Match
            else -> LineStatus.Short
        }
}

enum class LineStatus { NotScanned, Short, Match }

enum class BarKind { Confirmed, Read, Lost, Unread }

data class SearchHit(val percent: Int, val seenAt: Long, val manual: Boolean = false)

fun liveSignal(hit: SearchHit, now: Long): Int {
    if (hit.manual) return hit.percent
    val fading = now - hit.seenAt - SIGNAL_HOLD_MS
    if (fading <= 0) return hit.percent
    if (fading >= SIGNAL_FADE_MS) return MIN_READ_PERCENT
    val drop = (hit.percent - MIN_READ_PERCENT) * fading / SIGNAL_FADE_MS
    return (hit.percent - drop.toInt()).coerceAtLeast(MIN_READ_PERCENT)
}

private fun isLost(hit: SearchHit, now: Long): Boolean =
    !hit.manual && now - hit.seenAt >= SIGNAL_HOLD_MS + SIGNAL_FADE_MS

data class SearchBar(val epc: String, val percent: Int, val kind: BarKind)

sealed interface StockScan {
    data class Counted(val epc: String) : StockScan
    data object AlreadyCounted : StockScan
    data object Unknown : StockScan
}

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

    fun acceptStockRead(pending: Set<String>, raw: String?): String? =
        (stockScan(pending, raw) as? StockScan.Counted)?.epc

    fun stockScan(pending: Set<String>, raw: String?): StockScan {
        val epc = normalizeEpc(raw) ?: return StockScan.Unknown
        val tag = byEpc[epc] ?: return StockScan.Unknown
        if (tag.isScanned || epc in pending) return StockScan.AlreadyCounted
        val group = groupOf(tag)
        val counted = group.count { it.isScanned || it.epc.uppercase() in pending }
        if (counted >= group.size) return StockScan.AlreadyCounted
        return StockScan.Counted(epc)
    }

    private fun groupOf(tag: ScanTag): List<ScanTag> =
        tags.filter { it.itemCode == tag.itemCode && nameOf(it) == nameOf(tag) }

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

    fun searchBars(itemName: String?, read: Map<String, SearchHit>, now: Long): List<SearchBar> {
        if (itemName == null) return emptyList()
        val group = tags.filter { nameOf(it) == itemName }
        val confirmed = group.filter { it.isScanned }
            .sortedBy { it.epc }
            .map { SearchBar(it.epc, 100, BarKind.Confirmed) }
        val reading = group.filter { !it.isScanned && it.epc.uppercase() in read }
            .map {
                val hit = read.getValue(it.epc.uppercase())
                SearchBar(it.epc, liveSignal(hit, now), if (isLost(hit, now)) BarKind.Lost else BarKind.Read)
            }
            .sortedWith(compareBy<SearchBar> { it.kind == BarKind.Lost }.thenByDescending { it.percent })
        val unread = group.filter { !it.isScanned && it.epc.uppercase() !in read }
            .sortedBy { it.epc }
            .map { SearchBar(it.epc, UNREAD_PERCENT, BarKind.Unread) }
        return confirmed + reading + unread
    }
}
