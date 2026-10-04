namespace Ims.Domain.Returns;

public class ReturnResult
{
    public long No { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ReturnNo { get; set; }
    public string? ReturnBranchNo { get; set; }
    public string? PartnerCode { get; set; }
    public string ItemCode { get; set; } = null!;
    public decimal Qty { get; set; }
    public string? LocationCode { get; set; }
    public string? LotNo { get; set; }
    public string? ReasonCode { get; set; }
    public string? CreatedBy { get; set; }
}
