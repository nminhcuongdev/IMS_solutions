namespace Ims.Domain.Distribution;

public class DistCheck
{
    public long Id { get; set; }
    public string DeviceCode { get; set; } = null!;
    public DateTime ReceivedAt { get; set; }
    public int AppVersion { get; set; }
}
