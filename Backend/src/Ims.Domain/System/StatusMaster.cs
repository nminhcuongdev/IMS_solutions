namespace Ims.Domain.System;

public class StatusMaster
{
    public int StatusCode { get; set; }
    public string StatusNameLang1 { get; set; } = null!;
    public string StatusNameLang2 { get; set; } = null!;
    public string StatusNameLang3 { get; set; } = null!;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
