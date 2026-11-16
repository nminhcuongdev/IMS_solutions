using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Infrastructure.Persistence;
using Ims.Infrastructure.Persistence.Catalog;

namespace Ims.Api.Features.Permissions;

internal sealed class PermissionEvaluator(ImsDbContext db)
{
    public async Task<IReadOnlySet<string>> DisabledModulesAsync(CancellationToken ct)
    {
        var flags = await db.GeneralSettings.AsNoTracking()
            .Where(s => s.ConfigCode.StartsWith(MenuCatalog.ModulePrefix))
            .Select(s => new { s.ConfigCode, s.ConfigValue }).ToListAsync(ct);
        return flags.Where(f => (f.ConfigValue ?? "").Trim() == "0")
            .Select(f => f.ConfigCode[MenuCatalog.ModulePrefix.Length..].ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<HashSet<string>> UsableFunctionsAsync(IReadOnlySet<string> disabledModules, CancellationToken ct)
    {
        var hiddenGroups = await db.FunctionLarges.AsNoTracking()
            .Where(g => !g.IsVisible || !g.IsActive).Select(g => g.LargeCode).ToListAsync(ct);
        var functions = await db.AppFunctions.AsNoTracking()
            .Where(f => !f.IsDeleted && f.IsActive && f.IsVisible
                && (f.LargeCode == null || !hiddenGroups.Contains(f.LargeCode)))
            .Select(f => f.FunctionCode).ToListAsync(ct);
        return functions
            .Where(f => MenuCatalog.ModuleOf(f) is not { } module || !disabledModules.Contains(module))
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<PermissionSnapshot> ReadAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var userCode = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var super = user.FindFirstValue("is_super_admin") == "1";
        var role = await db.AppUsers.AsNoTracking()
            .Where(u => u.UserCode == userCode && !u.IsDeleted)
            .Select(u => u.RoleCode).FirstOrDefaultAsync(ct)
            ?? user.FindFirstValue("role_code") ?? "";
        var builtin = await db.Roles.AsNoTracking()
            .AnyAsync(r => r.RoleCode == role && r.IsBuiltin && !r.IsDeleted, ct);
        var disabled = await DisabledModulesAsync(ct);
        var usable = await UsableFunctionsAsync(disabled, ct);
        var permissions = (await db.Permissions.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive)
                .Select(p => new { p.FunctionCode, p.ActionType, p.PermissionCode }).ToListAsync(ct))
            .Where(p => usable.Contains(p.FunctionCode)).ToList();
        var everything = super || builtin;
        var grants = everything ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : (await db.RolePermissions.AsNoTracking()
            .Where(r => r.RoleCode == role && r.IsAllowed)
            .Select(r => r.PermissionCode).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedRows = permissions.Where(p => everything || grants.Contains(p.PermissionCode)).ToList();
        var allowed = allowedRows.Select(p => p.PermissionCode).ToHashSet(StringComparer.Ordinal);
        var functions = allowedRows
            .GroupBy(p => p.FunctionCode)
            .Select(g => new FunctionActions(g.Key, g.Select(p => p.ActionType).Distinct().Order().ToArray()))
            .Where(f => f.Actions.Contains(MenuCatalog.Open, StringComparer.Ordinal))
            .OrderBy(f => f.FunctionCode).ToArray();
        var openFunctions = functions.Select(f => f.FunctionCode).ToHashSet(StringComparer.Ordinal);
        allowed.RemoveWhere(code => !openFunctions.Contains(code[..code.LastIndexOf('.')]));
        return new PermissionSnapshot(userCode, role, super, builtin, allowed, functions, disabled);
    }
}
