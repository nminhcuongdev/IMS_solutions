using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;
using Ims.Infrastructure.Persistence.Catalog;

namespace Ims.Api.Features.Permissions;

internal sealed class PermissionService(ImsDbContext db, PermissionEvaluator evaluator) : IPermissionService
{
    public async Task<ServiceResult> MatrixAsync(string? roleCode, CancellationToken ct)
    {
        var code = (roleCode ?? "").Trim();
        if (code.Length == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldPermissionCode", "Permission Code")));
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleCode.ToUpper() == code.ToUpper() && !r.IsDeleted, ct);
        if (role is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Permission")));

        var rows = await ReadRowsAsync(ct);
        var granted = role.IsBuiltin
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : (await db.RolePermissions.AsNoTracking()
                .Where(rp => rp.RoleCode == role.RoleCode && rp.IsAllowed)
                .Select(rp => rp.PermissionCode).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matrix = rows.Select(r => r with
        {
            IsAllowed = role.IsBuiltin || granted.Contains(r.PermissionCode),
            Locked = role.IsBuiltin,
        }).ToList();
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<PermissionMatrixRowDto>>(matrix));
    }

    public async Task<ServiceResult> SaveAsync(PermissionMatrixSaveDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var code = (body?.RoleCode ?? "").Trim();
        if (code.Length == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldPermissionCode", "Permission Code")));
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleCode.ToUpper() == code.ToUpper() && !r.IsDeleted, ct);
        if (role is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Permission")));
        if (role.IsBuiltin) return Outcome.Json(new ApiError(ErrorCatalog.Auth006()), 403);

        var items = (body?.Items ?? []).Where(i => !string.IsNullOrWhiteSpace(i.PermissionCode))
            .GroupBy(i => i.PermissionCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last() with { PermissionCode = g.Key }).ToList();
        if (items.Count == 0) return await MatrixAsync(role.RoleCode, ct);

        var codes = items.Select(i => i.PermissionCode).ToList();
        var known = (await db.Permissions.AsNoTracking()
            .Where(p => codes.Contains(p.PermissionCode) && !p.IsDeleted)
            .Select(p => p.PermissionCode).ToListAsync(ct))
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        if (codes.Any(c => !known.ContainsKey(c)))
            return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Permission")));
        items = items.Select(i => i with { PermissionCode = known[i.PermissionCode] }).ToList();
        codes = items.Select(i => i.PermissionCode).ToList();

        var who = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var now = DateTime.Now;
        var existing = (await db.RolePermissions
            .Where(rp => rp.RoleCode == role.RoleCode && codes.Contains(rp.PermissionCode))
            .ToListAsync(ct)).ToDictionary(rp => rp.PermissionCode, StringComparer.OrdinalIgnoreCase);
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        foreach (var i in items)
        {
            if (existing.TryGetValue(i.PermissionCode, out var row))
            {
                if (row.IsAllowed == i.IsAllowed) continue;
                row.IsAllowed = i.IsAllowed;
                row.UpdatedAt = now;
                row.UpdatedBy = who;
            }
            else
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleCode = role.RoleCode, PermissionCode = i.PermissionCode,
                    IsAllowed = i.IsAllowed, CreatedAt = now, CreatedBy = who,
                });
            }
        }
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return await MatrixAsync(role.RoleCode, ct);
    }

    public async Task<ServiceResult> MeAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var s = await evaluator.ReadAsync(user, ct);
        var visible = await evaluator.UsableFunctionsAsync(s.DisabledModules, ct);
        return Outcome.Ok(new ApiSuccess<MyPermissionsDto>(new MyPermissionsDto(
            s.UserCode, s.RoleCode, s.IsSuperAdmin, s.IsBuiltin,
            s.Allowed.Order(StringComparer.Ordinal).ToList(), s.Functions,
            visible.Order(StringComparer.Ordinal).ToList())));
    }

    private async Task<List<PermissionMatrixRowDto>> ReadRowsAsync(CancellationToken ct)
    {
        var disabled = await evaluator.DisabledModulesAsync(ct);
        var rows = await (
            from p in db.Permissions.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive)
            join f in db.AppFunctions.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive) on p.FunctionCode equals f.FunctionCode
            join l in db.FunctionLarges.AsNoTracking() on f.LargeCode equals l.LargeCode into lj
            from l in lj.DefaultIfEmpty()
            select new
            {
                p.PermissionCode, p.ActionType, f.FunctionCode, f.FunctionName,
                FunctionOrder = f.DisplayOrder,
                LargeCode = l != null ? l.LargeCode : "",
                LargeName = l != null ? l.LargeName : "",
                LargeOrder = l != null ? l.DisplayOrder : null,
            }).ToListAsync(ct);
        return rows
            .Where(r => MenuCatalog.ModuleOf(r.FunctionCode) is not { } m || !disabled.Contains(m))
            .Select(r => new
            {
                Row = r,
                Device = MenuCatalog.DeviceOf(r.FunctionCode),
                ActionOrder = ActionOrder(r.FunctionCode, r.ActionType),
            })
            .OrderBy(x => x.Device == "PC" ? 0 : 1)
            .ThenBy(x => x.Row.LargeOrder ?? int.MaxValue)
            .ThenBy(x => x.Row.FunctionOrder ?? int.MaxValue)
            .ThenBy(x => x.Row.FunctionCode, StringComparer.Ordinal)
            .ThenBy(x => x.ActionOrder)
            .Select(x => new PermissionMatrixRowDto(
                x.Row.PermissionCode, x.Device, x.Row.LargeCode, x.Row.LargeName,
                x.Row.FunctionCode, x.Row.FunctionName, x.Row.ActionType, false, false))
            .ToList();
    }

    private static int ActionOrder(string functionCode, string actionType)
    {
        var actions = MenuCatalog.Find(functionCode)?.Actions ?? [];
        var index = Array.IndexOf(actions, actionType);
        return index < 0 ? int.MaxValue : index;
    }
}
