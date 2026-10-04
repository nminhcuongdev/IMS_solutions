package com.imssolutions.ht.data

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.serialization.builtins.MapSerializer
import kotlinx.serialization.builtins.serializer
import kotlinx.serialization.json.Json

class LabelStore(private val context: Context) {

    private val json = Json { ignoreUnknownKeys = true }

    private val mapSerializer = MapSerializer(String.serializer(), String.serializer())

    private fun key(lang: String) = stringPreferencesKey("labels_$lang")

    private object Keys {
        val LANG = stringPreferencesKey("ui_lang")
        val LANG_OWNER = stringPreferencesKey("ui_lang_owner")
    }

    suspend fun save(lang: String, labels: Map<String, String>) {
        context.dataStore.edit { it[key(lang)] = json.encodeToString(mapSerializer, labels) }
    }

    suspend fun load(lang: String): Map<String, String> =
        context.dataStore.data.first()[key(lang)]
            ?.let { runCatching { json.decodeFromString(mapSerializer, it) }.getOrNull() }
            ?: emptyMap()

    suspend fun clear() {
        context.dataStore.edit { p ->
            listOf("en", "ja", "vi").forEach { p.remove(key(it)) }
        }
    }

    val languageOverride: Flow<Pair<String, String>?> = context.dataStore.data.map { p ->
        val lang = p[Keys.LANG]
        if (lang.isNullOrBlank()) null else lang to p[Keys.LANG_OWNER].orEmpty()
    }

    suspend fun saveLanguageOverride(lang: String, owner: String) {
        context.dataStore.edit { it[Keys.LANG] = lang; it[Keys.LANG_OWNER] = owner }
    }

    suspend fun clearLanguageOverride() {
        context.dataStore.edit { it.remove(Keys.LANG); it.remove(Keys.LANG_OWNER) }
    }
}
