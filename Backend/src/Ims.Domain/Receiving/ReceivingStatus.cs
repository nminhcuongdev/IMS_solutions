namespace Ims.Domain.Receiving;

public static class ReceivingStatus
{
    public const int NotArrived = 0;
    public const int Partial = 1;
    public const int Arrived = 2;
    public const int Cancelled = 3;

    public static string ToApiName(int status) => status switch
    {
        NotArrived => "statusNotArrived",
        Partial => "statusPartial",
        Arrived => "statusArrived",
        Cancelled => "statusCancelled",
        _ => "statusNotArrived",
    };

    public static int? FromApiName(string? name) => name switch
    {
        "statusNotArrived" => NotArrived,
        "statusPartial" => Partial,
        "statusArrived" => Arrived,
        "statusCancelled" => Cancelled,
        _ => null,
    };
}
