namespace Ims.Api.Features.Traceability;

public sealed record TraceQueryDto(
    string Direction = "backward", string? ItemCode = null, string? LotNo = null,
    string? SoNo = null, string? Customer = null,
    DateOnly? ShippedFrom = null, DateOnly? ShippedTo = null,
    string? PoNo = null, DateOnly? ReceivedFrom = null, DateOnly? ReceivedTo = null,
    string? OrderNo = null, string? ProductionLotNo = null,
    DateOnly? ProductionDateFrom = null, DateOnly? ProductionDateTo = null,
    string? FgLotNo = null, string? MaterialLotNo = null,
    int? Page = null, int? PageSize = null, string? Sort = null);
