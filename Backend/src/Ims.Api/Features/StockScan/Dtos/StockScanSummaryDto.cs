namespace Ims.Api.Features.StockScan;

public sealed record StockScanSummaryDto(
    int Scanned,
    int Total,
    IReadOnlyList<StockScanItemDto> Items);
