namespace Ims.Api.Features.Production;

public sealed record ProductionOutputResultDto(string OrderNo, string ItemCode, string LotNo,
    decimal Qty, IReadOnlyList<string> Serials, bool OverPlan, int LabelCount,
    decimal StdQty, decimal RemainderQty)
{
    public ProductionLabelPlanDto LabelPlan => new(StdQty, LabelCount, RemainderQty);
}
