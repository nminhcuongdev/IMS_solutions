namespace Ims.Api.Features.Shipping;

public sealed record ShippingResultDto(
    long No,
    DateTime CreatedAt,
    string ItemCode,
    string? ItemName,
    decimal Qty,
    string? Unit,
    string? SlipNo,
    string? SerialNo,
    string? CustomerCode,
    string? CustomerName,
    string? LocationCode,
    string? LotNo,
    DateOnly? ExpiryDate,
    DateOnly? ScheduledDate,
    string? InvoiceNo,
    string? Worker,
    string Status,
    string? ShortageReason);
