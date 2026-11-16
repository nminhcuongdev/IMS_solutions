namespace Ims.Api.Features.StockScan;

public sealed record StockScanRegisteredEpcDto(string Epc, string? ItemCode, string? ItemName);
