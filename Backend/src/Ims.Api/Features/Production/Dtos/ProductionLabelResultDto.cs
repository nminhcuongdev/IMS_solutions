namespace Ims.Api.Features.Production;

public sealed record ProductionLabelResultDto(string ProductionOrderNo, string ItemCode, string? LotNo, decimal TotalQty,
    decimal StdQty, int LabelCount, decimal RemainderQty, IReadOnlyList<ProductionLabelDto> Labels);
