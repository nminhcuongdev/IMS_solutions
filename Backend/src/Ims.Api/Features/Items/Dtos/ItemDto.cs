namespace Ims.Api.Features.Items;

public sealed record ItemDto(
    string ItemCode, string ItemName,
    string ItemGroupCode,
    string? UomCode,
    string? PackingTypeCode,
    decimal PackageQty,
    int? ExpirationPeriod,
    string? Barcode,
    decimal? SurplusPoint, decimal? ReorderLevel1, decimal? ReorderLevel2,
    string? DefaultLocationCode,
    bool IsActive,
    string? Remark1, string? Remark2, string? Remark3, string? Remark4, string? Remark5,
    DateTime CreatedAt, DateTime? UpdatedAt);
