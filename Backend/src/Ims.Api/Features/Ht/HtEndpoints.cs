using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Ht;

public static class HtEndpoints
{
    public static IEndpointRouteBuilder MapHtEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/ht").WithTags("HT");

        g.MapGet("/config", GetConfigAsync).AllowAnonymous();

        g.MapGet("/printers", ListPrintersAsync).AllowAnonymous();

        g.MapGet("/lots", ListLotsAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetConfigAsync([FromServices] IHtService service, CancellationToken ct) =>
        (await service.GetConfigAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> ListPrintersAsync([FromServices] IHtService service, CancellationToken ct) =>
        (await service.ListPrintersAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> ListLotsAsync(
        [FromServices] IHtService service, CancellationToken ct, [FromQuery] string? q = null,
        [FromQuery] string? itemCode = null) =>
        (await service.ListLotsAsync(ct: ct, q: q, itemCode: itemCode)).ToHttpResult();
}
