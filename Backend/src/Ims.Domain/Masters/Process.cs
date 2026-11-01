using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class Process : SoftDeletableEntity
{
    public string ProcessCode { get; set; } = null!;
    public string ProcessName { get; set; } = null!;
    public int? DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }
}
