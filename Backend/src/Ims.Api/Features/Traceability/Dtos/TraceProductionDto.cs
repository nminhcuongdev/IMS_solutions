namespace Ims.Api.Features.Traceability;

public sealed record TraceProductionDto(string ProductionOrderNo, string? ProductionLotNo, string? OutputLotNo, DateTime? StartedAt, DateTime? CompletedAt, decimal OutputQty, string? UomCode);
