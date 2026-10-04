namespace Ims.Api.Features.Reasons;

public sealed record ReasonWriteDto(
    string ReasonCode, string ReasonName,
    string ReasonCategory,
    bool IsActive = true,
    string? Remark1 = null, string? Remark2 = null, string? Remark3 = null,
    string? Remark4 = null, string? Remark5 = null);
