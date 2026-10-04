using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Stocktaking;

public static class StocktakingEndpoints
{
    public static IEndpointRouteBuilder MapStocktakingEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/stocktaking").WithTags("Stocktaking").RequireAuthorization();
        g.MapGet("/session", GetSessionAsync);
        g.MapPost("/session/start", StartAsync);
        g.MapPost("/session/complete", CompleteAsync);
        g.MapPost("/session/finalize", FinalizeAsync);
        g.MapGet("/labels", ListLabelsAsync);
        g.MapGet("/labels/export.csv", ExportLabelsCsvAsync);
        g.MapGet("/items", ListItemsAtLocationAsync);
        g.MapPost("/result", SaveResultAsync);
        g.MapGet("/history", ListHistoryAsync);
        g.MapGet("/history/export.csv", ExportHistoryCsvAsync);
        return app;
    }

    private static async Task<IResult> GetSessionAsync(
        [FromServices] IStocktakingService service, CancellationToken ct) =>
        (await service.GetSessionAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> StartAsync(
        [FromServices] IStocktakingService service, [FromBody] StartStocktakingDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.StartAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> CompleteAsync(
        [FromServices] IStocktakingService service, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.CompleteAsync(user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> FinalizeAsync(
        [FromServices] IStocktakingService service, [FromBody] FinalizeStocktakingDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.FinalizeAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ListItemsAtLocationAsync(
        [FromServices] IStocktakingService service, CancellationToken ct, string? locationCode = null) =>
        (await service.ListItemsAtLocationAsync(ct: ct, locationCode: locationCode)).ToHttpResult();

    private static async Task<IResult> SaveResultAsync(
        [FromServices] IStocktakingService service, [FromBody] StocktakingResultDto body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.SaveResultAsync(body: body, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ListLabelsAsync(
        [FromServices] IStocktakingService service, CancellationToken ct, [FromQuery] string? view = null,
        [FromQuery] string? locationCode = null, [FromQuery] string? itemCode = null,
        [FromQuery] string? itemName = null, [FromQuery] bool notScanned = false, [FromQuery] bool statusDiff = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? sort = null) =>
        (await service.ListLabelsAsync(ct: ct, view: view, locationCode: locationCode, itemCode: itemCode, itemName: itemName, notScanned: notScanned, statusDiff: statusDiff, page: page, pageSize: pageSize, sort: sort)).ToHttpResult();

    private static async Task<IResult> ExportLabelsCsvAsync(
        [FromServices] IStocktakingService service, CancellationToken ct, [FromQuery] string? view = null,
        [FromQuery] string? locationCode = null, [FromQuery] string? itemCode = null,
        [FromQuery] string? itemName = null, [FromQuery] bool notScanned = false, [FromQuery] bool statusDiff = false) =>
        (await service.ExportLabelsCsvAsync(ct: ct, view: view, locationCode: locationCode, itemCode: itemCode, itemName: itemName, notScanned: notScanned, statusDiff: statusDiff)).ToHttpResult();

    private static async Task<IResult> ListHistoryAsync(
        [FromServices] IStocktakingService service, CancellationToken ct, [FromQuery] string? q = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? sort = null) =>
        (await service.ListHistoryAsync(ct: ct, q: q, page: page, pageSize: pageSize, sort: sort)).ToHttpResult();

    private static async Task<IResult> ExportHistoryCsvAsync(
        [FromServices] IStocktakingService service, CancellationToken ct, [FromQuery] string? q = null) =>
        (await service.ExportHistoryCsvAsync(ct: ct, q: q)).ToHttpResult();
}
