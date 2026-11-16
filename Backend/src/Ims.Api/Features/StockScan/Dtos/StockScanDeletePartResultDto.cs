namespace Ims.Api.Features.StockScan;

public sealed record StockScanDeletePartResultDto(int Deleted, IReadOnlyList<StockScanPartDto> Parts);
