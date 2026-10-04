package com.imssolutions.ht.data

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.runBlocking
import com.imssolutions.ht.BuildConfig

internal val Context.dataStore by preferencesDataStore(name = "ims_session")

class TokenStore(private val context: Context) {

    private object Keys {
        val TOKEN = stringPreferencesKey("token")
        val USER_CODE = stringPreferencesKey("user_code")
        val USER_NAME = stringPreferencesKey("user_name")
        val USER_LANG = stringPreferencesKey("user_lang")
        val BASE_URL = stringPreferencesKey("base_url")
        val PRINTER_DEVICE_CODE = stringPreferencesKey("printer_driver")
        val ZONE_CODE = stringPreferencesKey("zone_code")
        val LANG_DATA_AT = stringPreferencesKey("lang_data_at")
    }

    data class HtSettings(
        val serverIp: String = "",
        val printerDeviceCode: String = "",
        val zoneCode: String = "",
        val langDataAt: String? = null,
    )

    val htSettings: Flow<HtSettings> = context.dataStore.data.map { p ->
        HtSettings(
            serverIp = p[Keys.BASE_URL].orEmpty(),
            printerDeviceCode = p[Keys.PRINTER_DEVICE_CODE].orEmpty(),
            zoneCode = p[Keys.ZONE_CODE].orEmpty(),
            langDataAt = p[Keys.LANG_DATA_AT],
        )
    }

    fun htSettingsBlocking(): HtSettings = runBlocking { htSettings.first() }

    suspend fun saveHtSettings(serverIp: String, printerDeviceCode: String, zoneCode: String) {
        context.dataStore.edit {
            it[Keys.PRINTER_DEVICE_CODE] = printerDeviceCode
            it[Keys.ZONE_CODE] = zoneCode
        }
    }

    suspend fun saveLangDataAt(at: String?) {
        context.dataStore.edit {
            if (at == null) it.remove(Keys.LANG_DATA_AT) else it[Keys.LANG_DATA_AT] = at
        }
    }

    val langDataAt: Flow<String?> = context.dataStore.data.map { it[Keys.LANG_DATA_AT] }

    val baseUrl: Flow<String> = context.dataStore.data.map { p ->
        p[Keys.BASE_URL]?.takeIf { it.isNotBlank() } ?: BuildConfig.API_BASE_URL
    }

    suspend fun saveBaseUrl(url: String) {
        val normalized = url.trim().let { if (it.endsWith("/")) it else "$it/" }
        context.dataStore.edit { it[Keys.BASE_URL] = normalized }
    }

    fun baseUrlBlocking(): String = runBlocking { baseUrl.first() }

    val session: Flow<Session?> = context.dataStore.data.map { p ->
        val t = p[Keys.TOKEN]
        if (t.isNullOrBlank()) null
        else Session(t, p[Keys.USER_CODE].orEmpty(), p[Keys.USER_NAME].orEmpty(), p[Keys.USER_LANG])
    }

    suspend fun save(token: String, userCode: String, userName: String, defaultLanguage: String?) {
        context.dataStore.edit {
            it[Keys.TOKEN] = token
            it[Keys.USER_CODE] = userCode
            it[Keys.USER_NAME] = userName
            if (defaultLanguage.isNullOrBlank()) it.remove(Keys.USER_LANG)
            else it[Keys.USER_LANG] = defaultLanguage
        }
    }

    suspend fun clear() {
        context.dataStore.edit {
            it.remove(Keys.TOKEN); it.remove(Keys.USER_CODE); it.remove(Keys.USER_NAME)
            it.remove(Keys.USER_LANG)
        }
    }

    fun tokenBlocking(): String? = runBlocking { session.first()?.token }
}

data class Session(
    val token: String,
    val userCode: String,
    val userName: String,
    val defaultLanguage: String? = null,
)
