namespace Ims.Api.Features.Import;

public sealed record ImportRowErrorDto(
    int Row,
    string Code,
    string Message,
    string? Detail = null);
