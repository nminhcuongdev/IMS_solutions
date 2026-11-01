namespace Ims.Domain.Production;

public class ProductionOrder
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ProductionOrderNo { get; set; } = null!;
    public DateOnly OrderDate { get; set; }
    public string FactoryCode { get; set; } = null!;
    public string OutputItemCode { get; set; } = null!;
    public decimal PlannedQty { get; set; }
    public string UomCode { get; set; } = null!;
    public string? OutputLotNo { get; set; }
    public string? ProductionLotNo { get; set; }
    public string StorageLocationCode { get; set; } = null!;
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly PlannedEndDate { get; set; }
    public int StatusCode { get; set; }
    public string? CancelReasonCode { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string CreatedBy { get; set; } = null!;
}
