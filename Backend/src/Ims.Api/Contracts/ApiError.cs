namespace Ims.Api.Contracts;

public sealed class ApiError
{
    public bool Success { get; init; } = false;
    public ApiErrorBody Error { get; init; }
    public ApiError(ApiErrorBody error) { Error = error; }
}
