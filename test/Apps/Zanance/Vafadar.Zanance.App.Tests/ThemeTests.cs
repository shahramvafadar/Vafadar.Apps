using Vafadar.Core.Settings;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual theme preference policy, including system changes and native callback reentrancy.</summary>
public sealed class ThemeTests
{
    [Theory, Trait("AT", "AT-80")]
    [InlineData(null)]
    [InlineData("unknown")]
    [InlineData("99")]
    public void Invalid_or_missing_saved_choices_follow_the_system(string? saved)
    {
        var preferences = new InMemorySettingsStore(); preferences.Set("ui.theme", saved);
        var host = new ThemePlatform { SystemIsDark = true };
        var theme = new ThemeService(preferences, host); theme.Initialize();
        Assert.Equal(ThemeChoice.System, theme.Choice);
        Assert.Equal((ThemeChoice.System, true), Assert.Single(host.Applied));
    }

    [Fact, Trait("AT", "AT-80")]
    public void Explicit_theme_ignores_system_changes_and_system_choice_resumes_them()
    {
        var preferences = new InMemorySettingsStore(); var host = new ThemePlatform();
        var theme = new ThemeService(preferences, host); theme.Initialize(); theme.Set(ThemeChoice.Dark);
        var before = host.Applied.Count; host.SystemIsDark = false; host.Raise();
        Assert.Equal(before, host.Applied.Count); Assert.Equal("Dark", preferences.Get("ui.theme"));
        theme.Set(ThemeChoice.System); host.SystemIsDark = true; host.Raise();
        Assert.Equal((ThemeChoice.System, true), host.Applied[^1]); Assert.Null(preferences.Get("ui.theme"));
    }

    [Fact, Trait("AT", "AT-80")]
    public void Native_callbacks_do_not_reenter_palette_application_and_initialization_subscribes_once()
    {
        var host = new ThemePlatform { RaiseWhileApplying = true };
        var theme = new ThemeService(new InMemorySettingsStore(), host);
        theme.Initialize(); theme.Initialize(); Assert.Equal(2, host.Applied.Count);
        host.Raise(); Assert.Equal(3, host.Applied.Count);
    }

    [Fact, Trait("AT", "AT-80")]
    public void Choice_saved_without_a_window_is_applied_when_the_host_becomes_available()
    {
        var preferences = new InMemorySettingsStore(); var host = new ThemePlatform { IsAvailable = false };
        var theme = new ThemeService(preferences, host); theme.Set(ThemeChoice.Light);
        Assert.Empty(host.Applied); Assert.Equal("Light", preferences.Get("ui.theme"));
        host.IsAvailable = true; theme.Initialize(); Assert.Equal((ThemeChoice.Light, false), Assert.Single(host.Applied));
        Assert.Throws<ArgumentOutOfRangeException>(() => theme.Set((ThemeChoice)20));
        Assert.Equal(ThemeChoice.Light, theme.Choice);
    }
}
