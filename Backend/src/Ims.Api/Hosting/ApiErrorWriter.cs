using System.Text.Json;
using Ims.Api.Contracts;

namespace Ims.Api.Hosting;

internal static class ApiErrorWriter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static Task WriteAsync(HttpResponse response, int statusCode, ApiError error)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        return response.WriteAsync(JsonSerializer.Serialize(error, Options));
    }
}
