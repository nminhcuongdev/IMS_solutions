using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Shipping;

public static class ShippingEndpoints
{
    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        var schedule = app.MapGroup("/shipping-schedule").WithTags("Shipping").RequireAuthorization();
        schedule.MapGet("", ListPlansAsync);
        schedule.MapPost("", CreatePlanAsync);
        schedule.MapPut("/status", ChangeStatusAsync);
        schedule.MapPost("/{soNo}/shortage", RegisterShortageAsync);
        schedule.MapGet("/export.csv", ExportPlansCsvAsync);

        var result = app.MapGroup("/shipping-result").WithTags("Shipping").RequireAuthorization();
        result.MapGet("", ListResultsAsync);
        result.MapPost("", CreateResultAsync);
        result.MapGet("/export.csv", ExportResultsCsvAsync);
        return app;
    }

    private static async Task<IResult> ListPlansAsync(
        [FromServices] IShippingService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] string? q = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null, [FromQuery] string? slipNo = null,
        [FromQuery] string? status = null) =>
        (await service.ListPlansAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, q: q, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, slipNo: slipNo, status: status)).ToHttpResult();

    private static async Task<IResult> CreatePlanAsync(
        [FromServices] IShippingService service, [FromBody] ShippingPlanWriteDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.CreatePlanAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ChangeStatusAsync(
        [FromServices] IShippingService service, [FromBody] ShippingStatusChangeDto body, CancellationToken ct) =>
        (await service.ChangeStatusAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> RegisterShortageAsync(
        [FromServices] IShippingService service, string soNo, [FromBody] ShippingShortageDto body,
        CancellationToken ct) =>
        (await service.RegisterShortageAsync(soNo: soNo, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> ListResultsAsync(
        [FromServices] IShippingService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null, [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null,
        [FromQuery] string? slipNo = null, [FromQuery] string? slipNoPresence = null) =>
        (await service.ListResultsAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, slipNo: slipNo, slipNoPresence: slipNoPresence)).ToHttpResult();

    private static async Task<IResult> CreateResultAsync(
        [FromServices] IShippingService service, [FromBody] ShippingResultWriteDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.CreateResultAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportPlansCsvAsync(
        [FromServices] IShippingService service, CancellationToken ct) =>
        (await service.ExportPlansCsvAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportResultsCsvAsync(
        [FromServices] IShippingService service, CancellationToken ct) =>
        (await service.ExportResultsCsvAsync(ct: ct)).ToHttpResult();
}
