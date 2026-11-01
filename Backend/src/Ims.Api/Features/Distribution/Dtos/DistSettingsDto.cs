namespace Ims.Api.Features.Distribution;

public sealed record DistSettingsDto(string PackageName, int UploadMb, int Retention, int NDays, int MDays, bool AutoAdd);
