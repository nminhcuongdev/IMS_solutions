namespace Ims.Api.Features.Inventory;

public sealed record StockDto(
    string ItemCode,
    string SerialNo,
    string? ItemName,
    string? Category,
    string? Warehouse,
    string? LocationCode,
    string? LocationName,
    string? LotNo,
    bool IsOnHold,
    string? HoldReason,
    decimal StockQty,
    decimal ReservedQty,
    decimal AvailableQty,
    string? Unit,
    decimal? ReorderPoint,
    decimal? SurplusPoint,
    decimal? OverShort,
    DateTime UpdatedAt,
    DateOnly? ExpiryDate,
    int? RemainingDays);
