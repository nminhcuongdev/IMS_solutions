namespace Ims.Api.Features.Distribution;

public sealed record RuleWriteDto(int PackageId, string[]? Models = null, string[]? Locations = null, string[]? Include = null, string[]? Exclude = null, DateTime? DeliverAt = null, DateTime? EndAt = null, bool Force = false, int? MinVersionCode = null, string? NoteJa = null, string? NoteEn = null, string? NoteVi = null);
