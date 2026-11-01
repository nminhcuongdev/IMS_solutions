namespace Ims.Domain.Production;

public class ProductionLabel
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RequestId { get; set; } = null!;
    public string OutputRequestId { get; set; } = null!;
    public string ProductionOrderNo { get; set; } = null!;
    public string ItemCode { get; set; } = null!;
    public string LotNo { get; set; } = null!;
    public decimal TotalQty { get; set; }
    public decimal StdQty { get; set; }
    public int LabelCount { get; set; }
    public decimal RemainderQty { get; set; }
    public string CreatedBy { get; set; } = null!;
}
