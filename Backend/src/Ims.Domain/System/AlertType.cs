namespace Ims.Domain.System;

public static class AlertType
{
    public const string ExpiryWarning = "ExpiryWarning";
    public const string LowStock = "LowStock";
    public const string StockOverflow = "StockOverflow";
    public const string HoldReview = "HoldReview";
    public const string ReceivingDelay = "ReceivingDelay";
    public const string PickingDelay = "PickingDelay";
    public const string ShippingDelay = "ShippingDelay";

    public static readonly string[] Ordered =
    [
        ExpiryWarning, LowStock, StockOverflow, HoldReview,
        ReceivingDelay, PickingDelay, ShippingDelay,
    ];

    public static string LabelKey(string code) => code switch
    {
        ExpiryWarning => "alertExpiry7",
        LowStock => "alertLowInventory",
        StockOverflow => "alertStockOverflow",
        HoldReview => "alertHoldReview",
        ReceivingDelay => "alertReceivingDelay",
        PickingDelay => "alertPickingDelay",
        ShippingDelay => "alertShippingDelay",
        _ => code,
    };

    public static string ExtraLabelKey(string code) => code switch
    {
        ExpiryWarning => "thExpirationDate",
        LowStock or StockOverflow => "thStockQty",
        HoldReview => "thHoldReason",
        ReceivingDelay => "thPoNo",
        _ => "thSlipNo",
    };
}
