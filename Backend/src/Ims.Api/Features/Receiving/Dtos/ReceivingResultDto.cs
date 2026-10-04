namespace Ims.Api.Features.Receiving;

public sealed record ReceivingResultDto(
    long No,
    DateTime CreatedAt,
    string ItemCode,
    string? ItemName,
    decimal Qty,
    string? Unit,
    string? PoNo,
    string? SerialNo,
    string? InvoiceNo,
    string? LocationCode,
    string? Worker,
    string Status,
    string? ShortageReason,
    DateOnly? ExpiryDate,
    string? LotNo = null,
    string? SupplierCode = null,
    string? SupplierName = null,
    DateOnly? ScheduledDate = null);
