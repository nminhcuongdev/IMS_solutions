namespace Ims.Api.Features.Distribution;

public sealed record PackageDto(int Id, string PackageName, int VersionCode, string VersionName, string FileName, string OriginalFileName, long SizeBytes, string Sha256, string SignerFingerprint, string? CreatedBy, DateTime CreatedAt, bool InUse);
