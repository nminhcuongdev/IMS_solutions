using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Factories;

internal sealed class FactoryService(ImsDbContext db) : IFactoryService
{
    private static readonly SortMap<Factory> Sort = new SortMap<Factory>()
        .Add("factory_code", f => f.FactoryCode)
        .Add("factory_name", f => f.FactoryName)
        .Add("country", f => f.Country)
        .Add("address", f => f.Address)
        .Add("is_default", f => f.IsDefault)
        .Add("is_active", f => f.IsActive)
        .Add("remark_1", f => f.Remark1)
        .Add("remark_2", f => f.Remark2)
        .Add("remark_3", f => f.Remark3)
        .Add("remark_4", f => f.Remark4)
        .Add("remark_5", f => f.Remark5)
        .Add("created_at", f => f.CreatedAt)
        .Add("updated_at", f => f.UpdatedAt)
        .Default("factory_code")
        .ThenAlwaysBy(f => f.FactoryCode);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null)
    {
        var query = db.Factories.AsNoTracking().Where(f => !f.IsDeleted);
        if (isActive ?? true) query = query.Where(f => f.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(f => EF.Functions.Like(f.FactoryCode, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(f.FactoryName, LikeContains(s), LikeEscape));
        }
        query = Sort.Apply(query, sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(f => Map(f)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<FactoryDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var f = await db.Factories.AsNoTracking().FirstOrDefaultAsync(x => x.FactoryCode == code && !x.IsDeleted, ct);
        return f is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")))
            : Outcome.Ok(new ApiSuccess<FactoryDto>(Map(f)));
    }

    private static bool IsSuperAdmin(ClaimsPrincipal u) => u.FindFirst("is_super_admin")?.Value == "1";

    private static ServiceResult DenyDefaultChange() =>
        Outcome.Json(new ApiError(ErrorCatalog.Auth005()), statusCode: 403);

    public async Task<ServiceResult> CreateAsync(ClaimsPrincipal user, FactoryWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.FactoryCode) is { } e1) return e1;
        if (ValidateName(body.FactoryName) is { } e2) return e2;
        var code = body.FactoryCode.Trim();
        if (await db.Factories.AnyAsync(f => f.FactoryCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        if (body.IsDefault && !IsSuperAdmin(user)) return DenyDefaultChange();
        if (body.IsDefault)
            await db.Factories.Where(f => f.IsDefault && !f.IsDeleted)
                              .ExecuteUpdateAsync(u => u.SetProperty(f => f.IsDefault, false), ct);

        var f = new Factory
        {
            FactoryCode = code,
            FactoryName = body.FactoryName.Trim(),
            Country = body.Country, Address = body.Address,
            IsActive = body.IsActive, IsDefault = body.IsDefault,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.Now,
        };
        db.Factories.Add(f);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/factories/{f.FactoryCode}", new ApiSuccess<FactoryDto>(Map(f)));
    }

    public async Task<ServiceResult> UpdateAsync(
        string code, ClaimsPrincipal user, FactoryWriteDto body, CancellationToken ct)
    {
        var f = await db.Factories.FirstOrDefaultAsync(x => x.FactoryCode == code && !x.IsDeleted, ct);
        if (f is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));
        if (ValidateName(body.FactoryName) is { } e) return e;
        if (body.IsDefault != f.IsDefault && !IsSuperAdmin(user)) return DenyDefaultChange();
        if (body.IsDefault && !f.IsDefault)
            await db.Factories.Where(x => x.IsDefault && !x.IsDeleted && x.FactoryCode != code)
                              .ExecuteUpdateAsync(u => u.SetProperty(x => x.IsDefault, false), ct);

        f.FactoryName = body.FactoryName.Trim();
        f.Country = body.Country; f.Address = body.Address;
        f.IsActive = body.IsActive; f.IsDefault = body.IsDefault;
        f.Remark1 = body.Remark1; f.Remark2 = body.Remark2; f.Remark3 = body.Remark3;
        f.Remark4 = body.Remark4; f.Remark5 = body.Remark5;
        f.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<FactoryDto>(Map(f)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var f = await db.Factories.FirstOrDefaultAsync(x => x.FactoryCode == code && !x.IsDeleted, ct);
        if (f is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));
        if (await db.Zones.AnyAsync(z => z.FactoryCode == code && !z.IsDeleted, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Del001()));
        if (await db.AppUsers.AnyAsync(u => u.FactoryCode == code && !u.IsDeleted, ct)
            || await db.ProductionOrders.AnyAsync(x => x.FactoryCode == code, ct)
            || await db.ProductionConsumptions.AnyAsync(x => x.FactoryCode == code, ct)
            || await db.ProductionReturns.AnyAsync(x => x.FactoryCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Del001()));
        f.IsDeleted = true; f.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.Factories.AsNoTracking().Where(f => !f.IsDeleted).OrderBy(f => f.FactoryCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("factory_code,factory_name,country,address,is_active,is_default,remark_1,remark_2,remark_3,remark_4,remark_5");
        foreach (var f in rows)
            sb.Append(Csv(f.FactoryCode)).Append(',').Append(Csv(f.FactoryName)).Append(',')
              .Append(Csv(f.Country)).Append(',')
              .Append(Csv(f.Address)).Append(',').Append(f.IsActive ? "1" : "0").Append(',')
              .Append(f.IsDefault ? "1" : "0").Append(',')
              .Append(Csv(f.Remark1)).Append(',').Append(Csv(f.Remark2)).Append(',')
              .Append(Csv(f.Remark3)).Append(',').Append(Csv(f.Remark4)).Append(',')
              .Append(Csv(f.Remark5)).AppendLine();
        return CsvFile(sb, "factories.csv");
    }

    internal static FactoryDto Map(Factory f) => new(
        f.FactoryCode, f.FactoryName, f.Country, f.Address,
        f.IsActive, f.IsDefault,
        f.Remark1, f.Remark2, f.Remark3, f.Remark4, f.Remark5,
        f.CreatedAt, f.UpdatedAt);
}
