using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Vafadar.Localization;

/// <summary>
/// Looks up UI strings for the current language and notifies bindings when the language changes,
/// so the UI updates immediately without restarting the app.
/// </summary>
/// <remarks>
/// XAML binds to the indexer (<c>Translator.Instance["Key"]</c>) through the <c>Translate</c> markup extension of
/// <c>Vafadar.Maui</c>. Code uses <see cref="GetString"/> or <see cref="Format"/>.
/// A missing key returns the key itself, which makes untranslated strings easy to spot.
/// </remarks>
public sealed class Translator : INotifyPropertyChanged
{
    private static readonly ResourceManager SharedStrings =
        new("Vafadar.Localization.Resources.SharedStrings", typeof(Translator).Assembly);

    private ImmutableArray<ResourceManager> _resources = [SharedStrings];
    private CultureInfo _culture = CultureInfo.CurrentUICulture;

    /// <summary>Gets the app-wide instance used by XAML and registered in dependency injection.</summary>
    public static Translator Instance { get; } = new();

    /// <summary>Raised with an empty property name whenever the culture changes, refreshing every binding.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the culture strings are currently looked up for.</summary>
    public CultureInfo Culture => _culture;

    /// <summary>Gets the string for <paramref name="key"/> in the current culture.</summary>
    public string this[string key] => GetString(key);

    /// <summary>Gets the string for <paramref name="key"/> in the current culture, or the key when it is missing.</summary>
    public string GetString(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        foreach (var resources in _resources)
        {
            if (resources.GetString(key, _culture) is { } value)
            {
                return value;
            }
        }

        return key;
    }

    /// <summary>Formats the string for <paramref name="key"/> with the current culture.</summary>
    public string Format(string key, params object?[] args) => string.Format(_culture, GetString(key), args);

    /// <summary>
    /// Adds resources that take precedence over everything added before (resources added later win).
    /// </summary>
    public void AddResources(ResourceManager resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ImmutableInterlocked.Update(ref _resources, static (list, item) => list.Contains(item) ? list : list.Insert(0, item), resources);
    }

    /// <summary>Switches the lookup culture and refreshes all bindings.</summary>
    public void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        _culture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
