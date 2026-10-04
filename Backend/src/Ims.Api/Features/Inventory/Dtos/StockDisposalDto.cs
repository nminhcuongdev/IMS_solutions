namespace Ims.Api.Features.Inventory;

public sealed record StockDisposalDto(
    string ItemCode, string SerialNo, string? ReasonCode, decimal? Qty = null);
