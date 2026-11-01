namespace Ims.Api.Features.Distribution;

public sealed record TargetQueryDto(string[]? Models = null, string[]? Locations = null, string[]? Include = null, string[]? Exclude = null);
