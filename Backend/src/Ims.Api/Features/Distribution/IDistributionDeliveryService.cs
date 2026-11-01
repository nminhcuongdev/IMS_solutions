namespace Ims.Api.Features.Distribution;

internal interface IDistributionDeliveryService
{
    Task<VersionJsonResponse> VersionJsonAsync(string? deviceId, string? appVersion, CancellationToken ct);

    Task<ApkSource?> FindApkAsync(string fileName, CancellationToken ct);

    Stream OpenApk(ApkSource source);

    Task RecordDownloadAsync(string? deviceId, ApkSource source, CancellationToken ct);
}
