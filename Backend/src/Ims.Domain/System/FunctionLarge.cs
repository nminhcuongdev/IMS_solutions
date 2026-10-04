namespace Ims.Domain.System;

public class FunctionLarge
{
    public string LargeCode { get; set; } = null!;
    public string LargeName { get; set; } = null!;
    public int? DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
