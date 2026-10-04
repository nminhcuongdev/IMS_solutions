using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Inventory;
using Ims.Api.Tests.Infrastructure;
using Xunit;

namespace Ims.Api.Tests.Inventory;

public class TransferTests
{
    [Fact]
    public async Task Transfer_moves_the_row_and_writes_where_it_came_from()
    {
        using var t = await InventoryTestData.WithStockAsync(
            InventoryTestData.Stock(location: "W1_ZA", qty: 40, lot: "L-A"));

        var r = await new InventoryService(t.Db).TransferStockAsync(new StockTransferDto("IT-01", "0000000001", "W2_ZB"), InventoryTestData.User(), default);

        ResultAssert.Ok<ApiSuccess<StockDto>>(r);

        var row = await t.Db.Stocks.SingleAsync();
        Assert.Equal("W2_ZB", row.LocationCode);

        var log = await t.Db.LocationTransfers.SingleAsync();
        Assert.Equal("W1_ZA", log.FromLocationCode);
        Assert.Equal("W2_ZB", log.ToLocationCode);
        Assert.Equal("L-A", log.LotNo);
        Assert.Equal(40m, log.Qty);
        Assert.Equal("admin", log.CreatedBy);
    }

    [Fact]
    public async Task Transfer_leaves_the_quantity_untouched()
    {
        using var t = await InventoryTestData.WithStockAsync(
            InventoryTestData.Stock(location: "W1_ZA", qty: 40));

        await new InventoryService(t.Db).TransferStockAsync(new StockTransferDto("IT-01", "0000000001", "W2_ZB"), InventoryTestData.User(), default);

        var row = await t.Db.Stocks.SingleAsync();
        Assert.Equal(40m, row.StockQty);
        Assert.Equal(40m, row.AvailableQty);
        Assert.Empty(t.Db.InventoryHistories);
    }

    [Fact]
    public async Task Transfer_to_the_shelf_it_is_already_on_is_refused()
    {
        using var t = await InventoryTestData.WithStockAsync(
            InventoryTestData.Stock(location: "W1_ZA"));

        var r = await new InventoryService(t.Db).TransferStockAsync(new StockTransferDto("IT-01", "0000000001", "W1_ZA"), InventoryTestData.User(), default);

        ResultAssert.BadRequest(r, "LOC-001");
        Assert.Empty(t.Db.LocationTransfers);
    }

    [Fact]
    public async Task Transfer_to_a_location_that_does_not_exist_is_refused()
    {
        using var t = await InventoryTestData.WithStockAsync(InventoryTestData.Stock());

        var r = await new InventoryService(t.Db).TransferStockAsync(new StockTransferDto("IT-01", "0000000001", "NOPE"), InventoryTestData.User(), default);

        ResultAssert.NotFound(r);
        Assert.Empty(t.Db.LocationTransfers);
    }

    [Fact]
    public async Task Transfer_without_a_destination_is_refused()
    {
        using var t = await InventoryTestData.WithStockAsync(InventoryTestData.Stock());

        var r = await new InventoryService(t.Db).TransferStockAsync(new StockTransferDto("IT-01", "0000000001", "  "), InventoryTestData.User(), default);

        ResultAssert.BadRequest(r, "LOC-001");
    }

    [Fact]
    public async Task Transfer_of_an_empty_row_is_refused()
    {
        using var t = await InventoryTestData.WithStockAsync(
            InventoryTestData.Stock(location: "W1_ZA", qty: 0));

        var r = await new InventoryService(t.Db).TransferStockAsync(new StockTransferDto("IT-01", "0000000001", "W2_ZB"), InventoryTestData.User(), default);

        ResultAssert.BadRequest(r, "QTY-001");
    }

    [Fact]
    public async Task Held_stock_can_still_be_moved()
    {
        using var t = await InventoryTestData.WithStockAsync(
            InventoryTestData.Stock(location: "W1_ZA", hold: true, reason: "RS-QC"));

        var r = await new InventoryService(t.Db).TransferStockAsync(new StockTransferDto("IT-01", "0000000001", "W2_ZB"), InventoryTestData.User(), default);

        ResultAssert.Ok<ApiSuccess<StockDto>>(r);
        var row = await t.Db.Stocks.SingleAsync();
        Assert.Equal("W2_ZB", row.LocationCode);
        Assert.True(row.IsOnHold);
        Assert.Equal(0m, row.AvailableQty);
    }

    [Fact]
    public async Task Transfer_of_a_row_that_does_not_exist_is_refused()
    {
        using var t = await InventoryTestData.WithStockAsync(InventoryTestData.Stock());

        var r = await new InventoryService(t.Db).TransferStockAsync(new StockTransferDto("IT-01", "9999999999", "W2_ZB"), InventoryTestData.User(), default);

        ResultAssert.NotFound(r);
    }
}
