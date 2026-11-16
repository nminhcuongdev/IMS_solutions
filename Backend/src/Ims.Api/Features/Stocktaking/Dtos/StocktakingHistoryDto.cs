namespace Ims.Api.Features.Stocktaking;

public sealed record StocktakingHistoryDto(
    string ItemCode,
    string? ItemName,
    string LotNo,
    string StocktakingName,
    DateTime StocktakingDate,
    decimal PlannedQty,
    decimal ResultQty,
    decimal MissingQty,
    string? Unit,
    string BadgeKey,
    long StocktakingSessionNo = 0);
