package com.imssolutions.ht.scan

import java.net.URLDecoder

data class QrPayload(
    val raw: String,
    val ais: Map<String, String>,
) {
    val gtin: String? get() = ais["01"]
    val lotNo: String? get() = ais["10"]
    val serialNo: String? get() = ais["21"]

    val productionDate: String? get() = ais["11"]
    val packingDate: String? get() = ais["13"]
    val expiryDate: String? get() = ais["17"]
    val variableCount: String? get() = ais["30"]
    val itemsPerPack: String? get() = ais["37"]
    val customerPo: String? get() = ais["400"]

    val itemCode: String? get() = ais["item_cd"]
    val itemName: String? get() = ais["item_name"]
    val printedAt: String? get() = ais["print_at"]
    val locationCode: String? get() = ais["lc_cd"]
    val locationName: String? get() = ais["lc_name"]
    val seqNoSys: String? get() = ais["seq_no_sys"]
    val userCode: String? get() = ais["user_cd"]

    val kind: QrLabelKind
        get() = when {
            gtin != null && (itemsPerPack != null || ais.keys.any { it.length == 4 && it[0] == '3' }) ->
                QrLabelKind.Shipping
            gtin != null -> QrLabelKind.Product
            lotNo != null || itemCode != null -> QrLabelKind.Product
            userCode != null && locationCode == null -> QrLabelKind.User
            locationCode != null -> QrLabelKind.Location
            else -> QrLabelKind.Unknown
        }

    companion object {
        private const val DELIMITERS = "/?&="

        fun parse(raw: String): QrScan {
            val s = raw.trim()
            if (s.isEmpty() || s.none { it in DELIMITERS }) return QrScan.Invalid(ScanError.SCN_001, s)

            val q = s.indexOf('?')
            val path = if (q >= 0) s.substring(0, q) else s
            val query = if (q >= 0) s.substring(q + 1) else ""

            val ais = LinkedHashMap<String, String>()

            val tokens = path.split('/').filter { it.isNotEmpty() }
            var i = 0
            while (i + 1 < tokens.size) {
                ais[tokens[i]] = decode(tokens[i + 1])
                i += 2
            }

            for (part in query.split('&')) {
                val e = part.indexOf('=')
                if (e <= 0) continue
                ais[part.substring(0, e)] = decode(part.substring(e + 1))
            }

            if (ais.isEmpty()) return QrScan.Invalid(ScanError.SCN_001, s)
            return QrScan.Ok(QrPayload(s, ais))
        }

        private fun decode(v: String): String =
            if ('%' !in v) v
            else runCatching { URLDecoder.decode(v, "UTF-8") }.getOrDefault(v)
    }
}

enum class QrLabelKind { Product, Shipping, User, Location, Unknown }

sealed interface QrScan {
    data class Ok(val payload: QrPayload) : QrScan
    data class Invalid(val error: ScanError, val raw: String) : QrScan
}
