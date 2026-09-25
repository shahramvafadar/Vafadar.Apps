using System.Collections.Concurrent;

namespace Vafadar.Core.Settings;

/// <summary>
/// A non-persistent <see cref="ISettingsStore"/> for tests and hosts without platform preferences.
/// </summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

    /// <inheritdoc />
    public void Set(string key, string? value)
    {
        if (value is null)
        {
            Remove(key);
            return;
        }

        _values[key] = value;
    }

    /// <inheritdoc />
    public void Remove(string key) => _values.TryRemove(key, out _);
}
