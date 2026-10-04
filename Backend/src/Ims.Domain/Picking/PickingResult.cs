namespace Ims.Domain.Picking;

public class PickingResult
{
    public long No { get; set; }

    public DateTime CreatedAt { get; set; }

    public string SoNo { get; set; } = null!;
    public string SoBranchNo { get; set; } = "01";
    public string ItemCode { get; set; } = null!;
    public decimal Qty { get; set; }
    public string LocationCode { get; set; } = null!;
    public string? LotNo { get; set; }
    public string? CreatedBy { get; set; }
}
