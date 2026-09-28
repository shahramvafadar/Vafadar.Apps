using Vafadar.Core.Settings;

namespace Vafadar.Finance.App.Presentation;

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
public sealed class ThemeService(ISettingsStore preferences)
{
    private const string Key = "ui.theme";

    /// <summary>Raised after the effective theme changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the saved choice.</summary>
    public ThemeChoice Choice => Enum.TryParse<ThemeChoice>(preferences.Get(Key), out var choice) && Enum.IsDefined(choice) ? choice : ThemeChoice.System;

    /// <summary>Applies the saved choice and follows device changes while the choice is "System".</summary>
    public void Initialize(Application app)
    {
        ArgumentNullException.ThrowIfNull(app);
        Apply(app);
        app.RequestedThemeChanged += (_, _) =>
        {
            if (Choice == ThemeChoice.System)
            {
                Apply(app);
            }
        };
    }

    /// <summary>Saves and applies a choice.</summary>
    public void Set(ThemeChoice choice)
    {
        preferences.Set(Key, choice == ThemeChoice.System ? null : choice.ToString());
        if (Application.Current is { } app)
        {
            Apply(app);
        }
    }

    private void Apply(Application app)
    {
        app.UserAppTheme = Choice switch
        {
            ThemeChoice.Light => AppTheme.Light,
            ThemeChoice.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
        var dark = Choice == ThemeChoice.Dark || (Choice == ThemeChoice.System && app.PlatformAppTheme == AppTheme.Dark);
        Palette.Apply(app.Resources, dark);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}