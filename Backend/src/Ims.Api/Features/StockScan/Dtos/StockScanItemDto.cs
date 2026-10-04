namespace Ims.Api.Features.StockScan;

public sealed record StockScanItemDto(
    string? ItemCode,
    string? ItemName,
    string? ItemCategory,
    int Plan,
    int Qty);
