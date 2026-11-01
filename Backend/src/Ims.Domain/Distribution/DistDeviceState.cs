namespace Ims.Domain.Distribution;

public class DistDeviceState
{
    public string DeviceCode { get; set; } = null!;
    public int? VersionCode { get; set; }
    public DateTime? LastCheckAt { get; set; }
    public DateTime? LastUpdateAt { get; set; }
}
