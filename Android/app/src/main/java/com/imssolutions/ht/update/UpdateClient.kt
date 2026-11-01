package com.imssolutions.ht.update

import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.File
import java.io.FileOutputStream
import java.io.IOException
import java.util.concurrent.TimeUnit

class UpdateClient(private val baseUrl: String, private val deviceId: String, private val installedCode: Long) {

    class HttpStatusException(val status: Int) : IOException("HTTP $status")

    data class Fetched(val doc: VersionJson, val lastModified: Long?)

    private val client = OkHttpClient.Builder()
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .build()

    private fun url(name: String) = baseUrl.trimEnd('/') + "/dl/" + name

    fun fetchVersionJson(): Fetched {
        val req = Request.Builder()
            .url(url("version.json"))
            .header("X-Device-Id", deviceId)
            .header("X-App-Version", installedCode.toString())
            .header("Cache-Control", "no-cache")
            .build()
        client.newCall(req).execute().use { res ->
            if (!res.isSuccessful) throw HttpStatusException(res.code)
            val body = res.body?.string().orEmpty()
            val lastModified = res.headers.getDate("Last-Modified")?.time
            return Fetched(UpdateRules.parse(body), lastModified)
        }
    }

    fun totalSize(apkFile: String): Long? {
        val req = Request.Builder().url(url(apkFile)).head().header("X-Device-Id", deviceId).build()
        client.newCall(req).execute().use { res ->
            if (!res.isSuccessful) throw HttpStatusException(res.code)
            return res.header("Content-Length")?.toLongOrNull()
        }
    }

    fun download(apkFile: String, part: File, onProgress: (Long, Long) -> Unit) {
        var offset = if (part.exists()) part.length() else 0L
        val builder = Request.Builder().url(url(apkFile)).header("X-Device-Id", deviceId)
        if (offset > 0) builder.header("Range", "bytes=$offset-")
        client.newCall(builder.build()).execute().use { res ->
            if (res.code == 416 && offset > 0) {
                part.delete()
                return download(apkFile, part, onProgress)
            }
            if (!res.isSuccessful) throw HttpStatusException(res.code)
            val append = res.code == 206
            if (!append) offset = 0
            val body = res.body ?: throw IOException("empty body")
            val total = offset + body.contentLength().coerceAtLeast(0)
            FileOutputStream(part, append).use { out ->
                body.byteStream().use { input ->
                    val buf = ByteArray(64 * 1024)
                    var done = offset
                    var lastReport = 0L
                    while (true) {
                        val n = input.read(buf)
                        if (n < 0) break
                        out.write(buf, 0, n)
                        done += n
                        if (done - lastReport >= 256 * 1024) {
                            lastReport = done
                            onProgress(done, total)
                        }
                    }
                    onProgress(done, total)
                }
            }
        }
    }
}
