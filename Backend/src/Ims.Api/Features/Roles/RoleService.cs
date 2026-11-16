using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Roles;

internal sealed class RoleService(ImsDbContext db) : IRoleService
{
    private const int MaxNameLength = 100;

    public async Task<ServiceResult> ListAsync(CancellationToken ct)
    {
        var rows = await db.Roles.AsNoTracking().Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.IsBuiltin).ThenBy(r => r.RoleCode)
            .Select(r => new RoleDto(r.RoleCode, r.RoleName, r.IsBuiltin,
                db.AppUsers.Count(u => u.RoleCode == r.RoleCode && !u.IsDeleted)))
            .ToListAsync(ct);
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<RoleDto>>(rows));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var row = await db.Roles.AsNoTracking().Where(r => r.RoleCode == code && !r.IsDeleted)
            .Select(r => new RoleDto(r.RoleCode, r.RoleName, r.IsBuiltin,
                db.AppUsers.Count(u => u.RoleCode == r.RoleCode && !u.IsDeleted)))
            .FirstOrDefaultAsync(ct);
        return row is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Permission")))
            : Outcome.Ok(new ApiSuccess<RoleDto>(row));
    }

    public async Task<ServiceResult> CreateAsync(RoleWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body?.RoleCode))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldPermissionCode", "Permission Code")));
        if (ValidateCode(body.RoleCode) is { } invalid) return invalid;
        if (ValidateRoleName(body.RoleName) is { } badName) return badName;
        var code = body.RoleCode.Trim().ToUpperInvariant();
        if (await db.Roles.AnyAsync(r => r.RoleCode.ToUpper() == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        var role = new Role
        {
            RoleCode = code, RoleName = body.RoleName!.Trim(), IsActive = true, IsBuiltin = false,
            CreatedAt = DateTime.Now, CreatedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/roles/{role.RoleCode}",
            new ApiSuccess<RoleDto>(new RoleDto(role.RoleCode, role.RoleName, false, 0)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, RoleWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleCode == code && !r.IsDeleted, ct);
        if (role is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Permission")));
        if (role.IsBuiltin) return Outcome.Json(new ApiError(ErrorCatalog.Auth006()), 403);
        if (ValidateRoleName(body?.RoleName) is { } badName) return badName;
        role.RoleName = body!.RoleName!.Trim();
        role.UpdatedAt = DateTime.Now;
        role.UpdatedBy = user.FindFirstValue(ClaimTypes.NameIdentifier);
        await db.SaveChangesAsync(ct);
        return await GetAsync(role.RoleCode, ct);
    }

    public async Task<ServiceResult> DeleteAsync(string code, ClaimsPrincipal user, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleCode == code && !r.IsDeleted, ct);
        if (role is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Permission")));
        if (role.IsBuiltin) return Outcome.Json(new ApiError(ErrorCatalog.Auth006()), 403);
        var users = await db.AppUsers.CountAsync(u => u.RoleCode == role.RoleCode && !u.IsDeleted, ct);
        if (users > 0) return Outcome.Conflict(new ApiError(ErrorCatalog.Del001(users)));
        role.IsDeleted = true;
        role.UpdatedAt = DateTime.Now;
        role.UpdatedBy = user.FindFirstValue(ClaimTypes.NameIdentifier);
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    private static ServiceResult? ValidateRoleName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldPermissionName", "Permission Name")));
        if (name.Trim().Length > MaxNameLength)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005FieldTooLong()));
        return null;
    }
}
