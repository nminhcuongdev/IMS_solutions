package com.imssolutions.ht.ui

import org.junit.Assert.assertEquals
import org.junit.Test

class ListKeysTest {

    @Test
    fun `keys that are already unique are left alone`() {
        assertEquals(listOf("A/01", "A/02", "B/01"), distinctKeys(listOf("A/01", "A/02", "B/01")))
    }

    @Test
    fun `a repeated line gets a numbered key instead of crashing the list`() {
        val keys = distinctKeys(listOf("A/01", "B/01", "A/01", "A/01"))
        assertEquals(listOf("A/01", "B/01", "A/01#2", "A/01#3"), keys)
        assertEquals(keys.size, keys.toSet().size)
    }

    @Test
    fun `a suffix never collides with a real key that looks like one`() {
        val keys = distinctKeys(listOf("A/01#2", "A/01", "A/01"))
        assertEquals(keys.size, keys.toSet().size)
    }
}
