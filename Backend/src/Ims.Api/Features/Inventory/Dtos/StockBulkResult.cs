namespace Ims.Api.Features.Inventory;

public sealed record StockBulkResult(int Updated, IReadOnlyList<string> Skipped);
