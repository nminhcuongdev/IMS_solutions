namespace Ims.Domain.Distribution;

public class DistUnknownDevice
{
    public string DeviceCode { get; set; } = null!;
    public DateTime FirstAt { get; set; }
    public DateTime LastAt { get; set; }
    public int RequestCount { get; set; }
}
