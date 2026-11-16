namespace Ims.Api.Features.Inventory;

public sealed record StockTransferDto(string ItemCode, string SerialNo, string? ToLocationCode, string? RequestId = null);
