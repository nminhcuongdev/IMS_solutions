namespace Ims.Api.Features.Shipping;

public sealed record ShippingStatusChangeDto(
    IReadOnlyList<string>? SlipNos = null,
    string? Status = null,
    IReadOnlyList<ShippingPlanKeyDto>? Lines = null);
