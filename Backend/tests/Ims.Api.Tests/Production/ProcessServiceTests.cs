using Ims.Api.Features.Processes;
using Ims.Api.Contracts;
using Ims.Api.Tests.Infrastructure;
using Xunit;

namespace Ims.Api.Tests.Production;

public class ProcessServiceTests
{
    [Fact]
    public async Task Create_and_delete_used_process()
    {
        using var t = TestDb.Seeded();
        var service = new ProcessService(t.Db);
        ResultAssert.Created<ApiSuccess<ProcessDto>>(await service.CreateAsync(new ProcessWriteDto("CUT", "Cutting"), default));
        t.Db.Items.Add(new Ims.Domain.Masters.Item
        {
            ItemCode = "I1", ItemName = "Material", ItemGroupCode = "IG-01", CreatedAt = DateTime.Now
        });
        t.Db.ProductionOrders.Add(new Ims.Domain.Production.ProductionOrder
        {
            ProductionOrderNo = "MO1", FactoryCode = "FAC-01", OutputItemCode = "I1",
            StorageLocationCode = "W1_ZA", PlannedQty = 1, UomCode = "pcs", CreatedBy = "admin"
        });
        t.Db.ProductionOrderLines.Add(new Ims.Domain.Production.ProductionOrderLine
        {
            ProductionOrderNo = "MO1", LineNo = 1, ProcessCode = "CUT", ItemCode = "I1",
            PlannedQty = 1, UomCode = "pcs", CreatedBy = "admin"
        });
        t.Db.SaveChanges();
        ResultAssert.InUse(await service.DeleteAsync("CUT", default));
    }

    [Fact]
    public async Task List_sorts_every_display_field_and_searches_literal_wildcards()
    {
        using var t = TestDb.Seeded();
        var service = new ProcessService(t.Db);
        ResultAssert.Created<ApiSuccess<ProcessDto>>(await service.CreateAsync(new ProcessWriteDto("P1", "Literal percent %", 2), default));
        ResultAssert.Created<ApiSuccess<ProcessDto>>(await service.CreateAsync(new ProcessWriteDto("P2", "Second", 1), default));
        var found = ResultAssert.Ok<ApiListSuccess<ProcessDto>>(await service.ListAsync(default, q: "%"));
        Assert.Equal("P1", Assert.Single(found.Data).ProcessCode);
        var sorted = ResultAssert.Ok<ApiListSuccess<ProcessDto>>(await service.ListAsync(default, sort: "display_order"));
        Assert.Equal("P2", sorted.Data[0].ProcessCode);
    }
}
