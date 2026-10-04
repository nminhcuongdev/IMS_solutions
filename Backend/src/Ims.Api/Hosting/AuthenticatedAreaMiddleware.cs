using Ims.Api.Contracts;

namespace Ims.Api.Hosting;

internal sealed class AuthenticatedAreaMiddleware(RequestDelegate next)
{
    private static readonly PathString[] ProtectedAreas = ["/masters", "/super-admin"];

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path;
        if (ProtectedAreas.Any(area => path.StartsWithSegments(area)) && ctx.User.Identity?.IsAuthenticated != true)
        {
            await ApiErrorWriter.WriteAsync(ctx.Response, 401, new ApiError(ErrorCatalog.Auth005()));
            return;
        }
        await next(ctx);
    }
}
