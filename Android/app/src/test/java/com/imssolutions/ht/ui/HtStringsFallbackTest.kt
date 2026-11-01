package com.imssolutions.ht.ui

import org.junit.Assert.assertEquals
import org.junit.Test

class HtStringsFallbackTest {

    private val builtin = mapOf("NET-002" to "Network connection lost.")

    @Test
    fun `before any download an error code renders in the chosen language`() {
        val s = HtStrings(lang = HtLang("en", "English"), labels = emptyMap(), builtin = builtin)
        assertEquals("Network connection lost.", s.t("NET-002", "Mất kết nối mạng."))
    }

    @Test
    fun `downloaded labels win over the built-in copy`() {
        val s = HtStrings(lang = HtLang("en", "English"), labels = mapOf("NET-002" to "Offline"), builtin = builtin)
        assertEquals("Offline", s.t("NET-002", "Mất kết nối mạng."))
    }

    @Test
    fun `a key nobody knows still renders the fallback`() {
        val s = HtStrings(lang = HtLang("en", "English"), builtin = builtin)
        assertEquals("x", s.t("ZZZ-999", "x"))
    }
}
