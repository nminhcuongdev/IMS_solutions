namespace Ims.Domain.Production;

public class ProductionResult
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RequestId { get; set; } = null!;
    public string ProductionOrderNo { get; set; } = null!;
    public string ResultType { get; set; } = null!;
    public int? LineNo { get; set; }
    public string ItemCode { get; set; } = null!;
    public string? LotNo { get; set; }
    public string SerialNo { get; set; } = null!;
    public decimal Qty { get; set; }
    public string LocationCode { get; set; } = null!;
    public string FactoryCode { get; set; } = null!;
    public DateOnly? ExpiryDate { get; set; }
    public long? ReversesNo { get; set; }
    public string? ReasonCode { get; set; }
    public DateTime? LabelIssuedAt { get; set; }
    public string? LabelIssuedBy { get; set; }
    public string CreatedBy { get; set; } = null!;
}
