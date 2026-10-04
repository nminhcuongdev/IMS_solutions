namespace Ims.Api.Contracts;

public sealed class ApiListSuccess<T>
{
    public bool Success { get; init; } = true;
    public IReadOnlyList<T> Data { get; init; }
    public PageMeta Meta { get; init; }
    public ApiListSuccess(IReadOnlyList<T> data, PageMeta meta) { Data = data; Meta = meta; }
}
