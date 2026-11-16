namespace Ims.Api.Features.Stocktaking;

public sealed record StocktakingResultDto(
    string? LocationCode,
    string? ItemCode,
    string? LotNo,
    int CountedLabels,
    string? RequestId = null);
