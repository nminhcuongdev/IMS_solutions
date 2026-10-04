namespace Ims.Api.Features.Alerts;

public sealed record AlertRowDto(
    string Code,
    string? Name,
    string? Extra);
