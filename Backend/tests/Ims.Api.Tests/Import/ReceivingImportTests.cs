using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Import;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Ims.Api.Tests.Import;

public class ReceivingImportTests
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
            PartnerCode = "BP001", PartnerName = "Supplier One",
            IsActive = true, CreatedAt = now,
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

    private static IFormFile Csv(string text) =>
        File(Encoding.UTF8.GetBytes(text), "plans.csv");

    private static readonly string[] Head =
        ["po_no", "po_branch_no", "partner_code", "item_code", "qty", "factory_code", "scheduled_date"];

    private static string[] Row(
        string po = "PO-1", string branch = "01", string partner = "BP001",
        string item = "ITM-A", string qty = "10", string factory = "FAC-01",
        string date = "2026-09-20") => [po, branch, partner, item, qty, factory, date];

    private static async Task<ImportResultDto> Import(TestDb t, IFormFile f)
    {
        var res = await new ImportService(t.Db).ImportReceivingScheduleAsync(f, User(), default);
        return ResultAssert.Ok<ApiSuccess<ImportResultDto>>(res).Data!;
    }


    [Fact]
    public async Task A_file_that_is_not_a_spreadsheet_is_refused()
    {
        using var t = Warehouse();
        var res = await new ImportService(t.Db).ImportReceivingScheduleAsync(File(Encoding.UTF8.GetBytes("po_no\nPO-1"), "plans.txt"), User(), default);
        ResultAssert.BadRequest(res, "IMP-001");
    }

    [Fact]
    public async Task No_file_at_all_is_refused_the_same_way()
    {
        using var t = Warehouse();
        var res = await new ImportService(t.Db).ImportReceivingScheduleAsync(null, User(), default);
        ResultAssert.BadRequest(res, "IMP-001");
    }

    [Fact]
    public async Task Both_xlsx_and_csv_are_accepted()
    {
        using var t = Warehouse();
        Assert.Equal(1, (await Import(t, Xlsx(Head, Row(po: "PO-X")))).ImportedCount);
        Assert.Equal(1, (await Import(t, Csv(
            "po_no,po_branch_no,partner_code,item_code,qty,factory_code,scheduled_date\n" +
            "PO-C,01,BP001,ITM-A,10,FAC-01,2026-09-20"))).ImportedCount);

        Assert.Equal(2, await t.Db.ReceivingPlans.CountAsync());
    }

    [Fact]
    public async Task Headers_that_belong_to_another_screen_are_refused()
    {
        using var t = Warehouse();
        var res = await new ImportService(t.Db).ImportReceivingScheduleAsync(Xlsx(["zone_code", "zone_name"], ["Z1", "Zone 1"]), User(), default);
        ResultAssert.BadRequest(res, "IMP-002");
    }

    [Fact]
    public async Task A_missing_required_column_is_a_header_problem_not_a_row_problem()
    {
        using var t = Warehouse();
        var res = await new ImportService(t.Db).ImportReceivingScheduleAsync(Xlsx(["po_no", "item_code"], ["PO-1", "ITM-A"]), User(), default);
        ResultAssert.BadRequest(res, "IMP-002");
    }

    [Fact]
    public async Task The_grid_export_headers_are_accepted_as_aliases()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(
            ["PO No", "Serial No", "Supplier", "Item Code", "Planned", "Scheduled Date"],
            ["PO-G", "02", "BP001", "ITM-A", "10", "2026-09-20"]));

        Assert.Equal(1, r.ImportedCount);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal("PO-G", plan.PoNo);
        Assert.Equal("02", plan.PoBranchNo);
        Assert.Equal("BP001", plan.PartnerCode);
    }

    [Fact]
    public async Task Columns_the_import_does_not_use_are_ignored()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(
            ["po_no", "item_code", "qty", "item_name", "status", "actual_qty"],
            ["PO-E", "ITM-A", "10", "Item A", "statusArrived", "7"]));

        Assert.Equal(1, r.ImportedCount);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal(0m, plan.ActualQty);
        Assert.Equal(Ims.Domain.Receiving.ReceivingStatus.NotArrived, plan.Status);
    }


    [Fact]
    public async Task One_bad_row_stops_the_whole_file()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head,
            Row(po: "PO-1"),
            Row(po: "PO-2"),
            Row(po: "PO-3", item: "ITM-NOPE")));

        Assert.Equal(0, r.ImportedCount);
        Assert.Equal(0, r.SkippedCount);
        Assert.Empty(await t.Db.ReceivingPlans.ToListAsync());

        var e = Assert.Single(r.Errors);
        Assert.Equal(4, e.Row);
        Assert.Equal("SYS-404", e.Code);
    }

    [Fact]
    public async Task Every_bad_row_is_reported_not_just_the_first()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head,
            Row(po: ""),
            Row(po: "PO-2"),
            Row(po: "PO-3", qty: "0")));

        Assert.Equal([2, 4], r.Errors.Select(x => x.Row));
        Assert.Equal(["VAL-001", "QTY-001"], r.Errors.Select(x => x.Code));
    }

    [Fact]
    public async Task A_blank_row_is_skipped_not_counted_as_an_error()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head,
            Row(po: "PO-1"),
            ["", "", "", "", "", "", ""],
            Row(po: "PO-2")));

        Assert.Empty(r.Errors);
        Assert.Equal(2, r.ImportedCount);
    }


    [Fact]
    public async Task A_qty_that_is_not_a_whole_number_of_packages_is_refused()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head, Row(item: "ITM-P", qty: "12")));

        Assert.Equal("QTY-001", Assert.Single(r.Errors).Code);
        Assert.Equal(0, r.ImportedCount);
    }

    [Fact]
    public async Task A_whole_number_of_packages_goes_through()
    {
        using var t = Warehouse();
        Assert.Equal(1, (await Import(t, Xlsx(Head, Row(item: "ITM-P", qty: "15")))).ImportedCount);
    }

    [Fact]
    public async Task A_qty_that_is_not_a_number_is_refused()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head, Row(qty: "mười")));
        Assert.Equal("QTY-001", Assert.Single(r.Errors).Code);
    }

    [Fact]
    public async Task Forbidden_characters_in_the_po_no_are_refused()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head, Row(po: "PO/1")));
        Assert.Equal("VAL-005", Assert.Single(r.Errors).Code);
    }

    [Fact]
    public async Task A_not_found_row_names_what_was_not_found()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head, Row(item: "ITM-NOPE")));

        var e = Assert.Single(r.Errors);
        Assert.Equal("SYS-404", e.Code);
        Assert.Equal("ITM-NOPE", e.Detail);
    }

    [Fact]
    public async Task An_unknown_partner_is_refused()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head, Row(partner: "BP-NOPE")));
        Assert.Equal("SYS-404", Assert.Single(r.Errors).Code);
    }

    [Fact]
    public async Task An_unknown_factory_is_refused()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head, Row(factory: "FAC-NOPE")));
        Assert.Equal("SYS-404", Assert.Single(r.Errors).Code);
    }

    [Fact]
    public async Task A_soft_deleted_item_is_not_a_valid_item()
    {
        using var t = Warehouse();
        (await t.Db.Items.SingleAsync(i => i.ItemCode == "ITM-A")).IsDeleted = true;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var r = await Import(t, Xlsx(Head, Row()));
        Assert.Equal("SYS-404", Assert.Single(r.Errors).Code);
    }


    [Fact]
    public async Task Serial_no_defaults_to_01_and_the_date_to_today()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(
            ["po_no", "item_code", "qty"], ["PO-D", "ITM-A", "10"]));

        Assert.Equal(1, r.ImportedCount);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal("01", plan.PoBranchNo);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), plan.ScheduledDate);
        Assert.Null(plan.PartnerCode);
    }

    [Fact]
    public async Task A_date_that_is_not_a_date_is_refused()
    {
        using var t = Warehouse();
        var r = await Import(t, Xlsx(Head, Row(date: "20 tháng 9")));
        Assert.Equal("VAL-005", Assert.Single(r.Errors).Code);
    }

    [Fact]
    public async Task A_real_excel_date_cell_is_read_as_a_date()
    {
        using var t = Warehouse();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "po_no"; ws.Cell(1, 2).Value = "item_code";
        ws.Cell(1, 3).Value = "qty"; ws.Cell(1, 4).Value = "scheduled_date";
        ws.Cell(2, 1).Value = "PO-DT"; ws.Cell(2, 2).Value = "ITM-A";
        ws.Cell(2, 3).Value = 10;
        ws.Cell(2, 4).Value = new DateTime(2026, 9, 20);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var r = await Import(t, File(ms.ToArray(), "plans.xlsx"));
        Assert.Empty(r.Errors);
        Assert.Equal(new DateOnly(2026, 9, 20), (await t.Db.ReceivingPlans.SingleAsync()).ScheduledDate);
    }


    [Fact]
    public async Task The_staging_table_keeps_the_reason_each_row_failed()
    {
        using var t = Warehouse();
        await Import(t, Xlsx(Head, Row(po: "PO-1"), Row(po: "PO-2", item: "ITM-NOPE")));

        var rows = await t.Db.ReceivingTemps.AsNoTracking().OrderBy(x => x.No).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Null(rows[0].Msg);
        Assert.Contains("SYS-404", rows[1].Msg);
        Assert.Contains("3", rows[1].Msg);
        Assert.Equal("ITM-NOPE", rows[1].ItemCode);
    }

    [Fact]
    public async Task A_second_import_replaces_that_users_staging_rows()
    {
        using var t = Warehouse();
        await Import(t, Xlsx(Head, Row(po: "PO-1"), Row(po: "PO-2")));
        await Import(t, Xlsx(Head, Row(po: "PO-3")));

        var rows = await t.Db.ReceivingTemps.AsNoTracking().ToListAsync();
        Assert.Equal("PO-3", Assert.Single(rows).PoNo);
    }

    [Fact]
    public async Task One_users_import_does_not_wipe_anothers()
    {
        using var t = Warehouse();
        await new ImportService(t.Db).ImportReceivingScheduleAsync(Xlsx(Head, Row(po: "PO-U1")), User("U001"), default);
        await new ImportService(t.Db).ImportReceivingScheduleAsync(Xlsx(Head, Row(po: "PO-U3")), User("U003"), default);

        var rows = await t.Db.ReceivingTemps.AsNoTracking().ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, x => x.CreatedBy == "U001");
        Assert.Contains(rows, x => x.CreatedBy == "U003");
    }

    [Fact]
    public async Task The_imported_rows_carry_the_importing_user()
    {
        using var t = Warehouse();
        await new ImportService(t.Db).ImportReceivingScheduleAsync(Xlsx(Head, Row(po: "PO-W")), User("U003"), default);

        Assert.Equal("U003", (await t.Db.ReceivingPlans.SingleAsync()).CreatedBy);
    }


    [Fact]
    public void The_template_carries_the_seven_columns_in_order()
    {
        var file = Assert.IsType<Ims.Api.Contracts.ServiceFile>(
            new ImportService(null!).ReceivingTemplateAsync().File);

        using var wb = new XLWorkbook(new MemoryStream(file.Contents));
        var ws = wb.Worksheets.First();
        for (var c = 0; c < Head.Length; c++)
            Assert.Equal(Head[c], ws.Cell(1, c + 1).GetString());
    }
}
