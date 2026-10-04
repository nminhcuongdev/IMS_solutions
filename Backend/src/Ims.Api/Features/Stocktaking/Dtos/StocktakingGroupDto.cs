namespace Ims.Api.Features.Stocktaking;

public sealed record StocktakingGroupDto(
    string ItemCode,
    string? ItemName,
    string LocationText,
    string? LotNo,
    int PlannedLabels,
    int ScannedLabels,
    decimal PlannedQty,
    decimal ResultQty,
    string? Unit,
    DateTime? UpdatedAt);
