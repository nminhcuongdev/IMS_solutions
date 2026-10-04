namespace Ims.Api.Features.Picking;

public sealed record PickingPlanDto(
    string SoNo,
    string SerialNo,
    string ItemCode,
    string? ItemName,
    string? LocationCode,
    string? LocationName,
    decimal PlannedQty,
    string? Unit,
    decimal PickedQty,
    DateOnly? ScheduledDate,
    string Status,
    DateTime CreatedAt,
    string? CreatedBy);
