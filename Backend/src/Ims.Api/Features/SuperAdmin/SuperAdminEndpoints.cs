using Ims.Api.Features.Permissions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.SuperAdmin;

public static class SuperAdminEndpoints
{
    public static IEndpointRouteBuilder MapSuperAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/super-admin").WithTags("Super Admin").RequireAuthorization().AuthenticatedOnly();
        g.MapGet("/general", GetGeneralAsync);
        g.MapPut("/general", SaveGeneralAsync);
        g.MapGet("/function-visibility", GetVisibilityAsync);
        g.MapPut("/function-visibility", SaveVisibilityAsync);
        g.MapGet("/default-master/{type}", GetDefaultMasterAsync);
        g.MapPut("/default-master/{type}", SaveDefaultMasterAsync);
        g.MapGet("/db-info", GetDbInfoAsync);
        return app;
    }

    private static async Task<IResult> GetGeneralAsync(
        [FromServices] ISuperAdminService service, ClaimsPrincipal user, CancellationToken ct,
        [FromQuery] string? category = null) =>
        (await service.GetGeneralAsync(user: user, ct: ct, category: category)).ToHttpResult();

    private static async Task<IResult> SaveGeneralAsync(
        [FromServices] ISuperAdminService service, ClaimsPrincipal user, GeneralSaveDto body, CancellationToken ct) =>
        (await service.SaveGeneralAsync(user: user, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> GetVisibilityAsync(
        [FromServices] ISuperAdminService service, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.GetVisibilityAsync(user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> SaveVisibilityAsync(
        [FromServices] ISuperAdminService service, ClaimsPrincipal user, VisibilitySaveDto body, CancellationToken ct) =>
        (await service.SaveVisibilityAsync(user: user, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> GetDefaultMasterAsync(
        [FromServices] ISuperAdminService service, string type, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.GetDefaultMasterAsync(type: type, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> SaveDefaultMasterAsync(
        [FromServices] ISuperAdminService service, string type, ClaimsPrincipal user, DefaultMasterSaveDto body,
        CancellationToken ct) =>
        (await service.SaveDefaultMasterAsync(type: type, user: user, body: body, ct: ct)).ToHttpResult();

    private static IResult GetDbInfoAsync(
        [FromServices] ISuperAdminService service, ClaimsPrincipal user, IConfiguration config) =>
        service.GetDbInfoAsync(user: user, config: config).ToHttpResult();
}
