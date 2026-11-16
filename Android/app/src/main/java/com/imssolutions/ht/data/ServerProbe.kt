package com.imssolutions.ht.data

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

object ServerProbe {

    private const val TIMEOUT_S = 4L

    private val client = OkHttpClient.Builder()
        .connectTimeout(TIMEOUT_S, TimeUnit.SECONDS)
        .readTimeout(TIMEOUT_S, TimeUnit.SECONDS)
        .build()

    suspend fun answers(baseUrl: String): Boolean = withContext(Dispatchers.IO) {
        runCatching {
            val req = Request.Builder().url(baseUrl + "languages").header("Accept", "application/json").build()
            client.newCall(req).execute().use { res ->
                res.header("Content-Type").orEmpty().contains("json", ignoreCase = true) &&
                    res.body?.string().orEmpty().contains("\"success\"")
            }
        }.getOrDefault(false)
    }

    suspend fun firstAnswering(candidates: List<String>): String? =
        candidates.firstOrNull { answers(it) }
}
