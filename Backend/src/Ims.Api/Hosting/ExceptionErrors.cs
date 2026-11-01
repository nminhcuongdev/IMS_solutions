using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;

namespace Ims.Api.Hosting;

internal static class ExceptionErrors
{
    private static readonly HashSet<int> Unreachable = [-1, 2, 53, 40, 233, 4060, 10053, 10054, 10060, 10061, 11001, 18456];

    public static (int Status, ApiErrorBody Body) For(Exception? ex) => ex switch
    {
        DbUpdateConcurrencyException => (409, ErrorCatalog.Inv005()),
        BadHttpRequestException => (400, ErrorCatalog.Sys004() with { HttpStatus = 400 }),
        DbUpdateException { InnerException: ArgumentException } => (400, ErrorCatalog.Qty001()),
        _ when SqlNumberOf(ex) is { } n => ForSqlNumber(n),
        _ => (500, ErrorCatalog.Sys004()),
    };

    public static (int Status, ApiErrorBody Body) ForSqlNumber(int number) => number switch
    {
        2628 or 8152 => (400, ErrorCatalog.Val005FieldTooLong()),
        8115 => (400, ErrorCatalog.Qty001()),
        547 => (404, ErrorCatalog.NotFound("参照先")),
        _ when Unreachable.Contains(number) => (500, ErrorCatalog.Db001()),
        _ => (500, ErrorCatalog.Sys004()),
    };

    private static int? SqlNumberOf(Exception? ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is SqlException sql) return sql.Number;
        return null;
    }
}
