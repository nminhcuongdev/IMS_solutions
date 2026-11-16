using Microsoft.AspNetCore.Mvc;
using Ims.Api.Features.Permissions;
using Ims.Api.Http;

namespace Ims.Api.Features.Roles;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/roles").WithTags("Roles");
        g.MapGet("", ListAsync).RequirePermission("m_permission.Open", "m_user.Open");
        g.MapGet("/{code}", GetAsync).RequirePermission("m_permission.Open", "m_user.Open");
        g.MapPost("", CreateAsync).RequirePermission("m_permission.New");
        g.MapPut("/{code}", UpdateAsync).RequirePermission("m_permission.Edit");
        g.MapDelete("/{code}", DeleteAsync).RequirePermission("m_permission.Delete");
        app.MapGet("/roles", ListAsync).WithTags("Roles").RequireAuthorization()
            .RequirePermission("m_permission.Open", "m_user.Open");
        return app;
    }

    private static async Task<IResult> ListAsync([FromServices] IRoleService service, CancellationToken ct) =>
        (await service.ListAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> GetAsync([FromServices] IRoleService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(
        [FromServices] IRoleService service, [FromBody] RoleWriteDto body, HttpContext http, CancellationToken ct) =>
        (await service.CreateAsync(body: body, user: http.User, ct: ct)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        [FromServices] IRoleService service, string code, [FromBody] RoleWriteDto body, HttpContext http, CancellationToken ct) =>
        (await service.UpdateAsync(code: code, body: body, user: http.User, ct: ct)).ToHttpResult();

    private static async Task<IResult> DeleteAsync(
        [FromServices] IRoleService service, string code, HttpContext http, CancellationToken ct) =>
        (await service.DeleteAsync(code: code, user: http.User, ct: ct)).ToHttpResult();
}
