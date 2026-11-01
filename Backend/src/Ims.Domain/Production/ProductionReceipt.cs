namespace Ims.Domain.Production;

public class ProductionReceipt
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RequestId { get; set; } = null!;
    public string ProductionOrderNo { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
}
