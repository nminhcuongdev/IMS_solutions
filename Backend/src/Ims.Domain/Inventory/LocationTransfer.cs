namespace Ims.Domain.Inventory;

public class LocationTransfer
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }

    public string ItemCode { get; set; } = null!;

    public string? LotNo { get; set; }

    public string FromFactoryCode { get; set; } = null!;
    public string FromLocationCode { get; set; } = null!;
    public string ToFactoryCode { get; set; } = null!;
    public string ToLocationCode { get; set; } = null!;

    public decimal Qty { get; set; }

    public string? CreatedBy { get; set; }
}
