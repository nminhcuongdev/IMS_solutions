using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Auth;
using Ims.Api.Features.Permissions;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Menu;

internal sealed class MenuService(ImsDbContext db, PermissionEvaluator evaluator) : IMenuService
{
    public async Task<ServiceResult> GetMenuAsync(HttpContext http, CancellationToken ct)
    {
        var rights = await evaluator.ReadAsync(http.User, ct);
        var userCode = rights.UserCode;
        var roleCode = http.User.FindFirst("role_code")?.Value ?? "";
        var isSuperAdmin = rights.IsSuperAdmin;

        var hiddenGroups = await db.FunctionVisibilities.AsNoTracking()
            .Where(v => v.TargetType == "Group" && !v.IsVisible)
            .Select(v => v.TargetCode).ToListAsync(ct);

        var hiddenFunctions = await db.FunctionVisibilities.AsNoTracking()
            .Where(v => v.TargetType == "Function" && !v.IsVisible)
            .Select(v => v.TargetCode).ToListAsync(ct);

        var viewableFunctions = rights.Functions.Select(f => f.FunctionCode).ToHashSet(StringComparer.Ordinal);

        var funcs = await db.AppFunctions.AsNoTracking()
            .Where(f => !f.IsDeleted && f.IsActive
                     && f.LargeCode != null
                     && !hiddenGroups.Contains(f.LargeCode)
                     && !hiddenFunctions.Contains(f.FunctionCode))
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.FunctionCode)
            .ToListAsync(ct);

        var pcFunctions = await db.FunctionVisibilities.AsNoTracking()
            .Where(v => v.TargetType == "PC" && v.IsVisible)
            .Select(v => v.TargetCode).ToListAsync(ct);
        funcs = funcs.Where(f => pcFunctions.Contains(f.FunctionCode)).ToList();

        funcs = funcs.Where(f => viewableFunctions.Contains(f.FunctionCode)).ToList();

        var groupCodesUsed = funcs.Select(f => f.LargeCode!).Distinct().ToList();
        var largeDict = await db.FunctionLarges.AsNoTracking()
            .Where(g => g.IsActive && groupCodesUsed.Contains(g.LargeCode))
            .ToDictionaryAsync(g => g.LargeCode, ct);

        var groups = funcs
            .GroupBy(f => f.LargeCode!)
            .Where(g => largeDict.ContainsKey(g.Key))
            .Select(g =>
            {
                var lg = largeDict[g.Key];
                return new MenuGroupDto(
                    lg.LargeCode, lg.LargeName, lg.DisplayOrder,
                    g.Select(f => new MenuFunctionDto(f.FunctionCode, f.FunctionName, f.DisplayOrder))
                     .ToList());
            })
            .OrderBy(g => g.DisplayOrder ?? int.MaxValue).ThenBy(g => g.LargeCode)
            .ToList();

        if (!isSuperAdmin)
        {
            groups = groups.Select(g => g with {
                Functions = g.Functions.Where(f => f.FunctionCode != AuthService.SuperAdminUserCode.ToLower()
                                                && f.FunctionCode != "super_admin").ToList()
            }).Where(g => g.Functions.Count > 0).ToList();
        }

        return Outcome.Ok(new ApiSuccess<MenuResponseData>(
            new MenuResponseData(userCode, roleCode, isSuperAdmin, groups)));
    }
}
