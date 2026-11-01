namespace Ims.Api.Features.Traceability;

internal sealed record TraceShipment(string ItemCode, string? LotNo, string? SerialNo,
    decimal Qty, string? SoNo, string? SoBranchNo, string? Customer,
    DateTime CreatedAt, long ResultNo);
