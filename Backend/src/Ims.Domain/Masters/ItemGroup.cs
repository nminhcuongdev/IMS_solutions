using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class ItemGroup : SoftDeletableEntity
{
    public string ItemGroupCode { get; set; } = null!;
    public string ItemGroupName { get; set; } = null!;
    public ItemGroupCategory Category { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
