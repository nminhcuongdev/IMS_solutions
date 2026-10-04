namespace Ims.Api.Features.Picking;

public sealed record PickingPlanWriteDto(
    string SoNo,
    string? SerialNo = null,
    string ItemCode = "",
    string? LocationCode = null,
    decimal PlannedQty = 0,
    DateOnly? ScheduledDate = null);
