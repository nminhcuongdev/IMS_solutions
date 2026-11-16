using System.Security.Claims;
using System.Text.Json;
using Ims.Api.Contracts;
using Ims.Api.Features.History;
using Ims.Api.Features.Print;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Print;

public class PrintSnapshotTests
{
    private static readonly ClaimsPrincipal Worker =
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static HistoryService Service(TestDb t) => new(t.Db, null!, new PrintTargetResolver(t.Db));

    [Fact]
    public async Task A_first_print_stores_its_fields_with_an_empty_flag_and_reprint_data_returns_them_with_REPRINT()
    {
        using var t = TestDb.Seeded();
        var service = Service(t);
        var fields = new Dictionary<string, string> { ["FactoryCode"] = "F02", ["Lot"] = "L-001", ["SupplierCode"] = "S01", ["QR"] = "?lot=L-001", ["ReprintFlag"] = "R", ["Quantiy"] = "3" };

        var created = ResultAssert.Created<ApiSuccess<PrintHistoryDto>>(await service.CreatePrintAsync(new PrintHistoryWriteDto(
            "Receiving", "PO-1", "IT-01", 5, PrintData: fields, LayoutFile: "Layout_IMS/ReceivingQR_24dpmm.mllayx"), Worker, default)).Data!;
        Assert.True(created.CanReprint);
        var stored = JsonSerializer.Deserialize<Dictionary<string, string>>(t.Db.PrintHistories.Single().PrintData!)!;
        Assert.Equal("", stored["ReprintFlag"]);
        Assert.False(stored.ContainsKey("Quantiy"));

        var data = ResultAssert.Ok<ApiSuccess<PrintReprintDataDto>>(await service.ReprintDataAsync(created.No, default)).Data!;
        Assert.Equal("Layout_IMS/ReceivingQR_24dpmm.mllayx", data.LayoutFile);
        Assert.Equal(("F02", "L-001", "S01", "?lot=L-001", "REPRINT"),
            (data.Fields!["FactoryCode"], data.Fields["Lot"], data.Fields["SupplierCode"], data.Fields["QR"], data.Fields["ReprintFlag"]));
    }

    [Fact]
    public async Task An_old_row_is_rebuilt_from_its_source_document_and_a_missing_document_is_SYS_404()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.Add(new Item { ItemCode = "IT-01", ItemName = "Item 1", ItemGroupCode = "IG-01", UomCode = "pcs" });
        t.Db.Factories.Add(new Factory { FactoryCode = "F02", FactoryName = "Second", Address = "Addr 2", IsActive = true });
        t.Db.Partners.Add(new Partner { PartnerCode = "S01", PartnerName = "Supplier", IsActive = true, IsSupplier = true });
        t.Db.ReceivingPlans.Add(new ReceivingPlan { PoNo = "PO-1", ItemCode = "IT-01", Qty = 5, FactoryCode = "F02", PartnerCode = "S01", LotNo = "L-001" });
        t.Db.PrintHistories.Add(new Domain.History.PrintHistory { Category = "Receiving", DocNo = "PO-1", ItemCode = "IT-01", Qty = 5, Status = 19 });
        t.Db.PrintHistories.Add(new Domain.History.PrintHistory { Category = "Receiving", DocNo = "PO-GONE", ItemCode = "IT-01", Qty = 5, Status = 19 });
        await t.Db.SaveChangesAsync();
        var service = Service(t);
        var rows = t.Db.PrintHistories.OrderBy(h => h.No).Select(h => h.No).ToList();

        var data = ResultAssert.Ok<ApiSuccess<PrintReprintDataDto>>(await service.ReprintDataAsync(rows[0], default)).Data!;
        Assert.Null(data.Fields);
        Assert.Equal(("F02", "Second", "Addr 2", "L-001", "S01", "Supplier"),
            (data.Source!.FactoryCode, data.Source.FactoryName, data.Source.Address, data.Source.LotNo, data.Source.PartnerCode, data.Source.PartnerName));
        ResultAssert.NotFound(await service.ReprintDataAsync(rows[1], default));
    }

    [Fact]
    public async Task Master_labels_are_not_recorded()
    {
        using var t = TestDb.Seeded();

        ResultAssert.BadRequest(await Service(t).CreatePrintAsync(new PrintHistoryWriteDto("Location", "W1", null, 1), Worker, default), "VAL-005");

        Assert.Empty(t.Db.PrintHistories);
    }
}
