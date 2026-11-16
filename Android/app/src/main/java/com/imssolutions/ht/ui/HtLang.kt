package com.imssolutions.ht.ui

import androidx.compose.runtime.Immutable

@Immutable
data class HtLang(val code: String, val name: String, val icon: String? = null) {
    companion object {
        val EN = HtLang("en", "English")
        val VI = HtLang("vi", "Tiếng Việt")

        val BUILT_IN = listOf(EN, HtLang("ja", "日本語"), VI)

        fun of(code: String?, list: List<HtLang>): HtLang {
            val wanted = code?.trim().orEmpty()
            return list.firstOrNull { it.code.equals(wanted, ignoreCase = true) }
                ?: list.firstOrNull { it.code == EN.code }
                ?: list.firstOrNull()
                ?: EN
        }
    }
}

fun iconPayload(uri: String?): String? {
    val s = uri?.trim() ?: return null
    if (!s.startsWith("data:", ignoreCase = true)) return null
    val marker = s.indexOf(";base64,")
    if (marker < 0) return null
    return s.substring(marker + ";base64,".length).takeIf { it.isNotEmpty() }
}
