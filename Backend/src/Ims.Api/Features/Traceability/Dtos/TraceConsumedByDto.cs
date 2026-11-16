namespace Ims.Api.Features.Traceability;

public sealed record TraceConsumedByDto(string? ProductionOrderNo, string? ProductionLotNo, string? ProcessCode, decimal UsedQty, decimal ReturnedQty, decimal Qty, IReadOnlyList<TraceOutputRefDto> Outputs);
