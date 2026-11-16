package com.imssolutions.rfid.logic

const val EPC_PREFIX_LENGTH = 5
const val EPC_MAX_LENGTH = 24
const val PART_CODE_MAX = 50
const val PART_NAME_MAX = 255

fun registrableEpc(raw: String?): String? {
    val epc = normalizeEpc(raw) ?: return null
    if (epc.length !in EPC_PREFIX_LENGTH..EPC_MAX_LENGTH) return null
    if (!epc.all { it in '0'..'9' || it in 'A'..'F' }) return null
    return epc
}

fun epcPrefix(epc: String): String = epc.take(EPC_PREFIX_LENGTH)

data class PartInput(val code: String = "", val name: String = "") {
    val isBlank: Boolean get() = code.isBlank() && name.isBlank()
}

data class KnownPart(val code: String?, val name: String?)

data class EpcGroup(
    val prefix: String,
    val newEpcs: List<String>,
    val registered: Map<String, KnownPart>,
) {
    val total: Int get() = newEpcs.size + registered.size
}

fun groupEpcs(reads: Collection<String>, registered: Map<String, KnownPart>): List<EpcGroup> =
    reads.mapNotNull(::registrableEpc)
        .distinct()
        .groupBy(::epcPrefix)
        .map { (prefix, epcs) ->
            val sorted = epcs.sorted()
            EpcGroup(
                prefix = prefix,
                newEpcs = sorted.filter { it !in registered },
                registered = sorted.filter { it in registered }.associateWith { registered.getValue(it) },
            )
        }
        .sortedBy { it.prefix }

data class RegisterLine(val code: String, val name: String, val epcs: List<String>)

sealed interface RegisterPlan {
    data object Nothing : RegisterPlan
    data class Missing(val prefix: String) : RegisterPlan
    data class TooLong(val prefix: String) : RegisterPlan
    data class Ready(val lines: List<RegisterLine>) : RegisterPlan {
        val tagCount: Int get() = lines.sumOf { it.epcs.size }
        val partCount: Int get() = lines.map { it.code }.distinct().size
    }
}

fun registerPlan(groups: List<EpcGroup>, inputs: Map<String, PartInput>): RegisterPlan {
    val pending = groups.filter { it.newEpcs.isNotEmpty() }
    if (pending.isEmpty()) return RegisterPlan.Nothing
    val lines = mutableListOf<RegisterLine>()
    for (g in pending) {
        val input = inputs[g.prefix] ?: PartInput()
        val code = input.code.trim()
        val name = input.name.trim()
        if (code.isEmpty() || name.isEmpty()) return RegisterPlan.Missing(g.prefix)
        if (code.length > PART_CODE_MAX || name.length > PART_NAME_MAX) return RegisterPlan.TooLong(g.prefix)
        lines += RegisterLine(code, name, g.newEpcs)
    }
    return RegisterPlan.Ready(lines)
}

fun prefillInputs(
    inputs: Map<String, PartInput>,
    known: Map<String, KnownPart>,
): Map<String, PartInput> {
    val next = inputs.toMutableMap()
    for ((prefix, part) in known) {
        if (next[prefix]?.isBlank == false) continue
        next[prefix] = PartInput(part.code.orEmpty(), part.name.orEmpty())
    }
    return next
}

fun acceptRegisterRead(reads: Set<String>, excluded: Set<String>, raw: String?): String? {
    val epc = registrableEpc(raw) ?: return null
    if (epc in reads || epcPrefix(epc) in excluded) return null
    return epc
}

fun withoutGroup(reads: Set<String>, prefix: String): Set<String> =
    reads.filterTo(mutableSetOf()) { epcPrefix(it) != prefix }
