using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.StockScan;

public static class StockScanEndpoints
{
    public static IEndpointRouteBuilder MapStockScanEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/stock-scan").WithTags("StockScan").RequireAuthorization();
        g.MapGet("", ListAsync);
        g.MapGet("/summary", SummaryAsync);
        g.MapPost("/read", ReadAsync);
        g.MapPost("/reset", ResetAsync);
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IStockScanService service, CancellationToken ct, [FromQuery] string? itemCode = null,
        [FromQuery] string? itemName = null, [FromQuery] string? scanned = null) =>
        (await service.ListAsync(ct: ct, itemCode: itemCode, itemName: itemName, scanned: scanned)).ToHttpResult();

    private static async Task<IResult> SummaryAsync(
        [FromServices] IStockScanService service, CancellationToken ct) =>
        (await service.SummaryAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> ReadAsync(
        [FromServices] IStockScanService service, [FromBody] StockScanReadDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.ReadAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ResetAsync(
        [FromServices] IStockScanService service, CancellationToken ct) =>
        (await service.ResetAsync(ct: ct)).ToHttpResult();
}
