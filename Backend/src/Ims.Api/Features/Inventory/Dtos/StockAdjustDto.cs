namespace Ims.Api.Features.Inventory;

public sealed record StockAdjustDto(
    string ItemCode, string SerialNo, decimal StockQty, string? ReasonCode);
