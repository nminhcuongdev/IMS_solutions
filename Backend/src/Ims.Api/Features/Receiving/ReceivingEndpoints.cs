using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Receiving;

public static class ReceivingEndpoints
{
    public static IEndpointRouteBuilder MapReceivingEndpoints(this IEndpointRouteBuilder app)
    {
        var schedule = app.MapGroup("/receiving-schedule").WithTags("Receiving").RequireAuthorization();
        schedule.MapGet("", ListPlansAsync);
        schedule.MapPost("", CreatePlanAsync);
        schedule.MapPut("/status", ChangeStatusAsync);
        schedule.MapPost("/{poNo}/shortage", RegisterShortageAsync);
        schedule.MapGet("/export.csv", ExportPlansCsvAsync);

        var result = app.MapGroup("/receiving-result").WithTags("Receiving").RequireAuthorization();
        result.MapGet("", ListResultsAsync);
        result.MapPost("", CreateResultAsync);
        result.MapGet("/export.csv", ExportResultsCsvAsync);
        return app;
    }

    private static async Task<IResult> ListPlansAsync(
        [FromServices] IReceivingService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] string? q = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null, [FromQuery] string? poNo = null,
        [FromQuery] string? status = null) =>
        (await service.ListPlansAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, q: q, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, poNo: poNo, status: status)).ToHttpResult();

    private static async Task<IResult> CreatePlanAsync(
        [FromServices] IReceivingService service, [FromBody] ReceivingPlanWriteDto body, CancellationToken ct) =>
        (await service.CreatePlanAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> ChangeStatusAsync(
        [FromServices] IReceivingService service, [FromBody] ReceivingStatusChangeDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.ChangeStatusAsync(body: body, ct: ct, user: user)).ToHttpResult();

    private static async Task<IResult> RegisterShortageAsync(
        [FromServices] IReceivingService service, string poNo, [FromBody] ReceivingShortageDto body,
        CancellationToken ct) =>
        (await service.RegisterShortageAsync(poNo: poNo, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> ListResultsAsync(
        [FromServices] IReceivingService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null, [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null,
        [FromQuery] string? poNo = null, [FromQuery] string? poNoPresence = null) =>
        (await service.ListResultsAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, poNo: poNo, poNoPresence: poNoPresence)).ToHttpResult();

    private static async Task<IResult> CreateResultAsync(
        [FromServices] IReceivingService service, [FromBody] ReceivingResultWriteDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.CreateResultAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportPlansCsvAsync(
        [FromServices] IReceivingService service, CancellationToken ct) =>
        (await service.ExportPlansCsvAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportResultsCsvAsync(
        [FromServices] IReceivingService service, CancellationToken ct) =>
        (await service.ExportResultsCsvAsync(ct: ct)).ToHttpResult();
}
