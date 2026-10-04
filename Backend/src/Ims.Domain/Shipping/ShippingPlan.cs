namespace Ims.Domain.Shipping;

public class ShippingPlan
{
    public long No { get; set; }

    public string SoNo { get; set; } = null!;
    public string SoBranchNo { get; set; } = "01";
    public string? PartnerCode { get; set; }
    public string ItemCode { get; set; } = null!;
    public decimal Qty { get; set; }

    public decimal ActualQty { get; set; }

    public string? FactoryCode { get; set; }

    public string? GateCode { get; set; }

    public DateOnly? ScheduledDate { get; set; }

    public int Status { get; set; } = ShippingStatus.Pending;

    public string? ReasonCode { get; set; }

    public string? InvoiceNo { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
