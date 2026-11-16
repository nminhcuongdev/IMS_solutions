using Ims.Api.Features.Permissions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Production;

public static class ProductionEndpoints
{
    public static IEndpointRouteBuilder MapProductionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/production-order").WithTags("Production").RequireAuthorization().RequireModule("production");
        group.MapGet("", ListAsync).RequirePermission("prd_import.Open", "ht_prd_receipt.Open", "ht_prd_input.Open", "ht_prd_return.Open", "ht_prd_output.Open");
        group.MapGet("/{orderNo}", GetAsync).RequirePermission("prd_import.Open", "prd_result.Open", "ht_prd_receipt.Open", "ht_prd_input.Open", "ht_prd_return.Open", "ht_prd_output.Open");
        group.MapPut("/status", StatusAsync).RequirePermission("prd_import.StatusChange");
        group.MapPut("/picking", PickingAsync).RequirePermission("prd_import.MaterialPicking");
        group.MapPost("/{orderNo}/results", RegisterAsync).RequirePermission("ht_prd_receipt.Open", "ht_prd_input.Open", "ht_prd_return.Open", "ht_prd_output.Open");
        app.MapPost("/production-result/{no:long}/reverse", ReverseAsync).RequireAuthorization().RequireModule("production").RequirePermission("prd_result.Reverse");
        app.MapGet("/production-result", ResultsAsync).RequireAuthorization().RequireModule("production").RequirePermission("prd_result.Open");
        app.MapGet("/export/production-order", ExportOrdersAsync).RequireAuthorization().RequireModule("production").RequirePermission("prd_import.Export");
        app.MapGet("/export/production-result", ExportResultsAsync).RequireAuthorization().RequireModule("production").RequirePermission("prd_result.Export");
        return app;
    }

    private static async Task<IResult> ListAsync([FromServices] IProductionService service, CancellationToken ct,
        [FromQuery] string? unit = null, [FromQuery] string? orderNo = null, [FromQuery] string? itemCode = null,
        [FromQuery] string? itemName = null, [FromQuery] string? productionLotNo = null,
        [FromQuery] string? fgLotNo = null, [FromQuery] int? status = null,
        [FromQuery] DateOnly? startDateFrom = null, [FromQuery] DateOnly? startDateTo = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null, [FromQuery] string? sort = null) =>
        (await service.ListAsync(ct, unit, orderNo, itemCode, itemName, productionLotNo, fgLotNo, status, startDateFrom, startDateTo, page, pageSize, sort)).ToHttpResult();

    private static async Task<IResult> GetAsync([FromServices] IProductionService service, string orderNo, CancellationToken ct, [FromQuery] string? processCode = null) =>
        (await service.GetAsync(orderNo, ct, processCode)).ToHttpResult();

    private static async Task<IResult> StatusAsync([FromServices] IProductionService service, [FromBody] StatusWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.StatusAsync(body, user, ct)).ToHttpResult();

    private static async Task<IResult> PickingAsync([FromServices] IProductionService service, [FromBody] PickingWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.PickingAsync(body, user, ct)).ToHttpResult();

    private static async Task<IResult> RegisterAsync([FromServices] IProductionService service, string orderNo, [FromBody] ProductionResultWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.RegisterAsync(orderNo, body, user, ct)).ToHttpResult();

    private static async Task<IResult> ReverseAsync([FromServices] IProductionService service, long no, [FromBody] ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ReverseAsync(no, body, user, ct)).ToHttpResult();

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
