namespace Ims.Api.Features.StockScan;

public sealed record StockScanPartDto(
    string? ItemCode,
    string? ItemName,
    int Total,
    int Scanned,
    bool IsTarget);
