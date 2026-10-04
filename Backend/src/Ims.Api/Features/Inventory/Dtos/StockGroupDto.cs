namespace Ims.Api.Features.Inventory;

public sealed record StockGroupDto(
    string Key,
    string? Name,
    string? Category,
    string? Warehouse,
    string? Unit,
    decimal StockQty,
    int LotCount,
    int ItemCount,
    bool AnyOnHold = false,
    DateOnly? EarliestExpiry = null,
    int? RemainingDays = null,
    decimal? ReorderPoint = null);
