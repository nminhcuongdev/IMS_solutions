namespace Ims.Domain.Shipping;

public class ShippingResult
{
    public long No { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? SoNo { get; set; }
    public string? SoBranchNo { get; set; }
    public string ItemCode { get; set; } = null!;
    public decimal Qty { get; set; }
    public string? LocationCode { get; set; }
    public string? LotNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? CreatedBy { get; set; }
}
