package com.imssolutions.ht.data

import android.content.Context
import com.jakewharton.retrofit2.converter.kotlinx.serialization.asConverterFactory
import kotlinx.serialization.json.Json
import okhttp3.Interceptor
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.HttpException
import retrofit2.Retrofit
import com.imssolutions.ht.BuildConfig
import java.io.IOException
import java.util.concurrent.TimeUnit

class ApiException(
    val code: String,
    val httpStatus: Int,
    override val message: String,
) : Exception(message) {
    val isUnauthorized: Boolean get() = httpStatus == 401
}

object ApiClient {

    private val json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
        coerceInputValues = true
    }

    @Volatile private var service: ApiService? = null
    @Volatile private var tokenStore: TokenStore? = null

    fun init(context: Context) {
        if (service != null) return
        val store = TokenStore(context.applicationContext)
        tokenStore = store

        val auth = Interceptor { chain ->
            val token = store.tokenBlocking()
            val req = chain.request().newBuilder().apply {
                if (!token.isNullOrBlank()) header("Authorization", "Bearer $token")
                header("Accept", "application/json")
            }.build()
            chain.proceed(req)
        }

        val rewriteHost = Interceptor { chain ->
            val target = store.baseUrlBlocking().toHttpUrlOrNull()
            val req = chain.request()
            if (target == null) {
                chain.proceed(req)
            } else {
                val url = req.url.newBuilder()
                    .scheme(target.scheme)
                    .host(target.host)
                    .port(target.port)
                    .build()
                chain.proceed(req.newBuilder().url(url).build())
            }
        }

        val logging = HttpLoggingInterceptor().apply {
            level = if (BuildConfig.DEBUG) HttpLoggingInterceptor.Level.BASIC
                    else HttpLoggingInterceptor.Level.NONE
        }

        val client = OkHttpClient.Builder()
            .connectTimeout(15, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .addInterceptor(rewriteHost)
            .addInterceptor(auth)
            .addInterceptor(logging)
            .build()

        service = Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(client)
            .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
            .build()
            .create(ApiService::class.java)
    }

    val api: ApiService
        get() = service ?: error("ApiClient.init() chưa được gọi — xem ImsApp.onCreate()")

    val tokens: TokenStore
        get() = tokenStore ?: error("ApiClient.init() chưa được gọi")

    suspend fun <T> call(block: suspend ApiService.() -> T): T =
        try {
            api.block()
        } catch (e: HttpException) {
            throw e.toApiException()
        } catch (e: IOException) {
            throw ApiException("NET-002", 0, "Không kết nối được máy chủ. Kiểm tra Wi-Fi.")
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
