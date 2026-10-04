using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.History;

public static class HistoryEndpoints
{
    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var p = app.MapGroup("/print-history").WithTags("Print History").RequireAuthorization();
        p.MapGet("", ListPrintAsync);
        p.MapPost("", CreatePrintAsync);
        p.MapPost("/{no:long}/reprint", ReprintAsync);
        p.MapGet("/export.csv", ExportPrintCsvAsync);

        var r = app.MapGroup("/rfid-gate-history").WithTags("RFID Gate History").RequireAuthorization();
        r.MapGet("", ListRfidAsync);
        r.MapGet("/export.csv", ExportRfidCsvAsync);
        return app;
    }

    private static async Task<IResult> ListPrintAsync(
        [FromServices] IHistoryService service, CancellationToken ct, [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null, [FromQuery] string? category = null, [FromQuery] int? status = null,
        [FromQuery] string? q = null, [FromQuery] string? sort = null, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? item = null) =>
        (await service.ListPrintAsync(ct: ct, dateFrom: dateFrom, dateTo: dateTo, category: category, status: status, q: q, sort: sort, page: page, pageSize: pageSize, item: item)).ToHttpResult();

    private static async Task<IResult> CreatePrintAsync(
        [FromServices] IHistoryService service, [FromBody] PrintHistoryWriteDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.CreatePrintAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ReprintAsync(
        [FromServices] IHistoryService service, long no, [FromBody] PrintReprintRequest body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.ReprintAsync(no: no, body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportPrintCsvAsync(
        [FromServices] IHistoryService service, CancellationToken ct, [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null, [FromQuery] string? category = null, [FromQuery] int? status = null,
        [FromQuery] string? q = null, [FromQuery] string? sort = null, [FromQuery] string? item = null) =>
        (await service.ExportPrintCsvAsync(ct: ct, dateFrom: dateFrom, dateTo: dateTo, category: category, status: status, q: q, sort: sort, item: item)).ToHttpResult();

    private static async Task<IResult> ListRfidAsync(
        [FromServices] IHistoryService service, CancellationToken ct, [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null, [FromQuery] string? q = null, [FromQuery] string? sort = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null) =>
        (await service.ListRfidAsync(ct: ct, dateFrom: dateFrom, dateTo: dateTo, q: q, sort: sort, page: page, pageSize: pageSize)).ToHttpResult();

    private static async Task<IResult> ExportRfidCsvAsync(
        [FromServices] IHistoryService service, CancellationToken ct, [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null, [FromQuery] string? q = null, [FromQuery] string? sort = null) =>
        (await service.ExportRfidCsvAsync(ct: ct, dateFrom: dateFrom, dateTo: dateTo, q: q, sort: sort)).ToHttpResult();
}
