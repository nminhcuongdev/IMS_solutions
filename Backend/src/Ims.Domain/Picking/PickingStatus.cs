namespace Ims.Domain.Picking;

public static class PickingStatus
{
    public const int NotPicked = 0;
    public const int Picked = 2;
    public const int Cancelled = 3;

    public static string ToApiName(int status) => status switch
    {
        Picked => "statusPicked",
        Cancelled => "statusCancelled",
        _ => "statusNotPicked",
    };

    public static int? FromApiName(string? name) => name switch
    {
        "statusNotPicked" => NotPicked,
        "statusPicked" => Picked,
        "statusCancelled" => Cancelled,
        _ => null,
    };
}
