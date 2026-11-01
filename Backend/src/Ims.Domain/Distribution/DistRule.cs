namespace Ims.Domain.Distribution;

public class DistRule
{
    public int Id { get; set; }
    public int PackageId { get; set; }
    public string Models { get; set; } = "[]";
    public string Locations { get; set; } = "[]";
    public string IncludeDevices { get; set; } = "[]";
    public string ExcludeDevices { get; set; } = "[]";
    public string StaticTargets { get; set; } = "[]";
    public bool AutoAdd { get; set; } = true;
    public DateTime DeliverAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DistRuleState State { get; set; }
    public bool Force { get; set; }
    public int? MinVersionCode { get; set; }
    public string? NoteJa { get; set; }
    public string? NoteEn { get; set; }
    public string? NoteVi { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? Error { get; set; }
    public string? StopReason { get; set; }
    public string? DeploySteps { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public DistPackage Package { get; set; } = null!;
}
