using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Keeps the user's display units (FX-07) in the local preferences, like the theme and the region, and applies them.
/// Nothing is set unless the user defines a unit.
/// </summary>
internal static class DisplayUnitPreferences
{
    private const string Key = "display_units";

    /// <summary>Characters that cannot be part of a unit name, because they separate the stored values.</summary>
    public static readonly char[] Reserved = ['|', ';'];

    /// <summary>Applies the stored units; called once at start.</summary>
    public static void Load() => DisplayUnits.Set(Parse(Preferences.Default.Get(Key, string.Empty)));

    /// <summary>Stores and applies the units.</summary>
    public static void Save(IEnumerable<DisplayUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        DisplayUnits.Set(units);
        Preferences.Default.Set(Key, string.Join(';', DisplayUnits.All.Select(u => string.Create(CultureInfo.InvariantCulture, $"{u.CurrencyCode}|{u.Name}|{u.Exponent}"))));
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