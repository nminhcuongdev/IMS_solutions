using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.SuperAdmin;

internal sealed class SuperAdminService(ImsDbContext db) : ISuperAdminService
{
    private const string TypeStatus = "status";
    private const string TypePacking = "packing-type";
    private const string TypeHold = "hold-name";

    private static ServiceResult? RequireSuperAdmin(ClaimsPrincipal user)
        => user.FindFirst("is_super_admin")?.Value == "1"
            ? null
            : Outcome.Json(new ApiError(ErrorCatalog.Auth005()), statusCode: 403);

    public async Task<ServiceResult> GetGeneralAsync(
        ClaimsPrincipal user, CancellationToken ct, string? category = null)
    {
        if (RequireSuperAdmin(user) is { } deny) return deny;
        var q = db.GeneralSettings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(s => s.Category == category);
        var rows = await q.OrderBy(s => s.Category).ThenBy(s => s.ConfigCode)
            .Select(s => new GeneralItemDto(s.ConfigCode, s.ConfigValue, s.Category))
            .ToListAsync(ct);
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<GeneralItemDto>>(rows));
    }

    public async Task<ServiceResult> SaveGeneralAsync(ClaimsPrincipal user, GeneralSaveDto body, CancellationToken ct)
    {
        if (RequireSuperAdmin(user) is { } deny) return deny;
        var items = body?.Items ?? new List<GeneralItemDto>();
        if (items.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));

        var codes = items.Select(i => i.ConfigCode).ToList();
        var existing = await db.GeneralSettings.Where(s => codes.Contains(s.ConfigCode)).ToListAsync(ct);
        foreach (var i in items)
        {
            var row = existing.FirstOrDefault(s => s.ConfigCode == i.ConfigCode);
            if (row is null)
            {
                var added = new GeneralSetting
                {
                    ConfigCode = i.ConfigCode, ConfigValue = i.ConfigValue,
                    Category = i.Category, CreatedAt = DateTime.Now,
                };
                db.GeneralSettings.Add(added);
                existing.Add(added);
            }
            else
            {
                row.ConfigValue = i.ConfigValue;
                if (!string.IsNullOrWhiteSpace(i.Category)) row.Category = i.Category;
                row.UpdatedAt = DateTime.Now;
            }
        }
        await db.SaveChangesAsync(ct);
        return await GetGeneralAsync(user, ct);
    }

    public async Task<ServiceResult> GetVisibilityAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (RequireSuperAdmin(user) is { } deny) return deny;
        var rows = await (
            from v in db.FunctionVisibilities.AsNoTracking()
            join f in db.AppFunctions.AsNoTracking().Where(x => !x.IsDeleted)
                on v.TargetCode equals f.FunctionCode
            join l in db.FunctionLarges.AsNoTracking() on f.LargeCode equals l.LargeCode into lj
            from l in lj.DefaultIfEmpty()
            orderby v.TargetType, l.DisplayOrder, f.DisplayOrder
            select new VisibilityRowDto(
                v.TargetType, v.TargetCode, f.FunctionName,
                l != null ? l.LargeCode : null, l != null ? l.LargeName : null, v.IsVisible)
        ).ToListAsync(ct);
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<VisibilityRowDto>>(rows));
    }

    public async Task<ServiceResult> SaveVisibilityAsync(
        ClaimsPrincipal user, VisibilitySaveDto body, CancellationToken ct)
    {
        if (RequireSuperAdmin(user) is { } deny) return deny;
        var items = body?.Items ?? new List<VisibilityItemDto>();
        if (items.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));

        var all = await db.FunctionVisibilities.ToListAsync(ct);
        foreach (var i in items)
        {
            var row = all.FirstOrDefault(v => v.TargetType == i.TargetType && v.TargetCode == i.TargetCode);
            if (row is null)
                db.FunctionVisibilities.Add(new FunctionVisibility
                {
                    TargetType = i.TargetType, TargetCode = i.TargetCode,
                    IsVisible = i.IsVisible, UpdatedAt = DateTime.Now,
                });
            else
            {
                row.IsVisible = i.IsVisible;
                row.UpdatedAt = DateTime.Now;
            }
        }
        await db.SaveChangesAsync(ct);
        return await GetVisibilityAsync(user, ct);
    }

    public async Task<ServiceResult> GetDefaultMasterAsync(string type, ClaimsPrincipal user, CancellationToken ct)
    {
        if (RequireSuperAdmin(user) is { } deny) return deny;
        var rows = await ReadDefaultMasterAsync(type, db, ct);
        return rows is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("DefaultMaster")))
            : Outcome.Ok(new ApiSuccess<IReadOnlyList<DefaultMasterRowDto>>(rows));
    }

    private static async Task<List<DefaultMasterRowDto>?> ReadDefaultMasterAsync(
        string type, ImsDbContext db, CancellationToken ct)
        => type.ToLowerInvariant() switch
        {
            TypeStatus => await db.Statuses.AsNoTracking()
                .Where(s => s.Category == "Master").OrderBy(s => s.StatusCode)
                .Select(s => new DefaultMasterRowDto(
                    s.StatusCode.ToString(), s.StatusNameLang1, s.StatusNameLang2, s.StatusNameLang3))
                .ToListAsync(ct),
            TypePacking => await ReadDefaultValueAsync(db, "PackingType", ct),
            TypeHold => await ReadDefaultValueAsync(db, "HoldName", ct),
            _ => null,
        };

    private static Task<List<DefaultMasterRowDto>> ReadDefaultValueAsync(
        ImsDbContext db, string valueType, CancellationToken ct)
        => db.DefaultValues.AsNoTracking()
            .Where(d => d.ValueType == valueType)
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.ValueCode)
            .Select(d => new DefaultMasterRowDto(d.ValueCode, d.NameLang1, d.NameLang2, d.NameLang3))
            .ToListAsync(ct);

    public async Task<ServiceResult> SaveDefaultMasterAsync(
        string type, ClaimsPrincipal user, DefaultMasterSaveDto body, CancellationToken ct)
    {
        if (RequireSuperAdmin(user) is { } deny) return deny;
        var items = body?.Items ?? new List<DefaultMasterRowDto>();
        if (items.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));

        switch (type.ToLowerInvariant())
        {
            case TypeStatus:
            {
                var rows = await db.Statuses.Where(s => s.Category == "Master").ToListAsync(ct);
                foreach (var i in items)
                {
                    if (!int.TryParse(i.Code, out var code)) continue;
                    var row = rows.FirstOrDefault(s => s.StatusCode == code);
                    if (row is null) continue;
                    row.StatusNameLang1 = i.NameLang1;
                    row.StatusNameLang2 = i.NameLang2;
                    row.StatusNameLang3 = i.NameLang3;
                    row.UpdatedAt = DateTime.Now;
                }
                break;
            }
            case TypePacking:
            case TypeHold:
            {
                var valueType = type.Equals(TypePacking, StringComparison.OrdinalIgnoreCase)
                    ? "PackingType" : "HoldName";
                var rows = await db.DefaultValues.Where(d => d.ValueType == valueType).ToListAsync(ct);
                foreach (var i in items)
                {
                    var row = rows.FirstOrDefault(d => d.ValueCode == i.Code);
                    if (row is null) continue;
                    row.NameLang1 = i.NameLang1;
                    row.NameLang2 = i.NameLang2;
                    row.NameLang3 = i.NameLang3;
                    row.UpdatedAt = DateTime.Now;
                }
                break;
            }
            default:
                return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("DefaultMaster")));
        }

        await db.SaveChangesAsync(ct);
        return await GetDefaultMasterAsync(type, user, ct);
    }

    public ServiceResult GetDbInfoAsync(ClaimsPrincipal user, IConfiguration config)
    {
        if (RequireSuperAdmin(user) is { } deny) return deny;
        var raw = config.GetConnectionString("Ims") ?? "";
        var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Select(p => p.Split('=', 2))
                       .Where(p => p.Length == 2)
                       .ToDictionary(p => p[0].Trim().ToLowerInvariant(), p => p[1].Trim());

        string Get(params string[] keys)
            => keys.Select(k => parts.GetValueOrDefault(k)).FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";

        var trusted = Get("trusted_connection", "integrated security");
        var userName = !string.IsNullOrEmpty(Get("user id", "uid"))
            ? Get("user id", "uid")
            : (trusted.Equals("True", StringComparison.OrdinalIgnoreCase) ? "(Windows Authentication)" : "");

        return Outcome.Ok(new ApiSuccess<DbInfoDto>(new DbInfoDto(
            Get("server", "data source"),
            userName,
            Get("database", "initial catalog"))));
    }
}
