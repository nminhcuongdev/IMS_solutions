package com.imssolutions.ht.data

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query
import kotlinx.serialization.json.JsonObject

interface ApiService {

    @GET("permissions/me")
    suspend fun myPermissions(): ApiSuccess<MyPermissions>

    @GET("production-order")
    suspend fun listProductionOrders(@Query("pageSize") pageSize: Int = 200, @Query("page") page: Int = 1,
        @Query("orderNo") orderNo: String? = null): ApiListSuccess<ProductionOrder>

    @GET("production-order")
    suspend fun listProductionLines(@Query("unit") unit: String = "line", @Query("pageSize") pageSize: Int = 200,
        @Query("page") page: Int = 1): ApiListSuccess<ProductionLine>

    @GET("production-order/{orderNo}")
    suspend fun productionOrder(@Path("orderNo") orderNo: String): ApiSuccess<ProductionOrderDetail>

    @POST("production-order/{orderNo}/results")
    suspend fun productionReceipt(@Path("orderNo") orderNo: String, @Body body: JsonObject): ApiSuccess<JsonObject>

    @POST("production-order/{orderNo}/results")
    suspend fun productionConsumption(@Path("orderNo") orderNo: String, @Body body: JsonObject): ApiSuccess<List<JsonObject>>

    @POST("production-order/{orderNo}/results")
    suspend fun productionReturn(@Path("orderNo") orderNo: String, @Body body: JsonObject): ApiSuccess<List<JsonObject>>

    @POST("production-order/{orderNo}/results")
    suspend fun productionOutput(@Path("orderNo") orderNo: String, @Body body: JsonObject): ApiSuccess<ProductionOutputResult>

    @POST("production-order/{orderNo}/results")
    suspend fun productionLabel(@Path("orderNo") orderNo: String, @Body body: JsonObject): ApiSuccess<JsonObject>

    @POST("auth/login")
    suspend fun login(@Body body: LoginRequest): ApiSuccess<LoginResponse>

    @POST("auth/logout")
    suspend fun logout()

    @GET("schedules/receiving")
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

    @PUT("schedules/receiving/status")
    suspend fun changeStatus(@Body body: StatusChangeRequest): ApiSuccess<StatusChangeResult>

    @POST("schedules/receiving/{poNo}/shortage")
    suspend fun registerShortage(
        @Path("poNo") poNo: String,
        @Body body: ShortageRequest,
    ): ApiSuccess<ReceivingPlan>

    @POST("receiving-result")
    suspend fun createReceivingResult(
        @Body body: ReceivingResultRequest,
    ): ApiSuccess<ReceivingResult>

    @GET("results/receiving")
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


    @GET("schedules/returns")
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


    @GET("schedules/shipping")
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


    @GET("schedules/picking")
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

    @GET("masters/items/{code}")
    suspend fun getItem(@Path("code") code: String): ApiSuccess<ItemBrief>

    @GET("masters/zones")
    suspend fun listZones(
        @Query("pageSize") pageSize: Int = 200,
        @Query("isActive") isActive: Boolean = true,
    ): ApiListSuccess<Zone>

    @GET("ht/printers")
    suspend fun listDevices(): ApiListSuccess<Device>

    @GET("languages")
    suspend fun languages(): ApiSuccess<List<LanguageOption>>

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
