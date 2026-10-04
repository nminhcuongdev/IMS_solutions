using Ims.Domain.Common;

namespace Ims.Domain.System;

public class LanguageData : AuditableEntity
{
    public long No { get; set; }
    public string LabelKey { get; set; } = null!;
    public string LanguageCode { get; set; } = null!;
    public string LabelText { get; set; } = null!;
    public string? DeviceScope { get; set; }
}
