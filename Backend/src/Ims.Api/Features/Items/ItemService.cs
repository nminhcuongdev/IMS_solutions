using Ims.Domain.Picking;
using Ims.Domain.Returns;
using Ims.Domain.Shipping;
using Ims.Domain.Receiving;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Items;

internal sealed class ItemService(ImsDbContext db) : IItemService
{
    private static readonly SortMap<Item> Sort = new SortMap<Item>()
        .Add("item_code", i => i.ItemCode)
        .Add("item_name", i => i.ItemName)
        .Add("item_short_name", i => i.ItemShortName)
        .Add("item_group_code", i => i.ItemGroupCode)
        .Add("uom_code", i => i.UomCode)
        .Add("packing_type_code", i => i.PackingTypeCode)
        .Add("package_qty", i => i.PackageQty)
        .Add("is_lot_managed", i => i.IsLotManaged)
        .Add("ht_scan_required", i => i.HtScanRequired)
        .Add("is_serial_managed", i => i.IsSerialManaged)
        .Add("barcode", i => i.Barcode)
        .Add("expiration_period", i => i.ExpirationPeriod)
        .Add("surplus_point", i => i.SurplusPoint)
        .Add("reorder_level_1", i => i.ReorderLevel1)
        .Add("reorder_level_2", i => i.ReorderLevel2)
        .Add("default_location_code", i => i.DefaultLocationCode)
        .Add("is_active", i => i.IsActive)
        .Add("remark_1", i => i.Remark1)
        .Add("remark_2", i => i.Remark2)
        .Add("remark_3", i => i.Remark3)
        .Add("remark_4", i => i.Remark4)
        .Add("remark_5", i => i.Remark5)
        .Add("created_at", i => i.CreatedAt)
        .Add("updated_at", i => i.UpdatedAt)
        .Default("item_code")
        .ThenAlwaysBy(i => i.ItemCode);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? itemGroupCode = null)
    {
        var query = db.Items.AsNoTracking().Where(i => !i.IsDeleted);
        if (isActive ?? true) query = query.Where(i => i.IsActive);
        if (!string.IsNullOrWhiteSpace(itemGroupCode))
            query = query.Where(i => i.ItemGroupCode == itemGroupCode);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(i => EF.Functions.Like(i.ItemCode, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(i.ItemName, LikeContains(s), LikeEscape));
        }
        query = Sort.Apply(query, sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(i => Map(i)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ItemDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var i = await db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.ItemCode == code && !x.IsDeleted, ct);
        return i is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")))
            : Outcome.Ok(new ApiSuccess<ItemDto>(Map(i)));
    }

    public async Task<ServiceResult> CreateAsync(ItemWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.ItemCode) is { } e1) return e1;
        if (ValidateName(body.ItemName) is { } e2) return e2;
        if (HasNegative(body)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));
        var code = body.ItemCode.Trim();
        if (await db.Items.AnyAsync(i => i.ItemCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        if (string.IsNullOrWhiteSpace(body.ItemGroupCode)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldItemGroupCode", "品目グループコード")));
        var wantedGroup = body.ItemGroupCode.Trim().ToUpper();
        var groupCode = await db.ItemGroups.Where(g => g.ItemGroupCode.ToUpper() == wantedGroup && !g.IsDeleted)
            .Select(g => g.ItemGroupCode).FirstOrDefaultAsync(ct);
        if (groupCode is null)
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("ItemGroup")));
        if (!string.IsNullOrWhiteSpace(body.PackingTypeCode)
            && !await db.DefaultValues.AnyAsync(d => d.ValueType == "PackingType" && d.ValueCode == body.PackingTypeCode && d.IsActive, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("PackingType")));
        if (!string.IsNullOrWhiteSpace(body.DefaultLocationCode)
            && !await db.Locations.AnyAsync(l => l.LocationCode == body.DefaultLocationCode && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));

        var i = new Item
        {
            ItemCode = code,
            ItemName = body.ItemName.Trim(),
            ItemGroupCode = groupCode,
            UomCode = body.UomCode?.Trim(),
            PackingTypeCode = NullIfBlank(body.PackingTypeCode),
            PackageQty = body.PackageQty,
            IsLotManaged = body.IsLotManaged,
            HtScanRequired = body.HtScanRequired,
            ExpirationPeriod = body.ExpirationPeriod,
            Barcode = body.Barcode,
            SurplusPoint = body.SurplusPoint,
            ReorderLevel1 = body.ReorderLevel1,
            ReorderLevel2 = body.ReorderLevel2,
            DefaultLocationCode = NullIfBlank(body.DefaultLocationCode),
            IsActive = body.IsActive,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.Now,
        };
        db.Items.Add(i);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/items/{i.ItemCode}", new ApiSuccess<ItemDto>(Map(i)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, ItemWriteDto body, CancellationToken ct)
    {
        var i = await db.Items.FirstOrDefaultAsync(x => x.ItemCode == code && !x.IsDeleted, ct);
        if (i is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));
        if (ValidateName(body.ItemName) is { } e) return e;
        if (HasNegative(body)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));
        if (string.IsNullOrWhiteSpace(body.ItemGroupCode)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldItemGroupCode", "品目グループコード")));
        var wantedGroup = body.ItemGroupCode.Trim().ToUpper();
        var groupCode = await db.ItemGroups.Where(g => g.ItemGroupCode.ToUpper() == wantedGroup && !g.IsDeleted)
            .Select(g => g.ItemGroupCode).FirstOrDefaultAsync(ct);
        if (groupCode is null)
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("ItemGroup")));
        if (!string.IsNullOrWhiteSpace(body.PackingTypeCode)
            && !await db.DefaultValues.AnyAsync(d => d.ValueType == "PackingType" && d.ValueCode == body.PackingTypeCode && d.IsActive, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("PackingType")));
        if (!string.IsNullOrWhiteSpace(body.DefaultLocationCode)
            && !await db.Locations.AnyAsync(l => l.LocationCode == body.DefaultLocationCode && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));

        i.ItemName = body.ItemName.Trim();
        i.ItemGroupCode = groupCode;
        i.UomCode = body.UomCode?.Trim();
        i.PackingTypeCode = NullIfBlank(body.PackingTypeCode);
        i.PackageQty = body.PackageQty;
        i.IsLotManaged = body.IsLotManaged;
        i.HtScanRequired = body.HtScanRequired;
        i.ExpirationPeriod = body.ExpirationPeriod;
        i.Barcode = body.Barcode;
        i.SurplusPoint = body.SurplusPoint;
        i.ReorderLevel1 = body.ReorderLevel1;
        i.ReorderLevel2 = body.ReorderLevel2;
        i.DefaultLocationCode = NullIfBlank(body.DefaultLocationCode);
        i.IsActive = body.IsActive;
        i.Remark1 = body.Remark1; i.Remark2 = body.Remark2; i.Remark3 = body.Remark3;
        i.Remark4 = body.Remark4; i.Remark5 = body.Remark5;
        i.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<ItemDto>(Map(i)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var i = await db.Items.FirstOrDefaultAsync(x => x.ItemCode == code && !x.IsDeleted, ct);
        if (i is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));
        if (await db.Stocks.AnyAsync(x => x.ItemCode == code && x.StockQty > 0, ct)
            || await db.ReceivingPlans.AnyAsync(x => x.ItemCode == code && (x.Status == ReceivingStatus.NotArrived || x.Status == ReceivingStatus.Partial), ct)
            || await db.ShippingPlans.AnyAsync(x => x.ItemCode == code && (x.Status == ShippingStatus.Pending || x.Status == ShippingStatus.Partial), ct)
            || await db.ReturnPlans.AnyAsync(x => x.ItemCode == code && (x.Status == ReturnStatus.Pending || x.Status == ReturnStatus.Partial), ct)
            || await db.PickingPlans.AnyAsync(x => x.ItemCode == code && x.Status == PickingStatus.NotPicked, ct)
            || await db.ProductionOrders.AnyAsync(x => x.OutputItemCode == code, ct)
            || await db.ProductionOrderLines.AnyAsync(x => x.ItemCode == code, ct)
            || await db.ProductionResults.AnyAsync(x => x.ItemCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Del001()));
        i.IsDeleted = true; i.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.Items.AsNoTracking().Where(i => !i.IsDeleted).OrderBy(i => i.ItemCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("item_code,item_name,item_group_code,uom_code,packing_type_code,package_qty,expiration_period,barcode,surplus_point,reorder_level_1,reorder_level_2,default_location_code,is_active,remark_1,remark_2,remark_3,remark_4,remark_5,is_lot_managed,ht_scan_required");
        foreach (var i in rows)
            sb.Append(Csv(i.ItemCode)).Append(',').Append(Csv(i.ItemName)).Append(',')
              .Append(Csv(i.ItemGroupCode)).Append(',').Append(Csv(i.UomCode)).Append(',')
              .Append(Csv(i.PackingTypeCode)).Append(',').Append(Num(i.PackageQty)).Append(',')
              .Append(Num(i.ExpirationPeriod)).Append(',').Append(Csv(i.Barcode)).Append(',')
              .Append(Num(i.SurplusPoint)).Append(',').Append(Num(i.ReorderLevel1)).Append(',').Append(Num(i.ReorderLevel2)).Append(',')
              .Append(Csv(i.DefaultLocationCode)).Append(',').Append(i.IsActive ? "1" : "0").Append(',')
              .Append(Csv(i.Remark1)).Append(',').Append(Csv(i.Remark2)).Append(',')
              .Append(Csv(i.Remark3)).Append(',').Append(Csv(i.Remark4)).Append(',')
              .Append(Csv(i.Remark5)).Append(',').Append(i.IsLotManaged ? "1" : "0").Append(',')
              .Append(i.HtScanRequired ? "1" : "0").AppendLine();
        return CsvFile(sb, "items.csv");
    }

    internal static ItemDto Map(Item i) => new(
        i.ItemCode, i.ItemName, i.ItemGroupCode, i.UomCode, i.PackingTypeCode,
        i.PackageQty, i.ExpirationPeriod, i.Barcode,
        i.SurplusPoint, i.ReorderLevel1, i.ReorderLevel2,
        i.DefaultLocationCode, i.IsActive,
        i.Remark1, i.Remark2, i.Remark3, i.Remark4, i.Remark5,
        i.CreatedAt, i.UpdatedAt, i.IsLotManaged, i.HtScanRequired);

    private static bool HasNegative(ItemWriteDto b) =>
        b.PackageQty < 0 || b.ExpirationPeriod < 0 || b.SurplusPoint < 0 || b.ReorderLevel1 < 0 || b.ReorderLevel2 < 0;
}
