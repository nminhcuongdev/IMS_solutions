namespace Ims.Api.Features.StockScan;

public sealed record StockScanRegisterGroupDto(
    string? ItemCode,
    string? ItemName,
    IReadOnlyList<string?>? Epcs);
