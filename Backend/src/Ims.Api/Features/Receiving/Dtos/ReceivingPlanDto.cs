namespace Ims.Api.Features.Receiving;

public sealed record ReceivingPlanDto(
    string PoNo,
    string SerialNo,
    string? SupplierCode,
    string? SupplierName,
    string ItemCode,
    string? ItemName,
    decimal PlannedQty,
    string? Unit,
    decimal ActualQty,
    string? PackingTypeCode,
    decimal? PackingQty,
    DateOnly? ScheduledDate,
    string? InvoiceNo,
    string Status,
    string? ShortageReason,
    string? LotNo,
    string? LocationCode,
    bool LabelIssued,
    DateTime CreatedAt,
    string? CreatedBy);
