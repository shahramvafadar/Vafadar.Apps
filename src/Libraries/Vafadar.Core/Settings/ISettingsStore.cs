namespace Vafadar.Core.Settings;

/// <summary>
/// A small key/value store for user preferences (language, calendar, backup schedule, ...).
/// </summary>
/// <remarks>
/// Not intended for app data (use the database) or secrets (use platform secure storage).
/// Keys are namespaced by feature, e.g. <c>localization.language</c>.
/// </remarks>
public interface ISettingsStore
{
    /// <summary>Gets the stored value, or <see langword="null"/> when the key does not exist.</summary>
    string? Get(string key);

    /// <summary>Stores a value. Storing <see langword="null"/> removes the key.</summary>
    void Set(string key, string? value);

    /// <summary>Removes a key if it exists.</summary>
    void Remove(string key);
}
