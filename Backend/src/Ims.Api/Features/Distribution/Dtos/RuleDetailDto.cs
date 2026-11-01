namespace Ims.Api.Features.Distribution;

public sealed record RuleDetailDto(RuleDto Rule, IReadOnlyList<DistDeviceDto> Targets, VersionJsonDto VersionJson);
