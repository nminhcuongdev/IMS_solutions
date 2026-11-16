using Ims.Api.Features.Permissions;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Defaults;

public static class DefaultValueEndpoints
{
    public static IEndpointRouteBuilder MapDefaultValueEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/default-values").WithTags("Default Values");
        g.MapGet("/{type}", ListAsync).RequireLookup();
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IDefaultValueService service, string type, CancellationToken ct,
        [FromQuery] bool? isActive = null) =>
        (await service.ListAsync(type: type, ct: ct, isActive: isActive)).ToHttpResult();
}
