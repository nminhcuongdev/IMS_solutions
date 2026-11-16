using System.Security.Claims;
using System.Text.Json;
using Moq;
using Ims.Api.Contracts;
using Ims.Api.Features.History;
using Ims.Api.Features.Print;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Print;

public class ResultReprintTests
{
    private static readonly ClaimsPrincipal Worker =
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static async Task<long> SeedAsync(TestDb t)
    {
        t.Db.Items.Add(new Item { ItemCode = "IT-01", ItemName = "Item 1", ItemGroupCode = "IG-01", UomCode = "pcs" });
        t.Db.Devices.Add(new Device { DeviceCode = "DEV-PR01", DeviceName = "Printer", DeviceType = DeviceType.Printer, IpAddress = "192.168.10.101" });
        var result = new ReceivingResult { PoNo = "PO-1", PoBranchNo = "01", ItemCode = "IT-01", Qty = 5, LotNo = "L-001" };
        t.Db.ReceivingResults.Add(result);
        t.Db.PrintHistories.Add(new Domain.History.PrintHistory
        {
            Category = "Receiving", DocNo = "PO-1", ItemCode = "IT-01", Qty = 5, Status = 19,
            LayoutFile = "Layout_IMS/ReceivingQR_24dpmm.mllayx",
            PrintData = JsonSerializer.Serialize(new Dictionary<string, string> { ["Lot"] = "L-001", ["ReprintFlag"] = "" }),
        });
        await t.Db.SaveChangesAsync();
        return result.No;
    }

    [Fact]
    public async Task A_result_reprint_prints_the_recorded_label_and_links_the_new_row()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t);
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);
        engine.Setup(e => e.PrintAsync("Layout_IMS/ReceivingQR_24dpmm.mllayx", "LAN:192.168.10.101",
                It.Is<IReadOnlyDictionary<string, string>>(d => d["Lot"] == "L-001" && d["ReprintFlag"] == "REPRINT"),
                2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new HistoryService(t.Db, engine.Object, new PrintTargetResolver(t.Db));

        var r = await service.ResultReprintAsync("receiving", no, new PrintReprintRequest("DEV-PR01", 2), Worker, default);

        var dto = ResultAssert.Created<ApiSuccess<PrintHistoryDto>>(r).Data!;
        Assert.Equal("ReceivingReprint", dto.Category);
        Assert.NotNull(dto.ReprintOfNo);
        engine.VerifyAll();
    }

    [Fact]
    public async Task A_result_with_no_recorded_label_or_an_unknown_type_is_refused()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t);
        t.Db.PrintHistories.RemoveRange(t.Db.PrintHistories);
        await t.Db.SaveChangesAsync();
        var service = new HistoryService(t.Db, null!, new PrintTargetResolver(t.Db));

        ResultAssert.NotFound(await service.ResultReprintAsync("receiving", no, new PrintReprintRequest("DEV-PR01", 1), Worker, default));
        ResultAssert.NotFound(await service.ResultReprintAsync("receiving", 999_999, new PrintReprintRequest("DEV-PR01", 1), Worker, default));
        ResultAssert.NotFound(await service.ResultReprintAsync("picking", no, new PrintReprintRequest("DEV-PR01", 1), Worker, default));
    }
}
