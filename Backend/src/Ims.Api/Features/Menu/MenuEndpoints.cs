using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Menu;

public static class MenuEndpoints
{
    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/menu", GetMenuAsync).RequireAuthorization().WithTags("Menu");
        return app;
    }

    private static async Task<IResult> GetMenuAsync(
        [FromServices] IMenuService service, HttpContext http, CancellationToken ct) =>
        (await service.GetMenuAsync(http: http, ct: ct)).ToHttpResult();
}
