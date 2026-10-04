namespace Ims.Api.Features.Stocktaking;

public sealed record StocktakingLabelDto(
    string SerialNo,
    string ItemCode,
    string? ItemName,
    decimal Qty,
    string? Unit,
    string LocationCode,
    string? LotNo,
    bool IsScanned,
    int Status,
    string StatusLabelKey,
    DateOnly? ExpiryDate,
    bool IsExpired,
    DateTime? UpdatedAt);
