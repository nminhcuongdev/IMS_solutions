using Microsoft.AspNetCore.Diagnostics;
using Ims.Api.Contracts;

namespace Ims.Api.Hosting;

internal static class UnhandledExceptionExtensions
{
    public static WebApplication UseApiExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(branch => branch.Run(async ctx =>
        {
            var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
            var correlationId = ctx.TraceIdentifier;
            app.Logger.LogError(ex, "Unhandled exception {CorrelationId} on {Method} {Path}", correlationId, ctx.Request.Method, ctx.Request.Path);
            ctx.Response.Headers["X-Correlation-Id"] = correlationId;
            var (status, body) = ExceptionErrors.For(ex);
            await ApiErrorWriter.WriteAsync(ctx.Response, status, new ApiError(body));
        }));
        return app;
    }
}
