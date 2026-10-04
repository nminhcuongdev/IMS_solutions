using Ims.Domain.Common;

namespace Ims.Domain.System;

public class DefaultValue : AuditableEntity
{
    public string ValueType { get; set; } = null!;
    public string ValueCode { get; set; } = null!;
    public string NameLang1 { get; set; } = null!;
    public string NameLang2 { get; set; } = null!;
    public string NameLang3 { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
