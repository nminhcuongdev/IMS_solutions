package com.imssolutions.ht.data

import android.content.Context
import kotlinx.serialization.json.Json

object BuiltinMessages {
    @Volatile private var cache: Map<String, Map<String, String>>? = null

    fun of(context: Context, lang: String): Map<String, String> {
        val all = cache ?: runCatching {
            val text = context.assets.open("builtin_messages.json").bufferedReader(Charsets.UTF_8).use { it.readText() }
            Json.decodeFromString<Map<String, Map<String, String>>>(text)
        }.getOrDefault(emptyMap()).also { cache = it }
        return all[lang] ?: all["en"] ?: emptyMap()
    }
}
