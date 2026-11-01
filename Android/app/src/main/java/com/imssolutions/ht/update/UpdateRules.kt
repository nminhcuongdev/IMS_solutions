package com.imssolutions.ht.update

import kotlinx.serialization.json.Json
import java.util.Calendar
import java.util.TimeZone

object UpdateRules {

    private val json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
        coerceInputValues = true
    }

    private val isoDate = Regex(
        """^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.\d+)?(Z|[+-]\d{2}:?\d{2})$"""
    )

    private const val MARGIN_BYTES = 20L * 1024 * 1024

    fun parse(text: String): VersionJson = json.decodeFromString(VersionJson.serializer(), text)

    fun decide(
        doc: VersionJson,
        packageName: String,
        deviceId: String,
        installedCode: Long,
        lastModified: Long?,
        lastSavedDate: Long?,
        knownPendingCode: Long?,
    ): UpdateDecision {
        val entry = doc.apps.firstOrNull { it.packageName == packageName }
            ?: return UpdateDecision(null, lastModified)
        val serverDate = entry.updatedAt?.let(::parseIsoDate) ?: lastModified
        val usable = isTarget(entry, deviceId) && safeFileName(entry.apkFile) && entry.sha256.isNotBlank()
        val offer = if (usable && available(entry, installedCode, serverDate, lastSavedDate, knownPendingCode)) {
            UpdateOffer(entry, forced(entry, installedCode))
        } else null
        return UpdateDecision(offer, serverDate)
    }

    fun available(
        entry: VersionEntry,
        installedCode: Long,
        serverDate: Long?,
        lastSavedDate: Long?,
        knownPendingCode: Long?,
    ): Boolean {
        if (entry.versionCode <= installedCode) return false
        val newer = serverDate == null || lastSavedDate == null || serverDate > lastSavedDate
        return newer || entry.versionCode == knownPendingCode
    }

    fun forced(entry: VersionEntry, installedCode: Long): Boolean =
        entry.force || (entry.minVersionCode != null && installedCode < entry.minVersionCode)

    fun isTarget(entry: VersionEntry, deviceId: String): Boolean {
        val t = entry.targets ?: return true
        return t.all || deviceId in t.deviceIds
    }

    fun safeFileName(name: String): Boolean =
        name.isNotBlank() && '/' !in name && '\\' !in name && ".." !in name

    fun note(entry: VersionEntry, lang: String): String? =
        entry.notes[lang]?.takeIf { it.isNotBlank() }
            ?: entry.notes["en"]?.takeIf { it.isNotBlank() }
            ?: entry.notes.values.firstOrNull { it.isNotBlank() }

    fun deviceIdOf(input: String): String? {
        val s = input.trim()
        if (s.isEmpty() || s.length > 40) return null
        if (s.any { it.isISOControl() }) return null
        return s
    }

    fun spaceNeeded(totalBytes: Long, alreadyDownloaded: Long): Long =
        totalBytes + totalBytes / 10 + MARGIN_BYTES - alreadyDownloaded

    fun parseIsoDate(text: String): Long? {
        val m = isoDate.matchEntire(text.trim()) ?: return null
        val g = m.groupValues
        val cal = Calendar.getInstance(TimeZone.getTimeZone("UTC"))
        cal.clear()
        cal.set(g[1].toInt(), g[2].toInt() - 1, g[3].toInt(), g[4].toInt(), g[5].toInt(), g[6].toInt())
        val zone = g[7]
        val offsetMinutes = if (zone == "Z") 0 else {
            val digits = zone.substring(1).replace(":", "")
            val minutes = digits.substring(0, 2).toInt() * 60 + digits.substring(2, 4).toInt()
            if (zone[0] == '-') -minutes else minutes
        }
        return cal.timeInMillis - offsetMinutes * 60_000L
    }
}
