namespace Ims.Domain.Receiving;

public class ReceivingResult
{
    public long No { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? PoNo { get; set; }
    public string? PoBranchNo { get; set; }
    public string ItemCode { get; set; } = null!;
    public decimal Qty { get; set; }
    public string? LocationCode { get; set; }
    public string? LotNo { get; set; }
    public bool IsOnHold { get; set; }
    public string? ReasonCode { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? CreatedBy { get; set; }

    public string? InvoiceNo { get; set; }

    public string? RequestId { get; set; }
}
