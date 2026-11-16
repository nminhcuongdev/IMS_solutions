using Ims.Api.Contracts;
using Ims.Api.Features.Partners;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class PartnerServiceTests
{
    private static PartnerWriteDto Valid(
        string code = "BP-NEW", string name = "New partner",
        bool supplier = false, bool customer = false) =>
        new(code, name, supplier, customer, Country: "Vietnam");


    [Fact]
    public async Task Create_persists_row_and_returns_201()
    {
        using var t = TestDb.Empty();
        var body = Valid(supplier: true);

        var res = await new PartnerService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<PartnerDto>>(res).Data!;
        Assert.Equal("BP-NEW", dto.PartnerCode);
        Assert.True(dto.IsSupplier);
        Assert.False(dto.IsCustomer);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Create_stores_supplier_and_customer_independently(bool supplier, bool customer)
    {
        using var t = TestDb.Empty();
        var body = Valid(supplier: supplier, customer: customer);

        var res = await new PartnerService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<PartnerDto>>(res).Data!;
        Assert.Equal(supplier, dto.IsSupplier);
        Assert.Equal(customer, dto.IsCustomer);
    }

    [Fact]
    public async Task Create_rejects_blank_code_blank_name_and_duplicates()
    {
        using var t = TestDb.Empty();

        var blankCode = await new PartnerService(t.Db).CreateAsync(Valid(code: " "), default);
        var blankName = await new PartnerService(t.Db).CreateAsync(Valid(name: " "), default);

        ResultAssert.BadRequest(blankCode, "VAL-001");
        ResultAssert.BadRequest(blankName, "VAL-001");

        var created = await new PartnerService(t.Db).CreateAsync(Valid(), default);
        ResultAssert.Created<ApiSuccess<PartnerDto>>(created);

        var duplicate = await new PartnerService(t.Db).CreateAsync(Valid(), default);
        var padded = await new PartnerService(t.Db).CreateAsync(Valid(code: "  BP-NEW  "), default);

        ResultAssert.Conflict(duplicate);
        ResultAssert.Conflict(padded);
    }


    [Fact]
    public async Task List_searches_by_code_or_name_and_hides_deleted()
    {
        using var t = TestDb.Empty();
        t.Db.Partners.AddRange(
            new Partner { PartnerCode = "BP001", PartnerName = "Contoso Corporation", IsSupplier = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Partner { PartnerCode = "BP002", PartnerName = "DEF Logistics", IsCustomer = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Partner { PartnerCode = "BP003", PartnerName = "Gone", IsActive = true, IsDeleted = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var allRes = await new PartnerService(t.Db).ListAsync(default);
        var hitRes = await new PartnerService(t.Db).ListAsync(default, q: "logistics");

        var all = ResultAssert.Ok<ApiListSuccess<PartnerDto>>(allRes);
        Assert.Equal(2, all.Meta.Total);

        var hit = ResultAssert.Ok<ApiListSuccess<PartnerDto>>(hitRes);
        Assert.Equal("BP002", Assert.Single(hit.Data).PartnerCode);
    }

    [Fact]
    public async Task Get_returns_404_for_unknown_code()
    {
        using var t = TestDb.Empty();

        var res = await new PartnerService(t.Db).GetAsync("NOPE", default);

        ResultAssert.NotFound(res);
    }


    [Fact]
    public async Task Update_replaces_every_writable_field()
    {
        using var t = TestDb.Empty();
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Before", IsSupplier = true, IsCustomer = false,
            Country = "Japan", IsActive = true, Remark1 = "old", CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
        var body = new PartnerWriteDto("BP001", "After", IsSupplier: false, IsCustomer: true,
            Country: "Vietnam", Address: "HCMC", IsActive: false);

        var res = await new PartnerService(t.Db).UpdateAsync("BP001", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<PartnerDto>>(res).Data!;
        Assert.Equal("After", dto.PartnerName);
        Assert.False(dto.IsSupplier);
        Assert.True(dto.IsCustomer);
        Assert.Equal("Vietnam", dto.Country);
        Assert.Equal("HCMC", dto.Address);
        Assert.False(dto.IsActive);
        Assert.Null(dto.Remark1);
    }

    [Fact]
    public async Task Update_returns_404_for_unknown_row_and_400_for_blank_name()
    {
        using var t = TestDb.Empty();
        t.Db.Partners.Add(new Partner { PartnerCode = "BP001", PartnerName = "X", IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var unknownRow = await new PartnerService(t.Db).UpdateAsync("NOPE", Valid(), default);
        var blankName = await new PartnerService(t.Db).UpdateAsync("BP001", Valid(name: ""), default);

        ResultAssert.NotFound(unknownRow);
        ResultAssert.BadRequest(blankName, "VAL-001");
    }


    [Fact]
    public async Task Delete_soft_deletes_and_is_idempotent_only_once()
    {
        using var t = TestDb.Empty();
        t.Db.Partners.Add(new Partner { PartnerCode = "BP001", PartnerName = "X", IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var first = await new PartnerService(t.Db).DeleteAsync("BP001", default);
        var second = await new PartnerService(t.Db).DeleteAsync("BP001", default);

        ResultAssert.NoContent(first);
        Assert.True((await t.Db.Partners.FindAsync("BP001"))!.IsDeleted);

        ResultAssert.NotFound(second);
    }


    [Fact]
    public async Task ExportCsv_writes_flags_as_one_or_zero()
    {
        using var t = TestDb.Empty();
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Contoso", IsSupplier = true, IsCustomer = false,
            IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new PartnerService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains("BP001,Contoso,1,0", csv);
    }
}
