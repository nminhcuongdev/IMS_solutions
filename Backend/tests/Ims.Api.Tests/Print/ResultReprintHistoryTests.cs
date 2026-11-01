using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.History;
using Ims.Api.Features.Ht;
using Ims.Api.Features.Print;
using Ims.Api.Features.Receiving;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Print;

public class ResultReprintHistoryTests
{
    private static ClaimsPrincipal Worker() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U002") }, "test"));

    private static HistoryService History(TestDb t) => new(t.Db, null!, new PrintTargetResolver(t.Db));

    [Fact]
    public async Task A_reprint_is_recorded_even_when_the_label_was_already_printed()
    {
        using var t = TestDb.Seeded();
        var first = new PrintHistoryWriteDto("Receiving", "PO-1", "IT-01", 5, "pcs");
        ResultAssert.Created<ApiSuccess<PrintHistoryDto>>(await History(t).CreatePrintAsync(first, Worker(), default));

        var res = await History(t).CreatePrintAsync(first with { Reprint = true }, Worker(), default);

        var dto = ResultAssert.Created<ApiSuccess<PrintHistoryDto>>(res).Data!;
        Assert.Equal(("ReceivingReprint", 20), (dto.Category, dto.Status));
        Assert.Equal(2, await t.Db.PrintHistories.CountAsync());
    }

    [Fact]
    public async Task A_second_normal_print_is_still_refused()
    {
        using var t = TestDb.Seeded();
        var first = new PrintHistoryWriteDto("Receiving", "PO-1", "IT-01", 5, "pcs");
        await History(t).CreatePrintAsync(first, Worker(), default);

        var res = await History(t).CreatePrintAsync(first, Worker(), default);

        ResultAssert.Error(res, 409, "DUP-002");
    }

    [Fact]
    public async Task Result_rows_carry_the_lot_supplier_and_scheduled_date_for_the_label()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01", UomCode = "pcs",
            PackingTypeCode = "BOX", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Contoso Corporation", IsSupplier = true, IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = "PO-1", PoBranchNo = "01", PartnerCode = "BP001", ItemCode = "IT-01", Qty = 5,
            ActualQty = 5, FactoryCode = "FAC-01", ScheduledDate = new DateOnly(2026, 9, 1),
            Status = ReceivingStatus.Arrived, LotNo = "LOT-PLAN", CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingResults.Add(new ReceivingResult
        {
            PoNo = "PO-1", PoBranchNo = "01", ItemCode = "IT-01", Qty = 5, LocationCode = "W1_ZA",
            LotNo = "LOT-RESULT", CreatedAt = DateTime.UtcNow, CreatedBy = "U002",
        });
        await t.Db.SaveChangesAsync();

        var res = await new ReceivingService(t.Db).ListResultsAsync(default);

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<ReceivingResultDto>>(res).Data);
        Assert.Equal(("LOT-RESULT", "BP001", "Contoso Corporation", new DateOnly(2026, 9, 1)),
            (row.LotNo, row.SupplierCode, row.SupplierName, row.ScheduledDate));
    }
}
