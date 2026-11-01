namespace Ims.Domain.Inventory;

public static class InventoryAction
{
    public const string Receiving = "Receiving";
    public const string Shipping = "Shipping";
    public const string Picking = "Picking";
    public const string Returns = "Returns";
    public const string Adjust = "Adjust";
    public const string Hold = "Hold";
    public const string HoldRelease = "HoldRelease";
    public const string Disposal = "Disposal";
    public const string Consume = "Consume";
    public const string Produce = "Produce";
    public const string ProdReturn = "ProdReturn";
    public const string ProdPick = "ProdPick";
    public const string ProdReceipt = "ProdReceipt";
    public const string ProdLabel = "ProdLabel";

    private static readonly string[] All =
    [
        Receiving, Shipping, Picking, Returns, Adjust, Hold, HoldRelease, Disposal,
        Consume, Produce, ProdReturn, ProdPick, ProdReceipt, ProdLabel,
    ];

    public static bool IsDefined(string? action) =>
        action is not null && Array.Exists(All, a => a == action);

    public static string ToLabelKey(string action) =>
        "action" + action;
}
