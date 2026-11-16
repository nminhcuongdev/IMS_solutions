namespace Ims.Api.Features.History;

public sealed record PrintHistoryDto(
    long No, DateTime WorkedDate, string Category, string? DocNo, DateOnly? ScheduledDate,
    string? ItemCode, string? ItemName, decimal? Qty, string? UomCode, int PackageCount,
    string? Spec, int Status, string? StatusName, string? CreatedBy, string? WorkerName,
    bool CanReprint = false, long? ReprintOfNo = null);
