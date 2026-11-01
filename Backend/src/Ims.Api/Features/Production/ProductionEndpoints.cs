using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Production;

public static class ProductionEndpoints
{
    public static IEndpointRouteBuilder MapProductionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/production-order").WithTags("Production").RequireAuthorization();
        group.MapGet("", ListAsync);
        group.MapGet("/{orderNo}", GetAsync);
        group.MapGet("/{orderNo}/lines", LinesAsync);
        group.MapGet("/{orderNo}/works", WorksAsync);
        group.MapPut("/status", StatusAsync);
        group.MapPut("/picking", PickingAsync);
        group.MapPost("/{orderNo}/receipt", ReceiptAsync);
        group.MapPost("/{orderNo}/consumption", ConsumptionAsync);
        group.MapPost("/{orderNo}/return", ReturnAsync);
        group.MapPost("/{orderNo}/output", OutputAsync);
        group.MapPost("/{orderNo}/label", LabelAsync);
        app.MapPost("/production-consumption/{no:long}/reverse", ReverseConsumptionAsync).RequireAuthorization();
        app.MapPost("/production-output/{no:long}/reverse", ReverseOutputAsync).RequireAuthorization();
        app.MapPost("/production-return/{no:long}/reverse", ReverseReturnAsync).RequireAuthorization();
        app.MapGet("/production-result", ResultsAsync).RequireAuthorization();
        app.MapGet("/export/production-order", ExportOrdersAsync).RequireAuthorization();
        app.MapGet("/export/production-result", ExportResultsAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> ListAsync([FromServices] IProductionService service, CancellationToken ct,
        [FromQuery] string? unit = null, [FromQuery] string? orderNo = null, [FromQuery] string? itemCode = null,
        [FromQuery] string? itemName = null, [FromQuery] string? productionLotNo = null,
        [FromQuery] string? fgLotNo = null, [FromQuery] int? status = null,
        [FromQuery] DateOnly? startDateFrom = null, [FromQuery] DateOnly? startDateTo = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null, [FromQuery] string? sort = null) =>
        (await service.ListAsync(ct, unit, orderNo, itemCode, itemName, productionLotNo, fgLotNo, status, startDateFrom, startDateTo, page, pageSize, sort)).ToHttpResult();

    private static async Task<IResult> GetAsync([FromServices] IProductionService service, string orderNo, CancellationToken ct) =>
        (await service.GetAsync(orderNo, ct)).ToHttpResult();

    private static async Task<IResult> LinesAsync([FromServices] IProductionService service, string orderNo, CancellationToken ct) =>
        (await service.LinesAsync(orderNo, ct)).ToHttpResult();

    private static async Task<IResult> WorksAsync([FromServices] IProductionService service, string orderNo, CancellationToken ct) =>
        (await service.WorksAsync(orderNo, ct)).ToHttpResult();

    private static async Task<IResult> StatusAsync([FromServices] IProductionService service, [FromBody] StatusWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.StatusAsync(body, user, ct)).ToHttpResult();

    private static async Task<IResult> PickingAsync([FromServices] IProductionService service, [FromBody] PickingWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.PickingAsync(body, user, ct)).ToHttpResult();

    private static async Task<IResult> ReceiptAsync([FromServices] IProductionService service, string orderNo, [FromBody] ReceiptWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ReceiptAsync(orderNo, body, user, ct)).ToHttpResult();

    private static async Task<IResult> ConsumptionAsync([FromServices] IProductionService service, string orderNo, [FromBody] ConsumptionWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ConsumptionAsync(orderNo, body, user, ct)).ToHttpResult();

    private static async Task<IResult> ReturnAsync([FromServices] IProductionService service, string orderNo, [FromBody] ReturnWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ReturnAsync(orderNo, body, user, ct)).ToHttpResult();

    private static async Task<IResult> OutputAsync([FromServices] IProductionService service, string orderNo, [FromBody] OutputWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.OutputAsync(orderNo, body, user, ct)).ToHttpResult();

    private static async Task<IResult> LabelAsync([FromServices] IProductionService service, string orderNo, [FromBody] LabelWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.LabelAsync(orderNo, body, user, ct)).ToHttpResult();

    private static async Task<IResult> ReverseConsumptionAsync([FromServices] IProductionService service, long no, [FromBody] ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ReverseConsumptionAsync(no, body, user, ct)).ToHttpResult();

    private static async Task<IResult> ReverseOutputAsync([FromServices] IProductionService service, long no, [FromBody] ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ReverseOutputAsync(no, body, user, ct)).ToHttpResult();

    private static async Task<IResult> ReverseReturnAsync([FromServices] IProductionService service, long no, [FromBody] ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ReverseReturnAsync(no, body, user, ct)).ToHttpResult();

    private static async Task<IResult> ResultsAsync([FromServices] IProductionService service, CancellationToken ct,
        [FromQuery] string? view = null, [FromQuery] string? type = null, [FromQuery] string? orderNo = null,
        [FromQuery] string? itemCode = null, [FromQuery] string? lotNo = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null, [FromQuery] string? sort = null,
        [FromQuery] string? itemName = null, [FromQuery] bool showReversed = false) =>
        (await service.ResultsAsync(ct, view, type, orderNo, itemCode, lotNo, dateFrom, dateTo, page, pageSize, sort, itemName, showReversed)).ToHttpResult();

    private static async Task<IResult> ExportOrdersAsync([FromServices] IProductionService service, CancellationToken ct) =>
        (await service.ExportOrdersAsync(ct)).ToHttpResult();

    private static async Task<IResult> ExportResultsAsync([FromServices] IProductionService service, CancellationToken ct) =>
        (await service.ExportResultsAsync(ct)).ToHttpResult();
}
