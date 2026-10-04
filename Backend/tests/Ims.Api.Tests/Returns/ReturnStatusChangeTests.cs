using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Returns;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Returns;
using Xunit;

namespace Ims.Api.Tests.Returns;

public class ReturnStatusChangeTests
{
    private static async Task SeedAsync(TestDb t)
    {
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01", UomCode = "pcs",
            PackingTypeCode = "BOX", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Customer", IsCustomer = true, IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
    }

    private static ReturnPlan Plan(string returnNo, string branch = "01", decimal qty = 5, decimal actual = 0,
        int status = ReturnStatus.Pending) => new()
    {
        ReturnNo = returnNo, ReturnBranchNo = branch, PartnerCode = "BP001", ItemCode = "IT-01",
        Qty = qty, ActualQty = actual, FactoryCode = "FAC-01", Status = status, LotNo = "LOT-A",
        CreatedAt = DateTime.UtcNow, CreatedBy = "admin",
    };

    private static async Task<ReturnStatusChangeResult> ChangeAsync(TestDb t, ReturnStatusChangeDto body)
    {
        t.Db.ChangeTracker.Clear();
        var res = await new ReturnsService(t.Db).ChangeStatusAsync(body, default);
        t.Db.ChangeTracker.Clear();
        return ResultAssert.Ok<ApiSuccess<ReturnStatusChangeResult>>(res).Data!;
    }

    [Fact]
    public async Task Done_fills_actual_with_the_expected_quantity()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReturnPlans.Add(Plan("RTN-A", qty: 5, actual: 2, status: ReturnStatus.Partial));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, new ReturnStatusChangeDto(new[] { "RTN-A" }, "done"));

        Assert.Equal(1, data.Updated);
        var plan = await t.Db.ReturnPlans.SingleAsync();
        Assert.Equal((ReturnStatus.Returned, 5m), (plan.Status, plan.ActualQty));
    }

    [Fact]
    public async Task NotDone_resets_actual_and_reopens_a_cancelled_line()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReturnPlans.Add(Plan("RTN-A", status: ReturnStatus.Cancelled));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, new ReturnStatusChangeDto(new[] { "RTN-A" }, "notDone"));

        Assert.Equal(1, data.Updated);
        var plan = await t.Db.ReturnPlans.SingleAsync();
        Assert.Equal((ReturnStatus.Pending, 0m), (plan.Status, plan.ActualQty));
    }

    [Fact]
    public async Task NotDone_skips_a_line_that_already_took_goods_back()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReturnPlans.Add(Plan("RTN-A", actual: 2, status: ReturnStatus.Partial));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, new ReturnStatusChangeDto(new[] { "RTN-A" }, "notDone"));

        Assert.Equal(0, data.Updated);
        Assert.Equal(new[] { "RTN-A" }, data.Skipped);
        Assert.Equal(2m, (await t.Db.ReturnPlans.SingleAsync()).ActualQty);
    }

    [Fact]
    public async Task Cancelled_keeps_what_was_already_returned()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReturnPlans.Add(Plan("RTN-A", actual: 2, status: ReturnStatus.Partial));
        await t.Db.SaveChangesAsync();

        await ChangeAsync(t, new ReturnStatusChangeDto(new[] { "RTN-A" }, "cancelled"));

        var plan = await t.Db.ReturnPlans.SingleAsync();
        Assert.Equal((ReturnStatus.Cancelled, 2m), (plan.Status, plan.ActualQty));
    }

    [Fact]
    public async Task Lines_change_only_the_selected_serial_and_report_unknown_ones()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReturnPlans.Add(Plan("RTN-A", branch: "01"));
        t.Db.ReturnPlans.Add(Plan("RTN-A", branch: "02"));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, new ReturnStatusChangeDto(null, "cancelled",
            new[] { new ReturnPlanKeyDto("RTN-A", "02"), new ReturnPlanKeyDto("RTN-A", "09") }));

        Assert.Equal(1, data.Updated);
        Assert.Equal(new[] { "RTN-A/09" }, data.Skipped);
        var rows = await t.Db.ReturnPlans.ToDictionaryAsync(p => p.ReturnBranchNo);
        Assert.Equal(ReturnStatus.Pending, rows["01"].Status);
        Assert.Equal(ReturnStatus.Cancelled, rows["02"].Status);
    }
}
