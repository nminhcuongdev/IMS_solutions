package com.imssolutions.rfid.logic

fun partKey(tag: ScanTag): String = tag.itemCode.orEmpty()

data class PartChoice(
    val key: String,
    val itemCode: String?,
    val itemName: String?,
    val scanned: Int,
    val plan: Int,
)

fun partChoices(board: Board): List<PartChoice> =
    board.tags.groupBy(::partKey)
        .map { (key, tags) ->
            PartChoice(
                key = key,
                itemCode = tags.first().itemCode,
                itemName = tags.firstNotNullOfOrNull { it.itemName },
                scanned = tags.count { it.isScanned },
                plan = tags.size,
            )
        }
        .sortedBy { it.key }

fun Board.onlyParts(keys: Set<String>): Board = Board(tags.filter { partKey(it) in keys })

fun keepChosen(board: Board, pending: Set<String>, keys: Set<String>): Set<String> {
    val keyOf = board.tags.associate { it.epc.uppercase() to partKey(it) }
    return pending.filterTo(mutableSetOf()) { keyOf[it] in keys }
}

fun initialChoice(available: List<PartChoice>, previous: Set<String>?): Set<String> {
    val all = available.map { it.key }.toSet()
    val kept = previous?.intersect(all).orEmpty()
    return kept.ifEmpty { all }
}
