namespace Ims.Api.Features.Production;

public sealed record ProductionLabelPlanDto(decimal StdQty, int LabelCount, decimal RemainderQty);
