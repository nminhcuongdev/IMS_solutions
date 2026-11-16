using Ims.Api.Contracts;
using Ims.Api.Hosting;

namespace Ims.Api.Features.Permissions;

internal sealed class AccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, PermissionEvaluator evaluator)
    {
        var metadata = http.GetEndpoint()?.Metadata;
        var module = metadata?.GetMetadata<ModuleRequirement>();
        var need = metadata?.GetMetadata<PermissionRequirement>();
        if ((module is null && need is null) || http.User.Identity?.IsAuthenticated != true)
        {
            await next(http);
            return;
        }

        if (module is not null)
        {
            var disabled = await evaluator.DisabledModulesAsync(http.RequestAborted);
            if (disabled.Contains(module.Module))
            {
                await ApiErrorWriter.WriteAsync(http.Response, 404, new ApiError(ErrorCatalog.Sys404(ModuleName(module.Module))));
                return;
            }
        }

        if (need is not null)
        {
            var rights = await evaluator.ReadAsync(http.User, http.RequestAborted);
            if (!rights.Satisfies(need.AnyOf))
            {
                await ApiErrorWriter.WriteAsync(http.Response, 403, new ApiError(ErrorCatalog.Auth006()));
                return;
            }
        }

        await next(http);
    }

    private static string ModuleName(string module) =>
        char.ToUpperInvariant(module[0]) + module[1..] + " module";
}
