namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Reuses presentation objects within one immutable source/display snapshot. The owner replaces this cache when
/// data or display preferences change; it never changes the complete source list or stores anything on disk.
/// </summary>
internal sealed class SnapshotProjectionCache<TSource, TKey, TValue>(Func<TSource, TKey> key, Func<TSource, TValue> project)
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _values = [];

    /// <summary>Returns the same presentation object when a filtered snapshot includes a previously seen source.</summary>
    public TValue Get(TSource source)
    {
        var id = key(source);
        if (!_values.TryGetValue(id, out var value))
        {
            value = project(source);
            _values.Add(id, value);
        }

        return value;
    }
}
