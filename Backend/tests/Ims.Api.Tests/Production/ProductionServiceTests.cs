using System.Security.Claims;
using Ims.Api.Contracts;
using Ims.Api.Features.Production;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Production;
using Ims.Domain.System;
using Xunit;

namespace Ims.Api.Tests.Production;

public class ProductionServiceTests
{
    private static ClaimsPrincipal User => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "admin"), new Claim("role_code", "Admin")
    }, "test"));

    private static TestDb Setup()
    {
        var t = TestDb.Seeded();
        var db = t.Db;
        db.ItemGroups.Add(new ItemGroup { ItemGroupCode = "FG", ItemGroupName = "Finished", Category = ItemGroupCategory.FinishedGoods, CreatedAt = DateTime.Now });
        db.Items.Add(new Item { ItemCode = "FG1", ItemName = "Finished", ItemGroupCode = "FG", UomCode = "pcs", PackageQty = 5, IsLotManaged = true, CreatedAt = DateTime.Now });
        db.Items.Add(new Item { ItemCode = "MAT", ItemName = "Material", ItemGroupCode = "IG-01", UomCode = "pcs", IsLotManaged = true, CreatedAt = DateTime.Now });
        db.Processes.Add(new Ims.Domain.Masters.Process { ProcessCode = "P1", ProcessName = "Process", CreatedAt = DateTime.Now });
        db.ProductionOrders.Add(new ProductionOrder { ProductionOrderNo = "MO1", FactoryCode = "FAC-01", OutputItemCode = "FG1", StorageLocationCode = "W1_ZA", PlannedQty = 10, UomCode = "pcs", OutputLotNo = "FGLOT", PlannedStartDate = DateOnly.FromDateTime(DateTime.Today), PlannedEndDate = DateOnly.FromDateTime(DateTime.Today), CreatedBy = "admin" });
        db.ProductionOrderLines.Add(new ProductionOrderLine { ProductionOrderNo = "MO1", LineNo = 1, ProcessCode = "P1", ItemCode = "MAT", PlannedQty = 10, UomCode = "pcs", ScheduledDate = DateOnly.FromDateTime(DateTime.Today), CreatedBy = "admin" });
        db.Stocks.Add(new Stock { ItemCode = "MAT", SerialNo = "S1", FactoryCode = "FAC-01", LocationCode = "W1_ZA", LotNo = "RAW", StockQty = 4, AvailableQty = 4, UpdatedAt = DateTime.Now });
        db.Stocks.Add(new Stock { ItemCode = "MAT", SerialNo = "S2", FactoryCode = "FAC-01", LocationCode = "W1_ZA", LotNo = "RAW", StockQty = 7, AvailableQty = 7, UpdatedAt = DateTime.Now });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return t;
    }

    [Fact]
    public async Task Line_rows_carry_the_process_name_and_state_for_the_handheld()
    {
        using var t = Setup();
        t.Db.Processes.Single().IsActive = false;
        await t.Db.SaveChangesAsync();
        var service = new ProductionService(t.Db);
        var line = Assert.Single(ResultAssert.Ok<ApiSuccess<IReadOnlyList<ProductionLineRowDto>>>(await service.LinesAsync("MO1", default)).Data!);
        Assert.Equal(("Process", false), (line.ProcessName, line.ProcessActive));
        var listed = Assert.Single(ResultAssert.Ok<ApiListSuccess<ProductionLineRowDto>>(await service.ListAsync(default, unit: "line")).Data);
        Assert.Equal("Process", listed.ProcessName);
    }

    [Fact]
    public async Task Output_splits_by_pack_and_retries_once()
    {
        using var t = Setup();
        var service = new ProductionService(t.Db);
        var body = new OutputWriteDto("out1", 12);
        var first = ResultAssert.Created<ApiSuccess<ProductionOutputResultDto>>(await service.OutputAsync("MO1", body, User, default));
        Assert.Equal(3, first.Data!.Serials.Count);
        Assert.Equal("PRD-1-1", first.Data.Serials[0]);
        Assert.True(first.Data.OverPlan);
        ResultAssert.Ok<ApiSuccess<ProductionOutputResultDto>>(await service.OutputAsync("MO1", body, User, default));
        Assert.Equal(3, t.Db.ProductionOutputs.Count());
        Assert.Equal(12, t.Db.Stocks.Where(x => x.ItemCode == "FG1").Sum(x => x.StockQty));
    }

    [Fact]
    public async Task Consumption_records_each_serial_and_return_respects_net_ceiling()
    {
        using var t = Setup();
        var service = new ProductionService(t.Db);
        ResultAssert.Created<ApiSuccess<IReadOnlyList<ProductionConsumption>>>(await service.ConsumptionAsync("MO1", new ConsumptionWriteDto("consume1", "P1", "MAT", "RAW", 6), User, default));
        Assert.Equal(2, t.Db.ProductionConsumptions.Count());
        ResultAssert.Created<ApiSuccess<IReadOnlyList<ProductionReturn>>>(await service.ReturnAsync("MO1", new ReturnWriteDto("return1", "MAT", "RAW", 5), User, default));
        ResultAssert.Error(await service.ReturnAsync("MO1", new ReturnWriteDto("return2", "MAT", "RAW", 2), User, default), 409, "QTY-004");
        Assert.Equal(10, t.Db.Stocks.Where(x => x.ItemCode == "MAT").Sum(x => x.StockQty));
    }

    [Fact]
    public async Task Order_and_result_lists_show_aggregated_output_before_paging()
    {
        using var t = Setup();
        var service = new ProductionService(t.Db);
        ResultAssert.Created<ApiSuccess<ProductionOutputResultDto>>(await service.OutputAsync("MO1", new OutputWriteDto("out1", 12), User, default));
        var orders = ResultAssert.Ok<ApiListSuccess<ProductionOrderRowDto>>(await service.ListAsync(default, sort: "-output_qty"));
        Assert.Equal(12, Assert.Single(orders.Data).OutputQty);
        var result = ResultAssert.Ok<ApiListSuccess<ProductionResultFgDto>>(await service.ResultsAsync(default, view: "fg"));
        var batch = Assert.Single(result.Data);
        Assert.Equal(3, batch.SerialCount);
        Assert.Equal(12, batch.Qty);
        Assert.Equal(("PRD-1-1", "PRD-1-3"), (batch.SerialFrom, batch.SerialTo));
        Assert.False(string.IsNullOrEmpty(batch.ItemName));
        Assert.False(string.IsNullOrEmpty(batch.Unit));
        Assert.False(string.IsNullOrEmpty(batch.LocationCode));
        var sorted = ResultAssert.Ok<ApiListSuccess<ProductionResultFgDto>>(await service.ResultsAsync(default, view: "fg", sort: "-item_name"));
        Assert.Single(sorted.Data);
    }

    [Fact]
    public async Task Output_reverse_requires_admin_and_reverses_once()
    {
        using var t = Setup();
        t.Db.Reasons.Add(new Reason { ReasonCode = "ADJ", ReasonName = "Correction",
            ReasonCategoryValue = ReasonCategory.Adjustment, CreatedAt = DateTime.Now });
        t.Db.AppFunctions.Add(new AppFunction { FunctionCode = "prd_result", FunctionName = "Production Result", IsActive = true });
        t.Db.Permissions.Add(new Permission { PermissionCode = "prd_result.view", FunctionCode = "prd_result", Action = "view", IsActive = true });
        t.Db.Permissions.Add(new Permission { PermissionCode = "prd_result.reverse", FunctionCode = "prd_result", Action = "reverse", IsActive = true });
        t.Db.RolePermissions.Add(new RolePermission { RoleCode = "Admin", PermissionCode = "prd_result.view", IsAllowed = true });
        t.Db.RolePermissions.Add(new RolePermission { RoleCode = "Admin", PermissionCode = "prd_result.reverse", IsAllowed = true });
        t.Db.SaveChanges();
        var service = new ProductionService(t.Db);
        ResultAssert.Created<ApiSuccess<ProductionOutputResultDto>>(await service.OutputAsync("MO1", new OutputWriteDto("out1", 2), User, default));
        var no = t.Db.ProductionOutputs.Single(x => x.ReversesNo == null).No;
        ResultAssert.Error(await service.ReverseOutputAsync(no, new ReverseWriteDto("ADJ"), new ClaimsPrincipal(), default), 403, "AUTH-006");
        ResultAssert.Ok<ApiSuccess<ProductionOutput>>(await service.ReverseOutputAsync(no, new ReverseWriteDto("ADJ"), User, default));
        ResultAssert.Error(await service.ReverseOutputAsync(no, new ReverseWriteDto("ADJ"), User, default), 409, "INV-008");
        Assert.Equal(0, t.Db.Stocks.Single(x => x.ItemCode == "FG1").StockQty);
    }
}
