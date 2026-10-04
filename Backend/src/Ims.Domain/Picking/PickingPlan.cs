namespace Ims.Domain.Picking;

public class PickingPlan
{
    public long No { get; set; }

    public string SoNo { get; set; } = null!;
    public string SoBranchNo { get; set; } = "01";
    public string ItemCode { get; set; } = null!;

    public string? LocationCode { get; set; }

    public decimal Qty { get; set; }

    public decimal PickedQty { get; set; }

    public DateOnly? ScheduledDate { get; set; }

    public int Status { get; set; } = PickingStatus.NotPicked;

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
