using System.Security.Claims;
using Moq;
using Ims.Api.Contracts;
using Ims.Api.Features.History;
using Ims.Api.Features.Print;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Print;

public class ReprintServiceTests
{
    private static readonly Dictionary<string, string> Fields = new()
    {
        ["PONo"] = "PO-20260914-001",
        ["Item Code"] = "PT-RBN-05",
        ["ReprintFlag"] = "REPRINT",
    };

    private static ClaimsPrincipal Worker(string code = "U002") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    private static async Task<long> SeedAsync(TestDb t, string category = "Receiving", bool printer = true)
    {
        if (printer)
            t.Db.Devices.Add(new Device
            {
                DeviceCode = "DEV-PR01", DeviceName = "Test printer", DeviceType = DeviceType.Printer,
                IpAddress = "192.168.10.101", CreatedAt = DateTime.UtcNow,
            });
        var row = new Domain.History.PrintHistory
        {
            Category = category, DocNo = "PO-20260914-001", ItemCode = "PT-RBN-05", Qty = 5,
            UomCode = "pcs", PackageCount = 1, Spec = "OLD-PRN", Status = 19,
            ScheduledDate = new DateOnly(2026, 9, 14), CreatedBy = "U001", CreatedAt = DateTime.UtcNow,
            PrintData = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>(Fields) { ["ReprintFlag"] = "" }),
        };
        t.Db.PrintHistories.Add(row);
        await t.Db.SaveChangesAsync();
        return row.No;
    }

    private static Mock<IMlEngineClient> PrintingEngine(string layoutFile, int copies)
    {
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);
        engine.Setup(e => e.PrintAsync(layoutFile, "LAN:192.168.10.101",
                It.Is<IReadOnlyDictionary<string, string>>(d => d.Count == Fields.Count && Fields.All(kv => d.ContainsKey(kv.Key) && d[kv.Key] == kv.Value)),
                copies, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return engine;
    }

    [Fact]
    public async Task An_unknown_history_row_is_refused_before_anything_reaches_the_printer()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(999_999, new PrintReprintRequest("DEV-PR01", 1, Fields), Worker(), CancellationToken.None);

        ResultAssert.NotFound(r);
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_count_below_one_is_refused_before_anything_reaches_the_printer()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t);
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no, new PrintReprintRequest("DEV-PR01", 0, Fields), Worker(), CancellationToken.None);

        ResultAssert.BadRequest(r, "QTY-001");
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_category_with_no_label_layout_is_refused()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t, category: "Inventory");
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no, new PrintReprintRequest("DEV-PR01", 1, Fields), Worker(), CancellationToken.None);

        ResultAssert.NotFound(r);
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task An_unknown_device_is_refused_before_anything_reaches_the_printer()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t, printer: false);
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no, new PrintReprintRequest("DEV-PR01", 1, Fields), Worker(), CancellationToken.None);

        ResultAssert.NotFound(r);
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_reprint_prints_count_copies_and_appends_a_Reprinted_row_without_touching_the_original()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t);
        var engine = PrintingEngine("Layout_IMS/ReceivingQR_24dpmm.mllayx", copies: 3);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no, new PrintReprintRequest(" DEV-PR01 ", 3, Fields), Worker(), CancellationToken.None);

        var dto = ResultAssert.Created<ApiSuccess<PrintHistoryDto>>(r).Data!;
        Assert.Equal("ReceivingReprint", dto.Category);
        Assert.Equal(20, dto.Status);

        var rows = t.Db.PrintHistories.OrderBy(h => h.No).ToList();
        Assert.Equal(2, rows.Count);
        var original = rows[0];
        Assert.Equal(("Receiving", 19, "OLD-PRN"), (original.Category, original.Status, original.Spec));

        var copy = rows[1];
        Assert.Equal("ReceivingReprint", copy.Category);
        Assert.Equal(20, copy.Status);
        Assert.Equal(3, copy.PackageCount);
        Assert.Equal("DEV-PR01", copy.Spec);
        Assert.Equal("U002", copy.CreatedBy);
        Assert.Equal((original.DocNo, original.ItemCode, original.Qty, original.UomCode, original.ScheduledDate),
            (copy.DocNo, copy.ItemCode, copy.Qty, copy.UomCode, copy.ScheduledDate));
        engine.VerifyAll();
    }

    [Fact]
    public async Task Reprinting_a_reprint_row_uses_the_base_layout_and_keeps_the_Reprint_category()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t, category: "ShippingReprint");
        var engine = PrintingEngine("Layout_IMS/ShippingQR_24dpmm.mllayx", copies: 1);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no, new PrintReprintRequest("DEV-PR01", 1, Fields), Worker(), CancellationToken.None);

        var dto = ResultAssert.Created<ApiSuccess<PrintHistoryDto>>(r).Data!;
        Assert.Equal("ShippingReprint", dto.Category);
        engine.VerifyAll();
    }

    [Fact]
    public async Task A_reprint_printed_by_the_browser_is_recorded_without_calling_the_engine()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t, printer: false);
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no,
            new PrintReprintRequest(" DRV:Label Printer 609dpi ", 2, Fields, ClientPrinted: true), Worker(), CancellationToken.None);

        var dto = ResultAssert.Created<ApiSuccess<PrintHistoryDto>>(r).Data!;
        Assert.Equal("ReceivingReprint", dto.Category);
        var copy = t.Db.PrintHistories.OrderBy(h => h.No).Last();
        Assert.Equal(("DRV:Label Printer 609dpi", 2, no), (copy.Spec, copy.PackageCount, copy.ReprintOfNo));
        Assert.Equal("Layout_IMS/ReceivingQR_24dpmm.mllayx", copy.LayoutFile);
        Assert.Contains("\"ReprintFlag\":\"REPRINT\"", copy.PrintData);
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_browser_reprint_still_refuses_a_count_below_one()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t, printer: false);
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no,
            new PrintReprintRequest("DRV:Label Printer 609dpi", 0, Fields, ClientPrinted: true), Worker(), CancellationToken.None);

        ResultAssert.BadRequest(r, "QTY-001");
        Assert.Single(t.Db.PrintHistories);
    }

    [Fact]
    public async Task A_browser_reprint_with_a_printer_name_longer_than_the_column_is_VAL_005()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t, printer: false);
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no,
            new PrintReprintRequest("DRV:" + new string('X', 97), 1, Fields, ClientPrinted: true), Worker(), CancellationToken.None);

        ResultAssert.BadRequest(r, "VAL-005");
        Assert.Single(t.Db.PrintHistories);
    }

    [Fact]
    public async Task A_browser_reprint_of_a_category_with_no_layout_is_refused()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t, category: "Inventory", printer: false);
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no,
            new PrintReprintRequest("DRV:Label Printer 609dpi", 1, Fields, ClientPrinted: true), Worker(), CancellationToken.None);

        ResultAssert.NotFound(r);
        Assert.Single(t.Db.PrintHistories);
    }

    [Fact]
    public async Task A_printer_failure_is_PRN_002_and_records_nothing()
    {
        using var t = TestDb.Seeded();
        var no = await SeedAsync(t);
        var engine = new Mock<IMlEngineClient>();
        engine.Setup(e => e.PrintAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MlEngineException("OpenPort", "cc=4"));

        var r = await new HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ReprintAsync(no, new PrintReprintRequest("DEV-PR01", 1, Fields), Worker(), CancellationToken.None);

        ResultAssert.Error(r, 500, "PRN-002");
        Assert.Single(t.Db.PrintHistories);
    }
}
