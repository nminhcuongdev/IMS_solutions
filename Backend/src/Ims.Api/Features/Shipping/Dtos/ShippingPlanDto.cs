namespace Ims.Api.Features.Shipping;

public sealed record ShippingPlanDto(
    string SlipNo,
    string SerialNo,
    string? CustomerCode,
    string? CustomerName,
    string ItemCode,
    string? ItemName,
    decimal PlannedQty,
    string? Unit,
    decimal ActualQty,
    string? PackingTypeCode,
    decimal? PackingQty,
    DateOnly? ScheduledDate,
    string? GateCode,
    string? InvoiceNo,
    string Status,
    string? ShortageReason,
    bool LabelIssued,
    DateTime CreatedAt,
    string? CreatedBy);
