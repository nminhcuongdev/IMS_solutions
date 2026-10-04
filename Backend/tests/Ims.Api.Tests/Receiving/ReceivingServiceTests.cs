using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Receiving;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Receiving;

public class ReceivingServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task SeedRefsAsync(TestDb t)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Industrial Printer 4in", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackingTypeCode = "BOX", PackageQty = 10,
            IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Contoso Corporation",
            IsSupplier = true, IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP-DEL", PartnerName = "Gone",
            IsSupplier = true, IsActive = true, IsDeleted = true, CreatedAt = now,
        });
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-SHORT", ReasonName = "Supplier shortage",
            ReasonCategoryValue = ReasonCategory.Shortage, IsActive = true, CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
    }

    private static ReceivingPlan Plan(
        string poNo = "PO-001", string branch = "01", decimal qty = 15,
        int status = ReceivingStatus.NotArrived, DateOnly? date = null,
        string? lot = "LOT-A") =>
        new()
        {
            PoNo = poNo, PoBranchNo = branch, PartnerCode = "BP001", ItemCode = "IT-01",
            Qty = qty, ActualQty = 0, FactoryCode = "FAC-01",
            ScheduledDate = date ?? Today, Status = status,
            InvoiceNo = "INV-001", LotNo = lot, LocationCode = "W1_ZA",
            CreatedAt = DateTime.UtcNow, CreatedBy = "admin",
        };

    private static ReceivingPlanWriteDto Valid(
        string poNo = "PO-NEW", string itemCode = "IT-01", decimal qty = 20,
        string? supplier = "BP001", string? serialNo = null, DateOnly? date = null) =>
        new(poNo, serialNo, supplier, itemCode, qty, Unit: null, ScheduledDate: date,
            InvoiceNo: "INV-NEW", FactoryCode: "FAC-01");


    [Fact]
    public async Task List_joins_supplier_item_and_packing_fields_from_the_masters()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res).Data);
        Assert.Equal("PO-001", row.PoNo);
        Assert.Equal("01", row.SerialNo);
        Assert.Equal("BP001", row.SupplierCode);
        Assert.Equal("Contoso Corporation", row.SupplierName);
        Assert.Equal("Industrial Printer 4in", row.ItemName);
        Assert.Equal("pcs", row.Unit);
        Assert.Equal("BOX", row.PackingTypeCode);
        Assert.Equal(10m, row.PackingQty);
        Assert.Equal("statusNotArrived", row.Status);
    }

    [Theory]
    [InlineData(ReceivingStatus.NotArrived, "statusNotArrived")]
    [InlineData(ReceivingStatus.Partial, "statusPartial")]
    [InlineData(ReceivingStatus.Arrived, "statusArrived")]
    [InlineData(ReceivingStatus.Cancelled, "statusCancelled")]
    public async Task List_maps_the_status_code_to_its_label_key(int status, string expected)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(status: status));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res).Data);
        Assert.Equal(expected, row.Status);
    }

    [Fact]
    public async Task List_includes_cancelled_rows_by_default()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-001"));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-002", status: ReceivingStatus.Cancelled));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default);

        var body = ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res);
        Assert.Equal(2, body.Meta.Total);
    }

    [Fact]
    public async Task List_filters_by_scheduled_date_range_inclusively()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        var d1 = new DateOnly(2026, 8, 9);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", date: d1));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-B", date: d1.AddDays(1)));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-C", date: d1.AddDays(2)));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, dateFrom: d1, dateTo: d1.AddDays(1));

        var body = ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res);
        Assert.Equal(new[] { "PO-A", "PO-B" }, body.Data.Select(r => r.PoNo));
    }

    [Fact]
    public async Task List_filters_by_partial_po_no_and_by_item()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-02", ItemName = "Thermal label", ItemGroupCode = "IG-01",
            PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-20260809-001"));
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = "XX-999", PoBranchNo = "01", ItemCode = "IT-02", Qty = 1,
            ScheduledDate = Today, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var byPo = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, poNo: "20260809");
        var byItemCode = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, itemCode: "IT-02");
        var byItemName = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, itemName: "thermal");

        Assert.Equal("PO-20260809-001",
            Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(byPo).Data).PoNo);
        Assert.Equal("XX-999",
            Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(byItemCode).Data).PoNo);
        Assert.Equal("XX-999",
            Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(byItemName).Data).PoNo);
    }

    [Fact]
    public async Task List_filters_by_status_label_key()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A"));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-B", status: ReceivingStatus.Arrived));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, status: "statusArrived");

        Assert.Equal("PO-B",
            Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res).Data).PoNo);
    }

    [Fact]
    public async Task List_ignores_an_unknown_status_filter_instead_of_returning_nothing()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, status: "statusNope");

        Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res).Data);
    }

    [Fact]
    public async Task List_sorts_in_both_directions_and_falls_back_for_an_unknown_column()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-B", qty: 5));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", qty: 50));
        await t.Db.SaveChangesAsync();

        var asc = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, sort: "qty");
        var desc = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, sort: "-qty");
        var junk = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, sort: "drop_table");

        Assert.Equal(new[] { 5m, 50m },
            ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(asc).Data.Select(r => r.PlannedQty));
        Assert.Equal(new[] { 50m, 5m },
            ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(desc).Data.Select(r => r.PlannedQty));
        ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(junk);
    }

    [Fact]
    public async Task List_paginates_while_reporting_the_full_total()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        for (var i = 1; i <= 45; i++) t.Db.ReceivingPlans.Add(Plan(poNo: $"PO-{i:D3}"));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default, page: 2, pageSize: 20);

        var body = ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res);
        Assert.Equal(45, body.Meta.Total);
        Assert.Equal(20, body.Data.Count);
    }


    [Fact]
    public async Task Create_persists_the_row_with_the_documented_defaults()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingPlanDto>>(res).Data!;
        Assert.Equal("PO-NEW", dto.PoNo);
        Assert.Equal("01", dto.SerialNo);
        Assert.Equal("statusNotArrived", dto.Status);
        Assert.Equal(0m, dto.ActualQty);
        Assert.Equal(20m, dto.PlannedQty);
        Assert.NotNull(await t.Db.ReceivingPlans.FirstOrDefaultAsync(p => p.PoNo == "PO-NEW"));
    }

    [Fact]
    public async Task Create_defaults_the_scheduled_date_to_today()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(date: null), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingPlanDto>>(res).Data!;
        Assert.Equal(Today, dto.ScheduledDate);
    }

    [Fact]
    public async Task Create_trims_the_code_fields()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(poNo: "  PO-T  ", itemCode: "  IT-01  ", supplier: "  BP001  "), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingPlanDto>>(res).Data!;
        Assert.Equal("PO-T", dto.PoNo);
        Assert.Equal("IT-01", dto.ItemCode);
        Assert.Equal("BP001", dto.SupplierCode);
    }

    [Theory]
    [InlineData("", "IT-01")]
    [InlineData("   ", "IT-01")]
    [InlineData("PO-NEW", "")]
    [InlineData("PO-NEW", "   ")]
    public async Task Create_rejects_a_blank_po_no_or_item_code_with_VAL001(string poNo, string itemCode)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(poNo: poNo, itemCode: itemCode), default);

        ResultAssert.BadRequest(res, "VAL-001");
    }

    [Theory]
    [InlineData("PO/1")]
    [InlineData("PO?1")]
    [InlineData("PO&1")]
    [InlineData("PO#1")]
    public async Task Create_rejects_forbidden_characters_in_the_po_no_with_VAL005(string poNo)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(poNo: poNo), default);

        ResultAssert.BadRequest(res, "VAL-005");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.5)]
    public async Task Create_rejects_a_quantity_below_one_with_QTY001(decimal qty)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(qty: qty), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Theory]
    [InlineData(15)]
    [InlineData(101)]
    [InlineData(9)]
    public async Task Create_rejects_a_quantity_that_is_not_a_whole_number_of_packages(decimal qty)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(qty: qty), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(1000)]
    public async Task Create_accepts_a_quantity_that_is_an_exact_multiple_of_the_package_qty(decimal qty)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(qty: qty), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingPlanDto>>(res).Data!;
        Assert.Equal(qty, dto.PlannedQty);
    }

    [Fact]
    public async Task Create_skips_the_package_check_when_the_item_has_no_package_qty()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        var item = await t.Db.Items.FirstAsync(i => i.ItemCode == "IT-01");
        item.PackageQty = 0;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(qty: 7), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingPlanDto>>(res).Data!;
        Assert.Equal(7m, dto.PlannedQty);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_or_deleted_item()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var unknown = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(itemCode: "NOPE"), default);
        var item = await t.Db.Items.FirstAsync(i => i.ItemCode == "IT-01");
        item.IsDeleted = true;
        await t.Db.SaveChangesAsync();
        var deleted = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(), default);

        ResultAssert.NotFound(unknown);
        ResultAssert.NotFound(deleted);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_or_deleted_supplier()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var unknown = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(supplier: "NOPE"), default);
        var deleted = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(supplier: "BP-DEL"), default);

        ResultAssert.NotFound(unknown);
        ResultAssert.NotFound(deleted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Create_treats_a_blank_supplier_as_null(string? supplier)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(supplier: supplier), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingPlanDto>>(res).Data!;
        Assert.Null(dto.SupplierCode);
    }

    [Fact]
    public async Task Create_allows_the_same_po_no_twice_because_no_is_the_key()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var first = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(serialNo: "01"), default);
        var second = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreatePlanAsync(Valid(serialNo: "02"), default);

        ResultAssert.Created<ApiSuccess<ReceivingPlanDto>>(first);
        ResultAssert.Created<ApiSuccess<ReceivingPlanDto>>(second);
        Assert.Equal(2, await t.Db.ReceivingPlans.CountAsync(p => p.PoNo == "PO-NEW"));
    }


    [Theory]
    [InlineData("done", ReceivingStatus.Arrived)]
    [InlineData("notDone", ReceivingStatus.NotArrived)]
    [InlineData("cancelled", ReceivingStatus.Cancelled)]
    public async Task ChangeStatus_maps_each_action_to_its_status_code(string action, int expected)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", status: ReceivingStatus.Partial));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ChangeStatusAsync(new ReceivingStatusChangeDto(new[] { "PO-A" }, action), default);

        var data = ResultAssert.Ok<ApiSuccess<ReceivingStatusChangeResult>>(res).Data!;
        Assert.Equal(1, data.Updated);
        Assert.Empty(data.Skipped);
        Assert.Equal(expected, (await t.Db.ReceivingPlans.FirstAsync(p => p.PoNo == "PO-A")).Status);
    }

    [Fact]
    public async Task ChangeStatus_skips_rows_already_arrived_or_cancelled_and_reports_them()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-OPEN"));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-DONE", status: ReceivingStatus.Arrived));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-CANX", status: ReceivingStatus.Cancelled));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ChangeStatusAsync(new ReceivingStatusChangeDto(new[] { "PO-OPEN", "PO-DONE", "PO-CANX" }, "done"), default);

        var data = ResultAssert.Ok<ApiSuccess<ReceivingStatusChangeResult>>(res).Data!;
        Assert.Equal(1, data.Updated);
        Assert.Equal(new[] { "PO-CANX", "PO-DONE" }, data.Skipped.OrderBy(x => x));
        Assert.Equal(ReceivingStatus.Cancelled,
            (await t.Db.ReceivingPlans.FirstAsync(p => p.PoNo == "PO-CANX")).Status);
    }

    [Fact]
    public async Task ChangeStatus_with_lines_changes_only_the_selected_serial_of_a_po()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", branch: "01"));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", branch: "02"));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ChangeStatusAsync(new ReceivingStatusChangeDto(null, "cancelled", new[] { new ReceivingPlanKeyDto("PO-A", "02") }), default);

        var data = ResultAssert.Ok<ApiSuccess<ReceivingStatusChangeResult>>(res).Data!;
        Assert.Equal(1, data.Updated);
        Assert.Empty(data.Skipped);
        var rows = await t.Db.ReceivingPlans.Where(p => p.PoNo == "PO-A").ToDictionaryAsync(p => p.PoBranchNo);
        Assert.Equal(ReceivingStatus.NotArrived, rows["01"].Status);
        Assert.Equal(ReceivingStatus.Cancelled, rows["02"].Status);
    }

    [Fact]
    public async Task ChangeStatus_with_lines_reports_skipped_lines_as_po_slash_serial()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", branch: "01", status: ReceivingStatus.Arrived));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ChangeStatusAsync(new ReceivingStatusChangeDto(null, "done",
                new[] { new ReceivingPlanKeyDto("PO-A", "01"), new ReceivingPlanKeyDto("PO-A", "09") }), default);

        var data = ResultAssert.Ok<ApiSuccess<ReceivingStatusChangeResult>>(res).Data!;
        Assert.Equal(0, data.Updated);
        Assert.Equal(new[] { "PO-A/01", "PO-A/09" }, data.Skipped);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("somethingElse")]
    public async Task ChangeStatus_rejects_a_missing_or_unknown_status_with_SEL006(string? status)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A"));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ChangeStatusAsync(new ReceivingStatusChangeDto(new[] { "PO-A" }, status), default);

        ResultAssert.BadRequest(res, "SEL-006");
    }

    [Fact]
    public async Task ChangeStatus_rejects_an_empty_selection_with_SEL002()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var empty = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ChangeStatusAsync(new ReceivingStatusChangeDto(Array.Empty<string>(), "done"), default);
        var missing = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ChangeStatusAsync(new ReceivingStatusChangeDto(null, "done"), default);

        ResultAssert.BadRequest(empty, "SEL-002");
        ResultAssert.BadRequest(missing, "SEL-002");
    }

    [Fact]
    public async Task ChangeStatus_updates_every_line_of_the_same_po_no()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", branch: "01"));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", branch: "02"));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ChangeStatusAsync(new ReceivingStatusChangeDto(new[] { "PO-A" }, "done"), default);

        var data = ResultAssert.Ok<ApiSuccess<ReceivingStatusChangeResult>>(res).Data!;
        Assert.Equal(2, data.Updated);
        Assert.All(await t.Db.ReceivingPlans.ToListAsync(),
            p => Assert.Equal(ReceivingStatus.Arrived, p.Status));
    }


    [Fact]
    public async Task Shortage_subtracts_from_the_planned_qty_and_moves_the_row_to_partial()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(4, "RS-SHORT"), default);

        var dto = ResultAssert.Ok<ApiSuccess<ReceivingPlanDto>>(res).Data!;
        Assert.Equal(11m, dto.ActualQty);
        Assert.Equal("statusPartial", dto.Status);
        Assert.Equal("RS-SHORT", dto.ShortageReason);
        var row = await t.Db.ReceivingPlans.FirstAsync(p => p.PoNo == "PO-A");
        Assert.Equal(15m, row.Qty);
        Assert.NotNull(row.UpdatedAt);
    }

    [Fact]
    public async Task Shortage_accepts_zero_as_a_valid_quantity()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(0, "RS-SHORT"), default);

        Assert.Equal(15m, ResultAssert.Ok<ApiSuccess<ReceivingPlanDto>>(res).Data!.ActualQty);
    }

    [Fact]
    public async Task Shortage_rejects_a_missing_quantity_with_QTY001()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A"));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(null, "RS-SHORT"), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public async Task Shortage_rejects_a_negative_quantity_with_QTY001(decimal qty)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A"));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(qty, "RS-SHORT"), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Fact]
    public async Task Shortage_rejects_a_quantity_larger_than_planned_with_QTY001()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", qty: 15));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(16, "RS-SHORT"), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Shortage_rejects_a_missing_reason_with_RSN001(string? reason)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A"));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(1, reason), default);

        ResultAssert.BadRequest(res, "RSN-001");
    }

    [Fact]
    public async Task Shortage_rejects_a_reason_from_another_category()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-DEF", ReasonName = "Damaged",
            ReasonCategoryValue = ReasonCategory.Defect, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A"));
        await t.Db.SaveChangesAsync();

        var unknown = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(1, "NOPE"), default);
        var wrongCategory = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(1, "RS-DEF"), default);

        ResultAssert.NotFound(unknown);
        ResultAssert.NotFound(wrongCategory);
    }

    [Fact]
    public async Task Shortage_returns_404_for_an_unknown_po_no()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("NOPE", new ReceivingShortageDto(1, "RS-SHORT"), default);

        ResultAssert.NotFound(res);
    }

    [Theory]
    [InlineData(ReceivingStatus.Arrived)]
    [InlineData(ReceivingStatus.Cancelled)]
    public async Task Shortage_refuses_a_closed_row(int status)
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", status: status));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(1, "RS-SHORT"), default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task Shortage_needs_a_serial_no_when_the_po_has_several_open_lines()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", branch: "01", qty: 10));
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", branch: "02", qty: 20));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var ambiguous = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(1, "RS-SHORT"), default);
        var explicitLine = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).RegisterShortageAsync("PO-A", new ReceivingShortageDto(1, "RS-SHORT", SerialNo: "02"), default);

        ResultAssert.NotFound(ambiguous);
        var dto = ResultAssert.Ok<ApiSuccess<ReceivingPlanDto>>(explicitLine).Data!;
        Assert.Equal("02", dto.SerialNo);
        Assert.Equal(19m, dto.ActualQty);
        Assert.Equal(ReceivingStatus.NotArrived,
            (await t.Db.ReceivingPlans.FirstAsync(p => p.PoBranchNo == "01")).Status);
    }


    [Fact]
    public async Task ExportPlansCsv_writes_the_header_and_the_status_label_key()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", status: ReceivingStatus.Arrived));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ExportPlansCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("po_no,po_branch_no,partner_code,partner_name,item_code,item_name,", lines[0]);
        Assert.Contains("statusArrived", lines[1]);
        Assert.Contains("PO-A", lines[1]);
    }

    [Fact]
    public async Task ExportPlansCsv_writes_decimals_with_a_dot_separator()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-A", qty: 12.5m));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ExportPlansCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains("12.5", csv);
        Assert.DoesNotContain("12,5", csv);
    }

    [Fact]
    public async Task ExportResultsCsv_writes_the_worker_name_from_the_join()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.AppUsers.Add(new AppUser
        {
            UserCode = "U001", UserName = "Nguyễn Văn An", RoleCode = "Admin",
            PasswordHash = "x", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingResults.Add(new ReceivingResult
        {
            PoNo = "PO-001", PoBranchNo = "01", ItemCode = "IT-01", Qty = 20,
            CreatedBy = "U001", CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ExportResultsCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("no,created_at,item_code,item_name,qty,unit,po_no,po_branch_no,", lines[0]);
        Assert.Contains("Nguyễn Văn An", lines[1]);
    }


    [Fact]
    public async Task ListResults_joins_the_item_name_and_the_worker_name()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.AppUsers.Add(new AppUser
        {
            UserCode = "U001", UserName = "Nguyễn Văn An", RoleCode = "Admin",
            PasswordHash = "x", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingResults.Add(new ReceivingResult
        {
            PoNo = "PO-001", PoBranchNo = "01", ItemCode = "IT-01", Qty = 20,
            LocationCode = "W1_ZA", CreatedBy = "U001", CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListResultsAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingResultDto>>(res).Data);
        Assert.Equal("Industrial Printer 4in", row.ItemName);
        Assert.Equal("pcs", row.Unit);
        Assert.Equal("Nguyễn Văn An", row.Worker);
        Assert.True(row.No > 0);
    }

    [Fact]
    public async Task ListResults_filters_by_po_no_presence()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingResults.Add(new ReceivingResult
        {
            PoNo = "PO-001", ItemCode = "IT-01", Qty = 1, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingResults.Add(new ReceivingResult
        {
            PoNo = null, ItemCode = "IT-01", Qty = 2, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var all = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListResultsAsync(default, poNoPresence: "all");
        var with = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListResultsAsync(default, poNoPresence: "with");
        var without = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListResultsAsync(default, poNoPresence: "without");

        Assert.Equal(2, ResultAssert.Ok<ApiListSuccess<ReceivingResultDto>>(all).Meta.Total);
        Assert.Equal("PO-001",
            Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingResultDto>>(with).Data).PoNo);
        Assert.Null(
            Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingResultDto>>(without).Data).PoNo);
    }

    [Fact]
    public async Task ListResults_filters_by_date_range_and_paginates()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        var start = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 30; i++)
            t.Db.ReceivingResults.Add(new ReceivingResult
            {
                PoNo = $"PO-{i:D3}", ItemCode = "IT-01", Qty = 1, CreatedAt = start.AddDays(i),
            });
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListResultsAsync(default, page: 1, pageSize: 20, dateFrom: DateOnly.FromDateTime(start), dateTo: DateOnly.FromDateTime(start.AddDays(9)));

        var body = ResultAssert.Ok<ApiListSuccess<ReceivingResultDto>>(res);
        Assert.Equal(10, body.Meta.Total);
        Assert.Equal(10, body.Data.Count);
    }


    private static ClaimsPrincipal Worker(string code = "U001") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    [Fact]
    public async Task CreateResult_without_a_scanned_location_is_LOC001_and_writes_nothing()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(locationCode: "  "), Worker(), default);

        ResultAssert.BadRequest(res, "LOC-001");
        Assert.Equal(0, await t.Db.ReceivingResults.CountAsync());
        Assert.Equal(0, await t.Db.Stocks.CountAsync(s => s.SerialNo.StartsWith("RCV-")));
        Assert.Equal(0m, (await t.Db.ReceivingPlans.SingleAsync()).ActualQty);
    }

    [Fact]
    public async Task CreateResult_declaring_zero_needs_no_location()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 0, reasonCode: "RS-SHORT", locationCode: null), Worker(), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res).Data!;
        Assert.Null(dto.LocationCode);
    }

    private static ReceivingResultWriteDto ValidResult(
        string itemCode = "IT-01", decimal? qty = 15, string? poNo = "PO-001",
        string? serialNo = "01", string? reasonCode = null, string? locationCode = "W1_ZA") =>
        new(itemCode, qty, poNo, serialNo, locationCode, LotNo: "LOT-A",
            ExpiryDate: null, ReasonCode: reasonCode, InvoiceNo: null);


    [Fact]
    public async Task CreateResult_puts_the_package_on_the_shelf_and_records_a_Receiving_history_row()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 20));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(
            await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default));

        var stock = Assert.Single(await t.Db.Stocks.ToListAsync());
        Assert.Equal(("IT-01", "FAC-01", "W1_ZA", "LOT-A"),
            (stock.ItemCode, stock.FactoryCode, stock.LocationCode, stock.LotNo));
        Assert.Equal((10m, 0m, 10m, false), (stock.StockQty, stock.ReservedQty, stock.AvailableQty, stock.IsOnHold));
        Assert.StartsWith("RCV-", stock.SerialNo);
        Assert.Equal("U001", stock.UpdatedBy);

        var h = Assert.Single(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(("IT-01", "FAC-01", "W1_ZA", "LOT-A", "Receiving", 10m, "U001"),
            (h.ItemCode, h.FactoryCode, h.LocationCode, h.LotNo, h.ActionType, h.Qty, h.CreatedBy));
    }

    [Fact]
    public async Task CreateResult_gives_each_package_its_own_stock_row()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 20));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default);
        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default);

        var rows = await t.Db.Stocks.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.NotEqual(rows[0].SerialNo, rows[1].SerialNo);
        Assert.Equal(20m, rows.Sum(r => r.StockQty));
        Assert.Equal(2, await t.Db.InventoryHistories.CountAsync());
    }

    [Fact]
    public async Task CreateResult_with_zero_quantity_puts_nothing_in_stock()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 20));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 0, reasonCode: "RS-SHORT"), Worker(), default));

        Assert.Empty(await t.Db.Stocks.ToListAsync());
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
    }

    [Fact]
    public async Task CreateResult_on_hold_is_in_stock_but_not_available()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 20));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10) with { IsOnHold = true }, Worker(), default);

        var stock = Assert.Single(await t.Db.Stocks.ToListAsync());
        Assert.Equal((10m, 0m, true), (stock.StockQty, stock.AvailableQty, stock.IsOnHold));
    }

    [Fact]
    public async Task CreateResult_derives_expiry_from_the_item_expiration_period_when_none_is_sent()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        var item = await t.Db.Items.FirstAsync(i => i.ItemCode == "IT-01");
        item.ExpirationPeriod = 365;
        t.Db.ReceivingPlans.Add(Plan(qty: 20));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default);

        var stock = Assert.Single(await t.Db.Stocks.ToListAsync());
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now).AddDays(365), stock.ExpiryDate);
    }

    [Fact]
    public async Task A_refused_CreateResult_puts_nothing_in_stock()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 20));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.BadRequest(await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 30), Worker(), default), "QTY-002");

        Assert.Empty(await t.Db.Stocks.ToListAsync());
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
    }


    [Fact]
    public async Task CreateResult_adds_each_package_to_the_running_total()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 100));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(
            await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default));
        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(
            await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default));

        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(20m, plan.ActualQty);
        Assert.Equal(ReceivingStatus.Partial, plan.Status);
    }

    [Fact]
    public async Task CreateResult_marks_the_line_arrived_only_when_the_last_package_is_in()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 30));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        for (var i = 0; i < 3; i++)
        {
            ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(
                await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default));
        }

        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(30m, plan.ActualQty);
        Assert.Equal(ReceivingStatus.Arrived, plan.Status);
    }

    [Fact]
    public async Task CreateResult_accepts_a_full_package_without_a_reason()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 100));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(10m, plan.ActualQty);
        Assert.Null(plan.ReasonCode);
    }

    [Fact]
    public async Task CreateResult_keeps_the_most_recent_shortage_reason()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-LATE", ReasonName = "Delayed shipment",
            ReasonCategoryValue = ReasonCategory.Shortage, IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        t.Db.ReceivingPlans.Add(Plan(qty: 100));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 6, reasonCode: "RS-SHORT"), Worker(), default);
        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 4, reasonCode: "RS-LATE"), Worker(), default);

        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal("RS-LATE", plan.ReasonCode);
    }

    [Fact]
    public async Task CreateResult_does_not_clear_an_earlier_reason_when_a_later_package_is_full()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 100));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 6, reasonCode: "RS-SHORT"), Worker(), default);
        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default);

        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal("RS-SHORT", plan.ReasonCode);
    }

    [Fact]
    public async Task CreateResult_rejects_a_package_that_pushes_the_total_past_planned()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 20));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 20), Worker(), default);

        ResultAssert.BadRequest(res, "QTY-002");
    }

    [Fact]
    public async Task CreateResult_falls_back_to_the_whole_line_when_the_item_has_no_package_qty()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        var item = await t.Db.Items.FirstAsync(i => i.ItemCode == "IT-01");
        item.PackageQty = 0;
        t.Db.ReceivingPlans.Add(Plan(qty: 100));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 10), Worker(), default);

        ResultAssert.BadRequest(res, "RSN-001");
    }

    [Fact]
    public async Task CreateResult_only_touches_the_confirmed_line_not_the_whole_slip()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(branch: "01", qty: 15));
        t.Db.ReceivingPlans.Add(Plan(branch: "02", qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(serialNo: "01", qty: 15), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        var line1 = await t.Db.ReceivingPlans.FirstAsync(p => p.PoBranchNo == "01");
        var line2 = await t.Db.ReceivingPlans.FirstAsync(p => p.PoBranchNo == "02");
        Assert.Equal(ReceivingStatus.Arrived, line1.Status);
        Assert.Equal(ReceivingStatus.NotArrived, line2.Status);
        Assert.Equal(0m, line2.ActualQty);
    }

    [Fact]
    public async Task CreateResult_writes_actual_qty_when_the_line_is_fully_received()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 15), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(15m, plan.ActualQty);
        Assert.Equal(ReceivingStatus.Arrived, plan.Status);
    }

    [Fact]
    public async Task CreateResult_marks_the_line_partial_and_keeps_planned_qty_when_short()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 12, reasonCode: "RS-SHORT"), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(15m, plan.Qty);
        Assert.Equal(12m, plan.ActualQty);
        Assert.Equal(ReceivingStatus.Partial, plan.Status);
        Assert.Equal("RS-SHORT", plan.ReasonCode);
    }

    [Fact]
    public async Task CreateResult_rejects_a_shortage_without_a_reason()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 100));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 7, reasonCode: null), Worker(), default);

        ResultAssert.BadRequest(res, "RSN-001");
        Assert.Equal(0m, (await t.Db.ReceivingPlans.FirstAsync()).ActualQty);
    }

    [Fact]
    public async Task CreateResult_does_not_require_a_reason_when_the_qty_matches()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 15, reasonCode: null), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
    }

    [Fact]
    public async Task CreateResult_rejects_a_reason_from_another_category()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-DEF", ReasonName = "Damaged",
            ReasonCategoryValue = ReasonCategory.Defect, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingPlans.Add(Plan(qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 12, reasonCode: "RS-DEF"), Worker(), default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task CreateResult_rejects_receiving_more_than_planned()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 16, reasonCode: "RS-SHORT"), Worker(), default);

        ResultAssert.BadRequest(res, "QTY-002");
    }

    [Fact]
    public async Task CreateResult_rejects_a_negative_qty()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: -1, reasonCode: "RS-SHORT"), Worker(), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Fact]
    public async Task CreateResult_rejects_a_missing_qty()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: null), Worker(), default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Fact]
    public async Task CreateResult_accepts_zero_qty_as_a_full_shortage()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 0, reasonCode: "RS-SHORT"), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(0m, plan.ActualQty);
        Assert.Equal(ReceivingStatus.Partial, plan.Status);
    }

    [Fact]
    public async Task CreateResult_requires_an_item_code()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(itemCode: "   "), Worker(), default);

        ResultAssert.BadRequest(res, "VAL-001");
    }

    [Fact]
    public async Task CreateResult_rejects_an_unknown_item()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(itemCode: "IT-NOPE"), Worker(), default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task CreateResult_rejects_an_unknown_location()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(locationCode: "W9_NOPE"), Worker(), default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task CreateResult_rejects_an_item_that_does_not_belong_to_the_line()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-02", ItemName = "Other", ItemGroupCode = "IG-01",
            UomCode = "pcs", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(itemCode: "IT-02"), Worker(), default);

        ResultAssert.NotFound(res);
        Assert.Empty(t.Db.ReceivingResults);
    }

    [Fact]
    public async Task CreateResult_refuses_to_confirm_an_already_arrived_line()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(status: ReceivingStatus.Arrived));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(), Worker(), default);

        ResultAssert.NotFound(res);
        Assert.Empty(t.Db.ReceivingResults);
    }

    [Fact]
    public async Task CreateResult_refuses_to_confirm_a_cancelled_line()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(status: ReceivingStatus.Cancelled));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(), Worker(), default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task CreateResult_refuses_to_guess_when_the_slip_has_several_open_lines()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(branch: "01"));
        t.Db.ReceivingPlans.Add(Plan(branch: "02"));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(serialNo: null), Worker(), default);

        ResultAssert.NotFound(res);
        Assert.Empty(t.Db.ReceivingResults);
    }

    [Fact]
    public async Task CreateResult_accepts_a_receipt_without_a_purchase_order()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(poNo: "PO-KHAC", status: ReceivingStatus.Arrived));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(poNo: null, serialNo: null, qty: 7), Worker(), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res).Data!;
        Assert.Null(dto.PoNo);
        Assert.Equal(7m, dto.Qty);
        Assert.Equal("statusArrived", dto.Status);
    }

    [Fact]
    public async Task CreateResult_stores_worker_location_lot_and_inherits_the_invoice_no()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.AppUsers.Add(new AppUser
        {
            UserCode = "U001", UserName = "Nguyen Van An", RoleCode = "Admin",
            PasswordHash = "x", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingPlans.Add(Plan(qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 15), Worker("U001"), default);

        var dto = ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res).Data!;
        Assert.Equal("PO-001", dto.PoNo);
        Assert.Equal("01", dto.SerialNo);
        Assert.Equal("W1_ZA", dto.LocationCode);
        Assert.Equal("INV-001", dto.InvoiceNo);
        Assert.Equal("Nguyen Van An", dto.Worker);
        Assert.Equal("pcs", dto.Unit);
        var row = await t.Db.ReceivingResults.FirstAsync();
        Assert.Equal("LOT-A", row.LotNo);
        Assert.Equal("U001", row.CreatedBy);
    }

    [Fact]
    public async Task CreateResult_row_shows_up_on_the_result_grid()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(qty: 15));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(qty: 15), Worker(), default);
        var list = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListResultsAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingResultDto>>(list).Data);
        Assert.Equal("PO-001", row.PoNo);
        Assert.Equal(15m, row.Qty);
        Assert.Equal("statusArrived", row.Status);
    }


    [Fact]
    public async Task CreateResult_requires_a_lot_no()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult() with { LotNo = "   " }, Worker(), default);

        ResultAssert.BadRequest(res, "LOT-001");
        Assert.Empty(t.Db.ReceivingResults);
    }

    [Fact]
    public async Task CreateResult_rejects_a_lot_that_is_unknown_to_the_system()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(lot: "LOT-A"));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult() with { LotNo = "LOT-KHONG-CO" }, Worker(), default);

        ResultAssert.Error(res, 404, "LOT-002");
    }

    [Fact]
    public async Task CreateResult_rejects_a_known_lot_that_belongs_to_another_line()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(branch: "01", lot: "LOT-A"));
        t.Db.ReceivingPlans.Add(Plan(branch: "02", lot: "LOT-B"));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(serialNo: "01") with { LotNo = "LOT-B" }, Worker(), default);

        ResultAssert.Error(res, 409, "SCN-006");
        Assert.Empty(t.Db.ReceivingResults);
    }

    [Fact]
    public async Task CreateResult_accepts_any_known_lot_when_the_line_has_none()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(branch: "01", lot: null));
        t.Db.ReceivingPlans.Add(Plan(branch: "02", lot: "LOT-B", status: ReceivingStatus.Cancelled));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).CreateResultAsync(ValidResult(serialNo: "01") with { LotNo = "LOT-B" }, Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        Assert.Equal("LOT-B", (await t.Db.ReceivingResults.FirstAsync()).LotNo);
    }

    [Fact]
    public async Task List_exposes_the_planned_lot_and_location()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan(lot: "LOT-A"));
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res).Data);
        Assert.Equal("LOT-A", row.LotNo);
        Assert.Equal("W1_ZA", row.LocationCode);
        Assert.False(row.LabelIssued);
    }

    [Fact]
    public async Task List_marks_a_line_as_label_issued_from_the_print_history()
    {
        using var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.ReceivingPlans.Add(Plan());
        t.Db.PrintHistories.Add(new Ims.Domain.History.PrintHistory
        {
            Category = "Receiving", DocNo = "PO-001", ItemCode = "IT-01",
            Qty = 15, PackageCount = 1, Status = 19, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db, new Ims.Api.Features.Ht.LotLookup(t.Db)).ListPlansAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingPlanDto>>(res).Data);
        Assert.True(row.LabelIssued);
    }
}
