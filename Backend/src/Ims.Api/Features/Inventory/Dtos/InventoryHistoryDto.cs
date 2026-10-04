namespace Ims.Api.Features.Inventory;

public sealed record InventoryHistoryDto(
    long No,
    DateTime CreatedAt,
    string ItemCode,
    string? ItemName,
    string? LotNo,
    string ActionType,
    string ActionLabelKey,
    decimal? Qty,
    string? Unit,
    string? LocationCode,
    string? ReasonCode,
    string? ReasonName,
    string? Worker);
