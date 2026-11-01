namespace Ims.Api.Features.Distribution;

public sealed record PackageUploadResultDto(bool Ok, IReadOnlyList<InspectionCheckDto> Checks, PackageDto? Package);
