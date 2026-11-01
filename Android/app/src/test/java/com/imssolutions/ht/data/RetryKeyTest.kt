package com.imssolutions.ht.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotEquals
import org.junit.Test

class RetryKeyTest {

    private fun keyer(): RetryKey {
        var n = 0
        return RetryKey { "id-${++n}" }
    }

    @Test
    fun `the same body sent again after a lost connection keeps its id`() {
        val k = keyer()
        val first = k.idFor("A")
        k.failed(ApiException("NET-002", 0, "offline"))
        assertEquals(first, k.idFor("A"))
    }

    @Test
    fun `a changed body after a lost connection is a new request`() {
        val k = keyer()
        val first = k.idFor("A")
        k.failed(ApiException("NET-002", 0, "offline"))
        assertNotEquals(first, k.idFor("B"))
    }

    @Test
    fun `after a success the next package gets a new id even with identical input`() {
        val k = keyer()
        val first = k.idFor("A")
        k.succeeded()
        assertNotEquals(first, k.idFor("A"))
    }

    @Test
    fun `an error the server answered drops the id because nothing was recorded`() {
        val k = keyer()
        val first = k.idFor("A")
        k.failed(ApiException("QTY-002", 400, "too many"))
        assertNotEquals(first, k.idFor("A"))
    }
}
