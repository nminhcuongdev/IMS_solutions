using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class Factory : SoftDeletableEntity
{
    public string FactoryCode { get; set; } = null!;
    public string FactoryName { get; set; } = null!;
    public string? Country { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }

    public ICollection<Zone> Zones { get; set; } = new List<Zone>();
}
