using Ims.Domain.Common;

namespace Ims.Domain.System;

public class RolePermission : AuditableEntity
{
    public long No { get; set; }
    public string RoleCode { get; set; } = null!;
    public string PermissionCode { get; set; } = null!;
    public bool IsAllowed { get; set; } = true;
}
