using Microsoft.AspNetCore.Http;
using Ims.Api.Contracts;
using Xunit;

namespace Ims.Api.Tests.Infrastructure;

internal static class ResultAssert
{
    private static string Describe(ServiceResult r)
    {
        var err = r.Body as ApiError;
        return err is null
            ? $"status {r.StatusCode}"
            : $"status {r.StatusCode}, code {err.Error.Code}";
    }

    public static T Ok<T>(ServiceResult r)
    {
        Assert.True(r.StatusCode == StatusCodes.Status200OK && r.File is null && r.Stream is null,
            $"Expected 200 OK, got {Describe(r)}");
        return Assert.IsType<T>(r.Body);
    }

    public static T Created<T>(ServiceResult r)
    {
        Assert.True(r.StatusCode == StatusCodes.Status201Created, $"Expected 201 Created, got {Describe(r)}");
        return Assert.IsType<T>(r.Body);
    }

    public static void NoContent(ServiceResult r)
        => Assert.True(r.StatusCode == StatusCodes.Status204NoContent, $"Expected 204 NoContent, got {Describe(r)}");

    public static void Error(ServiceResult r, int status, string errorCode)
    {
        Assert.True(r.StatusCode == status, $"Expected status {status}, got {Describe(r)}");
        var err = Assert.IsType<ApiError>(r.Body);
        Assert.False(err.Success);
        Assert.Equal(errorCode, err.Error.Code);
    }

    public static void BadRequest(ServiceResult r, string errorCode) => Error(r, 400, errorCode);
    public static void NotFound(ServiceResult r) => Error(r, 404, "SYS-404");
    public static void Conflict(ServiceResult r) => Error(r, 409, "DUP-001");
    public static void Forbidden(ServiceResult r) => Error(r, 403, "AUTH-005");
}
