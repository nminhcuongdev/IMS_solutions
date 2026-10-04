namespace Ims.Api.Features.Factories;

public sealed record FactoryDto(
    string FactoryCode, string FactoryName,
    string? Country, string? Address, bool IsActive, bool IsDefault,
    string? Remark1, string? Remark2, string? Remark3, string? Remark4, string? Remark5,
    DateTime CreatedAt, DateTime? UpdatedAt);
