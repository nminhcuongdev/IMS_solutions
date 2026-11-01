package com.imssolutions.ht.update

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class UpdateRulesTest {

    private val pkg = "com.imssolutions.ht"
    private val sha = "a".repeat(64)

    private fun entry(
        code: Long = 5,
        force: Boolean = false,
        min: Long? = null,
        targets: UpdateTargets? = null,
        apk: String = "imsht-0.5.0.apk",
        sha256: String = sha,
        updatedAt: String? = "2026-10-21T09:00:00+07:00",
        notes: Map<String, String> = emptyMap(),
    ) = VersionEntry(pkg, code, "0.5.0", updatedAt, apk, sha256, min, force, notes, targets)

    private fun decide(
        e: VersionEntry?,
        installed: Long = 4,
        lastSaved: Long? = null,
        pending: Long? = null,
        deviceId: String = "MT95",
        lastModified: Long? = null,
    ) = UpdateRules.decide(VersionJson(listOfNotNull(e)), pkg, deviceId, installed, lastModified, lastSaved, pending)

    private val d21 = UpdateRules.parseIsoDate("2026-10-21T09:00:00+07:00")!!

    @Test
    fun `a newer version with a newer server date is offered`() {
        val r = decide(entry())
        assertTrue(r.offer != null)
        assertEquals(d21, r.serverDate)
        assertFalse(r.offer!!.forced)
    }

    @Test
    fun `the same or an older version is never offered`() {
        assertNull(decide(entry(code = 4)).offer)
        assertNull(decide(entry(code = 3)).offer)
        assertNull(decide(entry(code = 4), pending = 4).offer)
    }

    @Test
    fun `a server date not newer than the saved one is not offered`() {
        assertNull(decide(entry(), lastSaved = d21).offer)
        assertNull(decide(entry(), lastSaved = d21 + 1000).offer)
        assertTrue(decide(entry(), lastSaved = d21 - 1000).offer != null)
    }

    @Test
    fun `a known pending update keeps being offered until installed`() {
        assertTrue(decide(entry(), lastSaved = d21, pending = 5).offer != null)
        assertNull(decide(entry(), lastSaved = d21, pending = 6).offer)
        assertNull(decide(entry(), installed = 5, lastSaved = d21, pending = 5).offer)
    }

    @Test
    fun `no server date at all counts as newer`() {
        val r = decide(entry(updatedAt = null), lastSaved = d21)
        assertTrue(r.offer != null)
        assertNull(r.serverDate)
    }

    @Test
    fun `updatedAt wins over Last-Modified and Last-Modified fills in when it is missing`() {
        assertEquals(d21, decide(entry(), lastModified = 42).serverDate)
        assertEquals(42L, decide(entry(updatedAt = null), lastModified = 42).serverDate)
    }

    @Test
    fun `force or an installed version below the minimum is forced`() {
        assertTrue(decide(entry(force = true)).offer!!.forced)
        assertTrue(decide(entry(min = 5)).offer!!.forced)
        assertFalse(decide(entry(min = 4)).offer!!.forced)
    }

    @Test
    fun `targets with all false and another device id mean no update`() {
        assertNull(decide(entry(targets = UpdateTargets(false, listOf("WH-1")))).offer)
        assertTrue(decide(entry(targets = UpdateTargets(false, listOf("WH-1", "MT95")))).offer != null)
        assertTrue(decide(entry(targets = UpdateTargets(true, emptyList()))).offer != null)
    }

    @Test
    fun `an entry for another package is no update`() {
        val other = entry().copy(packageName = "com.example.other")
        assertNull(decide(other).offer)
        assertNull(decide(null).offer)
    }

    @Test
    fun `an apk name with a path or an empty sha is refused`() {
        assertNull(decide(entry(apk = "../x.apk")).offer)
        assertNull(decide(entry(apk = "dl/x.apk")).offer)
        assertNull(decide(entry(apk = "dl\\x.apk")).offer)
        assertNull(decide(entry(apk = "")).offer)
        assertNull(decide(entry(sha256 = "")).offer)
    }

    @Test
    fun `iso dates with an offset or Z parse to the same instant`() {
        assertEquals(UpdateRules.parseIsoDate("2026-10-21T02:00:00Z"), d21)
        assertEquals(UpdateRules.parseIsoDate("2026-10-21T02:00:00.000+00:00"), d21)
        assertNull(UpdateRules.parseIsoDate("21/10/2026"))
        assertNull(UpdateRules.parseIsoDate(""))
    }

    @Test
    fun `version json parses and ignores unknown fields`() {
        val doc = UpdateRules.parse(
            """{"apps":[{"packageName":"$pkg","versionCode":7,"versionName":"0.7","updatedAt":"2026-10-21T09:00:00+07:00",
               "apkFile":"a.apk","sha256":"$sha","force":true,"notes":{"vi":"Sửa lỗi"},"extra":1,
               "targets":{"all":false,"deviceIds":["MT95"]}}]}"""
        )
        val e = doc.apps.single()
        assertEquals(7L, e.versionCode)
        assertTrue(e.force)
        assertEquals("Sửa lỗi", e.notes["vi"])
        assertEquals(listOf("MT95"), e.targets!!.deviceIds)
        assertTrue(UpdateRules.parse("""{"apps":[]}""").apps.isEmpty())
    }

    @Test
    fun `notes pick the ui language then english then anything`() {
        val e = entry(notes = mapOf("vi" to "VI", "en" to "EN", "ja" to "JA"))
        assertEquals("JA", UpdateRules.note(e, "ja"))
        assertEquals("EN", UpdateRules.note(e, "ko"))
        assertEquals("VI", UpdateRules.note(entry(notes = mapOf("vi" to "VI")), "ja"))
        assertNull(UpdateRules.note(entry(), "vi"))
    }

    @Test
    fun `the device id is trimmed and must be 1 to 40 characters without control characters`() {
        assertEquals("MT95", UpdateRules.deviceIdOf("  MT95 "))
        assertNull(UpdateRules.deviceIdOf("   "))
        assertNull(UpdateRules.deviceIdOf("A".repeat(41)))
        assertEquals("A".repeat(40), UpdateRules.deviceIdOf("A".repeat(40)))
        assertNull(UpdateRules.deviceIdOf("MT\t95"))
    }

    @Test
    fun `free space needs the file plus ten percent plus a margin`() {
        val size = 20L * 1024 * 1024
        assertEquals(size + size / 10 + 20L * 1024 * 1024, UpdateRules.spaceNeeded(size, 0))
        assertEquals(size + size / 10 + 20L * 1024 * 1024 - 5, UpdateRules.spaceNeeded(size, 5))
    }
}
