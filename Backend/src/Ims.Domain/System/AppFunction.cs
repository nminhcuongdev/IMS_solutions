using Ims.Domain.Common;

namespace Ims.Domain.System;

public class AppFunction : SoftDeletableEntity
{
    public string FunctionCode { get; set; } = null!;
    public string FunctionName { get; set; } = null!;
    public string? LargeCode { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
