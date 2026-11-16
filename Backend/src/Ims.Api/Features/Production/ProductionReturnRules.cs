using Ims.Domain.Production;

namespace Ims.Api.Features.Production;

internal static class ProductionReturnRules
{
    public static List<(ProductionResult Source, decimal Available)> OpenBySource(
        IEnumerable<ProductionResult> consumedNewestFirst, IEnumerable<ProductionResult> returned)
    {
        var left = returned
            .GroupBy(r => (r.ProductionOrderNo, r.LineNo, r.SerialNo))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Qty));
        var newestFirst = consumedNewestFirst.OrderByDescending(c => c.No).ToList();
        var open = new Dictionary<long, decimal>();
        foreach (var c in newestFirst)
        {
            var key = (c.ProductionOrderNo, c.LineNo, c.SerialNo);
            var already = left.GetValueOrDefault(key);
            var take = Math.Min(already, c.Qty);
            left[key] = already - take;
            open[c.No] = c.Qty - take;
        }
        return newestFirst.Select(c => (c, open.GetValueOrDefault(c.No))).Where(x => x.Item2 > 0).ToList();
    }

    public static decimal ReturnedAgainst(ProductionResult consume, IEnumerable<ProductionResult> consumedSameSerial,
        IEnumerable<ProductionResult> returnedSameSerial)
    {
        var open = OpenBySource(consumedSameSerial.OrderByDescending(c => c.No), returnedSameSerial);
        var left = open.Where(x => x.Source.No == consume.No).Select(x => x.Available).FirstOrDefault();
        return consume.Qty - left;
    }
}
