using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class Device : SoftDeletableEntity
{
    public string DeviceCode { get; set; } = null!;
    public string DeviceName { get; set; } = null!;
    public DeviceType DeviceType { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Spec { get; set; }
    public string? SerialNo { get; set; }
    public string? IpAddress { get; set; }
    public string? LocationCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }

    public Location? Location { get; set; }
}
