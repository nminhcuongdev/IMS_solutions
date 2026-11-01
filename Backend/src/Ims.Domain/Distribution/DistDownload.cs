namespace Ims.Domain.Distribution;

public class DistDownload
{
    public string DeviceCode { get; set; } = null!;
    public int RuleId { get; set; }
    public DateTime CompletedAt { get; set; }
}
