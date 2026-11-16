using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;
using Ims.Infrastructure.Persistence.Catalog;

namespace Ims.Api.Features.SuperAdmin;

internal sealed class SuperAdminService(ImsDbContext db) : ISuperAdminService
{
    private const string TypeStatus = "status";
    private const string TypePacking = "packing-type";
    private const string TypeHold = "hold-name";

    private static ServiceResult? RequireSuperAdmin(ClaimsPrincipal user)
        => user.FindFirst("is_super_admin")?.Value == "1"
            ? null
            : Outcome.Json(new ApiError(ErrorCatalog.Auth006()), statusCode: 403);

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
        var groups = await db.FunctionLarges.AsNoTracking().Where(g => g.IsActive)
            .OrderBy(g => g.DisplayOrder).ToListAsync(ct);
        var names = groups.ToDictionary(g => g.LargeCode, g => g.LargeName);
        var functions = await db.AppFunctions.AsNoTracking().Where(f => !f.IsDeleted)
            .OrderBy(f => f.DisplayOrder).ToListAsync(ct);
        var order = groups.Select((g, i) => (g.LargeCode, i)).ToDictionary(x => x.LargeCode, x => x.i);
        var rows = groups.Select(g => new VisibilityRowDto("Group", g.LargeCode, g.LargeName, g.LargeCode, g.LargeName, g.IsVisible))
            .Concat(functions
                .OrderBy(f => MenuCatalog.DeviceOf(f.FunctionCode))
                .ThenBy(f => f.LargeCode is { } l && order.TryGetValue(l, out var i) ? i : int.MaxValue)
                .ThenBy(f => f.DisplayOrder ?? int.MaxValue)
                .Select(f => new VisibilityRowDto(MenuCatalog.DeviceOf(f.FunctionCode), f.FunctionCode, f.FunctionName,
                    f.LargeCode, f.LargeCode is { } l ? names.GetValueOrDefault(l) : null, f.IsVisible)))
            .ToList();
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<VisibilityRowDto>>(rows));
    }

    public async Task<ServiceResult> SaveVisibilityAsync(
        ClaimsPrincipal user, VisibilitySaveDto body, CancellationToken ct)
    {
        if (RequireSuperAdmin(user) is { } deny) return deny;
        var items = body?.Items ?? new List<VisibilityItemDto>();
        if (items.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));

        var groups = await db.FunctionLarges.ToListAsync(ct);
        var functions = await db.AppFunctions.Where(f => !f.IsDeleted).ToListAsync(ct);
        var now = DateTime.Now;
        foreach (var i in items)
        {
            if (i.TargetType == "Group")
            {
                var g = groups.FirstOrDefault(x => x.LargeCode == i.TargetCode);
                if (g is null || g.IsVisible == i.IsVisible) continue;
                g.IsVisible = i.IsVisible;
                g.UpdatedAt = now;
                continue;
            }
            var f = functions.FirstOrDefault(x => x.FunctionCode == i.TargetCode);
            if (f is null || f.IsVisible == i.IsVisible) continue;
            f.IsVisible = i.IsVisible;
            f.UpdatedAt = now;
            f.UpdatedBy = user.FindFirstValue(ClaimTypes.NameIdentifier);
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
