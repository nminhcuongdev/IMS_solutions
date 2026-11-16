using Ims.Api.Features.Production;
using Ims.Domain.Production;
using Xunit;

namespace Ims.Api.Tests.Production;

public class ProductionReturnRulesTests
{
    private static ProductionResult Row(long no, string type, decimal qty, string serial = "S1") => new()
    {
        No = no, ResultType = type, ProductionOrderNo = "MO1", LineNo = 1, SerialNo = serial, Qty = qty,
        ItemCode = "MAT", RequestId = "r" + no, LocationCode = "L", FactoryCode = "F", CreatedBy = "u",
    };

    [Fact]
    public void A_return_is_taken_from_the_newest_consumption_of_the_serial_first()
    {
        var consumed = new[] { Row(1, ProductionResultType.Consume, 4), Row(2, ProductionResultType.Consume, 3) };
        var returned = new[] { Row(3, ProductionResultType.Return, 5) };

        var open = ProductionReturnRules.OpenBySource(consumed, returned);

        Assert.Equal(new[] { (1L, 2m) }, open.Select(x => (x.Source.No, x.Available)));
        Assert.Equal(3m, ProductionReturnRules.ReturnedAgainst(consumed[1], consumed, returned));
        Assert.Equal(2m, ProductionReturnRules.ReturnedAgainst(consumed[0], consumed, returned));
    }

    [Fact]
    public void Returns_of_another_serial_do_not_count()
    {
        var consumed = new[] { Row(1, ProductionResultType.Consume, 4, "S1") };
        var returned = new[] { Row(2, ProductionResultType.Return, 4, "S2") };

        Assert.Equal(4m, ProductionReturnRules.OpenBySource(consumed, returned).Single().Available);
    }
}
