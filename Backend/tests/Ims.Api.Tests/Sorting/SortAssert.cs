using Ims.Api.Contracts;
using Ims.Api.Tests.Infrastructure;
using Xunit;

namespace Ims.Api.Tests.Sorting;

internal static class SortAssert
{
    private sealed class NullFirst : IComparer<object?>
    {
        public int Compare(object? x, object? y) => (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (string a, string b) => string.CompareOrdinal(a, b),
            _ => Comparer<object>.Default.Compare(x, y),
        };
    }

    public static async Task SortsAsync<T>(Func<string, Task<ServiceResult>> list, string key, Func<T, object?> by)
    {
        var asc = ResultAssert.Ok<ApiListSuccess<T>>(await list(key)).Data.Select(by).ToList();
        var desc = ResultAssert.Ok<ApiListSuccess<T>>(await list("-" + key)).Data.Select(by).ToList();

        Assert.True(asc.Distinct().Count() > 1, $"'{key}': the seed must hold at least two different values");
        Assert.Equal(asc.OrderBy(x => x, new NullFirst()).ToList(), asc);
        Assert.Equal(Enumerable.Reverse(asc).ToList(), desc);
    }
}
