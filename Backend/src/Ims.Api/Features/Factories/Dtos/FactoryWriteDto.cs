namespace Ims.Api.Features.Factories;

public sealed record FactoryWriteDto(
    string FactoryCode, string FactoryName,
    string? Country = null, string? Address = null, bool IsActive = true, bool IsDefault = false,
    string? Remark1 = null, string? Remark2 = null, string? Remark3 = null,
    string? Remark4 = null, string? Remark5 = null);
