namespace Ims.Domain.System;

public class FunctionVisibility
{
    public string TargetType { get; set; } = null!;
    public string TargetCode { get; set; } = null!;
    public bool IsVisible { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
