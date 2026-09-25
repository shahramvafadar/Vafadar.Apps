using Vafadar.Core.Settings;

namespace Vafadar.Maui.Hosting;

/// <summary>
/// <see cref="ISettingsStore"/> backed by MAUI <see cref="IPreferences"/> (SharedPreferences, NSUserDefaults, ...).
/// </summary>
internal sealed class MauiSettingsStore(IPreferences preferences) : ISettingsStore
{
    public string? Get(string key) => preferences.Get<string?>(key, null);

    public void Set(string key, string? value)
    {
        if (value is null)
        {
            preferences.Remove(key);
        }
        else
        {
            preferences.Set(key, value);
        }
    }

    public void Remove(string key) => preferences.Remove(key);
}
