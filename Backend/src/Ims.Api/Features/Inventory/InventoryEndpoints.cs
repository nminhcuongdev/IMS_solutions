using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Inventory;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var stock = app.MapGroup("/stock").WithTags("Inventory").RequireAuthorization();
        stock.MapGet("", ListStockAsync);
        stock.MapGet("/export.csv", ExportStockCsvAsync);
        stock.MapPost("/adjust", AdjustStockAsync);
        stock.MapPost("/hold", HoldStockAsync);
        stock.MapPost("/release-hold", ReleaseHoldAsync);
        stock.MapPost("/disposal", DisposeStockAsync);
        stock.MapPost("/transfer", TransferStockAsync);

        var history = app.MapGroup("/inventory-history").WithTags("Inventory").RequireAuthorization();
        history.MapGet("", ListHistoryAsync);
        history.MapGet("/export.csv", ExportHistoryCsvAsync);
        history.MapPost("/restore", RestoreDisposalAsync);
        return app;
    }

    private static async Task<IResult> ListStockAsync(
        [FromServices] IInventoryService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] string? q = null,
        [FromQuery] string? warehouse = null, [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null,
        [FromQuery] string? category = null, [FromQuery] string? lot = null, [FromQuery] string? expiration = null,
        [FromQuery] string? stockLevel = null, [FromQuery] string? hold = null, [FromQuery] string? groupBy = null) =>
        (await service.ListStockAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, q: q, warehouse: warehouse, itemCode: itemCode, itemName: itemName, category: category, lot: lot, expiration: expiration, stockLevel: stockLevel, hold: hold, groupBy: groupBy)).ToHttpResult();

    private static async Task<IResult> AdjustStockAsync(
        [FromServices] IInventoryService service, [FromBody] StockAdjustDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.AdjustStockAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> HoldStockAsync(
        [FromServices] IInventoryService service, [FromBody] StockHoldDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.HoldStockAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ReleaseHoldAsync(
        [FromServices] IInventoryService service, [FromBody] StockKeysDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.ReleaseHoldAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> DisposeStockAsync(
        [FromServices] IInventoryService service, [FromBody] StockDisposalDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.DisposeStockAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> TransferStockAsync(
        [FromServices] IInventoryService service, [FromBody] StockTransferDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.TransferStockAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ListHistoryAsync(
        [FromServices] IInventoryService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? sort = null, [FromQuery] string? q = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null, [FromQuery] string? action = null) =>
        (await service.ListHistoryAsync(ct: ct, page: page, pageSize: pageSize, sort: sort, q: q, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, action: action)).ToHttpResult();

    private static async Task<IResult> RestoreDisposalAsync(
        [FromServices] IInventoryService service, [FromBody] RestoreDisposalDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.RestoreDisposalAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportStockCsvAsync(
        [FromServices] IInventoryService service, CancellationToken ct, [FromQuery] string? q = null,
        [FromQuery] string? warehouse = null, [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null,
        [FromQuery] string? category = null, [FromQuery] string? lot = null, [FromQuery] string? expiration = null,
        [FromQuery] string? stockLevel = null, [FromQuery] string? hold = null) =>
        (await service.ExportStockCsvAsync(ct: ct, q: q, warehouse: warehouse, itemCode: itemCode, itemName: itemName, category: category, lot: lot, expiration: expiration, stockLevel: stockLevel, hold: hold)).ToHttpResult();

    private static async Task<IResult> ExportHistoryCsvAsync(
        [FromServices] IInventoryService service, CancellationToken ct, [FromQuery] string? q = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? itemCode = null, [FromQuery] string? itemName = null, [FromQuery] string? action = null) =>
        (await service.ExportHistoryCsvAsync(ct: ct, q: q, dateFrom: dateFrom, dateTo: dateTo, itemCode: itemCode, itemName: itemName, action: action)).ToHttpResult();
}
