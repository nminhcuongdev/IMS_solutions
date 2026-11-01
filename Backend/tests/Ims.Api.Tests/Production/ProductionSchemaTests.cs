using Ims.Api.Tests.Infrastructure;
using Xunit;

namespace Ims.Api.Tests.Production;

public class ProductionSchemaTests
{
    [Theory]
    [InlineData("Ims.Domain.Masters.Process")]
    [InlineData("Ims.Domain.Production.ProductionOrder")]
    [InlineData("Ims.Domain.Production.ProductionOrderLine")]
    [InlineData("Ims.Domain.Production.ProductionConsumption")]
    [InlineData("Ims.Domain.Production.ProductionOutput")]
    [InlineData("Ims.Domain.Production.ProductionReceipt")]
    [InlineData("Ims.Domain.Production.ProductionLabel")]
    [InlineData("Ims.Domain.Production.ProductionReturn")]
    public void Entity_is_mapped_to_database(string name)
    {
        using var t = TestDb.Empty();
        Assert.Contains(t.Db.Model.GetEntityTypes(), x => x.ClrType.FullName == name);
    }
}
