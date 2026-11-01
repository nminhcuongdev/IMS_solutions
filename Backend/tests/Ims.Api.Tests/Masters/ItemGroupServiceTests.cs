using Ims.Api.Contracts;
using Ims.Api.Features.ItemGroups;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class ItemGroupServiceTests
{
    private static ItemGroupWriteDto Valid(
        string code = "IG-NEW", string name = "New group", byte category = 1) =>
        new(code, name, category);


    [Fact]
    public async Task Create_persists_row_and_returns_201()
    {
        using var t = TestDb.Seeded();
        var body = Valid();

        var res = await new ItemGroupService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ItemGroupDto>>(res).Data!;
        Assert.Equal("IG-NEW", dto.ItemGroupCode);
        Assert.Equal(1, dto.Category);
        Assert.NotNull(await t.Db.ItemGroups.FindAsync("IG-NEW"));
    }

    [Fact]
    public async Task Create_detects_a_duplicate_even_when_the_code_is_padded()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "  IG-01  ");

        var res = await new ItemGroupService(t.Db).CreateAsync(body, default);

        ResultAssert.Conflict(res);
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)8)]
    [InlineData((byte)255)]
    public async Task Create_rejects_category_outside_1_to_7(byte category)
    {
        using var t = TestDb.Seeded();
        var body = Valid(category: category);

        var res = await new ItemGroupService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Theory]
    [InlineData((byte)1)]
    [InlineData((byte)4)]
    [InlineData((byte)7)]
    public async Task Create_accepts_every_defined_category(byte category)
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "IG-" + category, category: category);

        var res = await new ItemGroupService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ItemGroupDto>>(res).Data!;
        Assert.Equal(category, dto.Category);
    }

    [Fact]
    public async Task Create_rejects_blank_code_and_name()
    {
        using var t = TestDb.Seeded();

        var blankCode = await new ItemGroupService(t.Db).CreateAsync(Valid(code: " "), default);
        var blankName = await new ItemGroupService(t.Db).CreateAsync(Valid(name: " "), default);

        ResultAssert.BadRequest(blankCode, "VAL-001");
        ResultAssert.BadRequest(blankName, "VAL-002");
    }


    [Fact]
    public async Task Get_returns_row_and_404_when_missing()
    {
        using var t = TestDb.Seeded();

        var found = await new ItemGroupService(t.Db).GetAsync("IG-01", default);
        var missing = await new ItemGroupService(t.Db).GetAsync("NOPE", default);

        Assert.Equal("Group 1", ResultAssert.Ok<ApiSuccess<ItemGroupDto>>(found).Data!.ItemGroupName);
        ResultAssert.NotFound(missing);
    }

    [Fact]
    public async Task List_searches_and_sorts_by_category()
    {
        using var t = TestDb.Seeded();
        t.Db.ItemGroups.Add(new ItemGroup
        {
            ItemGroupCode = "IG-02", ItemGroupName = "Finished",
            Category = ItemGroupCategory.FinishedGoods, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var hitRes = await new ItemGroupService(t.Db).ListAsync(default, q: "finish");
        var sortedRes = await new ItemGroupService(t.Db).ListAsync(default, sort: "category");

        var hit = ResultAssert.Ok<ApiListSuccess<ItemGroupDto>>(hitRes);
        Assert.Equal("IG-02", Assert.Single(hit.Data).ItemGroupCode);

        var sorted = ResultAssert.Ok<ApiListSuccess<ItemGroupDto>>(sortedRes);
        Assert.Equal(new byte[] { 1, 4 }, sorted.Data.Select(g => g.Category));
    }


    [Fact]
    public async Task Update_changes_name_and_category()
    {
        using var t = TestDb.Seeded();
        var body = new ItemGroupWriteDto("IG-01", "Renamed", 7);

        var res = await new ItemGroupService(t.Db).UpdateAsync("IG-01", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<ItemGroupDto>>(res).Data!;
        Assert.Equal("Renamed", dto.ItemGroupName);
        Assert.Equal(7, dto.Category);
        Assert.NotNull(dto.UpdatedAt);
    }

    [Fact]
    public async Task Update_rejects_invalid_category_and_missing_row()
    {
        using var t = TestDb.Seeded();

        var badCategory = await new ItemGroupService(t.Db).UpdateAsync("IG-01", Valid(category: 9), default);
        var missingRow = await new ItemGroupService(t.Db).UpdateAsync("NOPE", Valid(), default);

        ResultAssert.BadRequest(badCategory, "VAL-002");
        ResultAssert.NotFound(missingRow);
    }


    [Fact]
    public async Task Delete_is_refused_while_an_item_references_the_group()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-1", ItemName = "Item 1", ItemGroupCode = "IG-01",
            PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new ItemGroupService(t.Db).DeleteAsync("IG-01", default);

        ResultAssert.InUse(res);
    }

    [Fact]
    public async Task Delete_soft_deletes_when_unreferenced()
    {
        using var t = TestDb.Seeded();

        var res = await new ItemGroupService(t.Db).DeleteAsync("IG-01", default);

        ResultAssert.NoContent(res);
        Assert.True((await t.Db.ItemGroups.FindAsync("IG-01"))!.IsDeleted);
    }


    [Fact]
    public async Task ExportCsv_writes_the_category_as_a_number()
    {
        using var t = TestDb.Seeded();

        var res = await new ItemGroupService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("item_group_code,item_group_name,category,", lines[0]);
        Assert.Contains("IG-01,Group 1,4,1", lines[1]);
    }
}
