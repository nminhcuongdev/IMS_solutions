package com.imssolutions.ht.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class ServerAddressTest {

    private val device = "10.84.30.77"

    private fun url(input: String, dev: String? = device) = ServerAddress.toBaseUrl(input, dev)

    @Test
    fun `a dotted address gets the scheme and the default port`() {
        assertEquals("http://10.84.30.51:8091/", url("10.84.30.51"))
        assertEquals("http://192.168.1.100:8091/", url("192.168.1.100", null))
    }

    @Test
    fun `a pasted full url is accepted as it was before`() {
        assertEquals("http://10.84.30.51:8091/", url("http://10.84.30.51:8091/"))
        assertEquals("http://127.0.0.1:8095/", url("http://127.0.0.1:8095/"))
    }

    @Test
    fun `digits without dots are split on the device subnet`() {
        assertEquals("http://10.84.30.51:8091/", url("10843051"))
        assertEquals("http://192.168.100.21:8091/", url("19216810021", "192.168.100.5"))
    }

    @Test
    fun `a short number fills the rest from the device address`() {
        assertEquals("http://10.84.30.51:8091/", url("51"))
        assertEquals("http://10.84.30.51:8091/", url("3051"))
        assertEquals("http://10.84.30.51:8091/", url("30.51"))
    }

    @Test
    fun `without a device address only a full four part split is taken`() {
        assertEquals("http://192.168.1.100:8091/", url("1921681100", null))
        assertNull(url("51", null))
    }

    @Test
    fun `network and broadcast endings are never chosen`() {
        assertEquals("http://192.168.1.100:8091/", url("1921681100", "172.16.0.9"))
    }

    @Test
    fun `invalid input is refused`() {
        assertNull(url(""))
        assertNull(url("999.1.1.1"))
        assertNull(url("10.84.30.51:99999"))
        assertNull(url("10_84"))
    }

    @Test
    fun `a host name is kept as typed`() {
        assertEquals("http://localhost:8091/", url("localhost"))
    }

    @Test
    fun `the settings field shows the address without scheme and default port`() {
        assertEquals("10.84.30.51", ServerAddress.toDisplay("http://10.84.30.51:8091/"))
        assertEquals("127.0.0.1:8095", ServerAddress.toDisplay("http://127.0.0.1:8095/"))
    }
}
