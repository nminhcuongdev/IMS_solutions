namespace Ims.Api.Features.StockScan;

public sealed record StockScanLookupResultDto(
    IReadOnlyList<StockScanRegisteredEpcDto> Registered,
    IReadOnlyList<StockScanPrefixDto> Prefixes);
