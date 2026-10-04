namespace Ims.Api.Features.Picking;

public sealed record PickingResultDto(
    long No,
    DateTime CreatedAt,
    string ItemCode,
    string? ItemName,
    decimal Qty,
    string? Unit,
    string SoNo,
    string SerialNo,
    DateOnly? ScheduledDate,
    string LocationCode,
    string? LocationName,
    string? LotNo,
    string? Worker);
