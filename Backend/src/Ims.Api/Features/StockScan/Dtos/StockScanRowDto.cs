namespace Ims.Api.Features.StockScan;

public sealed record StockScanRowDto(
    string Epc,
    string? ItemCode,
    string? ItemName,
    string? ItemCategory,
    bool IsScanned,
    string? WorkerCode,
    string? WorkerName);
