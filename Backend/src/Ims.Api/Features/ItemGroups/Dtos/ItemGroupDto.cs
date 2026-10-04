namespace Ims.Api.Features.ItemGroups;

public sealed record ItemGroupDto(
    string ItemGroupCode, string ItemGroupName, byte Category, bool IsActive,
    string? Remark1, string? Remark2, string? Remark3, string? Remark4, string? Remark5,
    DateTime CreatedAt, DateTime? UpdatedAt);
