namespace Ims.Api.Features.Returns;

public sealed record ReturnStatusChangeResult(int Updated, IReadOnlyList<string> Skipped);
