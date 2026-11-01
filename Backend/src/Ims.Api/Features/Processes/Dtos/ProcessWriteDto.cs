namespace Ims.Api.Features.Processes;

public sealed record ProcessWriteDto(string ProcessCode, string ProcessName, int? DisplayOrder = null,
    bool IsActive = true, string? Remark1 = null, string? Remark2 = null, string? Remark3 = null,
    string? Remark4 = null, string? Remark5 = null);
