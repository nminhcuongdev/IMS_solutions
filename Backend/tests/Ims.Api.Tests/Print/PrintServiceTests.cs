using System.Security.Claims;
using Moq;
using Ims.Api.Contracts;
using Ims.Api.Features.History;
using Ims.Api.Features.Print;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Print;

public class PrintServiceTests
{
    private static readonly Dictionary<string, string> SampleFields = new()
    {
        ["PONo"] = "PO-20260914-001",
        ["Item Code"] = "PT-RBN-05",
    };

    private static Device Printer(string code = "DEV-PR01", string? ip = "192.168.10.101") => new()
    {
        DeviceCode = code, DeviceName = "Test printer", DeviceType = DeviceType.Printer,
        IpAddress = ip, CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task An_unknown_layout_key_is_refused()
    {
        using var t = TestDb.Seeded();
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).PreviewAsync(new PrintPreviewRequest("NoSuchLayout", "DEV-PR01", SampleFields), CancellationToken.None);

        ResultAssert.NotFound(r);
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task An_unknown_device_code_is_refused_before_calling_the_engine()
    {
        using var t = TestDb.Seeded();
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).PreviewAsync(new PrintPreviewRequest("ReceivingQR", "NOPE", SampleFields), CancellationToken.None);

        ResultAssert.NotFound(r);
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_device_with_no_ip_address_is_refused_before_calling_the_engine()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer(ip: null));
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).PreviewAsync(new PrintPreviewRequest("ReceivingQR", "DEV-PR01", SampleFields), CancellationToken.None);

        ResultAssert.BadRequest(r, "VAL-004");
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_known_layout_and_device_are_forwarded_to_the_engine_as_a_LAN_setting()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer());
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>();
        engine.Setup(e => e.PreviewAsync(
                "Layout_IMS/ReceivingQR_24dpmm.mllayx", "LAN:192.168.10.101", SampleFields, It.IsAny<CancellationToken>()))
            .ReturnsAsync("data:image/png;base64,AAAA");

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).PreviewAsync(new PrintPreviewRequest("ReceivingQR", "DEV-PR01", SampleFields), CancellationToken.None);

        var dto = ResultAssert.Ok<ApiSuccess<PrintPreviewResponseDto>>(r).Data!;
        Assert.Equal("data:image/png;base64,AAAA", dto.Image);
    }

    [Fact]
    public async Task Layout_key_matching_ignores_case()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer());
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>();
        engine.Setup(e => e.PreviewAsync(
                "Layout_IMS/ReturnsQR_24dpmm.mllayx", It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("img");

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).PreviewAsync(new PrintPreviewRequest("returnsqr", "DEV-PR01", SampleFields), CancellationToken.None);

        ResultAssert.Ok<ApiSuccess<PrintPreviewResponseDto>>(r);
    }

    [Fact]
    public async Task An_engine_failure_with_no_code_is_reported_as_PRN_001()
    {
        var r = await PreviewFailingWith(null);
        ResultAssert.Error(r, 500, "PRN-001");
    }

    [Fact]
    public async Task The_engine_return_value_becomes_the_PRT_code()
    {
        ResultAssert.Error(await PreviewFailingWith(802), 500, "PRT-802");
        ResultAssert.Error(await PreviewFailingWith(4), 500, "PRT-4");
        ResultAssert.Error(await PreviewFailingWith(610), 500, "PRT-610");
    }

    [Fact]
    public async Task A_negative_return_value_keeps_the_N_form()
    {
        ResultAssert.Error(await PreviewFailingWith(-5), 500, "PRT-N5");
    }

    [Fact]
    public async Task An_unlisted_return_value_falls_back()
    {
        ResultAssert.Error(await PreviewFailingWith(99999), 500, "PRN-001");
    }

    private static async Task<ServiceResult> PreviewFailingWith(int? componentCode)
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer());
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>();
        engine.Setup(e => e.PreviewAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MlEngineException(
                "Preview",
                componentCode is null ? null : $"componentCode={componentCode}",
                componentCode));

        return await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).PreviewAsync(new PrintPreviewRequest("ReceivingQR", "DEV-PR01", SampleFields), CancellationToken.None);
    }


    private static ClaimsPrincipal Worker(string code = "U001") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    private static PrintLabelRequest LabelBody(decimal qty = 5) => new(
        "ReceivingQR", "DEV-PR01", SampleFields,
        new PrintHistoryWriteDto("Receiving", "PO-20260914-001", "PT-RBN-05", qty, "pcs"));

    [Fact]
    public async Task A_label_already_issued_is_refused_before_anything_reaches_the_printer()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer());
        t.Db.PrintHistories.Add(new Domain.History.PrintHistory
        {
            Category = "Receiving", DocNo = "PO-20260914-001", ItemCode = "PT-RBN-05",
            Qty = 5, PackageCount = 1, Status = 19, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).LabelAsync(LabelBody(), Worker(), CancellationToken.None);

        ResultAssert.Error(r, 409, "DUP-002");
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_zero_quantity_is_refused_before_anything_reaches_the_printer()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer());
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).LabelAsync(LabelBody(qty: 0), Worker(), CancellationToken.None);

        ResultAssert.BadRequest(r, "QTY-001");
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task An_unknown_device_is_refused_before_anything_reaches_the_printer()
    {
        using var t = TestDb.Seeded();
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).LabelAsync(LabelBody(), Worker(), CancellationToken.None);

        ResultAssert.NotFound(r);
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task A_label_is_printed_over_LAN_and_then_recorded_against_the_device_and_worker()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer());
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>(MockBehavior.Strict);
        engine.Setup(e => e.PrintAsync(
                "Layout_IMS/ReceivingQR_24dpmm.mllayx", "LAN:192.168.10.101", SampleFields, 1,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).LabelAsync(LabelBody(), Worker(), CancellationToken.None);

        var dto = ResultAssert.Created<ApiSuccess<PrintHistoryDto>>(r).Data!;
        Assert.Equal("PO-20260914-001", dto.DocNo);
        var row = Assert.Single(t.Db.PrintHistories);
        Assert.Equal("DEV-PR01", row.Spec);
        Assert.Equal("U001", row.CreatedBy);
        engine.Verify(e => e.PrintAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_printer_failure_carries_the_engine_code_and_records_nothing()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer());
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>();
        engine.Setup(e => e.PrintAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MlEngineException("Output", "componentCode=610", 610));

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).LabelAsync(LabelBody(), Worker(), CancellationToken.None);

        ResultAssert.Error(r, 500, "PRT-610");
        Assert.Empty(t.Db.PrintHistories);
    }

    [Fact]
    public async Task A_dead_engine_is_still_PRN_002()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(Printer());
        await t.Db.SaveChangesAsync();
        var engine = new Mock<IMlEngineClient>();
        engine.Setup(e => e.PrintAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var r = await new PrintService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db), new Ims.Api.Features.History.HistoryService(t.Db, engine.Object, new Ims.Api.Features.Print.PrintTargetResolver(t.Db))).LabelAsync(LabelBody(), Worker(), CancellationToken.None);

        ResultAssert.Error(r, 500, "PRN-002");
        Assert.Empty(t.Db.PrintHistories);
    }
}
