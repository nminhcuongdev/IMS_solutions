using Microsoft.EntityFrameworkCore;
using Ims.Api.Features.History;
using Ims.Api.Features.Ht;
using Ims.Api.Features.Inventory;
using Ims.Api.Features.Picking;
using Ims.Api.Features.Print;
using Ims.Api.Features.Receiving;
using Ims.Api.Features.Returns;
using Ims.Api.Features.Shipping;
using Ims.Api.Features.Stocktaking;
using Ims.Api.Tests.Stocktaking;
using Ims.Domain.Stocktaking;
using Xunit;
using static Ims.Api.Tests.Sorting.SortAssert;

namespace Ims.Api.Tests.Sorting;

public class ColumnSortTests
{
    private static readonly string[] PlanStatusOrder =
        { "statusNotArrived", "statusPartial", "statusArrived", "statusCancelled" };
    private static readonly string[] ShipStatusOrder =
        { "statusNotShipped", "statusPartialShip", "statusShipped", "statusCancelled" };

    private static int Rank(string[] order, string status) => Array.IndexOf(order, status);

    [Fact]
    public async Task Receiving_plans_sort_on_every_column()
    {
        using var t = SortSeed.Receiving();
        var svc = new ReceivingService(t.Db, new LotLookup(t.Db));
        Task<Contracts.ServiceResult> List(string s) => svc.ListPlansAsync(default, sort: s);

        await SortsAsync<ReceivingPlanDto>(List, "no", d => d.PoNo);
        await SortsAsync<ReceivingPlanDto>(List, "item_name", d => d.ItemName);
        await SortsAsync<ReceivingPlanDto>(List, "unit", d => d.Unit);
        await SortsAsync<ReceivingPlanDto>(List, "packing_type", d => d.PackingTypeCode);
        await SortsAsync<ReceivingPlanDto>(List, "packing_qty", d => d.PackingQty);
        await SortsAsync<ReceivingPlanDto>(List, "shortage_reason", d => d.ShortageReason);
        await SortsAsync<ReceivingPlanDto>(List, "created_by", d => d.CreatedBy);
        await SortsAsync<ReceivingPlanDto>(List, "status", d => Rank(PlanStatusOrder, d.Status));
    }

    [Fact]
    public async Task Receiving_results_sort_on_every_column()
    {
        using var t = SortSeed.Receiving();
        var svc = new ReceivingService(t.Db, new LotLookup(t.Db));
        Task<Contracts.ServiceResult> List(string s) => svc.ListResultsAsync(default, sort: s);

        await SortsAsync<ReceivingResultDto>(List, "no", d => d.No);
        await SortsAsync<ReceivingResultDto>(List, "item_name", d => d.ItemName);
        await SortsAsync<ReceivingResultDto>(List, "unit", d => d.Unit);
        await SortsAsync<ReceivingResultDto>(List, "serial_no", d => d.SerialNo);
        await SortsAsync<ReceivingResultDto>(List, "invoice_no", d => d.InvoiceNo);
        await SortsAsync<ReceivingResultDto>(List, "worker", d => d.Worker);
        await SortsAsync<ReceivingResultDto>(List, "status", d => Rank(PlanStatusOrder, d.Status));
        await SortsAsync<ReceivingResultDto>(List, "shortage_reason", d => d.ShortageReason);
    }

    [Fact]
    public async Task Shipping_plans_sort_on_every_column()
    {
        using var t = SortSeed.Shipping();
        var svc = new ShippingService(t.Db);
        Task<Contracts.ServiceResult> List(string s) => svc.ListPlansAsync(default, sort: s);

        await SortsAsync<ShippingPlanDto>(List, "no", d => d.SlipNo);
        await SortsAsync<ShippingPlanDto>(List, "item_name", d => d.ItemName);
        await SortsAsync<ShippingPlanDto>(List, "unit", d => d.Unit);
        await SortsAsync<ShippingPlanDto>(List, "packing_type", d => d.PackingTypeCode);
        await SortsAsync<ShippingPlanDto>(List, "packing_qty", d => d.PackingQty);
        await SortsAsync<ShippingPlanDto>(List, "invoice_no", d => d.InvoiceNo);
        await SortsAsync<ShippingPlanDto>(List, "shortage_reason", d => d.ShortageReason);
        await SortsAsync<ShippingPlanDto>(List, "created_by", d => d.CreatedBy);
    }

    [Fact]
    public async Task Shipping_results_sort_on_every_column()
    {
        using var t = SortSeed.Shipping();
        var svc = new ShippingService(t.Db);
        Task<Contracts.ServiceResult> List(string s) => svc.ListResultsAsync(default, sort: s);

        await SortsAsync<ShippingResultDto>(List, "no", d => d.No);
        await SortsAsync<ShippingResultDto>(List, "item_name", d => d.ItemName);
        await SortsAsync<ShippingResultDto>(List, "unit", d => d.Unit);
        await SortsAsync<ShippingResultDto>(List, "serial_no", d => d.SerialNo);
        await SortsAsync<ShippingResultDto>(List, "scheduled_date", d => d.ScheduledDate);
        await SortsAsync<ShippingResultDto>(List, "invoice_no", d => d.InvoiceNo);
        await SortsAsync<ShippingResultDto>(List, "worker", d => d.Worker);
        await SortsAsync<ShippingResultDto>(List, "status", d => Rank(ShipStatusOrder, d.Status));
        await SortsAsync<ShippingResultDto>(List, "shortage_reason", d => d.ShortageReason);
        await SortsAsync<ShippingResultDto>(List, "expiry_date", d => d.ExpiryDate);
    }

    [Fact]
    public async Task Return_plans_sort_on_every_column()
    {
        using var t = SortSeed.Returns();
        var svc = new ReturnsService(t.Db);
        Task<Contracts.ServiceResult> List(string s) => svc.ListPlansAsync(default, sort: s);

        await SortsAsync<ReturnPlanDto>(List, "no", d => d.ReturnNo);
        await SortsAsync<ReturnPlanDto>(List, "item_name", d => d.ItemName);
        await SortsAsync<ReturnPlanDto>(List, "unit", d => d.Unit);
        await SortsAsync<ReturnPlanDto>(List, "shortage_reason", d => d.ShortageReason);
        await SortsAsync<ReturnPlanDto>(List, "created_by", d => d.CreatedBy);
    }

    [Fact]
    public async Task Picking_plans_and_results_sort_on_every_column()
    {
        using var t = SortSeed.Picking();
        var svc = new PickingService(t.Db);
        Task<Contracts.ServiceResult> Plans(string s) => svc.ListPlansAsync(default, sort: s);
        Task<Contracts.ServiceResult> Results(string s) => svc.ListResultsAsync(default, sort: s);

        await SortsAsync<PickingPlanDto>(Plans, "no", d => d.SoNo);
        await SortsAsync<PickingPlanDto>(Plans, "item_name", d => d.ItemName);
        await SortsAsync<PickingPlanDto>(Plans, "unit", d => d.Unit);
        await SortsAsync<PickingPlanDto>(Plans, "created_by", d => d.CreatedBy);

        await SortsAsync<PickingResultDto>(Results, "no", d => d.No);
        await SortsAsync<PickingResultDto>(Results, "item_name", d => d.ItemName);
        await SortsAsync<PickingResultDto>(Results, "unit", d => d.Unit);
        await SortsAsync<PickingResultDto>(Results, "serial_no", d => d.SerialNo);
        await SortsAsync<PickingResultDto>(Results, "scheduled_date", d => d.ScheduledDate);
        await SortsAsync<PickingResultDto>(Results, "worker", d => d.Worker);
    }

    [Fact]
    public async Task Print_and_rfid_history_sort_on_item_name()
    {
        using var t = SortSeed.History();
        var svc = new HistoryService(t.Db, null!, new PrintTargetResolver(t.Db));

        await SortsAsync<PrintHistoryDto>(s => svc.ListPrintAsync(default, sort: s), "item_name", d => d.ItemName);
        await SortsAsync<RfidGateHistoryDto>(s => svc.ListRfidAsync(default, sort: s), "item_name", d => d.ItemName);
    }

    [Fact]
    public async Task Stock_sorts_on_every_column()
    {
        using var t = SortSeed.Stock();
        var svc = new InventoryService(t.Db);
        Task<Contracts.ServiceResult> List(string s) => svc.ListStockAsync(default, sort: s);

        await SortsAsync<StockDto>(List, "no", d => d.ItemCode + "/" + d.SerialNo);
        await SortsAsync<StockDto>(List, "item_name", d => d.ItemName);
        await SortsAsync<StockDto>(List, "category", d => d.Category);
        await SortsAsync<StockDto>(List, "warehouse", d => d.Warehouse);
        await SortsAsync<StockDto>(List, "hold", d => d.IsOnHold);
        await SortsAsync<StockDto>(List, "hold_reason", d => d.HoldReason);
        await SortsAsync<StockDto>(List, "unit", d => d.Unit);
        await SortsAsync<StockDto>(List, "reorder_point", d => d.ReorderPoint);
        await SortsAsync<StockDto>(List, "over_short", d => d.OverShort);
        await SortsAsync<StockDto>(List, "remaining_days", d => d.RemainingDays);
    }

    [Fact]
    public async Task Grouped_stock_sorts_on_every_column()
    {
        using var t = SortSeed.Stock();
        var svc = new InventoryService(t.Db);
        Task<Contracts.ServiceResult> ByItem(string s) => svc.ListStockAsync(default, sort: s, groupBy: "item");
        Task<Contracts.ServiceResult> ByLoc(string s) => svc.ListStockAsync(default, sort: s, groupBy: "location");

        await SortsAsync<StockGroupDto>(ByItem, "no", d => d.Key);
        await SortsAsync<StockGroupDto>(ByItem, "item_code", d => d.Key);
        await SortsAsync<StockGroupDto>(ByItem, "item_name", d => d.Name);
        await SortsAsync<StockGroupDto>(ByItem, "category", d => d.Category);
        await SortsAsync<StockGroupDto>(ByItem, "stock_qty", d => d.StockQty);
        await SortsAsync<StockGroupDto>(ByItem, "unit", d => d.Unit);
        await SortsAsync<StockGroupDto>(ByItem, "lot_count", d => d.LotCount);

        await SortsAsync<StockGroupDto>(ByLoc, "no", d => d.Key);
        await SortsAsync<StockGroupDto>(ByLoc, "location_code", d => d.Key);
        await SortsAsync<StockGroupDto>(ByLoc, "warehouse", d => d.Warehouse);
        await SortsAsync<StockGroupDto>(ByLoc, "item_count", d => d.ItemCount);
        await SortsAsync<StockGroupDto>(ByLoc, "stock_qty", d => d.StockQty);
    }

    [Fact]
    public async Task Work_history_sorts_on_every_column()
    {
        using var t = SortSeed.Stock();
        var svc = new InventoryService(t.Db);
        Task<Contracts.ServiceResult> List(string s) => svc.ListHistoryAsync(default, sort: s);

        await SortsAsync<InventoryHistoryDto>(List, "no", d => d.No);
        await SortsAsync<InventoryHistoryDto>(List, "item_name", d => d.ItemName);
        await SortsAsync<InventoryHistoryDto>(List, "lot_no", d => d.LotNo);
        await SortsAsync<InventoryHistoryDto>(List, "unit", d => d.Unit);
        await SortsAsync<InventoryHistoryDto>(List, "location_code", d => d.LocationCode);
        await SortsAsync<InventoryHistoryDto>(List, "reason", d => d.ReasonName);
        await SortsAsync<InventoryHistoryDto>(List, "worker", d => d.Worker);
    }

    [Fact]
    public async Task Stocktaking_labels_and_groups_sort_on_every_column()
    {
        using var t = StocktakingSessionTests.Warehouse();
        await StocktakingSessionTests.StartAsync(t);
        var b = await t.Db.StocktakingLabels.SingleAsync(x => x.ItemCode == "ITM-B");
        b.IsScanned = true;
        b.Status = StocktakingLabelStatus.Normal;
        b.UpdatedAt = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var svc = new StocktakingService(t.Db);
        Task<Contracts.ServiceResult> Labels(string s) => svc.ListLabelsAsync(default, view: "label", sort: s);
        Task<Contracts.ServiceResult> Groups(string s) => svc.ListLabelsAsync(default, view: "model", sort: s);

        await SortsAsync<StocktakingLabelDto>(Labels, "no", d => d.ItemCode + "/" + d.SerialNo);
        await SortsAsync<StocktakingLabelDto>(Labels, "serial_no", d => d.SerialNo);
        await SortsAsync<StocktakingLabelDto>(Labels, "item_code", d => d.ItemCode);
        await SortsAsync<StocktakingLabelDto>(Labels, "item_name", d => d.ItemName);
        await SortsAsync<StocktakingLabelDto>(Labels, "qty", d => d.Qty);
        await SortsAsync<StocktakingLabelDto>(Labels, "location_code", d => d.LocationCode);
        await SortsAsync<StocktakingLabelDto>(Labels, "lot_no", d => d.LotNo);
        await SortsAsync<StocktakingLabelDto>(Labels, "status", d => d.Status);
        await SortsAsync<StocktakingLabelDto>(Labels, "is_scanned", d => d.IsScanned);
        await SortsAsync<StocktakingLabelDto>(Labels, "updated_at", d => d.UpdatedAt);

        await SortsAsync<StocktakingGroupDto>(Groups, "item_code", d => d.ItemCode);
        await SortsAsync<StocktakingGroupDto>(Groups, "item_name", d => d.ItemName);
        await SortsAsync<StocktakingGroupDto>(Groups, "location_code", d => d.LocationText);
        await SortsAsync<StocktakingGroupDto>(Groups, "lot_no", d => d.LotNo);
        await SortsAsync<StocktakingGroupDto>(Groups, "planned_qty", d => d.PlannedQty);
        await SortsAsync<StocktakingGroupDto>(Groups, "result_qty", d => d.ResultQty);
        await SortsAsync<StocktakingGroupDto>(Groups, "updated_at", d => d.UpdatedAt);
    }

    [Fact]
    public async Task Stocktaking_history_sorts_on_every_column()
    {
        using var t = SortSeed.Masters();
        t.Db.StocktakingHistories.Add(new StocktakingHistory
        {
            ItemCode = "IT-1", LotNo = "L1", StocktakingName = "B session",
            StocktakingDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            PlannedQty = 5, ResultQty = 5, MissingQty = 0,
        });
        t.Db.StocktakingHistories.Add(new StocktakingHistory
        {
            ItemCode = "IT-2", LotNo = "L2", StocktakingName = "A session",
            StocktakingDate = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
            PlannedQty = 3, ResultQty = 1, MissingQty = 2,
        });
        await t.Db.SaveChangesAsync();

        var svc = new StocktakingService(t.Db);
        Task<Contracts.ServiceResult> List(string s) => svc.ListHistoryAsync(default, sort: s);

        await SortsAsync<StocktakingHistoryDto>(List, "no", d => d.ItemCode);
        await SortsAsync<StocktakingHistoryDto>(List, "stocktaking_name", d => d.StocktakingName);
        await SortsAsync<StocktakingHistoryDto>(List, "stocktaking_date", d => d.StocktakingDate);
        await SortsAsync<StocktakingHistoryDto>(List, "item_code", d => d.ItemCode);
        await SortsAsync<StocktakingHistoryDto>(List, "item_name", d => d.ItemName);
        await SortsAsync<StocktakingHistoryDto>(List, "lot_no", d => d.LotNo);
        await SortsAsync<StocktakingHistoryDto>(List, "unit", d => d.Unit);
        await SortsAsync<StocktakingHistoryDto>(List, "planned_qty", d => d.PlannedQty);
        await SortsAsync<StocktakingHistoryDto>(List, "result_qty", d => d.ResultQty);
        await SortsAsync<StocktakingHistoryDto>(List, "missing_qty", d => d.MissingQty);
        await SortsAsync<StocktakingHistoryDto>(List, "status", d => d.BadgeKey);
    }
}
