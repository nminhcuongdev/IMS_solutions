using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Locations;

internal sealed class LocationService(ImsDbContext db) : ILocationService
{
    private static readonly SortMap<Location> Sort = new SortMap<Location>()
        .Add("location_code", l => l.LocationCode)
        .Add("location_type", l => l.LocationType)
        .Add("large_code", l => l.LargeCode)
        .Add("middle_code", l => l.MiddleCode)
        .Add("small_code", l => l.SmallCode)
        .Add("location_name", l => l.LocationName)
        .Add("zone_code", l => l.ZoneCode)
        .Add("parent_location_code", l => l.ParentLocationCode)
        .Add("is_active", l => l.IsActive)
        .Add("remark_1", l => l.Remark1)
        .Add("remark_2", l => l.Remark2)
        .Add("remark_3", l => l.Remark3)
        .Add("remark_4", l => l.Remark4)
        .Add("remark_5", l => l.Remark5)
        .Add("created_at", l => l.CreatedAt)
        .Add("updated_at", l => l.UpdatedAt)
        .Default("location_code")
        .ThenAlwaysBy(l => l.LocationCode);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? type = null, string? zoneCode = null)
    {
        var query = db.Locations.AsNoTracking().Where(l => !l.IsDeleted);
        if (isActive ?? true) query = query.Where(l => l.IsActive);
        if (!string.IsNullOrWhiteSpace(zoneCode))
        {
            var z = zoneCode.Trim();
            query = query.Where(l => l.ZoneCode == z);
        }
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<LocationType>(type, true, out var t))
            query = query.Where(l => l.LocationType == t);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(l => EF.Functions.Like(l.LocationCode, "%" + s + "%")
                                  || EF.Functions.Like(l.LocationName, "%" + s + "%"));
        }
        query = Sort.Apply(query, sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(l => Map(l)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<LocationDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var l = await db.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.LocationCode == code && !x.IsDeleted, ct);
        return l is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")))
            : Outcome.Ok(new ApiSuccess<LocationDto>(Map(l)));
    }

    public async Task<ServiceResult> CreateAsync(LocationWriteDto body, CancellationToken ct)
    {
        if (!Enum.TryParse<LocationType>(body.LocationType, true, out var lt))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        if (ValidateCode(body.InputCode) is { } e1) return e1;
        if (ValidateName(body.LocationName) is { } e2) return e2;

        string generatedCode;
        string largeCode; string? middleCode = null; string? smallCode = null; string? zone = NullIfBlank(body.ZoneCode);

        if (lt == LocationType.Large)
        {
            generatedCode = body.InputCode.Trim();
            largeCode = body.InputCode.Trim();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(body.ParentLocationCode))
                return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
            var parent = await db.Locations
                .FirstOrDefaultAsync(p => p.LocationCode == body.ParentLocationCode && !p.IsDeleted, ct);
            if (parent is null)
                return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Parent location")));

            if (lt == LocationType.Middle)
            {
                if (parent.LocationType != LocationType.Large)
                    return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));
                largeCode = parent.LargeCode;
                middleCode = body.InputCode.Trim();
                generatedCode = parent.LocationCode + "_" + middleCode;
                zone ??= parent.ZoneCode;
            }
            else
            {
                if (parent.LocationType != LocationType.Middle)
                    return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));
                largeCode = parent.LargeCode;
                middleCode = parent.MiddleCode;
                smallCode = body.InputCode.Trim();
                generatedCode = parent.LocationCode + "_" + smallCode;
                zone ??= parent.ZoneCode;
            }
        }

        if (generatedCode.Length > MaxCodeLength)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005TooLong(generatedCode, MaxCodeLength)));

        if (await db.Locations.AnyAsync(x => x.LocationCode == generatedCode, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));

        if (!string.IsNullOrWhiteSpace(zone)
            && !await db.Zones.AnyAsync(z => z.ZoneCode == zone && !z.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Zone")));

        var l = new Location
        {
            LocationCode = generatedCode,
            LocationType = lt,
            LargeCode = largeCode,
            MiddleCode = middleCode,
            SmallCode = smallCode,
            LocationName = body.LocationName.Trim(),
            ZoneCode = zone,
            ParentLocationCode = NullIfBlank(body.ParentLocationCode),
            IsActive = body.IsActive,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.UtcNow,
        };
        db.Locations.Add(l);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/locations/{l.LocationCode}", new ApiSuccess<LocationDto>(Map(l)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, LocationWriteDto body, CancellationToken ct)
    {
        var l = await db.Locations.FirstOrDefaultAsync(x => x.LocationCode == code && !x.IsDeleted, ct);
        if (l is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));
        if (ValidateName(body.LocationName) is { } e) return e;

        var zone = NullIfBlank(body.ZoneCode);
        if (zone is not null
            && !await db.Zones.AnyAsync(z => z.ZoneCode == zone && !z.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Zone")));

        l.LocationName = body.LocationName.Trim();
        l.ZoneCode = zone;
        l.IsActive = body.IsActive;
        l.Remark1 = body.Remark1; l.Remark2 = body.Remark2; l.Remark3 = body.Remark3;
        l.Remark4 = body.Remark4; l.Remark5 = body.Remark5;
        l.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<LocationDto>(Map(l)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var l = await db.Locations.FirstOrDefaultAsync(x => x.LocationCode == code && !x.IsDeleted, ct);
        if (l is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));
        if (await db.Locations.AnyAsync(c => c.ParentLocationCode == code && !c.IsDeleted, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        if (await db.Items.AnyAsync(i => i.DefaultLocationCode == code && !i.IsDeleted, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        l.IsDeleted = true; l.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.Locations.AsNoTracking().Where(l => !l.IsDeleted).OrderBy(l => l.LocationCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("location_code,location_type,large_code,middle_code,small_code,location_name,zone_code,parent_location_code,is_active,remark_1,remark_2,remark_3,remark_4,remark_5");
        foreach (var l in rows)
            sb.Append(Csv(l.LocationCode)).Append(',').Append(l.LocationType).Append(',')
              .Append(Csv(l.LargeCode)).Append(',').Append(Csv(l.MiddleCode)).Append(',')
              .Append(Csv(l.SmallCode)).Append(',').Append(Csv(l.LocationName)).Append(',')
              .Append(Csv(l.ZoneCode)).Append(',').Append(Csv(l.ParentLocationCode)).Append(',')
              .Append(l.IsActive ? "1" : "0").Append(',')
              .Append(Csv(l.Remark1)).Append(',').Append(Csv(l.Remark2)).Append(',')
              .Append(Csv(l.Remark3)).Append(',').Append(Csv(l.Remark4)).Append(',')
              .Append(Csv(l.Remark5)).AppendLine();
        return CsvFile(sb, "locations.csv");
    }

    internal static LocationDto Map(Location l) => new(
        l.LocationCode, l.LocationType.ToString(), l.LargeCode, l.MiddleCode, l.SmallCode,
        l.LocationName, l.ZoneCode, l.ParentLocationCode, l.IsActive,
        l.Remark1, l.Remark2, l.Remark3, l.Remark4, l.Remark5,
        l.CreatedAt, l.UpdatedAt);
}
