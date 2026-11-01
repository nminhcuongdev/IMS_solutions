using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Returns;

public static class ReturnsEndpoints
{
    public static IEndpointRouteBuilder MapReturnsEndpoints(this IEndpointRouteBuilder app)
    {
        var schedule = app.MapGroup("/returns-schedule").WithTags("Returns").RequireAuthorization();
        schedule.MapGet("", ListPlansAsync);
        schedule.MapPost("", CreatePlanAsync);
        schedule.MapPut("/status", ChangeStatusAsync);
        schedule.MapPost("/{returnNo}/shortage", RegisterShortageAsync);
        schedule.MapGet("/export.csv", ExportPlansCsvAsync);

        var result = app.MapGroup("/return-result").WithTags("Returns").RequireAuthorization();
        result.MapGet("", ListResultsAsync);
        result.MapPost("", CreateResultAsync);
        return app;
    }

    private static async Task<IResult> ListPlansAsync(
        [FromServices] IReturnsService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] string? q = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null, [FromQuery] string? returnNo = null,
        [FromQuery] string? status = null) =>
        (await service.ListPlansAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, q: q, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, returnNo: returnNo, status: status)).ToHttpResult();

    private static async Task<IResult> CreatePlanAsync(
        [FromServices] IReturnsService service, [FromBody] ReturnPlanWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.CreatePlanAsync(body: body, ct: ct, user: user)).ToHttpResult();

    private static async Task<IResult> ChangeStatusAsync(
        [FromServices] IReturnsService service, [FromBody] ReturnStatusChangeDto body, CancellationToken ct) =>
        (await service.ChangeStatusAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> RegisterShortageAsync(
        [FromServices] IReturnsService service, string returnNo, [FromBody] ReturnShortageDto body,
        ClaimsPrincipal user, CancellationToken ct) =>
        (await service.RegisterShortageAsync(returnNo: returnNo, body: body, ct: ct, user: user)).ToHttpResult();

    private static async Task<IResult> ListResultsAsync(
        [FromServices] IReturnsService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null, [FromQuery] string? itemCode = null, [FromQuery] string? returnNo = null) =>
        (await service.ListResultsAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, returnNo: returnNo)).ToHttpResult();

    private static async Task<IResult> CreateResultAsync(
        [FromServices] IReturnsService service, [FromBody] ReturnResultWriteDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.CreateResultAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportPlansCsvAsync(
        [FromServices] IReturnsService service, CancellationToken ct) =>
        (await service.ExportPlansCsvAsync(ct: ct)).ToHttpResult();
}
