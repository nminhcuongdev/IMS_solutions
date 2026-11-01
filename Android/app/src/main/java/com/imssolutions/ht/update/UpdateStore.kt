package com.imssolutions.ht.update

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.longPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import com.imssolutions.ht.data.dataStore

class UpdateStore(private val context: Context) {

    private object Keys {
        val DEVICE_ID = stringPreferencesKey("dist_device_id")
        val LAST_SAVED_DATE = longPreferencesKey("dist_last_saved_date")
        val PENDING_CODE = longPreferencesKey("dist_pending_code")
        val STARTED_CODE = longPreferencesKey("dist_started_code")
        val STARTED_NAME = stringPreferencesKey("dist_started_name")
        val FAILURE = stringPreferencesKey("dist_failure")
    }

    data class Snapshot(
        val deviceId: String = "",
        val lastSavedDate: Long? = null,
        val pendingCode: Long? = null,
        val startedCode: Long? = null,
        val startedName: String? = null,
        val failure: String? = null,
    )

    val snapshot: Flow<Snapshot> = context.dataStore.data.map { p ->
        Snapshot(
            deviceId = p[Keys.DEVICE_ID].orEmpty(),
            lastSavedDate = p[Keys.LAST_SAVED_DATE],
            pendingCode = p[Keys.PENDING_CODE],
            startedCode = p[Keys.STARTED_CODE],
            startedName = p[Keys.STARTED_NAME],
            failure = p[Keys.FAILURE],
        )
    }

    suspend fun current(): Snapshot = snapshot.first()

    suspend fun saveDeviceId(id: String) {
        context.dataStore.edit { it[Keys.DEVICE_ID] = id }
    }

    suspend fun saveCheck(serverDate: Long?, pendingCode: Long?) {
        context.dataStore.edit {
            if (serverDate != null) it[Keys.LAST_SAVED_DATE] = serverDate
            if (pendingCode != null) it[Keys.PENDING_CODE] = pendingCode
        }
    }

    suspend fun saveStarted(code: Long, name: String) {
        context.dataStore.edit {
            it[Keys.STARTED_CODE] = code
            it[Keys.STARTED_NAME] = name
            it.remove(Keys.FAILURE)
        }
    }

    suspend fun saveFailure(message: String) {
        context.dataStore.edit { it[Keys.FAILURE] = message }
    }

    suspend fun clearOutcome(installedCode: Long) {
        context.dataStore.edit {
            it.remove(Keys.STARTED_CODE)
            it.remove(Keys.STARTED_NAME)
            it.remove(Keys.FAILURE)
            val pending = it[Keys.PENDING_CODE]
            if (pending != null && pending <= installedCode) it.remove(Keys.PENDING_CODE)
        }
    }
}
