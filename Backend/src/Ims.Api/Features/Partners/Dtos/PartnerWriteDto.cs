namespace Ims.Api.Features.Partners;

public sealed record PartnerWriteDto(
    string PartnerCode, string PartnerName,
    bool IsSupplier = false, bool IsCustomer = false,
    string? Country = null, string? Address = null, bool IsActive = true,
    string? Remark1 = null, string? Remark2 = null, string? Remark3 = null,
    string? Remark4 = null, string? Remark5 = null);
