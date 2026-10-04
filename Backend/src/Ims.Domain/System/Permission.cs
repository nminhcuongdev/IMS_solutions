using Ims.Domain.Common;

namespace Ims.Domain.System;

public class Permission : SoftDeletableEntity
{
    public string PermissionCode { get; set; } = null!;
    public string FunctionCode { get; set; } = null!;
    public string Action { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
