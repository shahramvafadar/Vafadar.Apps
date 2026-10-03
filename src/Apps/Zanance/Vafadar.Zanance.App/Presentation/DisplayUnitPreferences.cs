using System.Globalization;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Keeps the user's display units (FX-07) in the settings of the open profile and applies them (ZEX-P20): each profile
/// has its own units, and they are part of its backups. Earlier versions kept them in the device preferences; a profile
/// without units of its own takes those once. Nothing is set unless the user defines a unit.
/// </summary>
internal static class DisplayUnitPreferences
{
    // The device preference of versions before ZEX-P20; read once per profile, never written any more.
    private const string LegacyKey = "display_units";

    /// <summary>Characters that cannot be part of a unit name, because they separate the stored values.</summary>
    public static readonly char[] Reserved = ['|', ';'];

    /// <summary>Applies the units of the open profile; called after the database is ready and after a profile switch.</summary>
    public static void Load(ZananceStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var settings = store.GetSettings();
        if (settings.DisplayUnits is null)
        {
            settings.DisplayUnits = Preferences.Default.Get(LegacyKey, string.Empty);
            store.SaveSettings(settings);
        }

        DisplayUnits.Set(Parse(settings.DisplayUnits));
    }

    /// <summary>Removes every unit of the open profile, e.g. with "Delete all data".</summary>
    public static Task ClearAsync(ZananceStore store) => SaveAsync(store, []);

    /// <summary>Stores the units in the open profile and applies them.</summary>
    public static async Task SaveAsync(ZananceStore store, IEnumerable<DisplayUnit> units)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(units);
        DisplayUnits.Set(units);
        var settings = await store.GetSettingsAsync();
        settings.DisplayUnits = string.Join(';', DisplayUnits.All.Select(u => string.Create(CultureInfo.InvariantCulture, $"{u.CurrencyCode}|{u.Name}|{u.Exponent}")));
        await store.SaveSettingsAsync(settings);
    }

    private static IEnumerable<DisplayUnit> Parse(string text)
    {
        foreach (var item in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (item.Split('|') is [var code, var name, var exponent] && int.TryParse(exponent, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                yield return new DisplayUnit(code, name, value);
            }
        }
    }
}