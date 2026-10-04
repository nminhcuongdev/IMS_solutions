using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Permissions;

public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/permissions").WithTags("Permissions").RequireAuthorization();
        g.MapGet("", ListAsync);
        g.MapPut("", SaveAsync);
        return app;
    }

    private static async Task<IResult> ListAsync([FromServices] IPermissionService service, CancellationToken ct) =>
        (await service.ListAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> SaveAsync(
        [FromServices] IPermissionService service, PermissionSaveDto body, CancellationToken ct) =>
        (await service.SaveAsync(body: body, ct: ct)).ToHttpResult();
}
