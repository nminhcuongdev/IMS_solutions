namespace Ims.Api.Features.History;

public sealed record PrintHistoryWriteDto(
    string Category = "",
    string? DocNo = null,
    string? ItemCode = null,
    decimal? Qty = null,
    string? UomCode = null,
    int PackageCount = 1,
    string? Spec = null,
    DateOnly? ScheduledDate = null,
    bool Reprint = false);
