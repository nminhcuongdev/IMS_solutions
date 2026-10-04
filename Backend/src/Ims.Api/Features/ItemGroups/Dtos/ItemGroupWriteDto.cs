namespace Ims.Api.Features.ItemGroups;

public sealed record ItemGroupWriteDto(
    string ItemGroupCode, string ItemGroupName, byte Category,
    bool IsActive = true,
    string? Remark1 = null, string? Remark2 = null, string? Remark3 = null,
    string? Remark4 = null, string? Remark5 = null);
