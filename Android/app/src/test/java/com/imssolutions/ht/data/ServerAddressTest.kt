package com.imssolutions.ht.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
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

    @Test
    fun `https is kept and defaults to its own port`() {
        assertEquals("https://ims-api.example.com/", url("https://ims-api.example.com"))
        assertEquals("https://ims-api.example.com/", url("https://ims-api.example.com/"))
        assertEquals("https://ims-api.example.com/", url("HTTPS://Ims-Api.Example.com"))
        assertEquals("https://ims-api.example.com/", url("https://ims-api.example.com:443"))
    }

    @Test
    fun `https keeps an explicit port`() {
        assertEquals("https://ims.example.com:8091/", url("https://ims.example.com:8091"))
    }

    @Test
    fun `https accepts an ip address`() {
        assertEquals("https://10.84.30.51/", url("https://10.84.30.51"))
    }

    @Test
    fun `a host name without a scheme stays on http`() {
        assertEquals("http://ims.example.com:8091/", url("ims.example.com"))
    }

    @Test
    fun `invalid https input is refused`() {
        assertNull(url("https://"))
        assertNull(url("https://ims.example.com:99999"))
        assertNull(url("https://ims.example.com/api"))
    }

    @Test
    fun `the settings field keeps the https scheme`() {
        assertEquals("https://ims-api.example.com", ServerAddress.toDisplay("https://ims-api.example.com/"))
        assertEquals("https://ims.example.com:8091", ServerAddress.toDisplay("https://ims.example.com:8091/"))
    }

    @Test
    fun `what the field shows saves back to the same url`() {
        listOf(
            "https://ims-api.example.com/",
            "https://ims.example.com:8091/",
            "http://10.84.30.51:8091/",
            "http://127.0.0.1:8095/",
        ).forEach { saved ->
            assertEquals(saved, url(ServerAddress.toDisplay(saved)))
        }
    }

    @Test
    fun `without a port a LAN address tries 8091 first and a cloud address tries 443 first`() {
        assertEquals(
            listOf("http://10.84.30.62:8091/", "http://10.84.30.62:443/", "http://10.84.30.62:80/"),
            ServerAddress.candidates("10.84.30.62", device),
        )
        assertEquals(
            listOf("http://203.0.113.10:443/", "http://203.0.113.10:8091/", "http://203.0.113.10:80/"),
            ServerAddress.candidates("http://203.0.113.10", device),
        )
        assertEquals("http://ims.example.com:443/", ServerAddress.candidates("ims.example.com", null).first())
    }

    @Test
    fun `a typed port or https is used as is with nothing to try`() {
        assertEquals(listOf("http://203.0.113.10:8091/"), ServerAddress.candidates("203.0.113.10:8091", device))
        assertEquals(listOf("https://ims-api.example.com/"), ServerAddress.candidates("https://ims-api.example.com", device))
        assertEquals(emptyList<String>(), ServerAddress.candidates("203.0.113.10:99999", device))
    }

    @Test
    fun `remembered servers put the latest first without repeats and keep at most five`() {
        val cloud = "http://203.0.113.10:443/"
        val local = "http://10.84.30.62:8091/"
        assertEquals(listOf(cloud, local), ServerAddress.remember(listOf(local, cloud), cloud))
        val many = (1..5).map { "http://10.0.0.$it:8091/" }
        assertEquals(listOf(cloud) + many.take(4), ServerAddress.remember(many, cloud))
    }

    @Test
    fun `the same server is recognised whatever the trailing slash or case`() {
        assertTrue(ServerAddress.sameServer("http://Host:8091/", "http://host:8091"))
        assertFalse(ServerAddress.sameServer("http://203.0.113.10:443/", "http://10.84.30.62:8091/"))
    }
}
