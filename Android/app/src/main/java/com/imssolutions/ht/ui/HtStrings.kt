package com.imssolutions.ht.ui

import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.ProvidableCompositionLocal
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.runtime.staticCompositionLocalOf

@Immutable
data class HtStrings(
    val lang: HtLang = HtLang.VI,
    val labels: Map<String, String> = emptyMap(),
    val languages: List<HtLang> = HtLang.BUILT_IN,
    val builtin: Map<String, String> = emptyMap(),
) {
    fun t(key: String, fallback: String): String =
        labels[key]?.takeIf { it.isNotBlank() } ?: builtin[key]?.takeIf { it.isNotBlank() } ?: fallback

    fun t(key: String, fallback: String, vararg args: Any?): String {
        var out = t(key, fallback)
        args.forEachIndexed { i, a -> out = out.replace("{$i}", a?.toString().orEmpty()) }
        return out
    }

    val loaded: Boolean get() = labels.isNotEmpty()
}

@Immutable
data class HtMsg(val key: String, val fallback: String, val args: List<Any?> = emptyList()) {
    constructor(key: String, fallback: String, vararg args: Any?) : this(key, fallback, args.toList())
}

fun HtStrings.render(m: HtMsg): String = t(m.key, m.fallback, *m.args.toTypedArray())

val LocalHtStrings: ProvidableCompositionLocal<HtStrings> = staticCompositionLocalOf { HtStrings() }

@Composable
@ReadOnlyComposable
fun t(key: String, fallback: String): String = LocalHtStrings.current.t(key, fallback)

@Composable
@ReadOnlyComposable
fun t(key: String, fallback: String, vararg args: Any?): String =
    LocalHtStrings.current.t(key, fallback, *args)
