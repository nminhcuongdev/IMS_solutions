package com.imssolutions.ht.scan

import java.io.ByteArrayOutputStream
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import com.imssolutions.ht.data.ApiException

object LabelQr {
    const val MAX_LENGTH = 250

    private const val HEX = "0123456789ABCDEF"
    private const val KEEP = "-_.!~*'()"
    private val DROP_ORDER = listOf("lc_name", "item_name", "user_cd", "print_at", "11", "13")

    fun encode(value: String): String {
        val out = StringBuilder()
        for (b in value.toByteArray(Charsets.UTF_8)) {
            val c = b.toInt() and 0xFF
            val ch = c.toChar()
            if (c < 0x80 && (ch in 'A'..'Z' || ch in 'a'..'z' || ch in '0'..'9' || ch in KEEP)) out.append(ch)
            else out.append('%').append(HEX[c shr 4]).append(HEX[c and 0x0F])
        }
        return out.toString()
    }

    fun decode(value: String): String {
        if ('%' !in value) return value
        val bytes = ByteArrayOutputStream()
        var i = 0
        while (i < value.length) {
            if (value[i] == '%' && i + 2 < value.length) {
                val hi = Character.digit(value[i + 1], 16)
                val lo = Character.digit(value[i + 2], 16)
                if (hi >= 0 && lo >= 0) {
                    bytes.write(hi * 16 + lo)
                    i += 3
                    continue
                }
            }
            val cp = value.codePointAt(i)
            bytes.write(String(Character.toChars(cp)).toByteArray(Charsets.UTF_8))
            i += Character.charCount(cp)
        }
        return String(bytes.toByteArray(), Charsets.UTF_8)
    }

    fun timestamp(at: Date): String = SimpleDateFormat("yyMMddHHmmss", Locale.US).format(at)

    fun date(value: String?): String? {
        val m = Regex("""^(\d{4})[-/]?(\d{2})[-/]?(\d{2})""").find(value?.trim() ?: return null) ?: return null
        val (y, mo, d) = m.destructured
        return y.substring(2) + mo + d
    }

    fun qty(value: Double): String =
        if (value == value.toLong().toDouble()) value.toLong().toString() else value.toString()

    fun product(
        gtin: String?,
        lotNo: String?,
        qty: Double,
        itemCode: String,
        itemName: String? = null,
        productionDate: String? = null,
        expiryDate: String? = null,
        userCode: String? = null,
        printedAt: Date = Date(),
    ): String = compose(
        gtin, lotNo,
        listOf(
            "11" to productionDate,
            "17" to expiryDate,
            "30" to qty(qty),
            "item_cd" to itemCode,
            "item_name" to itemName,
            "print_at" to timestamp(printedAt),
            "user_cd" to userCode,
        ),
    )

    fun shipping(
        gtin: String?,
        lotNo: String?,
        qty: Double,
        itemCode: String,
        itemName: String? = null,
        printedAt: Date = Date(),
    ): String = compose(
        gtin, lotNo,
        listOf(
            "13" to timestamp(printedAt).substring(0, 6),
            "37" to qty(qty),
            "item_cd" to itemCode,
            "item_name" to itemName,
            "print_at" to timestamp(printedAt),
        ),
    )

    private fun compose(gtin: String?, lotNo: String?, attrs: List<Pair<String, String?>>): String {
        val path = (gtin?.trim()?.takeIf { it.isNotEmpty() }?.let { "/01/${encode(it)}" } ?: "") +
            (lotNo?.trim()?.takeIf { it.isNotEmpty() }?.let { "/10/${encode(it)}" } ?: "")
        val present = attrs.filter { !it.second.isNullOrEmpty() }

        fun build(skip: Set<String>) = path + "?" + present
            .filter { it.first !in skip }
            .joinToString("&") { "${it.first}=${encode(it.second!!)}" }

        var out = build(emptySet())
        val skip = mutableSetOf<String>()
        for (key in DROP_ORDER) {
            if (out.length <= MAX_LENGTH) return out
            skip += key
            out = build(skip)
        }
        if (out.length <= MAX_LENGTH) return out
        throw ApiException("VAL-005", 400, "QR ${out.length} > $MAX_LENGTH")
    }
}
