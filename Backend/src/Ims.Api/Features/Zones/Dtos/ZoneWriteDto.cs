namespace Ims.Api.Features.Zones;

public sealed record ZoneWriteDto(
    string ZoneCode,
    string ZoneName,
    string FactoryCode,
    bool IsActive = true,
    string? Remark1 = null,
    string? Remark2 = null,
    string? Remark3 = null,
    string? Remark4 = null,
    string? Remark5 = null);
