using Microsoft.AspNetCore.Http;
using Ims.Api.Hosting;
using Xunit;

namespace Ims.Api.Tests.Hosting;

public class ExceptionErrorsTests
{
    [Theory]
    [InlineData(2628)]
    [InlineData(8152)]
    public void Text_longer_than_its_column_is_an_input_error_not_a_crash(int number)
    {
        var (status, body) = ExceptionErrors.ForSqlNumber(number);
        Assert.Equal((400, "VAL-005"), (status, body.Code));
    }

    [Fact]
    public void A_number_too_large_for_its_column_is_a_quantity_error()
    {
        var (status, body) = ExceptionErrors.ForSqlNumber(8115);
        Assert.Equal((400, "QTY-001"), (status, body.Code));
    }

    [Fact]
    public void A_reference_to_a_row_that_does_not_exist_is_not_found()
    {
        var (status, body) = ExceptionErrors.ForSqlNumber(547);
        Assert.Equal((404, "SYS-404"), (status, body.Code));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(53)]
    [InlineData(4060)]
    [InlineData(18456)]
    [InlineData(10060)]
    [InlineData(-1)]
    public void A_database_that_cannot_be_reached_is_DB_001(int number)
    {
        var (status, body) = ExceptionErrors.ForSqlNumber(number);
        Assert.Equal((500, "DB-001"), (status, body.Code));
    }

    [Fact]
    public void Any_other_sql_error_stays_a_system_error()
    {
        var (status, body) = ExceptionErrors.ForSqlNumber(1205);
        Assert.Equal((500, "SYS-004"), (status, body.Code));
    }

    [Fact]
    public void A_body_that_cannot_be_read_is_a_400_with_an_envelope()
    {
        var (status, body) = ExceptionErrors.For(new BadHttpRequestException("Failed to read parameter"));
        Assert.Equal((400, "SYS-004"), (status, body.Code));
    }

    [Fact]
    public void A_number_the_client_library_cannot_send_is_a_quantity_error()
    {
        var ex = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "save failed", new ArgumentException("Parameter value '100000000000000000000' is out of range."));

        var (status, body) = ExceptionErrors.For(ex);

        Assert.Equal((400, "QTY-001"), (status, body.Code));
    }

    [Fact]
    public void An_argument_error_outside_a_save_is_still_a_server_error()
    {
        var (status, body) = ExceptionErrors.For(new ArgumentException("bug"));

        Assert.Equal((500, "SYS-004"), (status, body.Code));
    }
}
