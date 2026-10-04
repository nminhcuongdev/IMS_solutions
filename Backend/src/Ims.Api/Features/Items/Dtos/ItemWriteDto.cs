namespace Ims.Api.Features.Items;

public sealed record ItemWriteDto(
    string ItemCode, string ItemName,
    string ItemGroupCode,
    string? UomCode = null,
    string? PackingTypeCode = null,
    decimal PackageQty = 1,
    int? ExpirationPeriod = null,
    string? Barcode = null,
    decimal? SurplusPoint = null, decimal? ReorderLevel1 = null, decimal? ReorderLevel2 = null,
    string? DefaultLocationCode = null,
    bool IsActive = true,
    string? Remark1 = null, string? Remark2 = null, string? Remark3 = null,
    string? Remark4 = null, string? Remark5 = null);
