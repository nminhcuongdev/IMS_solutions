namespace Ims.Api.Features.Receiving;

public sealed record ReceivingResultWriteDto(
    string ItemCode = "",
    decimal? Qty = null,
    string? PoNo = null,
    string? SerialNo = null,
    string? LocationCode = null,
    string? LotNo = null,
    DateOnly? ExpiryDate = null,
    string? ReasonCode = null,
    string? InvoiceNo = null,
    bool IsOnHold = false,
    string? RequestId = null);
