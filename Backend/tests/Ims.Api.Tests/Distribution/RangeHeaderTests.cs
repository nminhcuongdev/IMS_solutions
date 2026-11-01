using Ims.Api.Http;
using Xunit;

namespace Ims.Api.Tests.Distribution;

public class RangeHeaderTests
{
    [Theory]
    [InlineData("bytes=0-99", 1000, 0, 99)]
    [InlineData("bytes=500-", 1000, 500, 999)]
    [InlineData("bytes=900-5000", 1000, 900, 999)]
    [InlineData("bytes=-100", 1000, 900, 999)]
    [InlineData("bytes=-5000", 1000, 0, 999)]
    public void A_single_range_is_resolved_against_the_length(string header, long length, long from, long to)
    {
        Assert.True(RangeFileResult.TryParseRange(header, length, out var a, out var b));
        Assert.Equal((from, to), (a, b));
    }

    [Theory]
    [InlineData("bytes=1000-", 1000)]
    [InlineData("bytes=5-2", 1000)]
    [InlineData("bytes=0-1,5-6", 1000)]
    [InlineData("items=0-1", 1000)]
    [InlineData("bytes=-0", 1000)]
    [InlineData("bytes=x-1", 1000)]
    public void An_unsatisfiable_or_unsupported_range_is_refused(string header, long length)
    {
        Assert.False(RangeFileResult.TryParseRange(header, length, out _, out _));
    }
}
