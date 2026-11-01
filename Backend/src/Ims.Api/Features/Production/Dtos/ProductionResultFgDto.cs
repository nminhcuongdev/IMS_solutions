namespace Ims.Api.Features.Production;

public sealed record ProductionResultFgDto(long No, string RequestId, string ProductionOrderNo,
    string ItemCode, string LotNo, decimal Qty, int SerialCount, DateTime CreatedAt,
    string CreatedBy, bool LabelIssued, bool IsReversed = false, string? ItemName = null,
    string? ProductionLotNo = null, string? Unit = null, string? LocationCode = null,
    string? SerialFrom = null, string? SerialTo = null, string? Worker = null);
