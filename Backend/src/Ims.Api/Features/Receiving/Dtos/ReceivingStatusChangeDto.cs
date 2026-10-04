namespace Ims.Api.Features.Receiving;

public sealed record ReceivingStatusChangeDto(
    IReadOnlyList<string>? PoNos = null,
    string? Status = null,
    IReadOnlyList<ReceivingPlanKeyDto>? Lines = null);
