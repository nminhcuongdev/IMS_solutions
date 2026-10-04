namespace Ims.Api.Features.Reasons;

public sealed record ReasonDto(
    string ReasonCode, string ReasonName, string ReasonCategory, bool IsActive,
    string? Remark1, string? Remark2, string? Remark3, string? Remark4, string? Remark5,
    DateTime CreatedAt, DateTime? UpdatedAt);
