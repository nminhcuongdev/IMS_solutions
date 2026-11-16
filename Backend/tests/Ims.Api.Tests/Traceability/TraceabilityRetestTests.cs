using System.Globalization;
using Ims.Api.Contracts;
using Ims.Api.Features.Traceability;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Production;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Traceability;

public class TraceabilityRetestTests
{
    private static readonly DateTime Day = new(2026, 11, 4, 9, 0, 0);

    internal static async Task<TestDb> SeedAsync()
    {
        var t = TestDb.Seeded();
        var db = t.Db;
        db.ItemGroups.Add(new ItemGroup { ItemGroupCode = "FG", ItemGroupName = "Finished", Category = ItemGroupCategory.FinishedGoods, CreatedAt = Day });
        db.Items.AddRange(
            new Item { ItemCode = "MAT-A", ItemName = "Material A", ItemGroupCode = "IG-01", UomCode = "kg", IsLotManaged = true, CreatedAt = Day },
            new Item { ItemCode = "MAT-B", ItemName = "Material B", ItemGroupCode = "IG-01", UomCode = "kg", IsLotManaged = true, CreatedAt = Day },
            new Item { ItemCode = "FG1", ItemName = "Finished", ItemGroupCode = "FG", UomCode = "pcs", IsLotManaged = true, CreatedAt = Day });
        db.Partners.AddRange(
            new Partner { PartnerCode = "SUP-A", PartnerName = "Supplier A", IsSupplier = true, CreatedAt = Day },
            new Partner { PartnerCode = "SUP-B", PartnerName = "Supplier B", IsSupplier = true, CreatedAt = Day });
        db.Processes.Add(new Ims.Domain.Masters.Process { ProcessCode = "MIX", ProcessName = "Mix", CreatedAt = Day });
        db.Reasons.Add(new Reason { ReasonCode = "ADJ", ReasonName = "Correction", ReasonCategoryValue = ReasonCategory.Adjustment, CreatedAt = Day });
        db.ReceivingPlans.AddRange(
            new ReceivingPlan { PoNo = "PO-1", PoBranchNo = "01", ItemCode = "MAT-A", PartnerCode = "SUP-A", Qty = 10, CreatedAt = Day },
            new ReceivingPlan { PoNo = "PO-2", PoBranchNo = "01", ItemCode = "MAT-B", PartnerCode = "SUP-B", Qty = 10, CreatedAt = Day });
        db.ReceivingResults.AddRange(
            new ReceivingResult { PoNo = "PO-1", PoBranchNo = "01", ItemCode = "MAT-A", LotNo = "LOT-A", Qty = 10, LocationCode = "W1_ZA", CreatedAt = Day },
            new ReceivingResult { PoNo = "PO-2", PoBranchNo = "01", ItemCode = "MAT-B", LotNo = "LOT-B", Qty = 10, LocationCode = "W1_ZA", CreatedAt = Day });
        db.ProductionOrders.Add(new ProductionOrder { ProductionOrderNo = "MO1", FactoryCode = "FAC-01", OutputItemCode = "FG1",
            StorageLocationCode = "W1_ZA", OutputLotNo = "FGLOT", PlannedQty = 5, UomCode = "pcs", CreatedBy = "admin" });
        db.ProductionOrderLines.AddRange(
            new ProductionOrderLine { ProductionOrderNo = "MO1", LineNo = 1, ProcessCode = "MIX", ItemCode = "MAT-A", PlannedQty = 4, UomCode = "kg", CreatedBy = "admin" },
            new ProductionOrderLine { ProductionOrderNo = "MO1", LineNo = 2, ProcessCode = "MIX", ItemCode = "MAT-B", PlannedQty = 2, UomCode = "kg", CreatedBy = "admin" });
        db.ProductionResults.AddRange(
            new ProductionResult { ResultType = ProductionResultType.Consume, RequestId = "c1", ProductionOrderNo = "MO1", LineNo = 1, ItemCode = "MAT-A", LotNo = "LOT-A", SerialNo = "SN-A",
                Qty = 4, LocationCode = "W1_ZA", FactoryCode = "FAC-01", ExpiryDate = new DateOnly(2027, 1, 31), CreatedBy = "U001", CreatedAt = Day.AddHours(1) },
            new ProductionResult { ResultType = ProductionResultType.Consume, RequestId = "c2", ProductionOrderNo = "MO1", LineNo = 2, ItemCode = "MAT-B", LotNo = "LOT-B", SerialNo = "SN-B",
                Qty = 2, LocationCode = "W1_ZA", FactoryCode = "FAC-01", CreatedBy = "U001", CreatedAt = Day.AddHours(1) });
        db.ProductionResults.Add(new ProductionResult { ResultType = ProductionResultType.Output, FactoryCode = "FAC-01", RequestId = "o1", ProductionOrderNo = "MO1", ItemCode = "FG1", LotNo = "FGLOT",
            SerialNo = "PRD-1-1", Qty = 5, LocationCode = "W1_ZA", CreatedBy = "U003", CreatedAt = Day.AddHours(2) });
        db.Stocks.AddRange(
            Stock("MAT-A", "SN-A", "LOT-A", 6),
            Stock("MAT-B", "SN-B", "LOT-B", 8),
            Stock("FG1", "PRD-1-1", "FGLOT", 5));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return t;
    }

    private static Stock Stock(string item, string serial, string lot, decimal qty)
    {
        var s = new Stock { ItemCode = item, SerialNo = serial, FactoryCode = "FAC-01", LocationCode = "W1_ZA", LotNo = lot, StockQty = qty, UpdatedAt = Day };
        s.Recalculate();
        return s;
    }

    private static async Task AddSecondSupplierAsync(TestDb t)
    {
        t.Db.ReceivingPlans.Add(new ReceivingPlan { PoNo = "PO-3", PoBranchNo = "01", ItemCode = "MAT-A", PartnerCode = "SUP-B", Qty = 5, CreatedAt = Day });
        t.Db.ReceivingResults.Add(new ReceivingResult { PoNo = "PO-3", PoBranchNo = "01", ItemCode = "MAT-A", LotNo = "LOT-A", Qty = 5, LocationCode = "W1_ZA", CreatedAt = Day.AddHours(3) });
        t.Db.Stocks.Add(Stock("MAT-A", "SN-A2", "LOT-A", 5));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Materials_from_different_suppliers_are_not_a_multiple_supplier_lot()
    {
        using var t = await SeedAsync();
        var trace = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(
            await new TraceabilityService(t.Db).BackwardAsync("FG1", "FGLOT", default)).Data!;

        Assert.False(trace.MultipleSuppliers);
        Assert.All(trace.Nodes, n => Assert.Null(n.Suppliers));
    }

    [Fact]
    public async Task One_lot_from_two_suppliers_is_flagged_and_lists_both_in_either_direction()
    {
        using var t = await SeedAsync();
        await AddSecondSupplierAsync(t);
        var service = new TraceabilityService(t.Db);

        var forward = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(await service.ForwardAsync("MAT-A", "LOT-A", default)).Data!;
        Assert.True(forward.MultipleSuppliers);
        Assert.Equal(["Supplier A (PO-1)", "Supplier B (PO-3)"], forward.Nodes[0].Suppliers);

        var backward = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(await service.BackwardAsync("FG1", "FGLOT", default)).Data!;
        Assert.False(backward.MultipleSuppliers);
        var material = Assert.Single(backward.Nodes, n => n.ItemCode == "MAT-A" && n.Type == "lot");
        Assert.Equal(["Supplier A (PO-1)", "Supplier B (PO-3)"], material.Suppliers);
        Assert.Null(Assert.Single(backward.Nodes, n => n.ItemCode == "MAT-B" && n.Type == "lot").Suppliers);
    }

    [Fact]
    public async Task Timeline_keeps_a_reversed_row_beside_its_reversal_so_it_adds_up_to_stock()
    {
        using var t = await SeedAsync();
        var original = new ProductionResult { ResultType = ProductionResultType.Consume, RequestId = "c3", ProductionOrderNo = "MO1", LineNo = 2, ItemCode = "MAT-B", LotNo = "LOT-B",
            SerialNo = "SN-B", Qty = 1, LocationCode = "W1_ZA", FactoryCode = "FAC-01", CreatedBy = "U001", CreatedAt = Day.AddHours(4) };
        t.Db.ProductionResults.Add(original);
        await t.Db.SaveChangesAsync();
        t.Db.ProductionResults.Add(new ProductionResult { ResultType = ProductionResultType.Consume, RequestId = "REV-c3", ProductionOrderNo = "MO1", LineNo = 2, ItemCode = "MAT-B", LotNo = "LOT-B",
            SerialNo = "SN-B", Qty = 1, LocationCode = "W1_ZA", FactoryCode = "FAC-01", ReversesNo = original.No, ReasonCode = "ADJ",
            CreatedBy = "admin", CreatedAt = Day.AddHours(5) });
        t.Db.InventoryHistories.Add(new InventoryHistory { ItemCode = "MAT-B", SerialNo = "SN-B", FactoryCode = "FAC-01", LocationCode = "W1_ZA",
            LotNo = "LOT-B", Qty = 1, ActionType = InventoryAction.Adjust, ReasonCode = "ADJ", CreatedAt = Day.AddHours(5), CreatedBy = "admin" });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var events = ResultAssert.Ok<ApiListSuccess<TraceEventDto>>(
            await new TraceabilityService(t.Db).EventsAsync("MAT-B", "LOT-B", default)).Data;

        Assert.Equal(8m, events.Sum(e => e.Qty));
        Assert.Equal(2, events.Count(e => e.Type == "consumption"));
    }

    [Fact]
    public async Task Forward_lot_search_by_po_returns_only_lots_received_on_that_po()
    {
        using var t = await SeedAsync();
        var rows = ResultAssert.Ok<ApiListSuccess<TraceLotDto>>(await new TraceabilityService(t.Db)
            .LotsAsync(new TraceQueryDto(Direction: "forward", PoNo: "PO-2"), default)).Data;

        var only = Assert.Single(rows);
        Assert.Equal(("MAT-B", "LOT-B", "PO-2", "MO1"), (only.ItemCode, only.LotNo, only.PoNo, only.OrderNo));
    }

    [Fact]
    public async Task Forward_lot_search_reports_everything_received_for_the_lot()
    {
        using var t = await SeedAsync();
        await AddSecondSupplierAsync(t);
        var rows = ResultAssert.Ok<ApiListSuccess<TraceLotDto>>(await new TraceabilityService(t.Db)
            .LotsAsync(new TraceQueryDto(Direction: "forward", ItemCode: "MAT-A"), default)).Data;

        Assert.Equal(15m, Assert.Single(rows).Qty);
    }

    [Fact]
    public async Task Export_has_a_bom_iso_expiry_and_the_worker()
    {
        using var t = await SeedAsync();
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            var result = await new TraceabilityService(t.Db).ExportAsync("backward", "FG1", "FGLOT", default);
            var bytes = result.File!.Contents;
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
            var lines = (await CsvReader.ReadAsync(result)).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var material = Assert.Single(lines, l => l.Contains("\"MAT-A\"") && l.Contains("\"lot\""));
            Assert.Contains(",\"2027-01-31\",", material);
            Assert.Contains(",\"U001\",", material);
            var output = lines[1];
            Assert.Contains(",\"U003\",", output);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
