namespace Ims.Domain.Inventory;

public class InventoryHistory
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }

    public string ItemCode { get; set; } = null!;
    public string? FactoryCode { get; set; }
    public string? LocationCode { get; set; }
    public string? LotNo { get; set; }
    public string? SerialNo { get; set; }

    public string ActionType { get; set; } = null!;

    public decimal? Qty { get; set; }

    public string? ReasonCode { get; set; }
    public string? ReferenceNo { get; set; }
    public string? CreatedBy { get; set; }

    public Masters.Item? Item { get; set; }
    public Masters.Location? Location { get; set; }
}
