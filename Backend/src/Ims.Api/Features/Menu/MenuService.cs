using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Permissions;
using Ims.Infrastructure.Persistence;
using Ims.Infrastructure.Persistence.Catalog;

namespace Ims.Api.Features.Menu;

internal sealed class MenuService(ImsDbContext db, PermissionEvaluator evaluator) : IMenuService
{
    private const string Dashboard = "dashboard";
    private const string SuperAdmin = "super_admin";

    public async Task<ServiceResult> GetMenuAsync(HttpContext http, CancellationToken ct)
    {
        var rights = await evaluator.ReadAsync(http.User, ct);
        var usable = await evaluator.UsableFunctionsAsync(rights.DisabledModules, ct);
        var open = rights.Functions.Select(f => f.FunctionCode).ToHashSet(StringComparer.Ordinal);

        var funcs = (await db.AppFunctions.AsNoTracking()
                .Where(f => !f.IsDeleted && f.IsActive && f.LargeCode != null)
                .OrderBy(f => f.DisplayOrder).ThenBy(f => f.FunctionCode)
                .ToListAsync(ct))
            .Where(f => usable.Contains(f.FunctionCode) && MenuCatalog.DeviceOf(f.FunctionCode) == "PC")
            .Where(f => f.FunctionCode == Dashboard
                || (f.FunctionCode == SuperAdmin ? rights.IsSuperAdmin : open.Contains(f.FunctionCode)))
            .ToList();

        var groupCodesUsed = funcs.Select(f => f.LargeCode!).Distinct().ToList();
        var largeDict = await db.FunctionLarges.AsNoTracking()
            .Where(g => g.IsActive && g.IsVisible && groupCodesUsed.Contains(g.LargeCode))
            .ToDictionaryAsync(g => g.LargeCode, ct);

        var groups = funcs
            .GroupBy(f => f.LargeCode!)
            .Where(g => largeDict.ContainsKey(g.Key))
            .Select(g =>
            {
                var lg = largeDict[g.Key];
                return new MenuGroupDto(
                    lg.LargeCode, lg.LargeName, lg.DisplayOrder,
                    g.Select(f => new MenuFunctionDto(f.FunctionCode, f.FunctionName, f.DisplayOrder)).ToList());
            })
            .Where(g => g.Functions.Count > 0)
            .OrderBy(g => g.DisplayOrder ?? int.MaxValue).ThenBy(g => g.LargeCode)
            .ToList();

        return Outcome.Ok(new ApiSuccess<MenuResponseData>(
            new MenuResponseData(rights.UserCode, rights.RoleCode, rights.IsSuperAdmin, groups)));
    }
}
