namespace Ims.Domain.Distribution;

public class DistAudit
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    public string Who { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? Target { get; set; }
}
