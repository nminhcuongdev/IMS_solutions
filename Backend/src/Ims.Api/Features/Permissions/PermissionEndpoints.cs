using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Permissions;

public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/permissions").WithTags("Permissions").RequireAuthorization();
        g.MapGet("", MatrixAsync).RequirePermission("m_permission.Open");
        g.MapGet("/me", MeAsync).AuthenticatedOnly();
        g.MapPut("", SaveAsync).RequirePermission("m_permission.Save");
        return app;
    }

    private static async Task<IResult> MatrixAsync(
        [FromServices] IPermissionService service, CancellationToken ct, [FromQuery] string? roleCode = null) =>
        (await service.MatrixAsync(roleCode: roleCode, ct: ct)).ToHttpResult();

    private static async Task<IResult> MeAsync([FromServices] IPermissionService service, HttpContext http, CancellationToken ct) =>
        (await service.MeAsync(user: http.User, ct: ct)).ToHttpResult();

    private static async Task<IResult> SaveAsync(
        [FromServices] IPermissionService service, [FromBody] PermissionMatrixSaveDto body, HttpContext http, CancellationToken ct) =>
        (await service.SaveAsync(body: body, user: http.User, ct: ct)).ToHttpResult();
}
