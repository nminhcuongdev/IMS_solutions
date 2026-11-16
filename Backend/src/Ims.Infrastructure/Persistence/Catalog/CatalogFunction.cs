namespace Ims.Infrastructure.Persistence.Catalog;

public sealed record CatalogFunction(
    string Code,
    string Name,
    string Group,
    int Order,
    string? Module,
    FunctionKind Kind,
    string[] Actions)
{
    public string Device => Kind == FunctionKind.Handheld ? "HT" : "PC";
}
