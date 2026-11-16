namespace Ims.Api.Features.StockScan;

public sealed record StockScanRegisterDto(IReadOnlyList<StockScanRegisterGroupDto>? Groups);
