using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Import;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Picking;
using Ims.Domain.Returns;
using Ims.Domain.Shipping;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Ims.Api.Tests.Import;

public class ScheduleImportTests
{
    private static ClaimsPrincipal User(string code = "admin") =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, code)], "test"));

    private static TestDb Warehouse()
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "ITM-A", ItemName = "Item A", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackageQty = 1, IsActive = true, CreatedAt = now,
        });
        t.Db.Items.Add(new Item
        {
            ItemCode = "ITM-P", ItemName = "Packed Item", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackageQty = 5, IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Customer One", IsActive = true, CreatedAt = now,
        });
        t.Db.SaveChanges();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static IFormFile File(byte[] bytes, string name)
    {
        var ms = new MemoryStream(bytes);
        return new FormFile(ms, 0, ms.Length, "file", name);
    }

    private static IFormFile Xlsx(string[] header, params string[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        for (var c = 0; c < header.Length; c++) ws.Cell(1, c + 1).Value = header[c];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c];
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "plans.xlsx");
    }

    private static ImportResultDto Ok(ServiceResult res) =>
        ResultAssert.Ok<ApiSuccess<ImportResultDto>>(res).Data!;

    private static string[] TemplateHeader(ServiceResult r)
    {
        var file = Assert.IsType<Ims.Api.Contracts.ServiceFile>(r.File);
        using var wb = new XLWorkbook(new MemoryStream(file.Contents));
        var ws = wb.Worksheets.First();
        return ws.Row(1).CellsUsed().Select(c => c.GetString()).ToArray();
    }


    private static readonly string[] ShpHead =
        ["so_no", "so_branch_no", "partner_code", "item_code", "qty", "factory_code",
         "scheduled_date", "gate_code", "invoice_no"];

    private static string[] ShpRow(
        string so = "SO-1", string item = "ITM-A", string qty = "10", string partner = "BP001") =>
        [so, "02", partner, item, qty, "FAC-01", "2026-09-20", "G1", "INV-1"];

    private static Task<ServiceResult> ImportShp(TestDb t, IFormFile f, string user = "admin") =>
        new ImportService(t.Db).ImportShippingScheduleAsync(f, User(user), default);

    [Fact]
    public async Task Shipping_imports_every_column_of_the_template()
    {
        using var t = Warehouse();
        var r = Ok(await ImportShp(t, Xlsx(ShpHead, ShpRow())));

        Assert.Equal(1, r.ImportedCount);
        var p = await t.Db.ShippingPlans.SingleAsync();
        Assert.Equal(("SO-1", "02", "BP001", "ITM-A"), (p.SoNo, p.SoBranchNo, p.PartnerCode, p.ItemCode));
        Assert.Equal(10m, p.Qty);
        Assert.Equal(0m, p.ActualQty);
        Assert.Equal("FAC-01", p.FactoryCode);
        Assert.Equal(new DateOnly(2026, 9, 20), p.ScheduledDate);
        Assert.Equal(("G1", "INV-1"), (p.GateCode, p.InvoiceNo));
        Assert.Equal(ShippingStatus.Pending, p.Status);
        Assert.Equal("admin", p.CreatedBy);
    }

    [Fact]
    public async Task Shipping_one_bad_row_stops_the_whole_file_and_names_the_row()
    {
        using var t = Warehouse();
        var r = Ok(await ImportShp(t, Xlsx(ShpHead, ShpRow(), ShpRow(item: "ITM-NOPE"))));

        Assert.Equal(0, r.ImportedCount);
        var e = Assert.Single(r.Errors);
        Assert.Equal((3, "SYS-404", "ITM-NOPE"), (e.Row, e.Code, e.Detail));
        Assert.Empty(await t.Db.ShippingPlans.ToListAsync());
    }

    [Fact]
    public async Task Shipping_goes_through_its_staging_table_per_user()
    {
        using var t = Warehouse();
        await ImportShp(t, Xlsx(ShpHead, ShpRow(so: "SO-1"), ShpRow(so: "SO-2", partner: "BP-NOPE")), "U001");
        await ImportShp(t, Xlsx(ShpHead, ShpRow(so: "SO-9")), "U003");

        var rows = await t.Db.ShippingTemps.AsNoTracking().OrderBy(x => x.No).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.Null(rows[0].Msg);
        Assert.Contains("SYS-404", rows[1].Msg);
        Assert.Equal("BP-NOPE", rows[1].PartnerCode);

        await ImportShp(t, Xlsx(ShpHead, ShpRow(so: "SO-5")), "U001");
        var after = await t.Db.ShippingTemps.AsNoTracking().ToListAsync();
        Assert.Equal(["SO-5", "SO-9"], after.Select(x => x.SoNo).OrderBy(x => x));
    }

    [Fact]
    public async Task Shipping_does_not_apply_the_receiving_package_rule()
    {
        using var t = Warehouse();
        Assert.Empty(Ok(await ImportShp(t, Xlsx(ShpHead, ShpRow(item: "ITM-P", qty: "12")))).Errors);
    }

    [Fact]
    public async Task Shipping_accepts_its_own_export_headers()
    {
        using var t = Warehouse();
        var r = Ok(await ImportShp(t, Xlsx(
            ["so_no", "so_branch_no", "partner_code", "partner_name", "item_code", "qty", "actual_qty", "status"],
            ["SO-E", "01", "BP001", "Customer One", "ITM-A", "4", "4", "statusShipped"])));

        Assert.Equal(1, r.ImportedCount);
        var p = await t.Db.ShippingPlans.SingleAsync();
        Assert.Equal(0m, p.ActualQty);
        Assert.Equal(ShippingStatus.Pending, p.Status);
    }

    [Fact]
    public async Task Shipping_refuses_a_too_long_invoice()
    {
        using var t = Warehouse();
        var row = ShpRow(); row[8] = new string('I', 31);
        Assert.Equal("VAL-005", Assert.Single(Ok(await ImportShp(t, Xlsx(ShpHead, row))).Errors).Code);
    }

    [Fact]
    public void Shipping_template_carries_its_columns()
    {
        Assert.Equal(ShpHead, TemplateHeader(new ImportService(null!).ShippingTemplateAsync()));
    }

    [Fact]
    public async Task Shipping_refuses_a_non_spreadsheet_and_a_foreign_header()
    {
        using var t = Warehouse();
        ResultAssert.BadRequest(await ImportShp(t, File(Encoding.UTF8.GetBytes("x"), "a.txt")), "IMP-001");
        ResultAssert.BadRequest(await ImportShp(t, Xlsx(["po_no", "item_code", "qty"], ["PO-1", "ITM-A", "1"])), "IMP-002");
    }


    private static readonly string[] RtnHead =
        ["return_no", "return_branch_no", "partner_code", "item_code", "qty", "factory_code",
         "scheduled_date", "invoice_no", "lot_no"];

    private static string[] RtnRow(string no = "RTN-1", string item = "ITM-A", string qty = "2", string lot = "LOT-1") =>
        [no, "01", "BP001", item, qty, "FAC-01", "2026-09-21", "INV-9", lot];

    private static Task<ServiceResult> ImportRtn(TestDb t, IFormFile f) =>
        new ImportService(t.Db).ImportReturnsScheduleAsync(f, User(), default);

    [Fact]
    public async Task Returns_imports_every_column_including_lot_and_invoice()
    {
        using var t = Warehouse();
        var r = Ok(await ImportRtn(t, Xlsx(RtnHead, RtnRow())));

        Assert.Equal(1, r.ImportedCount);
        var p = await t.Db.ReturnPlans.SingleAsync();
        Assert.Equal(("RTN-1", "01", "BP001", "ITM-A"), (p.ReturnNo, p.ReturnBranchNo, p.PartnerCode, p.ItemCode));
        Assert.Equal((2m, 0m), (p.Qty, p.ActualQty));
        Assert.Equal(new DateOnly(2026, 9, 21), p.ScheduledDate);
        Assert.Equal(("INV-9", "LOT-1"), (p.InvoiceNo, p.LotNo));
        Assert.Equal(ReturnStatus.Pending, p.Status);
        Assert.Equal("admin", p.CreatedBy);
    }

    [Fact]
    public async Task Returns_all_or_nothing()
    {
        using var t = Warehouse();
        var r = Ok(await ImportRtn(t, Xlsx(RtnHead, RtnRow(), RtnRow(no: "RTN-2", qty: "0"))));

        Assert.Equal(0, r.ImportedCount);
        Assert.Equal((3, "QTY-001"), (Assert.Single(r.Errors).Row, r.Errors[0].Code));
        Assert.Empty(await t.Db.ReturnPlans.ToListAsync());
    }

    [Fact]
    public async Task Returns_lot_may_be_blank_and_is_stored_as_null()
    {
        using var t = Warehouse();
        Ok(await ImportRtn(t, Xlsx(RtnHead, RtnRow(lot: ""))));
        Assert.Null((await t.Db.ReturnPlans.SingleAsync()).LotNo);
    }

    [Fact]
    public async Task Returns_forbidden_characters_in_the_lot_are_refused()
    {
        using var t = Warehouse();
        Assert.Equal("VAL-005", Assert.Single(Ok(await ImportRtn(t, Xlsx(RtnHead, RtnRow(lot: "L/1")))).Errors).Code);
    }

    [Fact]
    public void Returns_template_carries_its_columns()
    {
        Assert.Equal(RtnHead, TemplateHeader(new ImportService(null!).ReturnsTemplateAsync()));
    }


    private static readonly string[] PckHead =
        ["so_no", "so_branch_no", "item_code", "location_code", "qty", "scheduled_date"];

    private static string[] PckRow(string so = "SO-P1", string loc = "W1_ZA", string date = "2026-09-22") =>
        [so, "01", "ITM-A", loc, "3", date];

    private static Task<ServiceResult> ImportPck(TestDb t, IFormFile f) =>
        new ImportService(t.Db).ImportPickingScheduleAsync(f, User(), default);

    [Fact]
    public async Task Picking_imports_every_column_including_the_location()
    {
        using var t = Warehouse();
        var r = Ok(await ImportPck(t, Xlsx(PckHead, PckRow())));

        Assert.Equal(1, r.ImportedCount);
        var p = await t.Db.PickingPlans.SingleAsync();
        Assert.Equal(("SO-P1", "01", "ITM-A", "W1_ZA"), (p.SoNo, p.SoBranchNo, p.ItemCode, p.LocationCode));
        Assert.Equal((3m, 0m), (p.Qty, p.PickedQty));
        Assert.Equal(new DateOnly(2026, 9, 22), p.ScheduledDate);
        Assert.Equal(PickingStatus.NotPicked, p.Status);
        Assert.Equal("admin", p.CreatedBy);
    }

    [Fact]
    public async Task Picking_an_unknown_location_is_named()
    {
        using var t = Warehouse();
        var r = Ok(await ImportPck(t, Xlsx(PckHead, PckRow(loc: "NOWHERE"))));

        var e = Assert.Single(r.Errors);
        Assert.Equal(("SYS-404", "NOWHERE"), (e.Code, e.Detail));
        Assert.Empty(await t.Db.PickingPlans.ToListAsync());
    }

    [Fact]
    public async Task Picking_a_blank_date_stays_blank_like_the_new_screen()
    {
        using var t = Warehouse();
        Ok(await ImportPck(t, Xlsx(PckHead, PckRow(date: ""))));
        Assert.Null((await t.Db.PickingPlans.SingleAsync()).ScheduledDate);
    }

    [Fact]
    public async Task Picking_has_no_partner_or_factory_so_those_headers_are_ignored()
    {
        using var t = Warehouse();
        var r = Ok(await ImportPck(t, Xlsx(
            ["so_no", "item_code", "qty", "partner_code"], ["SO-X", "ITM-A", "1", "BP-NOPE"])));
        Assert.Equal(1, r.ImportedCount);
    }

    [Fact]
    public void Picking_template_carries_its_columns()
    {
        Assert.Equal(PckHead, TemplateHeader(new ImportService(null!).PickingTemplateAsync()));
    }
}
