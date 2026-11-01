namespace Ims.Domain.Production;

public class ProductionOrderLine
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ProductionOrderNo { get; set; } = null!;
    public int LineNo { get; set; }
    public string ProcessCode { get; set; } = null!;
    public string ItemCode { get; set; } = null!;
    public decimal PlannedQty { get; set; }
    public string UomCode { get; set; } = null!;
    public DateOnly ScheduledDate { get; set; }
    public DateTime? PickedAt { get; set; }
    public string? PickedBy { get; set; }
    public string CreatedBy { get; set; } = null!;
}
