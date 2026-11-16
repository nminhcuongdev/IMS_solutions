namespace Ims.Api.Features.Shipping;

public sealed record ShippingResultWriteDto(
    string ItemCode = "",
    decimal? Qty = null,
    string? SlipNo = null,
    string? SerialNo = null,
    string? LocationCode = null,
    string? LotNo = null,
    DateOnly? ExpiryDate = null,
    string? ReasonCode = null,
    string? RequestId = null);
