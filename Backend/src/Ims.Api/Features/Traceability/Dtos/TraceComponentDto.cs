namespace Ims.Api.Features.Traceability;

public sealed record TraceComponentDto(int Level, string? ProcessCode, string? ProcessName, string ItemCode, string? ItemName, string? LotNo, decimal InputQty, decimal ReturnedQty, decimal Qty, string? UomCode, DateOnly? ExpiryDate, string? ExpiryCheck, DateTime? InputAt, string? ProductionOrderNo, bool LotUnknown);
