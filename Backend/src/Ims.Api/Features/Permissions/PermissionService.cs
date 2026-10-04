using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Permissions;

internal sealed class PermissionService(ImsDbContext db) : IPermissionService
{
    private static readonly string[] EditableRoles = { "General", "Worker" };

    public async Task<ServiceResult> ListAsync(CancellationToken ct)
    {
        var perms = await (
            from p in db.Permissions.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive)
            join f in db.AppFunctions.AsNoTracking().Where(x => !x.IsDeleted) on p.FunctionCode equals f.FunctionCode
            join l in db.FunctionLarges.AsNoTracking() on f.LargeCode equals l.LargeCode into lj
            from l in lj.DefaultIfEmpty()
            orderby l.DisplayOrder, f.DisplayOrder, p.Action
            select new
            {
                p.PermissionCode, p.Action,
                f.FunctionCode, f.FunctionName,
                LargeCode = l != null ? l.LargeCode : "",
                LargeName = l != null ? l.LargeName : "",
            }).ToListAsync(ct);

        var granted = await db.RolePermissions.AsNoTracking()
            .Where(rp => EditableRoles.Contains(rp.RoleCode))
            .ToDictionaryAsync(rp => rp.RoleCode + "|" + rp.PermissionCode, rp => rp.IsAllowed, ct);

        var rows = perms.Select(p => new PermissionRowDto(
            p.PermissionCode, p.LargeCode, p.LargeName, p.FunctionCode, p.FunctionName, p.Action,
            granted.GetValueOrDefault("General|" + p.PermissionCode),
            granted.GetValueOrDefault("Worker|"  + p.PermissionCode))).ToList();

        return Outcome.Ok(new ApiSuccess<IReadOnlyList<PermissionRowDto>>(rows));
    }

    public async Task<ServiceResult> SaveAsync(PermissionSaveDto body, CancellationToken ct)
    {
        var items = body?.Items ?? new List<PermissionItemDto>();
        if (items.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));

        if (items.Any(i => !EditableRoles.Contains(i.RoleCode, StringComparer.OrdinalIgnoreCase)))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));

        var codes = items.Select(i => i.PermissionCode).Distinct().ToList();
        var known = await db.Permissions.AsNoTracking()
            .Where(p => codes.Contains(p.PermissionCode))
            .Select(p => p.PermissionCode).ToListAsync(ct);
        var unknown = codes.Except(known).ToList();
        if (unknown.Count > 0)
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Permission")));

        var existing = await db.RolePermissions
            .Where(rp => codes.Contains(rp.PermissionCode) && EditableRoles.Contains(rp.RoleCode))
            .ToListAsync(ct);
        var index = existing.ToDictionary(rp => rp.RoleCode + "|" + rp.PermissionCode);

        foreach (var i in items)
        {
            var key = i.RoleCode + "|" + i.PermissionCode;
            if (index.TryGetValue(key, out var row))
            {
                row.IsAllowed = i.IsAllowed;
                row.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleCode = i.RoleCode, PermissionCode = i.PermissionCode,
                    IsAllowed = i.IsAllowed, CreatedAt = DateTime.UtcNow,
                });
            }
        }
        await db.SaveChangesAsync(ct);
        return await ListAsync(ct);
    }
}
