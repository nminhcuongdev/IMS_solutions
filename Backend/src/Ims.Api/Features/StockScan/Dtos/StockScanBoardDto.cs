namespace Ims.Api.Features.StockScan;

public sealed record StockScanBoardDto(
    int Scanned,
    int Total,
    IReadOnlyList<StockScanRowDto> Rows);
