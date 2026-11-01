namespace Ims.Api.Features.Distribution;

public sealed record ApkReadResult(
    string? Failure, string? PackageName, int VersionCode, string? VersionName, string? SignerFingerprint);
