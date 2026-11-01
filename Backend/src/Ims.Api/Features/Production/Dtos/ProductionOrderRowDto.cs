namespace Ims.Api.Features.Production;

public sealed record ProductionOrderRowDto(long No, string ProductionOrderNo, DateOnly OrderDate,
    string FactoryCode, string OutputItemCode, string? OutputItemName, decimal PlannedQty,
    decimal OutputQty, string UomCode, string? ProductionLotNo, string? OutputLotNo,
    string StorageLocationCode, DateOnly PlannedStartDate, DateOnly PlannedEndDate,
    DateTime? StartedAt, DateTime? CompletedAt, int StatusCode);
