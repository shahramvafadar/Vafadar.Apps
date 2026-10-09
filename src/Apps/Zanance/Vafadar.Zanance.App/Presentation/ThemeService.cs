using Vafadar.Core.Settings;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>The user's theme choice.</summary>
public enum ThemeChoice
{
    /// <summary>Follows the device.</summary>
    System,

    /// <summary>Always light.</summary>
    Light,

    /// <summary>Always dark.</summary>
    Dark,
}

/// <summary>
/// Applies the light or dark theme (UX-08): the semantic colors, the platform theme for built-in controls, and a
/// notification so screens with computed colors reload. The choice is a local preference.
/// </summary>
public sealed partial class ThemeService
{
    private const string Key = "ui.theme";
    private readonly ISettingsStore _preferences;
    private readonly IThemeHost _host;
    private bool _initialized;
    private bool _applying;

    /// <summary>Creates the preference policy with a platform theme host.</summary>
    public ThemeService(ISettingsStore preferences, IThemeHost host)
    {
        _preferences = preferences;
        _host = host;
    }

    /// <summary>Raised after the effective theme changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the valid saved choice, falling back to the system for an unknown value.</summary>
    public ThemeChoice Choice => Enum.TryParse<ThemeChoice>(_preferences.Get(Key), out var choice) && Enum.IsDefined(choice) ? choice : ThemeChoice.System;

    /// <summary>Applies the saved choice and subscribes once to system theme changes.</summary>
    public void Initialize()
    {
        if (!_initialized)
        {
            _initialized = true;
            _host.SystemThemeChanged += (_, _) => { if (Choice == ThemeChoice.System) { Apply(); } };
        }
        Apply();
    }

    /// <summary>Saves and applies an explicit choice without closing any form.</summary>
    public void Set(ThemeChoice choice)
    {
        if (!Enum.IsDefined(choice)) { throw new ArgumentOutOfRangeException(nameof(choice)); }
        _preferences.Set(Key, choice == ThemeChoice.System ? null : choice.ToString());
        Apply();
    }

    private void Apply()
    {
        if (!_host.IsAvailable || _applying) { return; }
        _applying = true;
        try
        {
            // Native theme application can itself raise the system event; avoid a reentrant palette rebuild.
            var choice = Choice;
            _host.Apply(choice, choice == ThemeChoice.Dark || (choice == ThemeChoice.System && _host.SystemIsDark));
            Changed?.Invoke(this, EventArgs.Empty);
        }
        finally { _applying = false; }
    }
}
