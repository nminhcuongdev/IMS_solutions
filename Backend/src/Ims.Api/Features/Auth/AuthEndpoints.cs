using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/auth").WithTags("Auth");
        g.MapPost("/login", LoginAsync).AllowAnonymous();
        g.MapPost("/logout", LogoutAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> LoginAsync(
        [FromServices] IAuthService service, [FromBody] LoginRequest body, CancellationToken ct) =>
        (await service.LoginAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> LogoutAsync(
        [FromServices] IAuthService service, HttpContext http, CancellationToken ct) =>
        (await service.LogoutAsync(http: http, ct: ct)).ToHttpResult();
}
