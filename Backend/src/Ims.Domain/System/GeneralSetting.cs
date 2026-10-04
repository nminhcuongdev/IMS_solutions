using Ims.Domain.Common;

namespace Ims.Domain.System;

public class GeneralSetting : AuditableEntity
{
    public string ConfigCode { get; set; } = null!;
    public string? ConfigValue { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
}
