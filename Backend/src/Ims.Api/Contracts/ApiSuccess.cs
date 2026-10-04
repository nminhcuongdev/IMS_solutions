namespace Ims.Api.Contracts;

public sealed class ApiSuccess<T>
{
    public bool Success { get; init; } = true;
    public T? Data { get; init; }
    public ApiSuccess(T data) { Data = data; }
}
