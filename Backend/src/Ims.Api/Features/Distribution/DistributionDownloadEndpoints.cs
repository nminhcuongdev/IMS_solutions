using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Distribution;

public static class DistributionDownloadEndpoints
{
    private const string DeviceHeader = "X-Device-Id";
    private const string VersionHeader = "X-App-Version";

    public static IEndpointRouteBuilder MapDistributionDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/dl").WithTags("Distribution (HT)").AllowAnonymous();
        g.MapMethods("/version.json", ["GET", "HEAD"], VersionJsonAsync);
        g.MapMethods("/{apkFile}", ["GET", "HEAD"], ApkAsync);
        return app;
    }

    private static async Task<IResult> VersionJsonAsync(
        [FromServices] IDistributionDeliveryService service, HttpContext http, CancellationToken ct)
    {
        var r = await service.VersionJsonAsync(http.Request.Headers[DeviceHeader].ToString(),
            http.Request.Headers[VersionHeader].ToString(), ct);
        http.Response.Headers.CacheControl = "no-cache";
        if (r.LastModified is { } lm)
            http.Response.Headers.LastModified = new DateTimeOffset(lm).ToString("R", CultureInfo.InvariantCulture);
        return Results.Json(r.Body);
    }

    private static async Task<IResult> ApkAsync(
        [FromServices] IDistributionDeliveryService service, string apkFile, HttpContext http, CancellationToken ct)
    {
        var source = await service.FindApkAsync(apkFile, ct);
        if (source is null) return Results.NotFound();
        var device = http.Request.Headers[DeviceHeader].ToString();
        return new RangeFileResult(() => service.OpenApk(source), source.Length,
            "application/vnd.android.package-archive", null,
            () => service.RecordDownloadAsync(device, source, CancellationToken.None));
    }
}
