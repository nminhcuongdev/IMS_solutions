namespace Ims.Domain.History;

public class PrintHistory
{
    public long No { get; set; }
    public string Category { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? DocNo { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public string? ItemCode { get; set; }
    public decimal? Qty { get; set; }
    public string? UomCode { get; set; }
    public int PackageCount { get; set; }
    public string? Spec { get; set; }
    public int Status { get; set; }
    public string? CreatedBy { get; set; }
    public string? PrintData { get; set; }
    public string? LayoutFile { get; set; }
    public long? ReprintOfNo { get; set; }
}
