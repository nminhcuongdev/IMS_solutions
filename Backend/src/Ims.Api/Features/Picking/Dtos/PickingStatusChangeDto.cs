namespace Ims.Api.Features.Picking;

public sealed record PickingStatusChangeDto(
    IReadOnlyList<string>? SoNos = null,
    string? Status = null,
    IReadOnlyList<PickingPlanKeyDto>? Lines = null);
