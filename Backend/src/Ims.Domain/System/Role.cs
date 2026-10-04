using Ims.Domain.Common;

namespace Ims.Domain.System;

public class Role : SoftDeletableEntity
{
    public string RoleCode { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
