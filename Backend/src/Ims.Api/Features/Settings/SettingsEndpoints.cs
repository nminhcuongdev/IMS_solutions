using Ims.Api.Features.Permissions;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/settings").WithTags("Settings").RequireAuthorization();
        g.MapGet("", GetAsync).AuthenticatedOnly();
        g.MapPut("", SaveAsync).AuthenticatedOnly();
        return app;
    }

    private static async Task<IResult> GetAsync([FromServices] ISettingsService service, CancellationToken ct) =>
        (await service.GetAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> SaveAsync(
        [FromServices] ISettingsService service, SettingsDto body, CancellationToken ct) =>
        (await service.SaveAsync(body: body, ct: ct)).ToHttpResult();
}
