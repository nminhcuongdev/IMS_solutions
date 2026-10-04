namespace Ims.Api.Features.Locations;

public sealed record LocationDto(
    string LocationCode, string LocationType, string LargeCode, string? MiddleCode, string? SmallCode,
    string LocationName, string? ZoneCode, string? ParentLocationCode, bool IsActive,
    string? Remark1, string? Remark2, string? Remark3, string? Remark4, string? Remark5,
    DateTime CreatedAt, DateTime? UpdatedAt);
