namespace Ims.Api.Contracts;

public sealed record ApiErrorBody(
    string Code,
    int HttpStatus,
    string Severity,
    string Message);
