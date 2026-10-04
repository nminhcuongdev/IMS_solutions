namespace Ims.Domain.System;

public class AlertConfig
{
    public string AlertTypeCode { get; set; } = null!;

    public string AlertTypeName { get; set; } = null!;

    public decimal? MaxThreshold { get; set; }

    public decimal? MinThreshold { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
