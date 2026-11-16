using System.Linq.Expressions;

namespace Ims.Api.Contracts;

public sealed class SortMap<T>
{
    private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> _primary =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Func<IOrderedQueryable<T>, IOrderedQueryable<T>>> _tieBreakers = new();
    private string _defaultKey = "";

    public SortMap<T> Add<TKey>(string key, Expression<Func<T, TKey>> selector)
    {
        _primary[key] = (q, desc) => desc ? q.OrderByDescending(selector) : q.OrderBy(selector);
        return this;
    }

    public SortMap<T> Add<TKey1, TKey2>(
        string key, Expression<Func<T, TKey1>> first, Expression<Func<T, TKey2>> second)
    {
        _primary[key] = (q, desc) => desc
            ? q.OrderByDescending(first).ThenByDescending(second)
            : q.OrderBy(first).ThenBy(second);
        return this;
    }

    public SortMap<T> Default(string key)
    {
        if (!_primary.ContainsKey(key))
            throw new InvalidOperationException($"SortMap<{typeof(T).Name}>: default column '{key}' has not been added.");
        _defaultKey = key;
        return this;
    }

    public SortMap<T> ThenAlwaysBy<TKey>(Expression<Func<T, TKey>> selector)
    {
        _tieBreakers.Add(q => q.ThenBy(selector));
        return this;
    }

    public IOrderedQueryable<T> Apply(IQueryable<T> query, string? sort)
    {
        var desc = sort?.StartsWith('-') == true;
        var key = (desc ? sort![1..] : sort ?? "").Trim();
        if (!_primary.TryGetValue(key, out var order))
        {
            order = _primary[_defaultKey];
            desc = key.Length == 0 && desc;
        }

        var ordered = order(query, desc);
        foreach (var t in _tieBreakers) ordered = t(ordered);
        return ordered;
    }
}
