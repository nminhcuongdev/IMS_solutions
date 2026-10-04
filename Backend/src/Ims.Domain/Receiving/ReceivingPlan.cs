namespace Ims.Domain.Receiving;

public class ReceivingPlan
{
    public long No { get; set; }

    public string PoNo { get; set; } = null!;
    public string PoBranchNo { get; set; } = "01";
    public string? PartnerCode { get; set; }
    public string ItemCode { get; set; } = null!;
    public decimal Qty { get; set; }

    public decimal ActualQty { get; set; }

    public string? FactoryCode { get; set; }
    public DateOnly? ScheduledDate { get; set; }

    public int Status { get; set; } = ReceivingStatus.NotArrived;

    public string? ReasonCode { get; set; }

    public string? InvoiceNo { get; set; }

    public string? LotNo { get; set; }

    public string? LocationCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
