namespace Ims.Api.Features.Zones;

public sealed record ZoneDto(
    string ZoneCode,
    string ZoneName,
    string FactoryCode,
    bool IsActive,
    string? Remark1,
    string? Remark2,
    string? Remark3,
    string? Remark4,
    string? Remark5,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
