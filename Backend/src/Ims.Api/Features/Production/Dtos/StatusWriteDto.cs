namespace Ims.Api.Features.Production;

public sealed record StatusWriteDto(IReadOnlyList<string> OrderNos, string Status);
