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

public class ProductionRegistrationTests
{
    private static ClaimsPrincipal Admin => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "admin"), new Claim("role_code", "ADMIN"),
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
        db.Roles.Add(new Role { RoleCode = "WORKER", RoleName = "Worker" });
        db.AppUsers.Add(new AppUser { UserCode = "w1", UserName = "Worker", RoleCode = "WORKER", PasswordHash = "x" });
        db.AppFunctions.Add(new AppFunction { FunctionCode = "ht_prd_receipt", FunctionName = "Material Receipt" });
        db.Permissions.Add(new Permission { PermissionCode = "ht_prd_receipt.Open", FunctionCode = "ht_prd_receipt", ActionType = "Open" });
        db.RolePermissions.Add(new RolePermission { RoleCode = "WORKER", PermissionCode = "ht_prd_receipt.Open", IsAllowed = true });
        foreach (var f in new[] { "ht_prd_input", "ht_prd_output", "prd_result" })
        {
            db.AppFunctions.Add(new AppFunction { FunctionCode = f, FunctionName = f });
            db.Permissions.Add(new Permission { PermissionCode = f + ".Open", FunctionCode = f, ActionType = "Open" });
        }
        db.Permissions.Add(new Permission { PermissionCode = "prd_result.Reverse", FunctionCode = "prd_result", ActionType = "Reverse" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return t;
    }

    [Fact]
    public async Task Receipt_stores_received_at_on_the_order_and_is_permission_checked_per_type()
    {
        using var t = Setup();
        var service = new ProductionService(t.Db);
        var worker = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "w1") }, "test"));

        ResultAssert.Created<ApiSuccess<ProductionReceiptDto>>(await service.RegisterAsync("MO1",
            new ProductionResultWriteDto("receipt", "rq1"), worker, default));
        ResultAssert.Error(await service.RegisterAsync("MO1",
            new ProductionResultWriteDto("output", "rq2", Qty: 1), worker, default), 403, "AUTH-006");
        ResultAssert.BadRequest(await service.RegisterAsync("MO1", new ProductionResultWriteDto("bogus", "rq3"), worker, default), "VAL-001");

        var order = t.Db.ProductionOrders.Single();
        Assert.NotNull(order.ReceivedAt);
        Assert.Equal("w1", order.ReceivedBy);
    }

    [Fact]
    public async Task Label_marks_the_output_rows_once_and_reverse_goes_through_one_endpoint()
    {
        using var t = Setup();
        t.Db.Reasons.Add(new Reason { ReasonCode = "ADJ", ReasonName = "Correction", ReasonCategoryValue = ReasonCategory.Adjustment, CreatedAt = DateTime.Now });
        await t.Db.SaveChangesAsync();
        var service = new ProductionService(t.Db);

        ResultAssert.Created<ApiSuccess<ProductionOutputResultDto>>(await service.RegisterAsync("MO1",
            new ProductionResultWriteDto("output", "out1", Qty: 7), Admin, default));
        var label = ResultAssert.Created<ApiSuccess<ProductionLabelResultDto>>(await service.RegisterAsync("MO1",
            new ProductionResultWriteDto("label", "lab1", OutputRequestId: "out1"), Admin, default)).Data!;
        Assert.Equal((7m, 2, 2m), (label.TotalQty, label.LabelCount, label.RemainderQty));
        Assert.All(t.Db.ProductionOutputs.ToList(), o => Assert.NotNull(o.LabelIssuedAt));
        ResultAssert.Error(await service.RegisterAsync("MO1",
            new ProductionResultWriteDto("label", "lab2", OutputRequestId: "out1"), Admin, default), 409, "DUP-002");

        ResultAssert.Created<ApiSuccess<IReadOnlyList<ProductionResult>>>(await service.RegisterAsync("MO1",
            new ProductionResultWriteDto("consume", "c1", "P1", "MAT", "RAW", 3), Admin, default));
        var consumeNo = t.Db.ProductionConsumptions.Single().No;
        ResultAssert.Ok<ApiSuccess<ProductionResult>>(await service.ReverseAsync(consumeNo, new ReverseWriteDto("ADJ"), Admin, default));
        ResultAssert.Error(await service.ReverseAsync(consumeNo, new ReverseWriteDto("ADJ"), Admin, default), 409, "INV-008");
        Assert.Equal(4, t.Db.Stocks.Single(x => x.ItemCode == "MAT").StockQty);
        ResultAssert.NotFound(await service.ReverseAsync(999_999, new ReverseWriteDto("ADJ"), Admin, default));
    }
}
