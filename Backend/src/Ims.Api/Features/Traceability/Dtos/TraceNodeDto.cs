namespace Ims.Api.Features.Traceability;

public sealed record TraceNodeDto(int Level, long? ParentNo, string Type, string ItemCode,
    string? LotNo, decimal Qty, string? DocumentNo, string? PartnerCode, DateTime? OccurredAt,
    string? ProcessCode = null, decimal? InputQty = null, decimal? ReturnedQty = null,
    DateOnly? ExpiryDate = null, string? ExpiryCheck = null, string? ItemName = null,
    string? ProcessName = null, string? ProductionLotNo = null, string? ExpiryState = null,
    string? Unit = null, string? PartnerName = null, IReadOnlyList<string>? Suppliers = null,
    string? Worker = null);
