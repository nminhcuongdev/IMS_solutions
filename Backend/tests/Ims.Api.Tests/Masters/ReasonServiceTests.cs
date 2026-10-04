using Ims.Api.Contracts;
using Ims.Api.Features.Reasons;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class ReasonServiceTests
{
    private static ReasonWriteDto Valid(
        string code = "RS-NEW", string name = "New reason", string category = "Shortage") =>
        new(code, name, category);


    [Fact]
    public async Task Create_persists_row_and_returns_201()
    {
        using var t = TestDb.Empty();
        var body = Valid();

        var res = await new ReasonService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ReasonDto>>(res).Data!;
        Assert.Equal("RS-NEW", dto.ReasonCode);
        Assert.Equal("Shortage", dto.ReasonCategory);
    }

    [Theory]
    [InlineData("Shortage")]
    [InlineData("Defect")]
    [InlineData("Adjustment")]
    [InlineData("Hold")]
    public async Task Create_accepts_every_defined_category(string category)
    {
        using var t = TestDb.Empty();
        var body = Valid(code: "RS-" + category, category: category);

        var res = await new ReasonService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ReasonDto>>(res).Data!;
        Assert.Equal(category, dto.ReasonCategory);
    }

    [Theory]
    [InlineData("shortage")]
    [InlineData("SHORTAGE")]
    [InlineData("ShOrTaGe")]
    public async Task Create_accepts_a_category_in_any_casing(string category)
    {
        using var t = TestDb.Empty();
        var body = Valid(category: category);

        var res = await new ReasonService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ReasonDto>>(res).Data!;
        Assert.Equal("Shortage", dto.ReasonCategory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unknown")]
    [InlineData("99")]
    public async Task Create_rejects_an_undefined_category(string category)
    {
        using var t = TestDb.Empty();
        var body = Valid(category: category);

        var res = await new ReasonService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Fact]
    public async Task Create_rejects_blank_code_blank_name_and_duplicates()
    {
        using var t = TestDb.Empty();

        var blankCode = await new ReasonService(t.Db).CreateAsync(Valid(code: " "), default);
        var blankName = await new ReasonService(t.Db).CreateAsync(Valid(name: " "), default);

        ResultAssert.BadRequest(blankCode, "VAL-001");
        ResultAssert.BadRequest(blankName, "VAL-002");

        var created = await new ReasonService(t.Db).CreateAsync(Valid(), default);
        ResultAssert.Created<ApiSuccess<ReasonDto>>(created);

        var padded = await new ReasonService(t.Db).CreateAsync(Valid(code: "  RS-NEW  "), default);

        ResultAssert.Conflict(padded);
    }


    [Fact]
    public async Task List_hides_deleted_rows_and_searches()
    {
        using var t = TestDb.Empty();
        t.Db.Reasons.AddRange(
            new Reason { ReasonCode = "R1", ReasonName = "Broken pallet", ReasonCategoryValue = ReasonCategory.Defect, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Reason { ReasonCode = "R2", ReasonName = "Gone", ReasonCategoryValue = ReasonCategory.Hold, IsActive = true, IsDeleted = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var allRes = await new ReasonService(t.Db).ListAsync(default);
        var hitRes = await new ReasonService(t.Db).ListAsync(default, q: "pallet");

        var all = ResultAssert.Ok<ApiListSuccess<ReasonDto>>(allRes);
        Assert.Equal("R1", Assert.Single(all.Data).ReasonCode);

        var hit = ResultAssert.Ok<ApiListSuccess<ReasonDto>>(hitRes);
        Assert.Single(hit.Data);
    }

    [Fact]
    public async Task Get_maps_the_enum_back_to_its_name()
    {
        using var t = TestDb.Empty();
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "R1", ReasonName = "Held for QC",
            ReasonCategoryValue = ReasonCategory.Hold, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new ReasonService(t.Db).GetAsync("R1", default);

        var dto = ResultAssert.Ok<ApiSuccess<ReasonDto>>(res).Data!;
        Assert.Equal("Hold", dto.ReasonCategory);
    }


    [Fact]
    public async Task Update_changes_name_and_category()
    {
        using var t = TestDb.Empty();
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "R1", ReasonName = "Before",
            ReasonCategoryValue = ReasonCategory.Shortage, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
        var body = new ReasonWriteDto("R1", "After", "Adjustment");

        var res = await new ReasonService(t.Db).UpdateAsync("R1", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<ReasonDto>>(res).Data!;
        Assert.Equal("After", dto.ReasonName);
        Assert.Equal("Adjustment", dto.ReasonCategory);
        Assert.NotNull(dto.UpdatedAt);
    }

    [Fact]
    public async Task Update_rejects_bad_category_and_unknown_row()
    {
        using var t = TestDb.Empty();
        t.Db.Reasons.Add(new Reason { ReasonCode = "R1", ReasonName = "X", IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var badCategory = await new ReasonService(t.Db).UpdateAsync("R1", Valid(category: "Nope"), default);
        var unknownRow = await new ReasonService(t.Db).UpdateAsync("NOPE", Valid(), default);

        ResultAssert.BadRequest(badCategory, "VAL-002");
        ResultAssert.NotFound(unknownRow);
    }


    [Fact]
    public async Task Delete_soft_deletes()
    {
        using var t = TestDb.Empty();
        t.Db.Reasons.Add(new Reason { ReasonCode = "R1", ReasonName = "X", IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var first = await new ReasonService(t.Db).DeleteAsync("R1", default);
        var second = await new ReasonService(t.Db).DeleteAsync("R1", default);

        ResultAssert.NoContent(first);
        Assert.True((await t.Db.Reasons.FindAsync("R1"))!.IsDeleted);

        ResultAssert.NotFound(second);
    }


    [Fact]
    public async Task ExportCsv_writes_the_category_name()
    {
        using var t = TestDb.Empty();
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "R1", ReasonName = "Held for QC",
            ReasonCategoryValue = ReasonCategory.Hold, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new ReasonService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains("R1,Held for QC,Hold", csv);
    }
}
