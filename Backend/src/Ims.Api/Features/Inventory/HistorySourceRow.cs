namespace Ims.Api.Features.Inventory;

internal sealed class HistorySourceRow
{
    public long No { get; init; }
    public DateTime CreatedAt { get; init; }
    public string ItemCode { get; init; } = "";
    public string? LotNo { get; init; }
    public string ActionType { get; init; } = "";
    public decimal? Qty { get; init; }
    public string? LocationCode { get; init; }
    public string? ReasonCode { get; init; }
    public string? CreatedBy { get; init; }
}
