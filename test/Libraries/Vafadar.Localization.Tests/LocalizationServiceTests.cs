using System.Globalization;
using Vafadar.Core.Settings;

namespace Vafadar.Localization.Tests;

public sealed class LocalizationServiceTests : IDisposable
{
    private readonly CultureScope _cultures = new();
    private readonly InMemorySettingsStore _settings = new();
    private readonly Translator _translator = new();

    public void Dispose() => _cultures.Dispose();

    [Theory]
    [InlineData("de-DE", "de")]
    [InlineData("fa-IR", "fa")]
    [InlineData("en-GB", "en")]
    public void Initialize_without_saved_choice_uses_the_device_language(string deviceCulture, string expected)
    {
        using var device = CultureScope.WithUiCulture(deviceCulture);
        var service = CreateService();

        service.Initialize();

        Assert.Equal(expected, service.CurrentLanguage.CultureName);
    }

    [Fact]
    public void Initialize_with_unsupported_device_language_uses_the_default_language()
    {
        using var device = CultureScope.WithUiCulture("ja-JP");
        var service = CreateService();

        service.Initialize();

        Assert.Equal(AppLanguages.English, service.CurrentLanguage);
    }

    [Fact]
    public void Initialize_prefers_the_saved_language()
    {
        using var device = CultureScope.WithUiCulture("de-DE");
        _settings.Set(LocalizationService.LanguageKey, "fa");
        var service = CreateService();

        service.Initialize();

        Assert.Equal(AppLanguages.Persian, service.CurrentLanguage);
    }

    [Fact]
    public void SetLanguage_saves_the_choice_applies_the_cultures_and_raises_Changed()
    {
        var service = CreateService();
        service.Initialize();
        var changed = 0;
        service.Changed += (_, _) => changed++;

        service.SetLanguage(AppLanguages.German);

        Assert.Equal("de", _settings.Get(LocalizationService.LanguageKey));
        Assert.Equal("de", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("de", CultureInfo.CurrentCulture.Name);
        Assert.Equal("de", CultureInfo.DefaultThreadCurrentUICulture?.Name);
        Assert.Equal("de", _translator.Culture.Name);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Persian_is_right_to_left_and_defaults_to_the_Persian_calendar()
    {
        var service = CreateService();
        service.Initialize();

        service.SetLanguage(AppLanguages.Persian);

        Assert.True(service.IsRightToLeft);
        Assert.Equal(CalendarSystem.Persian, service.CurrentCalendar);
        Assert.IsType<PersianCalendar>(service.CurrentCulture.DateTimeFormat.Calendar);
    }

    [Fact]
    public void Persian_numbers_use_unambiguous_latin_separators()
    {
        var service = CreateService();
        service.Initialize();

        service.SetLanguage(AppLanguages.Persian);

        Assert.Equal("1,250.50", 1250.5m.ToString("N2", service.CurrentCulture));
        Assert.Equal("1,250.50", 1250.5m.ToString("N2", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Calendar_follows_the_language_until_the_user_picks_one()
    {
        var service = CreateService();
        service.Initialize();

        service.SetLanguage(AppLanguages.Persian);
        Assert.Equal(CalendarSystem.Persian, service.CurrentCalendar);

        service.SetLanguage(AppLanguages.German);
        Assert.Equal(CalendarSystem.Gregorian, service.CurrentCalendar);
    }

    [Fact]
    public void An_explicit_calendar_choice_survives_language_changes()
    {
        var service = CreateService();
        service.Initialize();
        service.SetLanguage(AppLanguages.Persian);

        service.SetCalendar(CalendarSystem.Gregorian);
        service.SetLanguage(AppLanguages.German);
        service.SetLanguage(AppLanguages.Persian);

        Assert.Equal(CalendarSystem.Gregorian, service.CurrentCalendar);
        Assert.IsType<GregorianCalendar>(service.CurrentCulture.DateTimeFormat.Calendar);
    }

    [Fact]
    public void SetLanguage_rejects_languages_the_app_does_not_support()
    {
        var service = CreateService(options => options.SupportedLanguages.Remove(AppLanguages.German));
        service.Initialize();

        Assert.Throws<ArgumentException>(() => service.SetLanguage(AppLanguages.German));
    }

    private LocalizationService CreateService(Action<LocalizationOptions>? configure = null)
    {
        var options = new LocalizationOptions();
        configure?.Invoke(options);
        return new LocalizationService(options, _settings, _translator);
    }
}
