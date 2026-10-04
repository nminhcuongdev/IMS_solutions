using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Reasons;

internal sealed class ReasonService(ImsDbContext db) : IReasonService
{
    private static readonly SortMap<Reason> Sort = new SortMap<Reason>()
        .Add("reason_code", r => r.ReasonCode)
        .Add("reason_name", r => r.ReasonName)
        .Add("reason_category", r => r.ReasonCategoryValue)
        .Add("is_active", r => r.IsActive)
        .Add("remark_1", r => r.Remark1)
        .Add("remark_2", r => r.Remark2)
        .Add("remark_3", r => r.Remark3)
        .Add("remark_4", r => r.Remark4)
        .Add("remark_5", r => r.Remark5)
        .Add("created_at", r => r.CreatedAt)
        .Add("updated_at", r => r.UpdatedAt)
        .Default("reason_code")
        .ThenAlwaysBy(r => r.ReasonCode);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? category = null)
    {
        var query = db.Reasons.AsNoTracking().Where(r => !r.IsDeleted);
        if (isActive ?? true) query = query.Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ReasonCategory>(category, true, out var c))
            query = query.Where(r => r.ReasonCategoryValue == c);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(r => EF.Functions.Like(r.ReasonCode, "%" + s + "%")
                                  || EF.Functions.Like(r.ReasonName, "%" + s + "%"));
        }
        query = Sort.Apply(query, sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(r => Map(r)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ReasonDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var r = await db.Reasons.AsNoTracking().FirstOrDefaultAsync(x => x.ReasonCode == code && !x.IsDeleted, ct);
        return r is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")))
            : Outcome.Ok(new ApiSuccess<ReasonDto>(Map(r)));
    }

    public async Task<ServiceResult> CreateAsync(ReasonWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.ReasonCode) is { } e1) return e1;
        if (ValidateName(body.ReasonName) is { } e2) return e2;
        if (!Enum.TryParse<ReasonCategory>(body.ReasonCategory, true, out var cat)
            || !Enum.IsDefined(cat))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        var code = body.ReasonCode.Trim();
        if (await db.Reasons.AnyAsync(r => r.ReasonCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        var r = new Reason
        {
            ReasonCode = code,
            ReasonName = body.ReasonName.Trim(),
            ReasonCategoryValue = cat,
            IsActive = body.IsActive,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.UtcNow,
        };
        db.Reasons.Add(r);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/reasons/{r.ReasonCode}", new ApiSuccess<ReasonDto>(Map(r)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, ReasonWriteDto body, CancellationToken ct)
    {
        var r = await db.Reasons.FirstOrDefaultAsync(x => x.ReasonCode == code && !x.IsDeleted, ct);
        if (r is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));
        if (ValidateName(body.ReasonName) is { } e) return e;
        if (!Enum.TryParse<ReasonCategory>(body.ReasonCategory, true, out var cat)
            || !Enum.IsDefined(cat))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        r.ReasonName = body.ReasonName.Trim();
        r.ReasonCategoryValue = cat;
        r.IsActive = body.IsActive;
        r.Remark1 = body.Remark1; r.Remark2 = body.Remark2; r.Remark3 = body.Remark3;
        r.Remark4 = body.Remark4; r.Remark5 = body.Remark5;
        r.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<ReasonDto>(Map(r)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var r = await db.Reasons.FirstOrDefaultAsync(x => x.ReasonCode == code && !x.IsDeleted, ct);
        if (r is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));
        r.IsDeleted = true; r.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.Reasons.AsNoTracking().Where(r => !r.IsDeleted).OrderBy(r => r.ReasonCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("reason_code,reason_name,reason_category,is_active,remark_1,remark_2,remark_3,remark_4,remark_5");
        foreach (var r in rows)
            sb.Append(Csv(r.ReasonCode)).Append(',').Append(Csv(r.ReasonName)).Append(',')
              .Append(r.ReasonCategoryValue).Append(',').Append(r.IsActive ? "1" : "0").Append(',')
              .Append(Csv(r.Remark1)).Append(',').Append(Csv(r.Remark2)).Append(',')
              .Append(Csv(r.Remark3)).Append(',').Append(Csv(r.Remark4)).Append(',')
              .Append(Csv(r.Remark5)).AppendLine();
        return CsvFile(sb, "reasons.csv");
    }

    internal static ReasonDto Map(Reason r) => new(
        r.ReasonCode, r.ReasonName, r.ReasonCategoryValue.ToString(), r.IsActive,
        r.Remark1, r.Remark2, r.Remark3, r.Remark4, r.Remark5,
        r.CreatedAt, r.UpdatedAt);
}
