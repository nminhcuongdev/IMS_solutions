namespace Ims.Api.Features.Shipping;

public sealed record ShippingShortageDto(
    decimal? ShortageQty = null,
    string? ReasonCode = null,
    string? SerialNo = null);
