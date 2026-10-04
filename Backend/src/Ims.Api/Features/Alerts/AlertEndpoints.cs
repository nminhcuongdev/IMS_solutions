using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Alerts;

public static class AlertEndpoints
{
    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/alerts").WithTags("Alerts").RequireAuthorization();
        g.MapGet("", ListAsync);
        return app;
    }

    private static async Task<IResult> ListAsync([FromServices] IAlertService service, CancellationToken ct) =>
        (await service.ListAsync(ct: ct)).ToHttpResult();
}
