namespace Ims.Api.Features.StockScan;

public sealed record StockScanReadDto(IReadOnlyList<string?>? Epcs);
