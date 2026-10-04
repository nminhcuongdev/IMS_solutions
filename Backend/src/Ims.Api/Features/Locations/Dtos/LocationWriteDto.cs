namespace Ims.Api.Features.Locations;

public sealed record LocationWriteDto(
    string LocationType,
    string InputCode,
    string LocationName,
    string? ParentLocationCode = null,
    string? ZoneCode = null,
    bool IsActive = true,
    string? Remark1 = null, string? Remark2 = null, string? Remark3 = null,
    string? Remark4 = null, string? Remark5 = null);
