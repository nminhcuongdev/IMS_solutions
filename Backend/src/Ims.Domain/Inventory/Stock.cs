namespace Ims.Domain.Inventory;

public class Stock
{
    public string ItemCode { get; set; } = null!;
    public string SerialNo { get; set; } = null!;
    public string FactoryCode { get; set; } = null!;
    public string? LocationCode { get; set; }
    public string? LotNo { get; set; }

    public decimal StockQty { get; set; }
    public decimal ReservedQty { get; set; }

    public decimal AvailableQty { get; set; }

    public bool IsOnHold { get; set; }

    public string? ReasonCode { get; set; }

    public DateOnly? ExpiryDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public Masters.Item? Item { get; set; }
    public Masters.Location? Location { get; set; }

    public void Recalculate()
    {
        AvailableQty = IsOnHold ? 0 : StockQty - ReservedQty;
    }
}
