using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Domain.Production;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Import;

internal sealed class ProductionOrderImport(ImsDbContext db)
{
    private static readonly string[] Headers =
    [
        "Production Order No", "Order Date", "Factory Code", "Product Code", "Product Planned",
        "Product Unit", "Production Lot No", "Finished Goods Lot No", "Storage Location Code",
        "Start Date", "End Date", "Line No", "Process Code", "Material Code",
        "Material Planned", "Material Unit", "Production Scheduled Date"
    ];

    public async Task<ServiceResult> RunAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct)
    {
        if (file is null || !Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Imp001()));
        SpreadsheetReader.Sheet sheet;
        try
        {
            await using var stream = file.OpenReadStream();
            sheet = SpreadsheetReader.Read(stream, file.FileName);
        }
        catch
        {
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Imp001()));
        }
        var positions = Headers.Select(h => sheet.Header.ToList().FindIndex(x => string.Equals(x.Trim(), h, StringComparison.OrdinalIgnoreCase))).ToArray();
        if (new[] { 0, 1, 2, 3, 4, 5, 8, 9, 10, 11, 12, 13, 14, 15 }.Any(i => positions[i] < 0))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Imp002()));
        var source = sheet.Rows.Where(x => !x.IsBlank).ToList();
        var errors = new List<ImportRowErrorDto>();
        var orders = new Dictionary<string, ProductionOrder>(StringComparer.Ordinal);
        var lines = new List<ProductionOrderLine>();
        var worker = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var now = DateTime.Now;
        foreach (var row in source)
        {
            string V(int i) => row.Cell(positions[i]);
            void Add(ApiErrorBody error, string? detail = null) => errors.Add(new ImportRowErrorDto(row.RowNo, error.Code, error.Message, detail));
            var orderNo = V(0);
            if (ValidateCode(orderNo, 30) is { } codeError) { Add(((ApiError)codeError.Body!).Error); continue; }
            var invalidHeaderCode = new[] { 2, 3, 8 }.Select(i => ValidateCode(V(i))).FirstOrDefault(x => x is not null);
            if (invalidHeaderCode is not null) { Add(((ApiError)invalidHeaderCode.Body!).Error); continue; }
            if (string.IsNullOrWhiteSpace(V(5)) || V(5).Length > 10) { Add(ErrorCatalog.Val008()); continue; }
            if (!DateOnly.TryParse(V(1), CultureInfo.InvariantCulture, DateTimeStyles.None, out var orderDate)
                || !DateOnly.TryParse(V(9), CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate)
                || !DateOnly.TryParse(V(10), CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            { Add(ErrorCatalog.Val001()); continue; }
            if (endDate < startDate) { Add(ErrorCatalog.Val006()); continue; }
            if (!decimal.TryParse(V(4), NumberStyles.Number, CultureInfo.InvariantCulture, out var productQty) || productQty <= 0)
            { Add(ErrorCatalog.Qty003()); continue; }
            var factoryCode = V(2);
            var productCode = V(3);
            var locationCode = V(8);
            var outputLot = V(7);
            var factory = await db.Factories.AsNoTracking().FirstOrDefaultAsync(x => x.FactoryCode == factoryCode && x.IsActive && !x.IsDeleted, ct);
            var product = await db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.ItemCode == productCode && x.IsActive && !x.IsDeleted, ct);
            var location = await db.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.LocationCode == locationCode && x.IsActive && !x.IsDeleted, ct);
            if (factory is null || product is null || location is null) { Add(ErrorCatalog.NotFound(factory is null ? V(2) : product is null ? V(3) : V(8))); continue; }
            var productGroup = await db.ItemGroups.AsNoTracking().FirstAsync(x => x.ItemGroupCode == product.ItemGroupCode, ct);
            if (productGroup.Category is not (ItemGroupCategory.FinishedGoods or ItemGroupCategory.SemiFinished)) { Add(ErrorCatalog.Val009()); continue; }
            if (!product.IsLotManaged) { Add(ErrorCatalog.Val010()); continue; }
            if (!string.Equals(product.UomCode, V(5), StringComparison.OrdinalIgnoreCase)) { Add(ErrorCatalog.Val008()); continue; }
            var zoneFactory = await db.Zones.Where(x => x.ZoneCode == location.ZoneCode).Select(x => x.FactoryCode).FirstOrDefaultAsync(ct);
            if (zoneFactory != factory.FactoryCode) { Add(ErrorCatalog.Loc004()); continue; }
            if (V(6).Length > 20 || V(7).Length > 20 || V(6).AsSpan().IndexOfAny(BadCodeChars) >= 0
                || V(7).AsSpan().IndexOfAny(BadCodeChars) >= 0)
            { Add(ErrorCatalog.Val005()); continue; }
            if (await db.ProductionOrders.AnyAsync(x => x.ProductionOrderNo == orderNo, ct)) { Add(ErrorCatalog.Dup001()); continue; }
            if (!string.IsNullOrWhiteSpace(outputLot) && (await db.Stocks.AnyAsync(x => x.ItemCode == product.ItemCode && x.LotNo == outputLot, ct)
                || await db.ProductionOutputs.AnyAsync(x => x.ItemCode == product.ItemCode && x.LotNo == outputLot, ct)
                || await db.ProductionOrders.AnyAsync(x => x.OutputItemCode == product.ItemCode && x.OutputLotNo == outputLot, ct)
                || orders.Values.Any(x => x.OutputItemCode == product.ItemCode && x.OutputLotNo == outputLot && x.ProductionOrderNo != orderNo)))
            { Add(ErrorCatalog.Dup003()); continue; }
            var candidate = new ProductionOrder
            {
                ProductionOrderNo = orderNo, OrderDate = orderDate, FactoryCode = V(2), OutputItemCode = V(3),
                PlannedQty = productQty, UomCode = product.UomCode!, ProductionLotNo = NullIfBlank(V(6)),
                OutputLotNo = NullIfBlank(V(7)), StorageLocationCode = V(8), PlannedStartDate = startDate,
                PlannedEndDate = endDate, CreatedBy = worker, CreatedAt = now
            };
            if (orders.TryGetValue(orderNo, out var first))
            {
                if (first.OrderDate != candidate.OrderDate || first.FactoryCode != candidate.FactoryCode
                    || first.OutputItemCode != candidate.OutputItemCode || first.PlannedQty != candidate.PlannedQty
                    || first.UomCode != candidate.UomCode || first.ProductionLotNo != candidate.ProductionLotNo
                    || first.OutputLotNo != candidate.OutputLotNo || first.StorageLocationCode != candidate.StorageLocationCode
                    || first.PlannedStartDate != candidate.PlannedStartDate || first.PlannedEndDate != candidate.PlannedEndDate)
                { Add(ErrorCatalog.Imp004()); continue; }
            }
            else orders.Add(orderNo, candidate);
            var blank = Enumerable.Range(11, 6).All(i => string.IsNullOrWhiteSpace(V(i)));
            if (blank)
            {
                if (lines.Any(x => x.ProductionOrderNo == orderNo) || source.Count(x => x.Cell(positions[0]) == orderNo) > 1)
                    Add(ErrorCatalog.Imp004());
                continue;
            }
            if (!int.TryParse(V(11), out var lineNo) || lineNo <= 0 || string.IsNullOrWhiteSpace(V(12))
                || string.IsNullOrWhiteSpace(V(13)) || string.IsNullOrWhiteSpace(V(15)))
            { Add(ErrorCatalog.Val001()); continue; }
            var invalidLineCode = new[] { 12, 13 }.Select(i => ValidateCode(V(i))).FirstOrDefault(x => x is not null);
            if (invalidLineCode is not null) { Add(((ApiError)invalidLineCode.Body!).Error); continue; }
            if (V(15).Length > 10) { Add(ErrorCatalog.Val008()); continue; }
            if (lines.Any(x => x.ProductionOrderNo == orderNo && x.LineNo == lineNo)) { Add(ErrorCatalog.Imp005()); continue; }
            if (!decimal.TryParse(V(14), NumberStyles.Number, CultureInfo.InvariantCulture, out var materialQty) || materialQty <= 0)
            { Add(ErrorCatalog.Qty003()); continue; }
            var processCode = V(12);
            var materialCode = V(13);
            var process = await db.Processes.AsNoTracking().FirstOrDefaultAsync(x => x.ProcessCode == processCode && x.IsActive && !x.IsDeleted, ct);
            var material = await db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.ItemCode == materialCode && x.IsActive && !x.IsDeleted, ct);
            if (process is null || material is null) { Add(ErrorCatalog.NotFound(process is null ? V(12) : V(13))); continue; }
            var materialGroup = await db.ItemGroups.AsNoTracking().FirstAsync(x => x.ItemGroupCode == material.ItemGroupCode, ct);
            if (materialGroup.Category is not (ItemGroupCategory.RawMaterials or ItemGroupCategory.SemiFinished or ItemGroupCategory.PurchasedParts or ItemGroupCategory.AuxiliaryMaterials))
            { Add(ErrorCatalog.Val009()); continue; }
            if (!string.Equals(material.UomCode, V(15), StringComparison.OrdinalIgnoreCase)) { Add(ErrorCatalog.Val008()); continue; }
            var scheduleDate = startDate;
            if (!string.IsNullOrWhiteSpace(V(16)) && !DateOnly.TryParse(V(16), CultureInfo.InvariantCulture, DateTimeStyles.None, out scheduleDate))
            { Add(ErrorCatalog.Val001()); continue; }
            lines.Add(new ProductionOrderLine { ProductionOrderNo = orderNo, LineNo = lineNo, ProcessCode = V(12),
                ItemCode = V(13), PlannedQty = materialQty, UomCode = material.UomCode!, ScheduledDate = scheduleDate,
                CreatedAt = now, CreatedBy = worker });
        }
        if (errors.Count > 0) return Outcome.Ok(new ApiSuccess<ImportResultDto>(new ImportResultDto(0, 0, errors)));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.ProductionOrders.AddRange(orders.Values);
        db.ProductionOrderLines.AddRange(lines);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Ok(new ApiSuccess<ImportResultDto>(new ImportResultDto(orders.Count, 0, [], lines.Count)));
    }
}
