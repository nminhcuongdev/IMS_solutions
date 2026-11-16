namespace Ims.Api.Features.Production;

public sealed record ProductionWorkDto(string Type, long No, string ProductionOrderNo,
    string? ItemCode, string? LotNo, decimal? Qty, DateTime CreatedAt, string? CreatedBy,
    bool IsReversed = false, int? LineNo = null, string? SerialNo = null, string? LocationCode = null,
    DateOnly? ExpiryDate = null, long? ReversesNo = null, string? RequestId = null)
{
    public DateTime At => CreatedAt;
    public string? UserCode => CreatedBy;
    public string Status => ReversesNo != null ? "reversal" : IsReversed ? "reversed" : "normal";
}
