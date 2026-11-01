namespace Ims.Api.Features.Inventory;

internal sealed record StockAllocation(string SerialNo, string? LotNo, decimal Qty, string? LocationCode, DateOnly? ExpiryDate, string FactoryCode);
