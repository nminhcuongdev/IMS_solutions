using System.Text.Json;
using Ims.Api.Contracts;
using Ims.Api.Http;

namespace Ims.Api.Hosting;

internal static class ApiErrorWriter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task WriteAsync(HttpResponse response, int statusCode, ApiError error)
    {
        var body = await LocalizedErrorResult.LocalizeAsync(response.HttpContext, error.Error);
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        await response.WriteAsync(JsonSerializer.Serialize(new ApiError(body), Options));
    }
}
