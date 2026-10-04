using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Alerts;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Picking;
using Ims.Domain.Receiving;
using Ims.Domain.Shipping;
using Ims.Domain.System;
using Xunit;

namespace Ims.Api.Tests.Alerts;

public class AlertServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private static TestDb Configured(params (string code, decimal? max)[] overrides)
    {
        var t = TestDb.Seeded();
        var db = t.Db;
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var code in AlertType.Ordered)
        {
            var o = overrides.FirstOrDefault(x => x.code == code);
            db.AlertConfigs.Add(new AlertConfig
            {
                AlertTypeCode = code,
                AlertTypeName = code,
                MaxThreshold = o.code is null ? DefaultMax(code) : o.max,
                IsActive = true,
                CreatedAt = now,
            });
        }
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return t;
    }

    private static decimal? DefaultMax(string code) => code switch
    {
        AlertType.ExpiryWarning => 7m,
        AlertType.ReceivingDelay or AlertType.PickingDelay or AlertType.ShippingDelay => 0m,
        _ => null,
    };

    private static void Item(TestDb t, string code, decimal? reorder = null, decimal? surplus = null)
    {
        t.Db.Items.Add(new Item
        {
            ItemCode = code, ItemName = $"Name of {code}", ItemGroupCode = "IG-01",
            UomCode = "pcs", ReorderLevel1 = reorder, SurplusPoint = surplus,
            IsActive = true, CreatedAt = DateTime.UtcNow,
        });
    }

    private static void Stock(
        TestDb t, string item, string serial, decimal qty,
        DateOnly? expiry = null, bool onHold = false, string? reason = null)
    {
        t.Db.Stocks.Add(new Stock
        {
            ItemCode = item, SerialNo = serial, FactoryCode = "FAC-01",
            LocationCode = "W1_ZA", StockQty = qty, AvailableQty = onHold ? 0 : qty,
            ExpiryDate = expiry, IsOnHold = onHold, ReasonCode = reason,
            UpdatedAt = DateTime.UtcNow,
        });
    }

    private static async Task<AlertBlockDto> BlockAsync(TestDb t, string code)
    {
        t.Db.SaveChanges();
        t.Db.ChangeTracker.Clear();
        var res = await new AlertService(t.Db).ListAsync(default);
        var blocks = ResultAssert.Ok<ApiSuccess<List<AlertBlockDto>>>(res).Data!;
        return blocks.Single(b => b.AlertTypeCode == code);
    }

    private static async Task<List<AlertBlockDto>> AllAsync(TestDb t)
    {
        t.Db.SaveChanges();
        t.Db.ChangeTracker.Clear();
        var res = await new AlertService(t.Db).ListAsync(default);
        return ResultAssert.Ok<ApiSuccess<List<AlertBlockDto>>>(res).Data!;
    }


    [Fact]
    public async Task Seven_blocks_come_back_in_the_demo_order()
    {
        using var t = Configured();
        var blocks = await AllAsync(t);

        Assert.Equal(AlertType.Ordered, blocks.Select(b => b.AlertTypeCode));
        Assert.All(blocks, b => Assert.Equal(0, b.Total));
    }

    [Fact]
    public async Task Each_block_carries_its_own_label_keys()
    {
        using var t = Configured();
        var blocks = await AllAsync(t);

        var expiry = blocks.Single(b => b.AlertTypeCode == AlertType.ExpiryWarning);
        Assert.Equal("alertExpiry7", expiry.LabelKey);
        Assert.Equal("thExpirationDate", expiry.ExtraLabelKey);

        var rcv = blocks.Single(b => b.AlertTypeCode == AlertType.ReceivingDelay);
        Assert.Equal("alertReceivingDelay", rcv.LabelKey);
        Assert.Equal("thPoNo", rcv.ExtraLabelKey);
    }

    [Fact]
    public async Task An_inactive_alert_type_has_no_block_at_all()
    {
        using var t = Configured();
        var row = await t.Db.AlertConfigs.SingleAsync(x => x.AlertTypeCode == AlertType.HoldReview);
        row.IsActive = false;

        var blocks = await AllAsync(t);
        Assert.DoesNotContain(blocks, b => b.AlertTypeCode == AlertType.HoldReview);
        Assert.Equal(6, blocks.Count);
    }

    [Fact]
    public async Task No_config_rows_means_no_blocks()
    {
        using var t = TestDb.Seeded();
        Assert.Empty(await AllAsync(t));
    }


    [Fact]
    public async Task Expiry_covers_today_through_the_threshold_inclusive()
    {
        using var t = Configured();
        Item(t, "IT-EXP");
        Stock(t, "IT-EXP", "SN-TODAY", 5, Today);
        Stock(t, "IT-EXP", "SN-D7", 5, Today.AddDays(7));
        Stock(t, "IT-EXP", "SN-D8", 5, Today.AddDays(8));

        var b = await BlockAsync(t, AlertType.ExpiryWarning);
        Assert.Equal(2, b.Total);
        Assert.DoesNotContain(b.Rows, r => r.Extra == Today.AddDays(8).ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task Already_expired_stock_is_not_an_expiry_warning()
    {
        using var t = Configured();
        Item(t, "IT-EXP");
        Stock(t, "IT-EXP", "SN-GONE", 5, Today.AddDays(-1));

        Assert.Equal(0, (await BlockAsync(t, AlertType.ExpiryWarning)).Total);
    }

    [Fact]
    public async Task The_expiry_window_comes_from_the_config_row()
    {
        using var t = Configured((AlertType.ExpiryWarning, 3m));
        Item(t, "IT-EXP");
        Stock(t, "IT-EXP", "SN-D5", 5, Today.AddDays(5));

        Assert.Equal(0, (await BlockAsync(t, AlertType.ExpiryWarning)).Total);
    }

    [Fact]
    public async Task An_expiry_row_shows_the_date_it_expires()
    {
        using var t = Configured();
        Item(t, "IT-EXP");
        Stock(t, "IT-EXP", "SN-1", 5, Today.AddDays(2));

        var r = Assert.Single((await BlockAsync(t, AlertType.ExpiryWarning)).Rows);
        Assert.Equal("IT-EXP", r.Code);
        Assert.Equal("Name of IT-EXP", r.Name);
        Assert.Equal(Today.AddDays(2).ToString("yyyy-MM-dd"), r.Extra);
    }


    [Fact]
    public async Task Low_stock_is_strictly_below_the_reorder_point()
    {
        using var t = Configured();
        Item(t, "IT-LOW", reorder: 10m);
        Stock(t, "IT-LOW", "SN-9", 9);
        Item(t, "IT-AT", reorder: 10m);
        Stock(t, "IT-AT", "SN-10", 10);

        var b = await BlockAsync(t, AlertType.LowStock);
        Assert.Equal("IT-LOW", Assert.Single(b.Rows).Code);
    }

    [Fact]
    public async Task An_item_with_no_reorder_point_is_never_low()
    {
        using var t = Configured();
        Item(t, "IT-NONE");
        Stock(t, "IT-NONE", "SN-0", 0);

        Assert.Equal(0, (await BlockAsync(t, AlertType.LowStock)).Total);
    }

    [Fact]
    public async Task A_low_row_shows_stock_against_the_threshold()
    {
        using var t = Configured();
        Item(t, "IT-LOW", reorder: 10m);
        Stock(t, "IT-LOW", "SN-9", 9);

        var r = Assert.Single((await BlockAsync(t, AlertType.LowStock)).Rows);
        Assert.Equal("9 / 10", r.Extra);
    }

    [Fact]
    public async Task Overflow_starts_AT_the_surplus_point()
    {
        using var t = Configured();
        Item(t, "IT-OVER", surplus: 100m);
        Stock(t, "IT-OVER", "SN-100", 100);
        Item(t, "IT-UNDER", surplus: 100m);
        Stock(t, "IT-UNDER", "SN-99", 99);

        var b = await BlockAsync(t, AlertType.StockOverflow);
        Assert.Equal("IT-OVER", Assert.Single(b.Rows).Code);
    }


    [Fact]
    public async Task Hold_review_lists_held_rows_with_their_reason()
    {
        using var t = Configured();
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-QC", ReasonName = "Quality Inspection",
            ReasonCategoryValue = ReasonCategory.Defect, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        Item(t, "IT-HOLD");
        Stock(t, "IT-HOLD", "SN-H", 5, onHold: true, reason: "RS-QC");
        Stock(t, "IT-HOLD", "SN-FREE", 5);

        var b = await BlockAsync(t, AlertType.HoldReview);
        var r = Assert.Single(b.Rows);
        Assert.Equal("IT-HOLD", r.Code);
        Assert.Equal("Quality Inspection", r.Extra);
    }


    private static void Plan(TestDb t, DateOnly? date, int status, string po = "PO-1")
    {
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = po, PoBranchNo = "01", ItemCode = "IT-D", FactoryCode = "FAC-01",
            ScheduledDate = date, Qty = 10, Status = status, CreatedAt = DateTime.UtcNow,
        });
    }

    [Fact]
    public async Task A_plan_dated_before_today_and_not_done_is_late()
    {
        using var t = Configured();
        Item(t, "IT-D");
        Plan(t, Today.AddDays(-1), ReceivingStatus.NotArrived, "PO-LATE");

        var b = await BlockAsync(t, AlertType.ReceivingDelay);
        var r = Assert.Single(b.Rows);
        Assert.Equal("IT-D", r.Code);
        Assert.Equal("PO-LATE", r.Extra);
    }

    [Fact]
    public async Task Todays_plan_is_not_late_yet()
    {
        using var t = Configured();
        Item(t, "IT-D");
        Plan(t, Today, ReceivingStatus.NotArrived, "PO-TODAY");

        Assert.Equal(0, (await BlockAsync(t, AlertType.ReceivingDelay)).Total);
    }

    [Fact]
    public async Task A_finished_plan_is_never_late()
    {
        using var t = Configured();
        Item(t, "IT-D");
        Plan(t, Today.AddDays(-5), ReceivingStatus.Arrived, "PO-DONE");

        Assert.Equal(0, (await BlockAsync(t, AlertType.ReceivingDelay)).Total);
    }

    [Fact]
    public async Task A_cancelled_plan_is_not_late_either()
    {
        using var t = Configured();
        Item(t, "IT-D");
        Plan(t, Today.AddDays(-5), ReceivingStatus.Cancelled, "PO-CANCEL");

        Assert.Equal(0, (await BlockAsync(t, AlertType.ReceivingDelay)).Total);
    }

    [Fact]
    public async Task A_plan_with_no_scheduled_date_is_not_late()
    {
        using var t = Configured();
        Item(t, "IT-D");
        Plan(t, null, ReceivingStatus.NotArrived, "PO-NODATE");

        Assert.Equal(0, (await BlockAsync(t, AlertType.ReceivingDelay)).Total);
    }

    [Fact]
    public async Task The_grace_period_comes_from_the_config_row()
    {
        using var t = Configured((AlertType.ReceivingDelay, 1m));
        Item(t, "IT-D");
        Plan(t, Today.AddDays(-1), ReceivingStatus.NotArrived, "PO-1DAY");

        Assert.Equal(0, (await BlockAsync(t, AlertType.ReceivingDelay)).Total);
    }

    [Fact]
    public async Task Picking_and_shipping_use_their_own_done_statuses()
    {
        using var t = Configured();
        Item(t, "IT-D");
        t.Db.PickingPlans.Add(new PickingPlan
        {
            SoNo = "SO-P", SoBranchNo = "01", ItemCode = "IT-D", LocationCode = "W1_ZA", ScheduledDate = Today.AddDays(-2), Qty = 5,
            Status = PickingStatus.NotPicked, CreatedAt = DateTime.UtcNow,
        });
        t.Db.PickingPlans.Add(new PickingPlan
        {
            SoNo = "SO-DONE", SoBranchNo = "01", ItemCode = "IT-D", LocationCode = "W1_ZA", ScheduledDate = Today.AddDays(-2), Qty = 5,
            Status = PickingStatus.Picked, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ShippingPlans.Add(new ShippingPlan
        {
            SoNo = "SO-S", SoBranchNo = "01", ItemCode = "IT-D", FactoryCode = "FAC-01",
            ScheduledDate = Today.AddDays(-2), Qty = 5,
            Status = ShippingStatus.Partial, CreatedAt = DateTime.UtcNow,
        });

        Assert.Equal("SO-P", Assert.Single((await BlockAsync(t, AlertType.PickingDelay)).Rows).Extra);
        Assert.Equal("SO-S", Assert.Single((await BlockAsync(t, AlertType.ShippingDelay)).Rows).Extra);
    }


    [Fact]
    public async Task The_count_is_the_real_total_even_when_the_list_is_trimmed()
    {
        using var t = Configured();
        Item(t, "IT-LOW", reorder: 10m);
        for (var i = 0; i < 60; i++) Stock(t, "IT-LOW", $"SN-{i:D3}", 1);

        var b = await BlockAsync(t, AlertType.LowStock);
        Assert.Equal(60, b.Total);
        Assert.Equal(50, b.Rows.Count);
    }
}
