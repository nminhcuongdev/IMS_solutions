namespace Ims.Domain.Returns;

public static class ReturnStatus
{
    public const int Pending = 0;
    public const int Partial = 1;
    public const int Returned = 2;
    public const int Cancelled = 3;

    public static string ToApiName(int status) => status switch
    {
        Pending => "statusPendingReturn",
        Partial => "statusPartialReturn",
        Returned => "statusReturned",
        Cancelled => "statusCancelled",
        _ => "statusPendingReturn",
    };

    public static int? FromApiName(string? name) => name switch
    {
        "statusPendingReturn" => Pending,
        "statusPartialReturn" => Partial,
        "statusReturned" => Returned,
        "statusCancelled" => Cancelled,
        _ => null,
    };
}
