namespace Ims.Domain.Stocktaking;

public static class StocktakingSessionStatus
{
    public const int InProgress = 1;
    public const int Completed = 2;
    public const int Finalized = 21;

    public static string ToApiName(int status) => status switch
    {
        InProgress => "slStatusInProgressBadge",
        Completed => "slStatusCompletedBadge",
        Finalized => "slStatusBeforeBadge",
        _ => "slStatusBeforeBadge",
    };
}
