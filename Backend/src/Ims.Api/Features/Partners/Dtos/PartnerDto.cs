namespace Ims.Api.Features.Partners;

public sealed record PartnerDto(
    string PartnerCode, string PartnerName, bool IsSupplier, bool IsCustomer,
    string? Country, string? Address, bool IsActive,
    string? Remark1, string? Remark2, string? Remark3, string? Remark4, string? Remark5,
    DateTime CreatedAt, DateTime? UpdatedAt);
