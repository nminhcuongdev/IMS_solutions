using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.ItemGroups;

internal sealed class ItemGroupService(ImsDbContext db) : IItemGroupService
{
    private static readonly SortMap<ItemGroup> Sort = new SortMap<ItemGroup>()
        .Add("item_group_code", g => g.ItemGroupCode)
        .Add("item_group_name", g => g.ItemGroupName)
        .Add("category", g => g.Category)
        .Add("is_active", g => g.IsActive)
        .Add("remark_1", g => g.Remark1)
        .Add("remark_2", g => g.Remark2)
        .Add("remark_3", g => g.Remark3)
        .Add("remark_4", g => g.Remark4)
        .Add("remark_5", g => g.Remark5)
        .Add("created_at", g => g.CreatedAt)
        .Add("updated_at", g => g.UpdatedAt)
        .Default("item_group_code")
        .ThenAlwaysBy(g => g.ItemGroupCode);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null)
    {
        var query = db.ItemGroups.AsNoTracking().Where(g => !g.IsDeleted);
        if (isActive ?? true) query = query.Where(g => g.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(g => EF.Functions.Like(g.ItemGroupCode, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(g.ItemGroupName, LikeContains(s), LikeEscape));
        }
        query = Sort.Apply(query, sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(g => Map(g)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ItemGroupDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var g = await db.ItemGroups.AsNoTracking().FirstOrDefaultAsync(x => x.ItemGroupCode == code && !x.IsDeleted, ct);
        return g is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("ItemGroup")))
            : Outcome.Ok(new ApiSuccess<ItemGroupDto>(Map(g)));
    }

    public async Task<ServiceResult> CreateAsync(ItemGroupWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.ItemGroupCode) is { } e1) return e1;
        if (ValidateName(body.ItemGroupName) is { } e2) return e2;
        if (body.Category < 1 || body.Category > 7)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        var code = body.ItemGroupCode.Trim();
        if (await db.ItemGroups.AnyAsync(g => g.ItemGroupCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        var g = new ItemGroup
        {
            ItemGroupCode = code,
            ItemGroupName = body.ItemGroupName.Trim(),
            Category = (ItemGroupCategory)body.Category,
            IsActive = body.IsActive,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.Now,
        };
        db.ItemGroups.Add(g);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/item-groups/{g.ItemGroupCode}", new ApiSuccess<ItemGroupDto>(Map(g)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, ItemGroupWriteDto body, CancellationToken ct)
    {
        var g = await db.ItemGroups.FirstOrDefaultAsync(x => x.ItemGroupCode == code && !x.IsDeleted, ct);
        if (g is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("ItemGroup")));
        if (ValidateName(body.ItemGroupName) is { } e) return e;
        if (body.Category < 1 || body.Category > 7)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        g.ItemGroupName = body.ItemGroupName.Trim();
        g.Category = (ItemGroupCategory)body.Category;
        g.IsActive = body.IsActive;
        g.Remark1 = body.Remark1; g.Remark2 = body.Remark2; g.Remark3 = body.Remark3;
        g.Remark4 = body.Remark4; g.Remark5 = body.Remark5;
        g.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<ItemGroupDto>(Map(g)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var g = await db.ItemGroups.FirstOrDefaultAsync(x => x.ItemGroupCode == code && !x.IsDeleted, ct);
        if (g is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("ItemGroup")));
        if (await db.Items.AnyAsync(i => i.ItemGroupCode == code && !i.IsDeleted, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Del001()));
        g.IsDeleted = true; g.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.ItemGroups.AsNoTracking().Where(g => !g.IsDeleted).OrderBy(g => g.ItemGroupCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("item_group_code,item_group_name,category,is_active,remark_1,remark_2,remark_3,remark_4,remark_5");
        foreach (var g in rows)
            sb.Append(Csv(g.ItemGroupCode)).Append(',').Append(Csv(g.ItemGroupName)).Append(',')
              .Append((byte)g.Category).Append(',').Append(g.IsActive ? "1" : "0").Append(',')
              .Append(Csv(g.Remark1)).Append(',').Append(Csv(g.Remark2)).Append(',')
              .Append(Csv(g.Remark3)).Append(',').Append(Csv(g.Remark4)).Append(',')
              .Append(Csv(g.Remark5)).AppendLine();
        return CsvFile(sb, "item-groups.csv");
    }

    internal static ItemGroupDto Map(ItemGroup g) => new(
        g.ItemGroupCode, g.ItemGroupName, (byte)g.Category, g.IsActive,
        g.Remark1, g.Remark2, g.Remark3, g.Remark4, g.Remark5,
        g.CreatedAt, g.UpdatedAt);
}
