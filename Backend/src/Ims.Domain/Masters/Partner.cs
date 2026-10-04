using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class Partner : SoftDeletableEntity
{
    public string PartnerCode { get; set; } = null!;
    public string PartnerName { get; set; } = null!;
    public bool IsSupplier { get; set; }
    public bool IsCustomer { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }
}
