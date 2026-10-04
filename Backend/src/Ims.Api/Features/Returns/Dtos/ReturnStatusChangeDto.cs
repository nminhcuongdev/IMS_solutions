namespace Ims.Api.Features.Returns;

public sealed record ReturnStatusChangeDto(
    IReadOnlyList<string>? ReturnNos = null,
    string? Status = null,
    IReadOnlyList<ReturnPlanKeyDto>? Lines = null);
