namespace Ims.Api.Features.Stocktaking;

public sealed record StocktakingSessionDto(
    long No,
    string SessionName,
    int Status,
    string StatusLabelKey,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    bool AcceptsResults,
    int LabelCount,
    int ScannedCount);
