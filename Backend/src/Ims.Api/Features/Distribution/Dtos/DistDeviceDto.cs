namespace Ims.Api.Features.Distribution;

public sealed record DistDeviceDto(string DeviceCode, string DeviceName, string? Model, string? LocationCode, bool IsActive, int? VersionCode, string? VersionName, DateTime? LastCheckAt, DateTime? LastUpdateAt, DateTime? DownloadedAt, string Status, int? TargetVersionCode, string? TargetVersionName);
