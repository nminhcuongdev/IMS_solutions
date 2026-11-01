package com.imssolutions.rfid.data

import java.net.Inet4Address
import java.net.NetworkInterface

object ServerAddress {

    const val DEFAULT_PORT = 8091

    fun toBaseUrl(input: String, deviceIp: String?, defaultPort: Int = DEFAULT_PORT): String? {
        var s = input.trim().lowercase()
        s = s.removePrefix("http://").removePrefix("https://").trimEnd('/')
        if (s.isEmpty()) return null
        var port = defaultPort
        val colon = s.lastIndexOf(':')
        if (colon >= 0) {
            port = s.substring(colon + 1).toIntOrNull()?.takeIf { it in 1..65535 } ?: return null
            s = s.substring(0, colon)
        }
        val host = resolveHost(s, deviceIp) ?: return null
        return "http://$host:$port/"
    }

    fun toDisplay(baseUrl: String, defaultPort: Int = DEFAULT_PORT): String {
        var s = baseUrl.trim().removePrefix("http://").removePrefix("https://").trimEnd('/')
        val colon = s.lastIndexOf(':')
        if (colon >= 0 && s.substring(colon + 1).toIntOrNull() == defaultPort) s = s.substring(0, colon)
        return s
    }

    fun resolveHost(text: String, deviceIp: String?): String? {
        if (text.isEmpty()) return null
        if (text.any { it.isLetter() }) {
            return text.takeIf { t -> t.all { it.isLetterOrDigit() || it == '.' || it == '-' } }
        }
        if (!text.all { it.isDigit() || it == '.' }) return null
        val device = deviceIp?.let(::octetsOf)?.takeIf { it.size == 4 }
        val typed = if (text.contains('.')) {
            listOf(text.split('.').map { octet(it) ?: return null })
        } else {
            splits(text)
        }
        val candidates = typed.mapNotNull { part ->
            when {
                part.size == 4 -> part
                part.size in 1..3 && device != null -> device.take(4 - part.size) + part
                else -> null
            }
        }.filter(::isUsableHost)
        val best = candidates.withIndex().maxWithOrNull(
            compareBy<IndexedValue<List<Int>>>(
                { commonPrefix(it.value, device) },
                { if (isPrivate(it.value)) 1 else 0 },
                { -it.index },
            ),
        ) ?: return null
        return best.value.joinToString(".")
    }

    private fun splits(digits: String): List<List<Int>> {
        val out = mutableListOf<List<Int>>()
        fun walk(rest: String, acc: List<Int>) {
            if (rest.isEmpty()) {
                if (acc.isNotEmpty()) out += acc
                return
            }
            if (acc.size == 4) return
            for (len in 1..minOf(3, rest.length)) {
                val v = octet(rest.substring(0, len)) ?: continue
                walk(rest.substring(len), acc + v)
            }
        }
        walk(digits, emptyList())
        return out.sortedByDescending { it.size }
    }

    private fun octet(s: String): Int? {
        if (s.isEmpty() || s.length > 3) return null
        if (s.length > 1 && s[0] == '0') return null
        return s.toIntOrNull()?.takeIf { it in 0..255 }
    }

    private fun octetsOf(ip: String): List<Int>? =
        ip.split('.').map { it.toIntOrNull()?.takeIf { v -> v in 0..255 } ?: return null }

    private fun isUsableHost(o: List<Int>): Boolean =
        o.size == 4 && o[0] in 1..223 && o[3] in 1..254

    private fun isPrivate(o: List<Int>): Boolean =
        o[0] == 10 || (o[0] == 172 && o[1] in 16..31) || (o[0] == 192 && o[1] == 168) || o[0] == 127

    private fun commonPrefix(o: List<Int>, device: List<Int>?): Int {
        if (device == null) return 0
        var n = 0
        while (n < 4 && o[n] == device[n]) n++
        return n
    }

    fun deviceIpv4(): String? = runCatching {
        NetworkInterface.getNetworkInterfaces().toList()
            .filter { it.isUp && !it.isLoopback }
            .sortedByDescending { it.name.startsWith("wlan") }
            .flatMap { it.inetAddresses.toList() }
            .filterIsInstance<Inet4Address>()
            .firstOrNull { it.isSiteLocalAddress }
            ?.hostAddress
    }.getOrNull()
}
