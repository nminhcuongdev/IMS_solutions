package com.imssolutions.rfid.data

import com.jakewharton.retrofit2.converter.kotlinx.serialization.asConverterFactory
import kotlinx.serialization.json.Json
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.HttpException
import retrofit2.Retrofit
import com.imssolutions.rfid.BuildConfig
import java.io.IOException
import java.util.concurrent.TimeUnit

class ApiClient(private val prefs: Prefs) {

    @Volatile var token: String? = null

    private val json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
        coerceInputValues = true
    }

    private val api: ImsApi = run {
        val target = Interceptor { chain ->
            val base = prefs.serverUrl.toHttpUrlOrNull()
            val req = chain.request()
            val url = if (base == null) req.url else req.url.newBuilder()
                .scheme(base.scheme).host(base.host).port(base.port).build()
            val builder = req.newBuilder().url(url).header("Accept", "application/json")
            token?.takeIf { it.isNotBlank() }?.let { builder.header("Authorization", "Bearer $it") }
            chain.proceed(builder.build())
        }
        val logging = HttpLoggingInterceptor().apply {
            level = if (BuildConfig.DEBUG) HttpLoggingInterceptor.Level.BASIC else HttpLoggingInterceptor.Level.NONE
        }
        val client = OkHttpClient.Builder()
            .connectTimeout(10, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .addInterceptor(target)
            .addInterceptor(logging)
            .build()
        Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(client)
            .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
            .build()
            .create(ImsApi::class.java)
    }

    suspend fun <T> call(block: suspend ImsApi.() -> T): T =
        try {
            api.block()
        } catch (e: HttpException) {
            throw e.toApiException()
        } catch (e: IOException) {
            throw ApiException("NET-002", 0, e.message ?: "")
        }

    private fun HttpException.toApiException(): ApiException {
        val raw = response()?.errorBody()?.string()
        if (!raw.isNullOrBlank()) {
            runCatching { json.decodeFromString<ApiErrorEnvelope>(raw) }
                .getOrNull()?.error
                ?.takeIf { it.code.isNotBlank() }
                ?.let { return ApiException(it.code, it.httpStatus, it.message) }
        }
        return ApiException("HTTP-${code()}", code(), message())
    }
}
