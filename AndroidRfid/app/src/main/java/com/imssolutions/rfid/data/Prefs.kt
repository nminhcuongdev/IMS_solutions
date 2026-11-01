package com.imssolutions.rfid.data

import android.content.Context
import android.os.Build
import com.imssolutions.rfid.BuildConfig
import com.imssolutions.rfid.rfid.ReaderKind

class Prefs(context: Context) {
    private val sp = context.applicationContext.getSharedPreferences("ims_rfid", Context.MODE_PRIVATE)

    var serverUrl: String
        get() = sp.getString(KEY_SERVER, null)?.takeIf { it.isNotBlank() } ?: BuildConfig.API_BASE_URL
        set(value) {
            val v = value.trim().let { if (it.isEmpty() || it.endsWith("/")) it else "$it/" }
            sp.edit().putString(KEY_SERVER, v).apply()
        }

    var language: String
        get() = sp.getString(KEY_LANG, null) ?: "en"
        set(value) { sp.edit().putString(KEY_LANG, value).apply() }

    var lastUserId: String
        get() = sp.getString(KEY_USER, null).orEmpty()
        set(value) { sp.edit().putString(KEY_USER, value).apply() }

    var rememberMe: Boolean
        get() = sp.getBoolean(KEY_REMEMBER, true)
        set(value) { sp.edit().putBoolean(KEY_REMEMBER, value).apply() }

    var savedSession: Triple<String, String, String>?
        get() {
            val token = sp.getString(KEY_TOKEN, null)?.takeIf { it.isNotBlank() } ?: return null
            return Triple(token, sp.getString(KEY_USER_CODE, null).orEmpty(), sp.getString(KEY_USER_NAME, null).orEmpty())
        }
        set(value) {
            sp.edit().apply {
                if (value == null) {
                    remove(KEY_TOKEN); remove(KEY_USER_CODE); remove(KEY_USER_NAME)
                } else {
                    putString(KEY_TOKEN, value.first); putString(KEY_USER_CODE, value.second); putString(KEY_USER_NAME, value.third)
                }
            }.apply()
        }

    var txPower: Int
        get() = sp.getInt(KEY_POWER, -1)
        set(value) { sp.edit().putInt(KEY_POWER, value).apply() }

    var readerKind: ReaderKind
        get() = sp.getString(KEY_READER, null)
            ?.let { name -> ReaderKind.entries.firstOrNull { it.name == name } }
            ?: when {
                Build.MANUFACTURER.contains("cipherlab", ignoreCase = true) -> ReaderKind.CipherLab
                Build.MODEL.contains("MT93", ignoreCase = true) -> ReaderKind.NewlandMt93
                else -> ReaderKind.DensoSp1
            }
        set(value) { sp.edit().putString(KEY_READER, value.name).apply() }

    private companion object {
        const val KEY_READER = "reader_kind"
        const val KEY_SERVER = "server_url"
        const val KEY_LANG = "language"
        const val KEY_USER = "last_user_id"
        const val KEY_POWER = "tx_power"
        const val KEY_REMEMBER = "remember_me"
        const val KEY_TOKEN = "session_token"
        const val KEY_USER_CODE = "session_user_code"
        const val KEY_USER_NAME = "session_user_name"
    }
}
