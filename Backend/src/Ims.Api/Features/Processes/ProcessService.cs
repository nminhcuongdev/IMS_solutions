using System.Text;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;
using Process = Ims.Domain.Masters.Process;

namespace Ims.Api.Features.Processes;

internal sealed class ProcessService(ImsDbContext db) : IProcessService
{
    private static readonly SortMap<Process> Sort = new SortMap<Process>()
        .Add("process_code", x => x.ProcessCode)
        .Add("process_name", x => x.ProcessName)
        .Add("display_order", x => x.DisplayOrder)
        .Add("is_active", x => x.IsActive)
        .Add("remark_1", x => x.Remark1)
        .Add("remark_2", x => x.Remark2)
        .Add("remark_3", x => x.Remark3)
        .Add("remark_4", x => x.Remark4)
        .Add("remark_5", x => x.Remark5)
        .Add("created_at", x => x.CreatedAt)
        .Add("updated_at", x => x.UpdatedAt)
        .Default("process_code").ThenAlwaysBy(x => x.ProcessCode);

    public async Task<ServiceResult> ListAsync(CancellationToken ct, int? page = null, int? pageSize = null,
        string? q = null, string? sort = null, bool? isActive = null)
    {
        var query = db.Processes.AsNoTracking().Where(x => !x.IsDeleted);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = LikeContains(q.Trim());
            query = query.Where(x => EF.Functions.Like(x.ProcessCode, pattern, LikeEscape)
                || EF.Functions.Like(x.ProcessName, pattern, LikeEscape));
        }
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page);
        var sz = NormalizePageSize(pageSize);
        var rows = await Sort.Apply(query, sort).Skip((pg - 1) * sz).Take(sz).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ProcessDto>(rows.Select(Map).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var entity = await db.Processes.AsNoTracking().FirstOrDefaultAsync(x => x.ProcessCode == code && !x.IsDeleted, ct);
        return entity is null ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Process")))
            : Outcome.Ok(new ApiSuccess<ProcessDto>(Map(entity)));
    }

    public async Task<ServiceResult> CreateAsync(ProcessWriteDto body, CancellationToken ct, ClaimsPrincipal? user = null)
    {
        if (ValidateCode(body.ProcessCode) is { } codeError) return codeError;
        if (ValidateName(body.ProcessName) is { } nameError) return nameError;
        var code = body.ProcessCode.Trim();
        if (await db.Processes.AnyAsync(x => x.ProcessCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        var entity = new Process { ProcessCode = code, CreatedAt = DateTime.Now,
            CreatedBy = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value };
        Apply(entity, body);
        db.Processes.Add(entity);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/processes/{code}", new ApiSuccess<ProcessDto>(Map(entity)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, ProcessWriteDto body, CancellationToken ct, ClaimsPrincipal? user = null)
    {
        var entity = await db.Processes.FirstOrDefaultAsync(x => x.ProcessCode == code && !x.IsDeleted, ct);
        if (entity is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Process")));
        if (ValidateName(body.ProcessName) is { } error) return error;
        Apply(entity, body);
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<ProcessDto>(Map(entity)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct, ClaimsPrincipal? user = null)
    {
        var entity = await db.Processes.FirstOrDefaultAsync(x => x.ProcessCode == code && !x.IsDeleted, ct);
        if (entity is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Process")));
        if (await db.ProductionOrderLines.AnyAsync(x => x.ProcessCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Del001()));
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.Processes.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.ProcessCode).ToListAsync(ct);
        var sb = new StringBuilder("Process Code,Process Name,Display Order,Status,Remark1,Remark2,Remark3,Remark4,Remark5\r\n");
        foreach (var x in rows)
            sb.Append(Csv(x.ProcessCode)).Append(',').Append(Csv(x.ProcessName)).Append(',')
              .Append(Num(x.DisplayOrder)).Append(',').Append(x.IsActive ? "Enable" : "Disable").Append(',')
              .Append(Csv(x.Remark1)).Append(',').Append(Csv(x.Remark2)).Append(',')
              .Append(Csv(x.Remark3)).Append(',').Append(Csv(x.Remark4)).Append(',')
              .Append(Csv(x.Remark5)).AppendLine();
        return CsvFile(sb, "processes.csv");
    }

    private static void Apply(Process entity, ProcessWriteDto body)
    {
        entity.ProcessName = body.ProcessName.Trim();
        entity.DisplayOrder = body.DisplayOrder;
        entity.IsActive = body.IsActive;
        entity.Remark1 = body.Remark1;
        entity.Remark2 = body.Remark2;
        entity.Remark3 = body.Remark3;
        entity.Remark4 = body.Remark4;
        entity.Remark5 = body.Remark5;
    }

    private static ProcessDto Map(Process x) => new(x.ProcessCode, x.ProcessName, x.DisplayOrder,
        x.IsActive, x.Remark1, x.Remark2, x.Remark3, x.Remark4, x.Remark5, x.CreatedAt, x.UpdatedAt);
}
