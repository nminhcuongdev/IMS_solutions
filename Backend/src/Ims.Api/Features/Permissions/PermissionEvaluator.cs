using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Permissions;

internal sealed class PermissionEvaluator(ImsDbContext db)
{
    public async Task<PermissionSnapshot> ReadAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var userCode = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var super = user.FindFirstValue("is_super_admin") == "1";
        var role = user.FindFirstValue("role_code") ?? "";
        var hiddenGroups = await db.FunctionVisibilities.AsNoTracking()
            .Where(v => v.TargetType == "Group" && !v.IsVisible).Select(v => v.TargetCode).ToListAsync(ct);
        var hiddenFunctions = await db.FunctionVisibilities.AsNoTracking()
            .Where(v => v.TargetType == "Function" && !v.IsVisible).Select(v => v.TargetCode).ToListAsync(ct);
        var functions = await db.AppFunctions.AsNoTracking()
            .Where(f => !f.IsDeleted && f.IsActive && !hiddenFunctions.Contains(f.FunctionCode)
                && (f.LargeCode == null || !hiddenGroups.Contains(f.LargeCode)))
            .Select(f => f.FunctionCode).ToListAsync(ct);
        var permissions = await db.Permissions.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive && functions.Contains(p.FunctionCode))
            .Select(p => new { p.FunctionCode, p.Action, p.PermissionCode }).ToListAsync(ct);
        var grants = super ? new HashSet<string>() : (await db.RolePermissions.AsNoTracking()
            .Where(r => r.RoleCode == role && r.IsAllowed)
            .Select(r => r.PermissionCode).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        var rows = permissions.Where(p => super || grants.Contains(p.PermissionCode))
            .GroupBy(p => p.FunctionCode)
            .Select(g => new FunctionActions(g.Key, g.Select(p => p.Action).Distinct().Order().ToArray()))
            .Where(f => f.Actions.Contains("view", StringComparer.Ordinal))
            .OrderBy(f => f.FunctionCode).ToArray();
        return new PermissionSnapshot(userCode, super, rows);
    }
}
