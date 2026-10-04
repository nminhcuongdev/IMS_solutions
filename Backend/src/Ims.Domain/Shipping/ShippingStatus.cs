namespace Ims.Domain.Shipping;

public static class ShippingStatus
{
    public const int Pending = 0;
    public const int Partial = 1;
    public const int Shipped = 2;
    public const int Cancelled = 3;

    public static string ToApiName(int status) => status switch
    {
        Pending => "statusNotShipped",
        Partial => "statusPartialShip",
        Shipped => "statusShipped",
        Cancelled => "statusCancelled",
        _ => "statusNotShipped",
    };

    public static int? FromApiName(string? name) => name switch
    {
        "statusNotShipped" => Pending,
        "statusPartialShip" => Partial,
        "statusShipped" => Shipped,
        "statusCancelled" => Cancelled,
        _ => null,
    };
}
