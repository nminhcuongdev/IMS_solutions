using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;

namespace Ims.Api.Features.History;

internal sealed partial class HistoryService
{
    public const string ReprintFlagField = "ReprintFlag";
    public const string ReprintFlagValue = "REPRINT";
    private const int SpecMaxLength = 100;

    private static readonly string[] RecordedCategories = ["Receiving", "Returns", "Shipping", "Production"];

    private static readonly string[] ReprintableCategories =
        [.. RecordedCategories, .. RecordedCategories.Select(c => c + "Reprint")];

    private static readonly Dictionary<string, string> FallbackLayouts = new(StringComparer.Ordinal)
    {
        ["Receiving"] = "Layout_IMS/ReceivingQR_24dpmm.mllayx",
        ["Returns"] = "Layout_IMS/ReturnsQR_24dpmm.mllayx",
        ["Shipping"] = "Layout_IMS/ShippingQR_24dpmm.mllayx",
        ["Production"] = "Layout_IMS/ProductionQR_24dpmm.mllayx",
    };

    private static string BaseCategory(string category) =>
        category.EndsWith(ReprintSuffix, StringComparison.Ordinal) ? category[..^ReprintSuffix.Length] : category;

    private static string? Snapshot(Dictionary<string, string>? fields, bool reprint)
    {
        if (fields is null || fields.Count == 0) return null;
        var copy = new Dictionary<string, string>(fields, StringComparer.Ordinal)
        {
            [ReprintFlagField] = reprint ? ReprintFlagValue : "",
        };
        copy.Remove("Quantiy");
        return JsonSerializer.Serialize(copy);
    }

    private static Dictionary<string, string>? ReadSnapshot(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json);

    public async Task<ServiceResult> ReprintDataAsync(long no, CancellationToken ct)
    {
        var source = await db.PrintHistories.AsNoTracking().FirstOrDefaultAsync(h => h.No == no, ct);
        if (source is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Print history")));
        var category = BaseCategory(source.Category);
        var layout = source.LayoutFile ?? FallbackLayouts.GetValueOrDefault(category);
        if (layout is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Layout")));
        if (ReadSnapshot(source.PrintData) is { } fields)
        {
            fields[ReprintFlagField] = ReprintFlagValue;
            return Outcome.Ok(new ApiSuccess<PrintReprintDataDto>(new PrintReprintDataDto(layout, fields, null)));
        }
        var doc = await SourceDocumentAsync(category, source, ct);
        return doc is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.Sys404(source.DocNo ?? "Document")))
            : Outcome.Ok(new ApiSuccess<PrintReprintDataDto>(new PrintReprintDataDto(layout, null, doc)));
    }

    public async Task<ServiceResult> ReprintAsync(long no, PrintReprintRequest body, ClaimsPrincipal user, CancellationToken ct)
    {
        var source = await db.PrintHistories.AsNoTracking().FirstOrDefaultAsync(h => h.No == no, ct);
        if (source is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Print history")));
        if (body.Count < 1) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));
        var category = BaseCategory(source.Category);
        var fields = ReadSnapshot(source.PrintData);
        if (fields is null)
        {
            if (await SourceDocumentAsync(category, source, ct) is null)
                return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404(source.DocNo ?? "Document")));
            fields = new Dictionary<string, string>(body.Fields ?? [], StringComparer.Ordinal);
        }
        fields[ReprintFlagField] = ReprintFlagValue;

        var spec = (body.DeviceCode ?? "").Trim();
        string layout;
        if (body.ClientPrinted)
        {
            if (spec.Length > SpecMaxLength) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));
            var known = source.LayoutFile ?? FallbackLayouts.GetValueOrDefault(category);
            if (known is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Layout")));
            layout = known;
        }
        else
        {
            var (target, refused) = await targets.ResolveAsync(targets.LayoutKeyFor(category), body.DeviceCode, ct);
            if (refused is not null) return refused;
            layout = source.LayoutFile ?? target!.Value.LayoutFile;
            try
            {
                await engine.PrintAsync(layout, target!.Value.Setting, fields, body.Count, ct);
            }
            catch (Exception e) when (e is Print.MlEngineException or HttpRequestException or TaskCanceledException)
            {
                return Outcome.Json(new ApiError(ErrorCatalog.Prn002()), statusCode: 500);
            }
        }

        var row = new Domain.History.PrintHistory
        {
            Category = category + ReprintSuffix,
            CreatedAt = DateTime.Now,
            DocNo = source.DocNo,
            ScheduledDate = source.ScheduledDate,
            ItemCode = source.ItemCode,
            Qty = source.Qty,
            UomCode = source.UomCode,
            PackageCount = body.Count,
            Spec = spec.Length == 0 ? null : spec,
            Status = ReprintedStatus,
            CreatedBy = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            PrintData = JsonSerializer.Serialize(fields),
            LayoutFile = layout,
            ReprintOfNo = source.No,
        };
        db.PrintHistories.Add(row);
        await db.SaveChangesAsync(ct);

        var dto = await PrintProjected(db, db.PrintHistories.AsNoTracking().Where(h => h.No == row.No))
            .FirstOrDefaultAsync(ct);
        return Outcome.Created($"/print-history?q={row.DocNo}", new ApiSuccess<PrintHistoryDto>(dto!));
    }

    public async Task<ServiceResult> ResultReprintAsync(string type, long no, PrintReprintRequest body, ClaimsPrincipal user, CancellationToken ct)
    {
        (string Category, string? DocNo, string ItemCode)? source = type.ToLowerInvariant() switch
        {
            "receiving" => await db.ReceivingResults.AsNoTracking().Where(r => r.No == no)
                .Select(r => new ValueTuple<string, string?, string>("Receiving", r.PoNo, r.ItemCode)).FirstOrDefaultAsync(ct),
            "returns" => await db.ReturnResults.AsNoTracking().Where(r => r.No == no)
                .Select(r => new ValueTuple<string, string?, string>("Returns", r.ReturnNo, r.ItemCode)).FirstOrDefaultAsync(ct),
            "shipping" => await db.ShippingResults.AsNoTracking().Where(r => r.No == no)
                .Select(r => new ValueTuple<string, string?, string>("Shipping", r.SoNo, r.ItemCode)).FirstOrDefaultAsync(ct),
            _ => null,
        };
        if (source is not { Category: not null } s || s.DocNo is null)
            return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Result")));
        var original = await db.PrintHistories.AsNoTracking()
            .Where(h => h.Category == s.Category && h.DocNo == s.DocNo && h.ItemCode == s.ItemCode)
            .OrderBy(h => h.No).Select(h => (long?)h.No).FirstOrDefaultAsync(ct);
        return original is { } historyNo
            ? await ReprintAsync(historyNo, body, user, ct)
            : Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Print history")));
    }

    private async Task<PrintSourceDto?> SourceDocumentAsync(string category, Domain.History.PrintHistory h, CancellationToken ct)
    {
        var doc = h.DocNo;
        var item = h.ItemCode;
        if (doc is null || item is null) return null;
        string? factoryCode, partnerCode, lot;
        DateOnly? expiry;
        DateTime? produced;
        switch (category)
        {
            case "Receiving":
            {
                var plan = await db.ReceivingPlans.AsNoTracking().Where(p => p.PoNo == doc && p.ItemCode == item)
                    .OrderBy(p => p.PoBranchNo).FirstOrDefaultAsync(ct);
                if (plan is null) return null;
                var result = await db.ReceivingResults.AsNoTracking().Where(r => r.PoNo == doc && r.ItemCode == item)
                    .OrderByDescending(r => r.No).FirstOrDefaultAsync(ct);
                (factoryCode, partnerCode) = (plan.FactoryCode, plan.PartnerCode);
                (lot, expiry, produced) = (result?.LotNo ?? plan.LotNo, result?.ExpiryDate, result?.CreatedAt);
                break;
            }
            case "Returns":
            {
                var plan = await db.ReturnPlans.AsNoTracking().Where(p => p.ReturnNo == doc && p.ItemCode == item)
                    .OrderBy(p => p.ReturnBranchNo).FirstOrDefaultAsync(ct);
                if (plan is null) return null;
                var result = await db.ReturnResults.AsNoTracking().Where(r => r.ReturnNo == doc && r.ItemCode == item)
                    .OrderByDescending(r => r.No).FirstOrDefaultAsync(ct);
                (factoryCode, partnerCode) = (plan.FactoryCode, plan.PartnerCode);
                (lot, expiry, produced) = (result?.LotNo ?? plan.LotNo, null, result?.CreatedAt);
                break;
            }
            case "Shipping":
            {
                var plan = await db.ShippingPlans.AsNoTracking().Where(p => p.SoNo == doc && p.ItemCode == item)
                    .OrderBy(p => p.SoBranchNo).FirstOrDefaultAsync(ct);
                if (plan is null) return null;
                var result = await db.ShippingResults.AsNoTracking().Where(r => r.SoNo == doc && r.ItemCode == item)
                    .OrderByDescending(r => r.No).FirstOrDefaultAsync(ct);
                (factoryCode, partnerCode) = (plan.FactoryCode, plan.PartnerCode);
                (lot, expiry, produced) = (result?.LotNo, null, result?.CreatedAt);
                break;
            }
            case "Production":
            {
                var output = await db.ProductionOutputs.AsNoTracking().Where(o => o.SerialNo == doc && o.ItemCode == item)
                    .FirstOrDefaultAsync(ct);
                if (output is null) return null;
                (factoryCode, partnerCode) = (output.FactoryCode, null);
                (lot, expiry, produced) = (output.LotNo, output.ExpiryDate, output.CreatedAt);
                break;
            }
            default:
                return null;
        }
        var factory = factoryCode is null ? null
            : await db.Factories.AsNoTracking().FirstOrDefaultAsync(f => f.FactoryCode == factoryCode, ct);
        var partner = partnerCode is null ? null
            : await db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.PartnerCode == partnerCode, ct);
        var master = await db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemCode == item, ct);
        return new PrintSourceDto(doc, item, master?.ItemName, h.UomCode ?? master?.UomCode, h.Qty ?? 0, lot, expiry,
            produced is { } at ? DateOnly.FromDateTime(at) : h.ScheduledDate, h.ScheduledDate, factory?.FactoryCode,
            factory?.FactoryName, factory?.Address, partner?.PartnerCode, partner?.PartnerName, master?.Barcode);
    }
}
