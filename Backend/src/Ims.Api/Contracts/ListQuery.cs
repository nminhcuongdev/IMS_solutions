namespace Ims.Api.Contracts;

public sealed class ListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Q { get; set; }
    public string? Sort { get; set; }
    public bool? IsActive { get; set; }
}
