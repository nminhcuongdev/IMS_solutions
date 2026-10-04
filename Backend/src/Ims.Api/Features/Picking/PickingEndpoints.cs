using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Picking;

public static class PickingEndpoints
{
    public static IEndpointRouteBuilder MapPickingEndpoints(this IEndpointRouteBuilder app)
    {
        var schedule = app.MapGroup("/picking-schedule").WithTags("Picking").RequireAuthorization();
        schedule.MapGet("", ListPlansAsync);
        schedule.MapPost("", CreatePlanAsync);
        schedule.MapPut("/status", ChangeStatusAsync);
        schedule.MapGet("/export.csv", ExportPlansCsvAsync);

        var result = app.MapGroup("/picking-result").WithTags("Picking").RequireAuthorization();
        result.MapGet("", ListResultsAsync);
        result.MapPost("", CreateResultAsync);
        result.MapGet("/export.csv", ExportResultsCsvAsync);
        return app;
    }

    private static async Task<IResult> ListPlansAsync(
        [FromServices] IPickingService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] string? q = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null, [FromQuery] string? soNo = null,
        [FromQuery] string? status = null) =>
        (await service.ListPlansAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, q: q, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, soNo: soNo, status: status)).ToHttpResult();

    private static async Task<IResult> CreatePlanAsync(
        [FromServices] IPickingService service, [FromBody] PickingPlanWriteDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.CreatePlanAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ChangeStatusAsync(
        [FromServices] IPickingService service, [FromBody] PickingStatusChangeDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.ChangeStatusAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ListResultsAsync(
        [FromServices] IPickingService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null, [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null,
        [FromQuery] string? soNo = null) =>
        (await service.ListResultsAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, soNo: soNo)).ToHttpResult();

    private static async Task<IResult> CreateResultAsync(
        [FromServices] IPickingService service, [FromBody] PickingResultWriteDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.CreateResultAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportPlansCsvAsync(
        [FromServices] IPickingService service, CancellationToken ct) =>
        (await service.ExportPlansCsvAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportResultsCsvAsync(
        [FromServices] IPickingService service, CancellationToken ct) =>
        (await service.ExportResultsCsvAsync(ct: ct)).ToHttpResult();
}
