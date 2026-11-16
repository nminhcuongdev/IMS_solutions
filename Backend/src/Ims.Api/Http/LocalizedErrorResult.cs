using Microsoft.Extensions.Options;
using Ims.Api.Contracts;
using Ims.Api.Features.LanguageData;

namespace Ims.Api.Http;

public sealed class LocalizedErrorResult(int statusCode, ApiError error) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var body = await LocalizeAsync(httpContext, error.Error);
        var options = httpContext.RequestServices.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?.Value.SerializerOptions;
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ApiError(body), options, httpContext.RequestAborted);
    }

    public static async Task<ApiErrorBody> LocalizeAsync(HttpContext http, ApiErrorBody body)
    {
        var catalog = http.RequestServices.GetService<IMessageCatalog>();
        return catalog is null ? body : await catalog.LocalizeAsync(body, http, http.RequestAborted);
    }
}
