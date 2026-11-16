using Ims.Api.Features.Permissions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.History;

public static class HistoryEndpoints
{
    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var p = app.MapGroup("/print-history").WithTags("Print History").RequireAuthorization();
        p.MapGet("", ListPrintAsync).RequirePermission("print_history.Open");
        p.MapPost("", CreatePrintAsync).RequirePermission("rcv_import.Label", "returns.Label", "shp_import.Label", "rcv_result.Reprint", "shp_result.Reprint", "print_history.Reprint", "ht_receiving.Open", "ht_returns.Open", "ht_shipping.Open", "ht_prd_output.Open");
        p.MapGet("/{no:long}/reprint-data", ReprintDataAsync).RequirePermission("print_history.Reprint", "rcv_result.Reprint", "shp_result.Reprint");
        p.MapPost("/{no:long}/reprint", ReprintAsync).RequirePermission("print_history.Reprint");
        app.MapGet("/export/print-history", ExportPrintCsvAsync).WithTags("Export").RequireAuthorization().RequirePermission("print_history.Export");

        app.MapPost("/results/receiving/{no:long}/reprint", (IHistoryService s, long no, PrintReprintRequest b, ClaimsPrincipal u, CancellationToken ct) => ResultReprintAsync(s, "receiving", no, b, u, ct))
            .WithTags("Print History").RequireAuthorization().RequireModule("receiving").RequirePermission("rcv_result.Reprint");
        app.MapPost("/results/returns/{no:long}/reprint", (IHistoryService s, long no, PrintReprintRequest b, ClaimsPrincipal u, CancellationToken ct) => ResultReprintAsync(s, "returns", no, b, u, ct))
            .WithTags("Print History").RequireAuthorization().RequireModule("returns").RequirePermission("rcv_result.Reprint");
        app.MapPost("/results/shipping/{no:long}/reprint", (IHistoryService s, long no, PrintReprintRequest b, ClaimsPrincipal u, CancellationToken ct) => ResultReprintAsync(s, "shipping", no, b, u, ct))
            .WithTags("Print History").RequireAuthorization().RequireModule("shipping").RequirePermission("shp_result.Reprint");

        var r = app.MapGroup("/rfid-gate-history").WithTags("RFID Gate History").RequireAuthorization().RequireModule("rfid");
        r.MapGet("", ListRfidAsync).RequirePermission("rfid_gate_history.Open");
        app.MapGet("/export/rfid-gate-history", ExportRfidCsvAsync).WithTags("Export").RequireAuthorization().RequireModule("rfid").RequirePermission("rfid_gate_history.Export");
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

    private static async Task<IResult> ResultReprintAsync(IHistoryService service, string type, long no, PrintReprintRequest body,
        ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ResultReprintAsync(type, no, body, user, ct)).ToHttpResult();

    private static async Task<IResult> ReprintDataAsync([FromServices] IHistoryService service, long no, CancellationToken ct) =>
        (await service.ReprintDataAsync(no, ct)).ToHttpResult();

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
