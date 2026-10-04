using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class Zone : SoftDeletableEntity
{
    public string ZoneCode { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
    public string FactoryCode { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }

    public Factory? Factory { get; set; }
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
