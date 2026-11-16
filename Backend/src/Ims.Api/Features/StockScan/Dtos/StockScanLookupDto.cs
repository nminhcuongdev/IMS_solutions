namespace Ims.Api.Features.StockScan;

public sealed record StockScanLookupDto(IReadOnlyList<string?>? Epcs);
