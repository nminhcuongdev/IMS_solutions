namespace Ims.Domain.Stocktaking;

public static class StocktakingLabelStatus
{
    public const int NotStarted = 0;
    public const int Normal = 14;
    public const int Missing = 22;
    public const int Over = 23;

    public static string ToApiName(int status) => status switch
    {
        NotStarted => "slFilterNotScanned",
        Missing => "slStatusDisposal",
        Over => "slStatusOver",
        _ => "slStatusNormal",
    };
}
