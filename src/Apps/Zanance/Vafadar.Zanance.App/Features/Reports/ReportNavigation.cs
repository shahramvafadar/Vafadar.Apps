namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>Copies an original report destination and its scalar/id scope before any native or source-validation wait.</summary>
public sealed class ReportNavigation
{
    private readonly Dictionary<string, object> _query;
    /// <summary>Gets the original route without consulting a later report selection.</summary>
    public string Route { get; }
    /// <summary>Captures only supported immutable routing values and independent id arrays.</summary>
    public ReportNavigation(string route, IReadOnlyDictionary<string, object>? query = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route); Route = route;
        _query = query?.ToDictionary(item => item.Key, item => Copy(item.Value), StringComparer.Ordinal) ?? [];
    }
    /// <summary>Adds a captured drill parameter without losing the original account, date, currency or review scope.</summary>
    public ReportNavigation WithQueryItem(string key, object value)
    { ArgumentException.ThrowIfNullOrWhiteSpace(key); var next = CopyQuery(); next[key] = Copy(value); return new(Route, next); }
    /// <summary>Returns a distinct native query, so a consumer cannot mutate the retained original destination.</summary>
    public Dictionary<string, object> CopyQuery() => _query.ToDictionary(item => item.Key, item => Copy(item.Value), StringComparer.Ordinal);
    private static object Copy(object value) => value switch
    {
        string or Guid or DateOnly or DateTime or DateTimeOffset or bool or int or long or decimal or Enum => value,
        IEnumerable<Guid> ids => ids.ToArray(),
        _ => throw new ArgumentException("The original report route contains an unsupported mutable value.", nameof(value)),
    };
}
