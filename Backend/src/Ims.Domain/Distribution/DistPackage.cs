namespace Ims.Domain.Distribution;

public class DistPackage
{
    public int Id { get; set; }
    public string PackageName { get; set; } = null!;
    public int VersionCode { get; set; }
    public string VersionName { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = null!;
    public string SignerFingerprint { get; set; } = null!;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
