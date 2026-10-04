using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Partners;

internal sealed class PartnerService(ImsDbContext db) : IPartnerService
{
    private static readonly SortMap<Partner> Sort = new SortMap<Partner>()
        .Add("partner_code", p => p.PartnerCode)
        .Add("partner_name", p => p.PartnerName)
        .Add("is_supplier", p => p.IsSupplier)
        .Add("is_customer", p => p.IsCustomer)
        .Add("country", p => p.Country)
        .Add("address", p => p.Address)
        .Add("is_active", p => p.IsActive)
        .Add("remark_1", p => p.Remark1)
        .Add("remark_2", p => p.Remark2)
        .Add("remark_3", p => p.Remark3)
        .Add("remark_4", p => p.Remark4)
        .Add("remark_5", p => p.Remark5)
        .Add("created_at", p => p.CreatedAt)
        .Add("updated_at", p => p.UpdatedAt)
        .Default("partner_code")
        .ThenAlwaysBy(p => p.PartnerCode);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, bool? isSupplier = null, bool? isCustomer = null)
    {
        var query = db.Partners.AsNoTracking().Where(p => !p.IsDeleted);
        if (isActive ?? true) query = query.Where(p => p.IsActive);
        if (isSupplier == true) query = query.Where(p => p.IsSupplier);
        if (isCustomer == true) query = query.Where(p => p.IsCustomer);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(p => EF.Functions.Like(p.PartnerCode, "%" + s + "%")
                                  || EF.Functions.Like(p.PartnerName, "%" + s + "%"));
        }
        query = Sort.Apply(query, sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(p => Map(p)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<PartnerDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var p = await db.Partners.AsNoTracking().FirstOrDefaultAsync(x => x.PartnerCode == code && !x.IsDeleted, ct);
        return p is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Partner")))
            : Outcome.Ok(new ApiSuccess<PartnerDto>(Map(p)));
    }

    public async Task<ServiceResult> CreateAsync(PartnerWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.PartnerCode) is { } e1) return e1;
        if (ValidateName(body.PartnerName) is { } e2) return e2;
        var code = body.PartnerCode.Trim();
        if (await db.Partners.AnyAsync(p => p.PartnerCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        var p = new Partner
        {
            PartnerCode = code,
            PartnerName = body.PartnerName.Trim(),
            IsSupplier = body.IsSupplier, IsCustomer = body.IsCustomer,
            Country = body.Country, Address = body.Address,
            IsActive = body.IsActive,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.UtcNow,
        };
        db.Partners.Add(p);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/partners/{p.PartnerCode}", new ApiSuccess<PartnerDto>(Map(p)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, PartnerWriteDto body, CancellationToken ct)
    {
        var p = await db.Partners.FirstOrDefaultAsync(x => x.PartnerCode == code && !x.IsDeleted, ct);
        if (p is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Partner")));
        if (ValidateName(body.PartnerName) is { } e) return e;
        p.PartnerName = body.PartnerName.Trim();
        p.IsSupplier = body.IsSupplier; p.IsCustomer = body.IsCustomer;
        p.Country = body.Country; p.Address = body.Address;
        p.IsActive = body.IsActive;
        p.Remark1 = body.Remark1; p.Remark2 = body.Remark2; p.Remark3 = body.Remark3;
        p.Remark4 = body.Remark4; p.Remark5 = body.Remark5;
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<PartnerDto>(Map(p)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var p = await db.Partners.FirstOrDefaultAsync(x => x.PartnerCode == code && !x.IsDeleted, ct);
        if (p is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Partner")));
        p.IsDeleted = true; p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.Partners.AsNoTracking().Where(p => !p.IsDeleted).OrderBy(p => p.PartnerCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("partner_code,partner_name,is_supplier,is_customer,country,address,is_active,remark_1,remark_2,remark_3,remark_4,remark_5");
        foreach (var p in rows)
            sb.Append(Csv(p.PartnerCode)).Append(',').Append(Csv(p.PartnerName)).Append(',')
              .Append(p.IsSupplier ? "1" : "0").Append(',').Append(p.IsCustomer ? "1" : "0").Append(',')
              .Append(Csv(p.Country)).Append(',').Append(Csv(p.Address)).Append(',')
              .Append(p.IsActive ? "1" : "0").Append(',')
              .Append(Csv(p.Remark1)).Append(',').Append(Csv(p.Remark2)).Append(',')
              .Append(Csv(p.Remark3)).Append(',').Append(Csv(p.Remark4)).Append(',')
              .Append(Csv(p.Remark5)).AppendLine();
        return CsvFile(sb, "partners.csv");
    }

    internal static PartnerDto Map(Partner p) => new(
        p.PartnerCode, p.PartnerName, p.IsSupplier, p.IsCustomer,
        p.Country, p.Address, p.IsActive,
        p.Remark1, p.Remark2, p.Remark3, p.Remark4, p.Remark5,
        p.CreatedAt, p.UpdatedAt);
}
