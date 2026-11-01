using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Traceability;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Ims.Domain.Shipping;
using Ims.Domain.Production;
using Xunit;

namespace Ims.Api.Tests.Traceability;

public class TraceabilityServiceTests
{
    private static async Task<TestDb> SeedAsync()
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 10, 1);
        t.Db.Items.Add(new Item { ItemCode = "IT-01", ItemName = "Part", ItemGroupCode = "IG-01", CreatedAt = now });
        t.Db.Partners.AddRange(
            new Partner { PartnerCode = "SUP-A", PartnerName = "Supplier A", IsSupplier = true, CreatedAt = now },
            new Partner { PartnerCode = "CUST-A", PartnerName = "Customer A", IsCustomer = true, CreatedAt = now });
        t.Db.ReceivingPlans.Add(new ReceivingPlan { PoNo = "PO-1", PoBranchNo = "01", ItemCode = "IT-01",
            PartnerCode = "SUP-A", Qty = 10, CreatedAt = now });
        t.Db.ReceivingResults.Add(new ReceivingResult { PoNo = "PO-1", PoBranchNo = "01", ItemCode = "IT-01",
            LotNo = "LOT-A", Qty = 10, LocationCode = "W1_ZA", CreatedAt = now });
        t.Db.ShippingPlans.Add(new ShippingPlan { SoNo = "SO-1", SoBranchNo = "01", ItemCode = "IT-01",
            PartnerCode = "CUST-A", Qty = 4, CreatedAt = now.AddDays(1) });
        var result = new ShippingResult { SoNo = "SO-1", SoBranchNo = "01", ItemCode = "IT-01",
            LotNo = "LOT-A", Qty = 4, CreatedAt = now.AddDays(1) };
        t.Db.ShippingResults.Add(result);
        t.Db.ShippingResultLots.Add(new ShippingResultLot { ShippingResult = result, ItemCode = "IT-01",
            LotNo = "LOT-A", SerialNo = "SN-1", Qty = 4, CreatedAt = now.AddDays(1), CreatedBy = "U001" });
        var stock = new Stock { ItemCode = "IT-01", SerialNo = "SN-1", FactoryCode = "FAC-01",
            LocationCode = "W1_ZA", LotNo = "LOT-A", StockQty = 6, UpdatedAt = now };
        stock.Recalculate();
        t.Db.Stocks.Add(stock);
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    [Fact]
    public async Task Forward_trace_uses_shipping_child_and_reconciles_stock()
    {
        using var t = await SeedAsync();
        var service = new TraceabilityService(t.Db);

        var trace = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(
            await service.ForwardAsync("IT-01", "LOT-A", default)).Data!;
        var shipment = Assert.Single(trace.Nodes.Where(n => n.Type == "shipping"));
        Assert.Equal(4m, shipment.Qty);
        Assert.Equal("CUST-A", shipment.PartnerCode);
        Assert.True(trace.Balance.Ok);
        Assert.Equal(6m, trace.Balance.StockQty);
    }

    [Fact]
    public async Task Lot_search_filters_by_sales_order_and_customer()
    {
        using var t = await SeedAsync();
        var service = new TraceabilityService(t.Db);

        var result = ResultAssert.Ok<ApiListSuccess<TraceLotDto>>(
            await service.LotsAsync(new TraceQueryDto(SoNo: "SO-1", Customer: "CUST-A"), default));
        var lot = Assert.Single(result.Data);
        Assert.Equal("LOT-A", lot.LotNo);
        Assert.Equal(4m, lot.Qty);
    }

    [Fact]
    public async Task Backward_trace_reaches_the_receiving_supplier()
    {
        using var t = await SeedAsync();
        var trace = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(
            await new TraceabilityService(t.Db).BackwardAsync("IT-01", "LOT-A", default)).Data!;

        Assert.Contains(trace.Nodes, n => n.Type == "receipt" && n.DocumentNo == "PO-1"
            && n.PartnerCode == "SUP-A");
    }

    [Fact]
    public async Task Production_links_material_to_output_and_reconciles_net_return()
    {
        using var t = await SeedAsync();
        var now = new DateTime(2026, 10, 2);
        t.Db.ItemGroups.Add(new ItemGroup { ItemGroupCode = "FG", ItemGroupName = "Finished", Category = ItemGroupCategory.FinishedGoods, CreatedAt = now });
        t.Db.Items.Add(new Item { ItemCode = "FG1", ItemName = "Finished", ItemGroupCode = "FG", IsLotManaged = true, CreatedAt = now });
        t.Db.Processes.Add(new Ims.Domain.Masters.Process { ProcessCode = "CUT", ProcessName = "Cut", CreatedAt = now });
        t.Db.ProductionOrders.Add(new ProductionOrder { ProductionOrderNo = "MO1", FactoryCode = "FAC-01", OutputItemCode = "FG1", StorageLocationCode = "W1_ZA", OutputLotNo = "FGLOT", PlannedQty = 1, UomCode = "pcs", CreatedBy = "admin" });
        t.Db.ProductionOrderLines.Add(new ProductionOrderLine { ProductionOrderNo = "MO1", LineNo = 1, ProcessCode = "CUT", ItemCode = "IT-01", PlannedQty = 2, UomCode = "pcs", CreatedBy = "admin" });
        var consumption = new ProductionConsumption { RequestId = "c1", ProductionOrderNo = "MO1", LineNo = 1, ItemCode = "IT-01", LotNo = "LOT-A", SerialNo = "SN-1", Qty = 2, LocationCode = "W1_ZA", FactoryCode = "FAC-01", CreatedBy = "admin", CreatedAt = now };
        t.Db.ProductionConsumptions.Add(consumption);
        t.Db.ProductionOutputs.Add(new ProductionOutput { RequestId = "o1", ProductionOrderNo = "MO1", ItemCode = "FG1", LotNo = "FGLOT", SerialNo = "PRD-1-1", Qty = 1, LocationCode = "W1_ZA", CreatedBy = "admin", CreatedAt = now });
        t.Db.Stocks.Add(new Stock { ItemCode = "FG1", SerialNo = "PRD-1-1", FactoryCode = "FAC-01", LocationCode = "W1_ZA", LotNo = "FGLOT", StockQty = 1, AvailableQty = 1, UpdatedAt = now });
        t.Db.Stocks.Single(x => x.ItemCode == "IT-01").StockQty = 5;
        t.Db.Stocks.Single(x => x.ItemCode == "IT-01").Recalculate();
        await t.Db.SaveChangesAsync();
        t.Db.ProductionReturns.Add(new ProductionReturn { RequestId = "r1", ProductionOrderNo = "MO1", LineNo = 1, ConsumptionNo = consumption.No, ItemCode = "IT-01", LotNo = "LOT-A", SerialNo = "SN-1", Qty = 1, LocationCode = "W1_ZA", FactoryCode = "FAC-01", CreatedBy = "admin", CreatedAt = now });
        await t.Db.SaveChangesAsync();
        var service = new TraceabilityService(t.Db);
        var forward = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(await service.ForwardAsync("IT-01", "LOT-A", default)).Data!;
        Assert.Contains(forward.Nodes, x => x.Type == "lot-made" && x.LotNo == "FGLOT" && x.ProductionLotNo == null);
        Assert.Contains(forward.Nodes, x => x.Type == "process" && x.ProcessName == "Cut");
        Assert.True(forward.Balance.Ok);
        var backward = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(await service.BackwardAsync("FG1", "FGLOT", default)).Data!;
        Assert.Equal("lot-made", backward.Nodes[0].Type);
        Assert.Equal("Finished", backward.Nodes[0].ItemName);
        Assert.Single(backward.Nodes, x => x.Type == "process");
        Assert.Contains(backward.Nodes, x => x.Type == "receipt" && x.Level > 1 && x.PartnerCode == "SUP-A");
        Assert.Contains(backward.Nodes, x => x.Type == "lot" && x.ItemCode == "IT-01" && x.Qty == 1 && x.ReturnedQty == 1 && x.ExpiryState != null);
        Assert.True(backward.Balance.Ok);
        var byOrder = ResultAssert.Ok<ApiListSuccess<TraceLotDto>>(
            await service.LotsAsync(new TraceQueryDto(OrderNo: "MO1"), default)).Data;
        var material = Assert.Single(ResultAssert.Ok<ApiListSuccess<TraceLotDto>>(
            await service.LotsAsync(new TraceQueryDto(Direction: "forward", MaterialLotNo: "LOT-A"), default)).Data);
        Assert.Equal(("PO-1", "MO1"), (material.PoNo, material.OrderNo));
        Assert.DoesNotContain(forward.Nodes, x => x.Type == "receipt");
        var only = Assert.Single(byOrder);
        Assert.Equal(("FG1", "FGLOT", "MO1"), (only.ItemCode, only.LotNo, only.OrderNo));
    }
}
