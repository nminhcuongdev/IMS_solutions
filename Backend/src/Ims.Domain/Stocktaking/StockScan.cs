namespace Ims.Domain.Stocktaking;

public class StockScan
{
    public string Epc { get; set; } = null!;
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? ItemCategory { get; set; }
    public bool IsScanned { get; set; }
    public bool IsTarget { get; set; } = true;
    public string? WorkerCode { get; set; }
    public string? WorkerName { get; set; }
}
