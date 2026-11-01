namespace Ims.Domain.Shipping;

public class ShippingResultLot
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }
    public long ShippingResultNo { get; set; }
    public string ItemCode { get; set; } = null!;
    public string? LotNo { get; set; }
    public string SerialNo { get; set; } = null!;
    public decimal Qty { get; set; }
    public string CreatedBy { get; set; } = null!;
    public ShippingResult ShippingResult { get; set; } = null!;
}
