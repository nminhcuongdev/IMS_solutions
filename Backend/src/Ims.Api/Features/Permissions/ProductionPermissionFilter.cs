using Ims.Api.Contracts;
using Ims.Api.Http;

namespace Ims.Api.Features.Permissions;

internal sealed class ProductionPermissionFilter(PermissionEvaluator evaluator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var path = context.HttpContext.Request.Path.Value ?? "";
        var method = context.HttpContext.Request.Method;
        var check = Resolve(path, method, context.HttpContext.Request.Query["direction"].ToString());
        if (check is null) return await next(context);
        var rights = await evaluator.ReadAsync(context.HttpContext.User, context.HttpContext.RequestAborted);
        var allowed = rights.Allows(check.Value.Function, check.Value.Action)
            || method == "GET" && path.StartsWith("/production-order", StringComparison.OrdinalIgnoreCase)
                && new[] { "ht_prd_receipt", "ht_prd_input", "ht_prd_return", "ht_prd_output" }
                    .Any(f => rights.Allows(f, "view"))
            || path.StartsWith("/traceability", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/traceability/backward", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/traceability/forward", StringComparison.OrdinalIgnoreCase)
                && rights.Allows("trace_fwd", "view")
            || method == "GET" && path.Equals("/distribution/devices", StringComparison.OrdinalIgnoreCase)
                && rights.Allows("m_device", "view");
        return allowed
            ? await next(context)
            : Outcome.Json(new ApiError(ErrorCatalog.Auth006()), 403).ToHttpResult();
    }

    private static (string Function, string Action)? Resolve(string path, string method, string direction)
    {
        if (path.StartsWith("/production-consumption/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/production-output/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/production-return/", StringComparison.OrdinalIgnoreCase))
            return ("prd_result", "reverse");
        if (path.Equals("/production-result", StringComparison.OrdinalIgnoreCase)) return ("prd_result", "view");
        if (path.Equals("/export/production-result", StringComparison.OrdinalIgnoreCase)) return ("prd_result", "export");
        if (path.Equals("/export/production-order", StringComparison.OrdinalIgnoreCase)) return ("prd_import", "export");
        if (path.Equals("/import/production-order", StringComparison.OrdinalIgnoreCase)) return ("prd_import", "import");
        if (path.StartsWith("/production-order", StringComparison.OrdinalIgnoreCase))
        {
            if (method == "GET") return ("prd_import", "view");
            if (path.EndsWith("/picking", StringComparison.OrdinalIgnoreCase)) return ("prd_import", "material_picking");
            if (path.EndsWith("/status", StringComparison.OrdinalIgnoreCase)) return ("prd_import", "status_change");
            if (path.EndsWith("/receipt", StringComparison.OrdinalIgnoreCase)) return ("ht_prd_receipt", "view");
            if (path.EndsWith("/return", StringComparison.OrdinalIgnoreCase)) return ("ht_prd_return", "view");
            if (path.EndsWith("/output", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/label", StringComparison.OrdinalIgnoreCase)) return ("ht_prd_output", "view");
            return ("ht_prd_input", "view");
        }
        if (path.StartsWith("/distribution", StringComparison.OrdinalIgnoreCase))
            return ("dist_status", method == "GET" ? "view" : "distribute");
        if (path.Equals("/export/processes", StringComparison.OrdinalIgnoreCase)) return ("m_process", "export");
        if (path.StartsWith("/masters/processes", StringComparison.OrdinalIgnoreCase))
            return ("m_process", method switch { "GET" when path.EndsWith("export.csv", StringComparison.OrdinalIgnoreCase) => "export", "GET" => "view", "POST" => "new", "PUT" => "edit", "DELETE" => "delete", _ => "view" });
        if (path.Equals("/export/traceability", StringComparison.OrdinalIgnoreCase))
            return (direction.Equals("forward", StringComparison.OrdinalIgnoreCase) ? "trace_fwd" : "trace_back", "export");
        if (path.StartsWith("/traceability/forward", StringComparison.OrdinalIgnoreCase)) return ("trace_fwd", "view");
        if (path.StartsWith("/traceability", StringComparison.OrdinalIgnoreCase)) return ("trace_back", "view");
        return null;
    }
}
