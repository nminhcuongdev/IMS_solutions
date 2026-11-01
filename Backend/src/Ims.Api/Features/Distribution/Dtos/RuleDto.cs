namespace Ims.Api.Features.Distribution;

public sealed record RuleDto(int Id, int PackageId, string VersionName, int VersionCode, string ApkFile, string Sha256, string[] Models, string[] Locations, string[] Include, string[] Exclude, bool AutoAdd, DateTime DeliverAt, DateTime? EndAt, string State, bool Force, int? MinVersionCode, string? NoteJa, string? NoteEn, string? NoteVi, DateTime? PublishedAt, DateTime? ActivatedAt, string? Error, string? StopReason, string[] DeploySteps, string? CreatedBy, DateTime CreatedAt, RuleSummaryDto Summary);
