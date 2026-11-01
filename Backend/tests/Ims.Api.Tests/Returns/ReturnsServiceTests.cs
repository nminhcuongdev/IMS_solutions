using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Returns;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Returns;
using Xunit;

namespace Ims.Api.Tests.Returns;

public class ReturnsServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static ClaimsPrincipal Worker(string code = "U001") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    private static async Task SeedRefsAsync(TestDb t)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Industrial Printer 4in", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackingTypeCode = "BOX", PackageQty = 10, DefaultLocationCode = "W1_ZA",
            IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "DEF Logistics Co.",
            IsCustomer = true, IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP-DEL", PartnerName = "Gone",
            IsCustomer = true, IsActive = true, IsDeleted = true, CreatedAt = now,
        });
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-SHORT", ReasonName = "Damaged in transit",
            ReasonCategoryValue = ReasonCategory.Shortage, IsActive = true, CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
    }

    private static ReturnPlan Plan(
        string returnNo = "RTN-001", string branch = "01", decimal qty = 5,
        int status = ReturnStatus.Pending, DateOnly? date = null, string? lot = "LOT-A") =>
        new()
        {
            ReturnNo = returnNo, ReturnBranchNo = branch, PartnerCode = "BP001",
            ItemCode = "IT-01", Qty = qty, ActualQty = 0, FactoryCode = "FAC-01",
            ScheduledDate = date ?? Today, Status = status, InvoiceNo = "INV-R01",
            LotNo = lot, CreatedAt = DateTime.UtcNow, CreatedBy = "admin",
        };

    private static ReturnPlanWriteDto Valid(
        string returnNo = "RTN-NEW", string itemCode = "IT-01", decimal qty = 5,
        string? returnedBy = "BP001", string? serialNo = null) =>
        new(returnNo, serialNo, returnedBy, itemCode, qty, ReturnDate: null,
            InvoiceNo: "INV-NEW", FactoryCode: "FAC-01", LotNo: "LOT-A");

    private static ReturnResultWriteDto ValidResult(
        string itemCode = "IT-01", decimal? qty = 5, string? returnNo = "RTN-001",
        string? serialNo = "01", string? locationCode = "W1_ZA", string? lotNo = "LOT-A") =>
        new(itemCode, qty, returnNo, serialNo, locationCode, lotNo);


    [Fact]
    public async Task List_maps_db_columns_onto_the_api_field_names()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).ListPlansAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReturnPlanDto>>(res).Data);
        Assert.Equal("RTN-001", row.ReturnNo);
        Assert.Equal("01", row.SerialNo);
        Assert.Equal("BP001", row.ReturnedByCode);
        Assert.Equal("DEF Logistics Co.", row.ReturnedByName);
        Assert.Equal(5m, row.ExpectedQty);
        Assert.Equal(Today, row.ReturnDate);
        Assert.Equal("pcs", row.Unit);
        Assert.Equal("LOT-A", row.LotNo);
        Assert.False(row.LabelIssued);
    }

    [Theory]
    [InlineData(ReturnStatus.Pending, "statusPendingReturn")]
    [InlineData(ReturnStatus.Partial, "statusPartialReturn")]
    [InlineData(ReturnStatus.Returned, "statusReturned")]
    [InlineData(ReturnStatus.Cancelled, "statusCancelled")]
    public async Task List_uses_the_returns_specific_status_names(int status, string expected)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(status: status));
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).ListPlansAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReturnPlanDto>>(res).Data);
        Assert.Equal(expected, row.Status);
    }

    [Fact]
    public async Task List_filters_by_return_no_item_code_and_status()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(returnNo: "RTN-001"));
        t.Db.ReturnPlans.Add(Plan(returnNo: "RTN-002", status: ReturnStatus.Returned));
        await t.Db.SaveChangesAsync();

        var byNo = await new ReturnsService(t.Db).ListPlansAsync(default, returnNo: "002");
        var byStatus = await new ReturnsService(t.Db).ListPlansAsync(default, status: "statusReturned");
        var byItem = await new ReturnsService(t.Db).ListPlansAsync(default, itemCode: "IT-01");

        Assert.Equal("RTN-002",
            Assert.Single(ResultAssert.Ok<ApiListSuccess<ReturnPlanDto>>(byNo).Data).ReturnNo);
        Assert.Equal("RTN-002",
            Assert.Single(ResultAssert.Ok<ApiListSuccess<ReturnPlanDto>>(byStatus).Data).ReturnNo);
        Assert.Equal(2, ResultAssert.Ok<ApiListSuccess<ReturnPlanDto>>(byItem).Meta.Total);
    }

    [Fact]
    public async Task List_ignores_an_unknown_status_filter()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).ListPlansAsync(default, status: "statusKhongCo");

        Assert.Single(ResultAssert.Ok<ApiListSuccess<ReturnPlanDto>>(res).Data);
    }


    [Fact]
    public async Task Create_stores_the_plan_and_returns_it_in_grid_shape()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReturnsService(t.Db).CreatePlanAsync(Valid(), default);

        var dto = ResultAssert.Created<ApiSuccess<ReturnPlanDto>>(res).Data!;
        Assert.Equal("RTN-NEW", dto.ReturnNo);
        Assert.Equal("01", dto.SerialNo);
        Assert.Equal("statusPendingReturn", dto.Status);
        Assert.Equal(0m, dto.ActualQty);
        Assert.Equal("DEF Logistics Co.", dto.ReturnedByName);
    }

    [Fact]
    public async Task Create_requires_a_return_no()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReturnsService(t.Db).CreatePlanAsync(Valid(returnNo: "   "), default);

        ResultAssert.BadRequest(res, "VAL-001");
    }

    [Fact]
    public async Task Create_rejects_a_qty_below_one()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReturnsService(t.Db).CreatePlanAsync(Valid(qty: 0), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Fact]
    public async Task Create_rejects_an_unknown_item()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReturnsService(t.Db).CreatePlanAsync(Valid(itemCode: "IT-NOPE"), default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task Create_rejects_a_soft_deleted_partner()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReturnsService(t.Db).CreatePlanAsync(Valid(returnedBy: "BP-DEL"), default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task Create_allows_the_same_return_no_twice()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        await new ReturnsService(t.Db).CreatePlanAsync(Valid(serialNo: "01"), default);
        var res = await new ReturnsService(t.Db).CreatePlanAsync(Valid(serialNo: "02"), default);

        ResultAssert.Created<ApiSuccess<ReturnPlanDto>>(res);
        Assert.Equal(2, await t.Db.ReturnPlans.CountAsync());
    }


    [Fact]
    public async Task ChangeStatus_updates_every_line_of_the_listed_returns()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(branch: "01"));
        t.Db.ReturnPlans.Add(Plan(branch: "02"));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).ChangeStatusAsync(new ReturnStatusChangeDto(new[] { "RTN-001" }, "done"), default);

        var body = ResultAssert.Ok<ApiSuccess<ReturnStatusChangeResult>>(res).Data!;
        Assert.Equal(2, body.Updated);
        Assert.All(await t.Db.ReturnPlans.ToListAsync(),
            p => Assert.Equal(ReturnStatus.Returned, p.Status));
    }

    [Fact]
    public async Task ChangeStatus_requires_at_least_one_return_no()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReturnsService(t.Db).ChangeStatusAsync(new ReturnStatusChangeDto(Array.Empty<string>(), "done"), default);

        ResultAssert.BadRequest(res, "SEL-002");
    }

    [Fact]
    public async Task ChangeStatus_requires_a_known_action()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).ChangeStatusAsync(new ReturnStatusChangeDto(new[] { "RTN-001" }, "khongCo"), default);

        ResultAssert.BadRequest(res, "SEL-006");
    }

    [Fact]
    public async Task ChangeStatus_skips_returned_and_cancelled_lines()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(returnNo: "RTN-DONE", status: ReturnStatus.Returned));
        t.Db.ReturnPlans.Add(Plan(returnNo: "RTN-CANCEL", status: ReturnStatus.Cancelled));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).ChangeStatusAsync(new ReturnStatusChangeDto(new[] { "RTN-DONE", "RTN-CANCEL" }, "cancelled"), default);

        var body = ResultAssert.Ok<ApiSuccess<ReturnStatusChangeResult>>(res).Data!;
        Assert.Equal(0, body.Updated);
        Assert.Equal(new[] { "RTN-CANCEL", "RTN-DONE" }, body.Skipped.ToArray());
    }

    [Fact]
    public async Task ChangeStatus_reports_return_nos_that_match_nothing()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReturnsService(t.Db).ChangeStatusAsync(new ReturnStatusChangeDto(new[] { "RTN-KHONG-CO" }, "done"), default);

        var body = ResultAssert.Ok<ApiSuccess<ReturnStatusChangeResult>>(res).Data!;
        Assert.Equal(0, body.Updated);
        Assert.Equal("RTN-KHONG-CO", Assert.Single(body.Skipped));
    }


    [Fact]
    public async Task Shortage_subtracts_from_the_expected_qty_and_marks_partial()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(qty: 5));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).RegisterShortageAsync("RTN-001", new ReturnShortageDto(2, "RS-SHORT", "01"), default);

        var dto = ResultAssert.Ok<ApiSuccess<ReturnPlanDto>>(res).Data!;
        Assert.Equal(5m, dto.ExpectedQty);
        Assert.Equal(3m, dto.ActualQty);
        Assert.Equal("statusPartialReturn", dto.Status);
        Assert.Equal("RS-SHORT", dto.ShortageReason);
    }

    [Fact]
    public async Task Shortage_requires_a_reason()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).RegisterShortageAsync("RTN-001", new ReturnShortageDto(2, null, "01"), default);

        ResultAssert.BadRequest(res, "RSN-001");
    }

    [Fact]
    public async Task Shortage_rejects_more_than_the_expected_qty()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(qty: 5));
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).RegisterShortageAsync("RTN-001", new ReturnShortageDto(6, "RS-SHORT", "01"), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Fact]
    public async Task Shortage_refuses_to_guess_between_open_lines()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(branch: "01"));
        t.Db.ReturnPlans.Add(Plan(branch: "02"));
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).RegisterShortageAsync("RTN-001", new ReturnShortageDto(1, "RS-SHORT"), default);

        ResultAssert.NotFound(res);
    }


    [Fact]
    public async Task CreateResult_requires_a_location()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan());
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(locationCode: "   "), Worker(), default);

        ResultAssert.BadRequest(res, "LOC-001");
        Assert.Empty(t.Db.ReturnResults);
    }

    [Fact]
    public async Task CreateResult_rejects_an_unknown_location()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(locationCode: "W9_KHONG_CO"), Worker(), default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task CreateResult_requires_a_lot_no()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(lot: null));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(lotNo: null), Worker(), default);

        ResultAssert.BadRequest(res, "LOT-001");
    }

    [Fact]
    public async Task CreateResult_accepts_a_lot_that_no_other_document_knows()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(lot: "LOT-CHUA-TUNG-CO"));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(lotNo: "LOT-CHUA-TUNG-CO"), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(res);
    }

    [Fact]
    public async Task CreateResult_accepts_a_shortage_without_any_reason()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(qty: 5));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(qty: 3), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(res);
        var plan = await t.Db.ReturnPlans.FirstAsync();
        Assert.Equal(5m, plan.Qty);
        Assert.Equal(3m, plan.ActualQty);
        Assert.Equal(ReturnStatus.Partial, plan.Status);
        Assert.Null(plan.ReasonCode);
    }

    [Fact]
    public async Task CreateResult_writes_actual_qty_and_marks_the_line_returned()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(qty: 5));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(qty: 5), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(res);
        var plan = await t.Db.ReturnPlans.FirstAsync();
        Assert.Equal(5m, plan.ActualQty);
        Assert.Equal(ReturnStatus.Returned, plan.Status);
        Assert.Equal("U001", plan.UpdatedBy);
    }

    [Fact]
    public async Task CreateResult_only_touches_the_confirmed_line()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(branch: "01", qty: 5));
        t.Db.ReturnPlans.Add(Plan(branch: "02", qty: 3));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new ReturnsService(t.Db).CreateResultAsync(ValidResult(serialNo: "01", qty: 5), Worker(), default);

        var line2 = await t.Db.ReturnPlans.FirstAsync(p => p.ReturnBranchNo == "02");
        Assert.Equal(ReturnStatus.Pending, line2.Status);
        Assert.Equal(0m, line2.ActualQty);
    }

    [Fact]
    public async Task CreateResult_rejects_more_than_expected()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(qty: 5));
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(qty: 6), Worker(), default);

        ResultAssert.BadRequest(res, "QTY-002");
    }

    [Fact]
    public async Task CreateResult_rejects_a_negative_qty()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(qty: -1), Worker(), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Fact]
    public async Task CreateResult_refuses_an_already_returned_line()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(status: ReturnStatus.Returned));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(), Worker(), default);

        ResultAssert.NotFound(res);
        Assert.Empty(t.Db.ReturnResults);
    }

    [Fact]
    public async Task CreateResult_stores_partner_location_lot_and_worker()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.AppUsers.Add(new AppUser
        {
            UserCode = "U001", UserName = "Nguyen Van An", RoleCode = "Admin",
            PasswordHash = "x", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReturnPlans.Add(Plan(qty: 5));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReturnsService(t.Db).CreateResultAsync(ValidResult(qty: 5), Worker("U001"), default);

        var dto = ResultAssert.Created<ApiSuccess<ReturnResultDto>>(res).Data!;
        Assert.Equal("RTN-001", dto.ReturnNo);
        Assert.Equal("01", dto.SerialNo);
        Assert.Equal("BP001", dto.ReturnedByCode);
        Assert.Equal("W1_ZA", dto.LocationCode);
        Assert.Equal("LOT-A", dto.LotNo);
        Assert.Equal("Nguyen Van An", dto.Worker);
        Assert.Equal("pcs", dto.Unit);
        Assert.Equal("statusReturned", dto.Status);
    }

    [Fact]
    public async Task CreateResult_row_shows_up_on_the_result_grid()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(qty: 5));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new ReturnsService(t.Db).CreateResultAsync(ValidResult(qty: 5), Worker(), default);
        var list = await new ReturnsService(t.Db).ListResultsAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReturnResultDto>>(list).Data);
        Assert.Equal("RTN-001", row.ReturnNo);
        Assert.Equal(5m, row.Qty);
    }


    [Fact]
    public async Task ExportCsv_writes_physical_column_names_and_escapes_commas()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP-C", PartnerName = "Alpha, Beta & Co.",
            IsCustomer = true, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        var plan = Plan();
        plan.PartnerCode = "BP-C";
        t.Db.ReturnPlans.Add(plan);
        await t.Db.SaveChangesAsync();

        var csv = await CsvReader.ReadAsync(await new ReturnsService(t.Db).ExportPlansCsvAsync(default));

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("return_no,return_branch_no,partner_code,partner_name", lines[0]);
        Assert.Contains("\"Alpha, Beta & Co.\"", lines[1]);
        Assert.Contains("statusPendingReturn", lines[1]);
    }

    [Fact]
    public async Task CreatePlan_rejects_the_same_return_and_branch_twice_even_padded()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        ResultAssert.Created<ApiSuccess<ReturnPlanDto>>(await new ReturnsService(t.Db).CreatePlanAsync(Valid(), default));
        t.Db.ChangeTracker.Clear();
        var first = Valid();

        var dup = await new ReturnsService(t.Db).CreatePlanAsync(first with { ReturnNo = " " + first.ReturnNo + " ", SerialNo = " " + (first.SerialNo ?? "01") + " " }, default);

        ResultAssert.Conflict(dup);
        Assert.Equal(1, await t.Db.ReturnPlans.CountAsync());
    }

    [Fact]
    public async Task CreatePlan_records_the_signed_in_user_as_the_creator()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        ResultAssert.Created<ApiSuccess<ReturnPlanDto>>(
            await new ReturnsService(t.Db).CreatePlanAsync(Valid(), default, Worker("U009")));
        Assert.Equal("U009", (await t.Db.ReturnPlans.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task Returning_more_on_a_completed_line_is_over_the_plan_not_missing()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ReturnPlans.Add(Plan(qty: 5));
        await t.Db.SaveChangesAsync(); t.Db.ChangeTracker.Clear();
        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(await new ReturnsService(t.Db).CreateResultAsync(ValidResult(qty: 5), Worker(), default));
        t.Db.ChangeTracker.Clear();

        ResultAssert.BadRequest(await new ReturnsService(t.Db).CreateResultAsync(ValidResult(qty: 6), Worker(), default), "QTY-002");
    }
}
