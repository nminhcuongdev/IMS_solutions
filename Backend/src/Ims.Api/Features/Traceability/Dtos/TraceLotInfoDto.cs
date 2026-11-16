namespace Ims.Api.Features.Traceability;

public sealed record TraceLotInfoDto(string ItemCode, string? ItemName, string? LotNo, DateOnly? ExpiryDate, string? ProductionOrderNo);
