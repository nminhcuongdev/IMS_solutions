namespace Ims.Api.Features.Processes;

public sealed record ProcessDto(string ProcessCode, string ProcessName, int? DisplayOrder, bool IsActive,
    string? Remark1, string? Remark2, string? Remark3, string? Remark4, string? Remark5,
    DateTime CreatedAt, DateTime? UpdatedAt);
