namespace Ims.Api.Features.Traceability;

public sealed record TraceLotDto(string ItemCode, string? LotNo, string? ItemName,
    decimal Qty, string? PoNo, DateTime? ReceivedAt, string? SoNo,
    string? Customer, DateTime? ShippedAt, string? OrderNo = null,
    string? ProductionLotNo = null, DateTime? ProductionDate = null);
