using Ims.Api.Features.Permissions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.StockScan;

public static class StockScanEndpoints
{
    public static IEndpointRouteBuilder MapStockScanEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/stock-scan").WithTags("StockScan").RequireAuthorization().RequireModule("rfid");
        g.MapGet("", ListAsync).RequirePermission("st_rfid_demo.Open");
        g.MapGet("/summary", SummaryAsync).RequirePermission("st_rfid_demo.Open");
        g.MapPost("/read", ReadAsync).RequirePermission("st_rfid_demo.Open");
        g.MapPost("/reset", ResetAsync).RequirePermission("st_rfid_demo.Reset");
        g.MapGet("/parts", PartsAsync).RequirePermission("st_rfid_demo.Open");
        g.MapPut("/targets", SetTargetsAsync).RequirePermission("st_rfid_demo.SelectTarget");
        g.MapDelete("/parts", DeletePartAsync).RequirePermission("st_rfid_demo.Delete");
        g.MapPost("/register/lookup", LookupAsync).RequirePermission("st_rfid_demo.Register");
        g.MapPost("/register", RegisterAsync).RequirePermission("st_rfid_demo.Register");
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

    private static async Task<IResult> PartsAsync(
        [FromServices] IStockScanService service, CancellationToken ct) =>
        (await service.PartsAsync(ct)).ToHttpResult();

    private static async Task<IResult> SetTargetsAsync(
        [FromServices] IStockScanService service, [FromBody] StockScanTargetsDto body, CancellationToken ct) =>
        (await service.SetTargetsAsync(body, ct)).ToHttpResult();

    private static async Task<IResult> LookupAsync(
        [FromServices] IStockScanService service, [FromBody] StockScanLookupDto body, CancellationToken ct) =>
        (await service.LookupAsync(body, ct)).ToHttpResult();

    private static async Task<IResult> RegisterAsync(
        [FromServices] IStockScanService service, [FromBody] StockScanRegisterDto body, CancellationToken ct) =>
        (await service.RegisterAsync(body, ct)).ToHttpResult();

    private static async Task<IResult> DeletePartAsync(
        [FromServices] IStockScanService service, CancellationToken ct, [FromQuery] string? itemCode = null) =>
        (await service.DeletePartAsync(itemCode, ct)).ToHttpResult();
}
