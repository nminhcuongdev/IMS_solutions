namespace Ims.Api.Features.Production;

public sealed record ProductionLineRowDto(long No, string ProductionOrderNo, string OutputItemCode,
    int LineNo, DateOnly ScheduledDate, string ProcessCode, string ItemCode,
    string? ItemName, string? Category, decimal PlannedQty, decimal ConsumedQty,
    string UomCode, string? ProductionLotNo, string? OutputLotNo,
    string StorageLocationCode, bool Picked, bool Received, int HtInputCount,
    bool HtScanRequired, DateOnly PlannedStartDate, DateOnly PlannedEndDate,
    DateTime? StartedAt, DateTime? CompletedAt, int StatusCode, string? ProcessName = null,
    bool ProcessActive = true);
