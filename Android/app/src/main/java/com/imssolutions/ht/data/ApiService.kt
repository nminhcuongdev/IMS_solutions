package com.imssolutions.ht.data

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface ApiService {

    @POST("auth/login")
    suspend fun login(@Body body: LoginRequest): ApiSuccess<LoginResponse>

    @POST("auth/logout")
    suspend fun logout()

    @GET("receiving-schedule")
    suspend fun listReceivingSchedule(
        @Query("page") page: Int? = null,
        @Query("pageSize") pageSize: Int? = null,
        @Query("sort") sort: String? = null,
        @Query("q") q: String? = null,
        @Query("dateFrom") dateFrom: String? = null,
        @Query("dateTo") dateTo: String? = null,
        @Query("poNo") poNo: String? = null,
        @Query("status") status: String? = null,
    ): ApiListSuccess<ReceivingPlan>

    @PUT("receiving-schedule/status")
    suspend fun changeStatus(@Body body: StatusChangeRequest): ApiSuccess<StatusChangeResult>

    @POST("receiving-schedule/{poNo}/shortage")
    suspend fun registerShortage(
        @Path("poNo") poNo: String,
        @Body body: ShortageRequest,
    ): ApiSuccess<ReceivingPlan>

    @POST("receiving-result")
    suspend fun createReceivingResult(
        @Body body: ReceivingResultRequest,
    ): ApiSuccess<ReceivingResult>

    @GET("receiving-result")
    suspend fun listReceivingResult(
        @Query("page") page: Int? = null,
        @Query("pageSize") pageSize: Int? = null,
        @Query("sort") sort: String? = null,
        @Query("poNo") poNo: String? = null,
        @Query("poNoPresence") poNoPresence: String? = null,
    ): ApiListSuccess<ReceivingResult>

    @GET("ht/config")
    suspend fun htConfig(): ApiSuccess<HtConfig>

    @GET("ht/lots")
    suspend fun htLots(
        @Query("q") q: String? = null,
        @Query("itemCode") itemCode: String? = null,
    ): ApiListSuccess<HtLot>

    @POST("print-history")
    suspend fun createPrintHistory(@Body body: PrintHistoryRequest): ApiSuccess<PrintHistoryRow>

    @POST("print/preview")
    suspend fun printPreview(@Body body: PrintPreviewRequest): ApiSuccess<PrintPreviewResponse>

    @POST("print/label")
    suspend fun printLabel(@Body body: PrintLabelRequest): ApiSuccess<PrintHistoryRow>


    @GET("returns-schedule")
    suspend fun listReturnsSchedule(
        @Query("page") page: Int? = null,
        @Query("pageSize") pageSize: Int? = null,
        @Query("sort") sort: String? = null,
        @Query("q") q: String? = null,
        @Query("dateFrom") dateFrom: String? = null,
        @Query("dateTo") dateTo: String? = null,
        @Query("returnNo") returnNo: String? = null,
        @Query("status") status: String? = null,
    ): ApiListSuccess<ReturnPlan>

    @POST("return-result")
    suspend fun createReturnResult(@Body body: ReturnResultRequest): ApiSuccess<ReturnResult>


    @GET("shipping-schedule")
    suspend fun listShippingSchedule(
        @Query("page") page: Int? = null,
        @Query("pageSize") pageSize: Int? = null,
        @Query("sort") sort: String? = null,
        @Query("q") q: String? = null,
        @Query("dateFrom") dateFrom: String? = null,
        @Query("dateTo") dateTo: String? = null,
        @Query("slipNo") slipNo: String? = null,
        @Query("status") status: String? = null,
    ): ApiListSuccess<ShippingPlan>

    @POST("shipping-result")
    suspend fun createShippingResult(@Body body: ShippingResultRequest): ApiSuccess<ShippingResult>


    @GET("picking-schedule")
    suspend fun listPickingSchedule(
        @Query("page") page: Int? = null,
        @Query("pageSize") pageSize: Int? = null,
        @Query("sort") sort: String? = null,
        @Query("q") q: String? = null,
        @Query("dateFrom") dateFrom: String? = null,
        @Query("dateTo") dateTo: String? = null,
        @Query("soNo") soNo: String? = null,
        @Query("status") status: String? = null,
    ): ApiListSuccess<PickingPlan>

    @POST("picking-result")
    suspend fun createPickingResult(@Body body: PickingResultRequest): ApiSuccess<PickingResult>


    @GET("stock")
    suspend fun listStockByItem(
        @Query("groupBy") groupBy: String = "item",
        @Query("page") page: Int? = null,
        @Query("pageSize") pageSize: Int? = null,
        @Query("q") q: String? = null,
        @Query("warehouse") warehouse: String? = null,
    ): ApiListSuccess<StockGroup>

    @GET("stock")
    suspend fun listStockLines(
        @Query("itemCode") itemCode: String? = null,
        @Query("page") page: Int? = null,
        @Query("pageSize") pageSize: Int? = null,
        @Query("sort") sort: String? = null,
        @Query("q") q: String? = null,
    ): ApiListSuccess<StockLine>

    @POST("stock/disposal")
    suspend fun disposeStock(@Body body: StockDisposalRequest): ApiSuccess<StockLine>

    @POST("stock/transfer")
    suspend fun transferStock(@Body body: StockTransferRequest): ApiSuccess<StockLine>


    @GET("stocktaking/session")
    suspend fun stocktakingSession(): ApiSuccess<StocktakingSession?>

    @GET("stocktaking/items")
    suspend fun stocktakingItems(
        @Query("locationCode") locationCode: String,
    ): ApiSuccess<List<StocktakingGroup>>

    @POST("stocktaking/result")
    suspend fun saveStocktakingResult(
        @Body body: StocktakingResultRequest,
    ): ApiSuccess<StocktakingGroup>

    @GET("masters/locations")
    suspend fun listLocations(
        @Query("pageSize") pageSize: Int = 200,
        @Query("q") q: String? = null,
        @Query("zoneCode") zoneCode: String? = null,
        @Query("type") type: String? = null,
    ): ApiListSuccess<Location>

    @GET("masters/zones")
    suspend fun listZones(
        @Query("pageSize") pageSize: Int = 200,
        @Query("isActive") isActive: Boolean = true,
    ): ApiListSuccess<Zone>

    @GET("ht/printers")
    suspend fun listDevices(): ApiListSuccess<Device>

    @GET("language-data/labels")
    suspend fun languageLabels(
        @Query("lang") lang: String? = null,
        @Query("deviceScope") deviceScope: String = "HT",
    ): ApiSuccess<Map<String, String>>

    @GET("masters/reasons")
    suspend fun listReasons(
        @Query("category") category: String = "Shortage",
        @Query("pageSize") pageSize: Int = 200,
        @Query("isActive") isActive: Boolean = true,
    ): ApiListSuccess<Reason>
}
