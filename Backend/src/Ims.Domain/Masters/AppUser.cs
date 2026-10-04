using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class AppUser : SoftDeletableEntity
{
    public string UserCode { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string RoleCode { get; set; } = null!;
    public string? FactoryCode { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }
}
