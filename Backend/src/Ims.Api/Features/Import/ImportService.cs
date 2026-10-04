using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Picking;
using Ims.Domain.Receiving;
using Ims.Domain.Returns;
using Ims.Domain.Shipping;
using Ims.Infrastructure.Persistence;
using System.Globalization;
using System.Security.Claims;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Import;

internal sealed class ImportService(ImsDbContext db) : IImportService
{
    private enum Fld
    {
        DocNo, BranchNo, PartnerCode, ItemCode, Qty, FactoryCode, ScheduledDate,
        LocationCode, GateCode, InvoiceNo, LotNo,
    }

    private sealed record ImportDef(
        string Name,
        string[] Header,
        string[] Sample,
        Dictionary<string, Fld> Aliases,
        bool BlankDateIsToday,
        bool PackageMultiple);

    private static int MaxLength(Fld f) =>
        f is Fld.DocNo or Fld.BranchNo or Fld.InvoiceNo ? 30 : MaxCodeLength;

    private static Dictionary<string, Fld> Aliases(params (string Key, Fld F)[] extra)
    {
        var d = new Dictionary<string, Fld>
        {
            ["itemcode"] = Fld.ItemCode,
            ["qty"] = Fld.Qty, ["planned"] = Fld.Qty, ["plannedqty"] = Fld.Qty,
            ["scheduleddate"] = Fld.ScheduledDate,
        };
        foreach (var (k, f) in extra) d[k] = f;
        return d;
    }

    private static readonly ImportDef Receiving = new(
        "receiving_schedule",
        ["po_no", "po_branch_no", "partner_code", "item_code", "qty", "factory_code", "scheduled_date"],
        ["PO-20260901-001", "01", "BP001", "PR-IND4-01", "10", "FAC-01", "2026-09-30"],
        Aliases(("pono", Fld.DocNo), ("pobranchno", Fld.BranchNo), ("serialno", Fld.BranchNo),
                ("partnercode", Fld.PartnerCode), ("supplier", Fld.PartnerCode), ("suppliercode", Fld.PartnerCode),
                ("factorycode", Fld.FactoryCode), ("factory", Fld.FactoryCode)),
        BlankDateIsToday: true, PackageMultiple: true);

    private static readonly ImportDef Shipping = new(
        "shipping_schedule",
        ["so_no", "so_branch_no", "partner_code", "item_code", "qty", "factory_code", "scheduled_date",
         "gate_code", "invoice_no"],
        ["SO-20260901-001", "01", "BP001", "PR-IND4-01", "10", "FAC-01", "2026-09-30",
         "G1", "INV-20260901-001"],
        Aliases(("sono", Fld.DocNo), ("slipno", Fld.DocNo), ("sobranchno", Fld.BranchNo), ("serialno", Fld.BranchNo),
                ("partnercode", Fld.PartnerCode), ("customer", Fld.PartnerCode), ("customercode", Fld.PartnerCode),
                ("factorycode", Fld.FactoryCode), ("factory", Fld.FactoryCode),
                ("gatecode", Fld.GateCode), ("gate", Fld.GateCode), ("invoiceno", Fld.InvoiceNo)),
        BlankDateIsToday: true, PackageMultiple: false);

    private static readonly ImportDef Returns = new(
        "returns_schedule",
        ["return_no", "return_branch_no", "partner_code", "item_code", "qty", "factory_code", "scheduled_date",
         "invoice_no", "lot_no"],
        ["RTN-20260901-001", "01", "BP001", "PR-IND4-01", "2", "FAC-01", "2026-09-30",
         "INV-20260901-001", "LOT260901"],
        Aliases(("returnno", Fld.DocNo), ("returnbranchno", Fld.BranchNo), ("serialno", Fld.BranchNo),
                ("partnercode", Fld.PartnerCode), ("returnedby", Fld.PartnerCode), ("returnedbycode", Fld.PartnerCode),
                ("factorycode", Fld.FactoryCode), ("factory", Fld.FactoryCode),
                ("expectedqty", Fld.Qty), ("returndate", Fld.ScheduledDate),
                ("invoiceno", Fld.InvoiceNo), ("lotno", Fld.LotNo), ("lot", Fld.LotNo)),
        BlankDateIsToday: true, PackageMultiple: false);

    private static readonly ImportDef Picking = new(
        "picking_schedule",
        ["so_no", "so_branch_no", "item_code", "location_code", "qty", "scheduled_date"],
        ["SO-20260901-001", "01", "PR-IND4-01", "X01_Y01_Z01", "5", "2026-09-30"],
        Aliases(("sono", Fld.DocNo), ("slipno", Fld.DocNo), ("sobranchno", Fld.BranchNo), ("serialno", Fld.BranchNo),
                ("locationcode", Fld.LocationCode), ("location", Fld.LocationCode)),
        BlankDateIsToday: false, PackageMultiple: false);

    internal static readonly string[] TemplateHeader = Receiving.Header;


    public async Task<ServiceResult> ImportReceivingScheduleAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct)
    {
        var (fail, rows) = await ParseAsync(file, Receiving, db, ct);
        if (fail is not null) return fail;
        var (userCode, now) = Who(user);

        db.ReceivingTemps.RemoveRange(
            await db.ReceivingTemps.Where(x => x.CreatedBy == userCode).ToListAsync(ct));
        db.ReceivingTemps.AddRange(rows.Select(r => new ReceivingTemp
        {
            PoNo = r.Val(Fld.DocNo), PoBranchNo = r.Val(Fld.BranchNo), PartnerCode = r.Val(Fld.PartnerCode),
            ItemCode = r.Val(Fld.ItemCode), Qty = r.Qty, FactoryCode = r.Val(Fld.FactoryCode),
            ScheduledDate = r.DateGiven ? r.Date : null, Msg = r.Msg,
            CreatedAt = now, CreatedBy = userCode,
        }));

        if (Clean(rows))
            db.ReceivingPlans.AddRange(rows.Select(r => new ReceivingPlan
            {
                PoNo = r.Val(Fld.DocNo)!,
                PoBranchNo = r.Val(Fld.BranchNo) ?? "01",
                PartnerCode = r.Val(Fld.PartnerCode),
                ItemCode = r.Val(Fld.ItemCode)!,
                Qty = r.Qty!.Value,
                ActualQty = 0,
                FactoryCode = r.Val(Fld.FactoryCode),
                ScheduledDate = r.Date,
                Status = ReceivingStatus.NotArrived,
                CreatedAt = now,
                CreatedBy = userCode,
            }));
        await db.SaveChangesAsync(ct);
        return Done(rows);
    }

    public async Task<ServiceResult> ImportShippingScheduleAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct)
    {
        var (fail, rows) = await ParseAsync(file, Shipping, db, ct);
        if (fail is not null) return fail;
        var (userCode, now) = Who(user);

        db.ShippingTemps.RemoveRange(
            await db.ShippingTemps.Where(x => x.CreatedBy == userCode).ToListAsync(ct));
        db.ShippingTemps.AddRange(rows.Select(r => new ShippingTemp
        {
            SoNo = r.Val(Fld.DocNo), SoBranchNo = r.Val(Fld.BranchNo), PartnerCode = r.Val(Fld.PartnerCode),
            ItemCode = r.Val(Fld.ItemCode), Qty = r.Qty, FactoryCode = r.Val(Fld.FactoryCode),
            ScheduledDate = r.DateGiven ? r.Date : null, Msg = r.Msg,
            CreatedAt = now, CreatedBy = userCode,
        }));

        if (Clean(rows))
            db.ShippingPlans.AddRange(rows.Select(r => new ShippingPlan
            {
                SoNo = r.Val(Fld.DocNo)!,
                SoBranchNo = r.Val(Fld.BranchNo) ?? "01",
                PartnerCode = r.Val(Fld.PartnerCode),
                ItemCode = r.Val(Fld.ItemCode)!,
                Qty = r.Qty!.Value,
                ActualQty = 0,
                FactoryCode = r.Val(Fld.FactoryCode),
                GateCode = r.Val(Fld.GateCode),
                InvoiceNo = r.Val(Fld.InvoiceNo),
                ScheduledDate = r.Date,
                Status = ShippingStatus.Pending,
                CreatedAt = now,
                CreatedBy = userCode,
            }));
        await db.SaveChangesAsync(ct);
        return Done(rows);
    }

    public async Task<ServiceResult> ImportReturnsScheduleAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct)
    {
        var (fail, rows) = await ParseAsync(file, Returns, db, ct);
        if (fail is not null) return fail;
        var (userCode, now) = Who(user);

        if (Clean(rows))
        {
            db.ReturnPlans.AddRange(rows.Select(r => new ReturnPlan
            {
                ReturnNo = r.Val(Fld.DocNo)!,
                ReturnBranchNo = r.Val(Fld.BranchNo) ?? "01",
                PartnerCode = r.Val(Fld.PartnerCode),
                ItemCode = r.Val(Fld.ItemCode)!,
                Qty = r.Qty!.Value,
                ActualQty = 0,
                FactoryCode = r.Val(Fld.FactoryCode),
                ScheduledDate = r.Date,
                Status = ReturnStatus.Pending,
                InvoiceNo = r.Val(Fld.InvoiceNo),
                LotNo = r.Val(Fld.LotNo),
                CreatedAt = now,
                CreatedBy = userCode,
            }));
            await db.SaveChangesAsync(ct);
        }
        return Done(rows);
    }

    public async Task<ServiceResult> ImportPickingScheduleAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct)
    {
        var (fail, rows) = await ParseAsync(file, Picking, db, ct);
        if (fail is not null) return fail;
        var (userCode, now) = Who(user);

        if (Clean(rows))
        {
            db.PickingPlans.AddRange(rows.Select(r => new PickingPlan
            {
                SoNo = r.Val(Fld.DocNo)!,
                SoBranchNo = r.Val(Fld.BranchNo) ?? "01",
                ItemCode = r.Val(Fld.ItemCode)!,
                LocationCode = r.Val(Fld.LocationCode),
                Qty = r.Qty!.Value,
                PickedQty = 0,
                ScheduledDate = r.Date,
                Status = PickingStatus.NotPicked,
                CreatedAt = now,
                CreatedBy = userCode,
            }));
            await db.SaveChangesAsync(ct);
        }
        return Done(rows);
    }

    public ServiceResult ReceivingTemplateAsync() => Template(Receiving);
    public ServiceResult ShippingTemplateAsync() => Template(Shipping);
    public ServiceResult ReturnsTemplateAsync() => Template(Returns);
    public ServiceResult PickingTemplateAsync() => Template(Picking);


    private sealed class ParsedRow
    {
        public required int RowNo { get; init; }
        public required Func<Fld, string> Get { get; init; }
        public decimal? Qty { get; init; }
        public DateOnly? Date { get; init; }
        public bool DateGiven { get; init; }
        public ApiErrorBody? Error { get; set; }
        public string? Detail { get; set; }

        public string? Val(Fld f)
        {
            var v = Get(f).Trim();
            return v.Length == 0 ? null : v;
        }

        public string? Msg => Error is null ? null : $"{RowNo}: [{Error.Code}] {Error.Message}";
    }

    private static bool Clean(List<ParsedRow> rows) => rows.All(r => r.Error is null);

    private static (string? User, DateTime Now) Who(ClaimsPrincipal user) =>
        (user.FindFirstValue(ClaimTypes.NameIdentifier), DateTime.Now);

    private static ServiceResult Done(List<ParsedRow> rows)
    {
        var errors = rows.Where(r => r.Error is not null)
            .Select(r => new ImportRowErrorDto(r.RowNo, r.Error!.Code, r.Error.Message, r.Detail))
            .ToList();
        return Outcome.Ok(new ApiSuccess<ImportResultDto>(
            new ImportResultDto(errors.Count == 0 ? rows.Count : 0, 0, errors)));
    }

    private static string Norm(string header) =>
        new(header.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static readonly Fld[] Required = [Fld.DocNo, Fld.ItemCode, Fld.Qty];

    private static async Task<(ServiceResult? Fail, List<ParsedRow> Rows)> ParseAsync(
        IFormFile? file, ImportDef def, ImsDbContext db, CancellationToken ct)
    {
        if (file is null || !SpreadsheetReader.IsSupported(file.FileName))
            return (Outcome.BadRequest(new ApiError(ErrorCatalog.Imp001())), []);

        SpreadsheetReader.Sheet sheet;
        try
        {
            await using var stream = file.OpenReadStream();
            sheet = SpreadsheetReader.Read(stream, file.FileName);
        }
        catch (Exception)
        {
            return (Outcome.BadRequest(new ApiError(ErrorCatalog.Imp001())), []);
        }

        var at = new Dictionary<Fld, int>();
        for (var i = 0; i < sheet.Header.Count; i++)
            if (def.Aliases.TryGetValue(Norm(sheet.Header[i]), out var f))
                at.TryAdd(f, i);

        if (Required.Any(c => !at.ContainsKey(c)))
            return (Outcome.BadRequest(new ApiError(ErrorCatalog.Imp002())), []);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var rows = new List<ParsedRow>();
        foreach (var sr in sheet.Rows.Where(x => !x.IsBlank))
        {
            var src = sr;
            string Get(Fld f) => at.TryGetValue(f, out var i) ? src.Cell(i) : "";
            var qtyText = Get(Fld.Qty);
            var dateText = Get(Fld.ScheduledDate);
            rows.Add(new ParsedRow
            {
                RowNo = src.RowNo,
                Get = Get,
                Qty = decimal.TryParse(qtyText, NumberStyles.Any, CultureInfo.InvariantCulture, out var q)
                    ? q : null,
                DateGiven = dateText.Length > 0,
                Date = dateText.Length == 0
                    ? (def.BlankDateIsToday ? today : null)
                    : DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                        ? d : null,
            });
        }

        List<string> Codes(Fld f) => rows.Select(x => x.Val(f)).OfType<string>().Distinct().ToList();

        var itemCodes = Codes(Fld.ItemCode);
        var items = await db.Items.AsNoTracking()
            .Where(i => itemCodes.Contains(i.ItemCode) && !i.IsDeleted)
            .Select(i => new { i.ItemCode, i.PackageQty })
            .ToDictionaryAsync(i => i.ItemCode, i => i.PackageQty, ct);
        var partnerCodes = Codes(Fld.PartnerCode);
        var partners = (await db.Partners.AsNoTracking()
            .Where(p => partnerCodes.Contains(p.PartnerCode) && !p.IsDeleted)
            .Select(p => p.PartnerCode).ToListAsync(ct)).ToHashSet();
        var factoryCodes = Codes(Fld.FactoryCode);
        var factories = (await db.Factories.AsNoTracking()
            .Where(f => factoryCodes.Contains(f.FactoryCode) && !f.IsDeleted)
            .Select(f => f.FactoryCode).ToListAsync(ct)).ToHashSet();
        var locationCodes = Codes(Fld.LocationCode);
        var locations = (await db.Locations.AsNoTracking()
            .Where(l => locationCodes.Contains(l.LocationCode) && !l.IsDeleted)
            .Select(l => l.LocationCode).ToListAsync(ct)).ToHashSet();

        foreach (var r in rows)
        {
            var itemCode = r.Val(Fld.ItemCode) ?? "";
            var partner = r.Val(Fld.PartnerCode);
            var factory = r.Val(Fld.FactoryCode);
            var location = r.Val(Fld.LocationCode);

            ApiErrorBody? Optional(Fld f) =>
                r.Val(f) is { } v && ValidateCode(v, MaxLength(f)) is { } e ? Body(e) : null;

            var (err, detail) =
                ValidateCode(r.Get(Fld.DocNo), MaxLength(Fld.DocNo)) is { } e1 ? (Body(e1), (string?)null)
                : Optional(Fld.BranchNo) is { } e2 ? (e2, null)
                : ValidateCode(r.Get(Fld.ItemCode)) is { } e3 ? (Body(e3), null)
                : r.Qty is null or < 1 ? (ErrorCatalog.Qty001(), null)
                : r.DateGiven && r.Date is null ? (ErrorCatalog.Val005(), null)
                : Optional(Fld.GateCode) is { } e4 ? (e4, null)
                : Optional(Fld.InvoiceNo) is { } e5 ? (e5, null)
                : Optional(Fld.LotNo) is { } e6 ? (e6, null)
                : !items.TryGetValue(itemCode, out var packageQty) ? (ErrorCatalog.NotFound(itemCode), itemCode)
                : def.PackageMultiple && packageQty > 0 && r.Qty.Value % packageQty != 0
                    ? (ErrorCatalog.Qty001(), null)
                : partner is not null && !partners.Contains(partner) ? (ErrorCatalog.NotFound(partner), partner)
                : factory is not null && !factories.Contains(factory) ? (ErrorCatalog.NotFound(factory), factory)
                : location is not null && !locations.Contains(location) ? (ErrorCatalog.NotFound(location), location)
                : ((ApiErrorBody?)null, null);
            r.Error = err;
            r.Detail = detail;
        }
        return (null, rows);
    }

    private static ServiceResult Template(ImportDef def)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(def.Name);
        for (var c = 0; c < def.Header.Length; c++)
        {
            ws.Column(c + 1).Style.NumberFormat.Format = "@";
            ws.Cell(1, c + 1).Value = def.Header[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(2, c + 1).Value = def.Sample[c];
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Outcome.File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            def.Name + "_template.xlsx");
    }

    private static ApiErrorBody Body(ServiceResult r) =>
        ((ApiError)r.Body!).Error;
}
