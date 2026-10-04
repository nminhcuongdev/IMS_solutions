namespace Ims.Api.Features.StockScan;

public sealed record StockScanReadResultDto(
    int Accepted,
    int AlreadyScanned,
    int Ignored,
    int Scanned,
    int Total);
