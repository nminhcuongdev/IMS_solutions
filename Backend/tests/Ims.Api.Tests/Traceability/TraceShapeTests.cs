using Ims.Api.Contracts;
using Ims.Api.Features.Traceability;
using Ims.Api.Tests.Infrastructure;
using Xunit;

namespace Ims.Api.Tests.Traceability;

public class TraceShapeTests
{
    [Fact]
    public async Task Backward_trace_carries_lot_production_and_components()
    {
        using var t = await TraceabilityRetestTests.SeedAsync();

        var r = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(
            await new TraceabilityService(t.Db).TraceAsync("backward", "FG1", "FGLOT", "events", default)).Data!;

        Assert.Equal("backward", r.Direction);
        Assert.Equal(("FG1", "FGLOT", "MO1"), (r.Lot!.ItemCode, r.Lot.LotNo, r.Lot.ProductionOrderNo));
        Assert.Equal(5, r.Production!.OutputQty);
        Assert.Contains(r.Components!, c => c.ItemCode == "MAT-A" && c.LotNo == "LOT-A" && c.InputQty == 4);
        Assert.NotNull(r.Events);
    }

    [Fact]
    public async Task Forward_trace_lists_the_orders_that_consumed_the_lot_and_their_outputs()
    {
        using var t = await TraceabilityRetestTests.SeedAsync();

        var r = ResultAssert.Ok<ApiSuccess<TraceResultDto>>(
            await new TraceabilityService(t.Db).TraceAsync("forward", "MAT-A", "LOT-A", null, default)).Data!;

        var used = Assert.Single(r.ConsumedBy!);
        Assert.Equal("MO1", used.ProductionOrderNo);
        Assert.Contains(used.Outputs, o => o.ItemCode == "FG1" && o.LotNo == "FGLOT");
        Assert.Null(r.Events);
        ResultAssert.BadRequest(await new TraceabilityService(t.Db).TraceAsync("sideways", "MAT-A", "LOT-A", null, default), "VAL-001");
    }
}
