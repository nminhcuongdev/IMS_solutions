using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Zones;

internal sealed class ZoneService(ImsDbContext db) : IZoneService
{
    private static readonly SortMap<Zone> Sort = new SortMap<Zone>()
        .Add("zone_code", z => z.ZoneCode)
        .Add("zone_name", z => z.ZoneName)
        .Add("factory_code", z => z.FactoryCode)
        .Add("is_active", z => z.IsActive)
        .Add("remark_1", z => z.Remark1)
        .Add("remark_2", z => z.Remark2)
        .Add("remark_3", z => z.Remark3)
        .Add("remark_4", z => z.Remark4)
        .Add("remark_5", z => z.Remark5)
        .Add("created_at", z => z.CreatedAt)
        .Add("updated_at", z => z.UpdatedAt)
        .Default("zone_code")
        .ThenAlwaysBy(z => z.ZoneCode);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null)
    {
        var query = db.Zones.AsNoTracking().Where(z => !z.IsDeleted);
        if (isActive ?? true) query = query.Where(z => z.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(z => EF.Functions.Like(z.ZoneCode, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(z.ZoneName, LikeContains(s), LikeEscape));
        }
        query = Sort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(z => Map(z)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ZoneDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var z = await db.Zones.AsNoTracking().FirstOrDefaultAsync(x => x.ZoneCode == code && !x.IsDeleted, ct);
        return z is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Zone")))
            : Outcome.Ok(new ApiSuccess<ZoneDto>(Map(z)));
    }

    public async Task<ServiceResult> CreateAsync(ZoneWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.ZoneCode) is { } e1) return e1;
        if (ValidateName(body.ZoneName) is { } e2) return e2;
        var code = body.ZoneCode.Trim();
        if (await db.Zones.AnyAsync(z => z.ZoneCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        var factoryCode = body.FactoryCode.Trim();
        if (!await db.Factories.AnyAsync(f => f.FactoryCode == factoryCode && !f.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));

        var z = new Zone
        {
            ZoneCode = code,
            ZoneName = body.ZoneName.Trim(),
            FactoryCode = factoryCode,
            IsActive = body.IsActive,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.Now,
        };
        db.Zones.Add(z);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/zones/{z.ZoneCode}", new ApiSuccess<ZoneDto>(Map(z)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, ZoneWriteDto body, CancellationToken ct)
    {
        var z = await db.Zones.FirstOrDefaultAsync(x => x.ZoneCode == code && !x.IsDeleted, ct);
        if (z is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Zone")));
        if (ValidateName(body.ZoneName) is { } e) return e;
        var factoryCode = body.FactoryCode.Trim();
        if (!await db.Factories.AnyAsync(f => f.FactoryCode == factoryCode && !f.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));

        z.ZoneName = body.ZoneName.Trim();
        z.FactoryCode = factoryCode;
        z.IsActive = body.IsActive;
        z.Remark1 = body.Remark1; z.Remark2 = body.Remark2; z.Remark3 = body.Remark3;
        z.Remark4 = body.Remark4; z.Remark5 = body.Remark5;
        z.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<ZoneDto>(Map(z)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var z = await db.Zones.FirstOrDefaultAsync(x => x.ZoneCode == code && !x.IsDeleted, ct);
        if (z is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Zone")));
        if (await db.Locations.AnyAsync(l => l.ZoneCode == code && !l.IsDeleted, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Del001()));
        z.IsDeleted = true; z.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.Zones.AsNoTracking().Where(z => !z.IsDeleted).OrderBy(z => z.ZoneCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("zone_code,zone_name,factory_code,is_active,remark_1,remark_2,remark_3,remark_4,remark_5");
        foreach (var z in rows)
            sb.Append(Csv(z.ZoneCode)).Append(',').Append(Csv(z.ZoneName)).Append(',')
              .Append(Csv(z.FactoryCode)).Append(',').Append(z.IsActive ? "1" : "0").Append(',')
              .Append(Csv(z.Remark1)).Append(',').Append(Csv(z.Remark2)).Append(',')
              .Append(Csv(z.Remark3)).Append(',').Append(Csv(z.Remark4)).Append(',')
              .Append(Csv(z.Remark5)).AppendLine();
        return CsvFile(sb, "zones.csv");
    }

    internal static ZoneDto Map(Zone z) => new(
        z.ZoneCode, z.ZoneName, z.FactoryCode, z.IsActive,
        z.Remark1, z.Remark2, z.Remark3, z.Remark4, z.Remark5,
        z.CreatedAt, z.UpdatedAt);
}
