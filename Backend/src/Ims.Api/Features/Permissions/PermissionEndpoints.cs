using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Permissions;

public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/permissions").WithTags("Permissions").RequireAuthorization();
        g.MapGet("", ListAsync);
        g.MapGet("/me", MeAsync);
        g.MapPut("", SaveAsync);
        return app;
    }

    private static async Task<IResult> ListAsync([FromServices] IPermissionService service, CancellationToken ct) =>
        (await service.ListAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> MeAsync([FromServices] PermissionEvaluator evaluator, HttpContext http, CancellationToken ct) =>
        Outcome.Ok(new ApiSuccess<PermissionSnapshot>(await evaluator.ReadAsync(http.User, ct))).ToHttpResult();

    private static async Task<IResult> SaveAsync(
        [FromServices] IPermissionService service, PermissionSaveDto body, CancellationToken ct) =>
        (await service.SaveAsync(body: body, ct: ct)).ToHttpResult();
}
