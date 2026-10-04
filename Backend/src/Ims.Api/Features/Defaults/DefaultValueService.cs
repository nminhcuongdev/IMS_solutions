using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Defaults;

internal sealed class DefaultValueService(ImsDbContext db) : IDefaultValueService
{
    public async Task<ServiceResult> ListAsync(string type, CancellationToken ct, bool? isActive = null)
    {
        var q = db.DefaultValues.AsNoTracking().Where(d => d.ValueType == type);
        if (isActive ?? true) q = q.Where(d => d.IsActive);
        var rows = await q.OrderBy(d => d.DisplayOrder).ThenBy(d => d.ValueCode)
            .Select(d => new DefaultValueDto(
                d.ValueType, d.ValueCode, d.NameLang1, d.NameLang2, d.NameLang3,
                d.DisplayOrder, d.IsActive))
            .ToListAsync(ct);
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<DefaultValueDto>>(rows));
    }
}
