using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class Location : SoftDeletableEntity
{
    public string LocationCode { get; set; } = null!;
    public LocationType LocationType { get; set; }
    public string LargeCode { get; set; } = null!;
    public string? MiddleCode { get; set; }
    public string? SmallCode { get; set; }
    public string LocationName { get; set; } = null!;
    public string? ZoneCode { get; set; }
    public string? ParentLocationCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }

    public Zone? Zone { get; set; }
    public Location? Parent { get; set; }
    public ICollection<Location> Children { get; set; } = new List<Location>();
}
