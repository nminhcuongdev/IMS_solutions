namespace Ims.Api.Features.Receiving;

public sealed record ReceivingShortageDto(
    decimal? ShortageQty = null,
    string? ReasonCode = null,
    string? SerialNo = null);
