namespace Ims.Api.Features.Returns;

public sealed record ReturnShortageDto(
    decimal? ShortageQty = null,
    string? ReasonCode = null,
    string? SerialNo = null);
