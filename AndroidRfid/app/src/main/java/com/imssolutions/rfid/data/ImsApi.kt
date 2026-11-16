package com.imssolutions.rfid.data

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST

interface ImsApi {
    @POST("auth/login")
    suspend fun login(@Body body: LoginRequest): ApiSuccess<LoginResponse>

    @POST("auth/logout")
    suspend fun logout()

    @GET("stock-scan")
    suspend fun stockScan(): ApiSuccess<StockScanBoardDto>

    @POST("stock-scan/read")
    suspend fun read(@Body body: StockScanReadRequest): ApiSuccess<StockScanReadResultDto>

    @POST("stock-scan/register/lookup")
    suspend fun lookupEpcs(@Body body: EpcLookupRequest): ApiSuccess<EpcLookupResultDto>

    @POST("stock-scan/register")
    suspend fun registerEpcs(@Body body: EpcRegisterRequest): ApiSuccess<EpcRegisterResultDto>
}
