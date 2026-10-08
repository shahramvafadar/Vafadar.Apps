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

    [Theory]
    [InlineData("IR", DayOfWeek.Saturday)]
    [InlineData("US", DayOfWeek.Sunday)]
    [InlineData("DE", DayOfWeek.Monday)]
    public void A_region_suggests_the_first_day_of_the_week(string region, DayOfWeek expected)
    {
        var service = CreateService();
        service.Initialize();

        service.SetRegion(region);

        Assert.Equal(region, service.CurrentRegion);
        Assert.Equal(expected, service.FirstDayOfWeek);
        Assert.Equal(expected, service.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
        Assert.True(service.IsFirstDayOfWeekAutomatic);
    }

    [Fact]
    public void A_chosen_first_day_wins_over_the_region_and_survives_a_restart_and_a_language_change()
    {
        var service = CreateService();
        service.Initialize();
        service.SetRegion("IR");
        service.SetFirstDayOfWeek(DayOfWeek.Monday);

        var restarted = CreateService();
        restarted.Initialize();
        restarted.SetLanguage(AppLanguages.German);

        Assert.Equal("IR", restarted.CurrentRegion);
        Assert.Equal(DayOfWeek.Monday, restarted.FirstDayOfWeek);
        Assert.False(restarted.IsFirstDayOfWeekAutomatic);

        restarted.SetFirstDayOfWeek(null);
        Assert.Equal(DayOfWeek.Saturday, restarted.FirstDayOfWeek);
    }

    [Fact]
    public void Weekend_days_follow_the_region_or_a_persian_ui()
    {
        var english = CultureInfo.GetCultureInfo("en");
        Assert.Equal([DayOfWeek.Friday], Regions.WeekendDays("IR", english));
        Assert.Equal([DayOfWeek.Saturday, DayOfWeek.Sunday], Regions.WeekendDays("DE", english));
        Assert.Equal([DayOfWeek.Friday], Regions.WeekendDays(null, CultureInfo.GetCultureInfo("fa")));
        Assert.Equal([DayOfWeek.Saturday, DayOfWeek.Sunday], Regions.WeekendDays(null, english));
    }

    [Fact]
    public void Region_names_are_in_the_app_language_not_the_region_language()
    {
        Assert.Equal("Germany", Regions.DisplayName("DE", CultureInfo.GetCultureInfo("en-US")));
        Assert.Equal("Deutschland", Regions.DisplayName("DE", CultureInfo.GetCultureInfo("de-DE")));
        Assert.Equal("آلمان", Regions.DisplayName("DE", CultureInfo.GetCultureInfo("fa-IR")));
        Assert.Equal("Iran", Regions.DisplayName("IR", CultureInfo.GetCultureInfo("en-US")));
    }

    [Fact]
    public void Region_does_not_change_language_calendar_or_number_format_and_can_be_cleared()
    {
        var service = CreateService();
        service.Initialize();
        service.SetLanguage(AppLanguages.German);
        var separator = service.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        service.SetRegion("US");

        Assert.Equal(AppLanguages.German, service.CurrentLanguage);
        Assert.Equal(separator, service.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        service.SetRegion(null);
        Assert.Null(service.CurrentRegion);
        Assert.Throws<ArgumentException>(() => service.SetRegion("XX1"));
    }
    [Fact]
    [Trait("AT", "AT-74")]
    public void English_with_German_formats_keeps_English_text_and_independent_holidays()
    {
        var service = CreateService();
        service.Initialize();
        service.SetLanguage(AppLanguages.English);
        service.SetCalendar(CalendarSystem.Gregorian);
        service.SetFormattingCulture("de-DE");
        service.SetRegion("IR");

        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("en", _translator.Culture.Name);
        Assert.False(service.IsRightToLeft);
        Assert.Equal("1.234,56", 1234.56m.ToString("N2", service.CurrentCulture));
        Assert.Equal("08.10.2026", new Vafadar.Localization.Formatting.DateFormatter(service).Format(new DateOnly(2026, 10, 8)));
        Assert.Equal("October", service.CurrentCulture.DateTimeFormat.GetMonthName(10));
        Assert.Equal("IR", service.CurrentRegion);
        Assert.Equal(CalendarSystem.Gregorian, service.CurrentCalendar);
    }

    [Fact]
    [Trait("AT", "AT-74")]
    public void Explicit_regional_format_and_digits_survive_language_change_and_restart()
    {
        var service = CreateService();
        service.Initialize();
        service.SetFormattingCulture("de-DE");
        service.SetDigits(DigitStyle.Latin);
        service.SetRegion("DE");
        service.SetCalendar(CalendarSystem.Gregorian);
        service.SetLanguage(AppLanguages.Persian);
        var restarted = CreateService();
        restarted.Initialize();

        Assert.True(restarted.IsRightToLeft);
        Assert.Equal("de-DE", restarted.FormattingCultureName);
        Assert.Equal(DigitStyle.Latin, restarted.CurrentDigits);
        Assert.Equal("DE", restarted.CurrentRegion);
        Assert.Equal("08.10.2026", new Vafadar.Localization.Formatting.DateFormatter(restarted).Format(new DateOnly(2026, 10, 8)));
        Assert.Equal("1.234,56", 1234.56m.ToString("N2", restarted.CurrentCulture));
        Assert.Equal(DayOfWeek.Monday, restarted.FirstDayOfWeek);
    }

    [Fact]
    public void Clearing_regional_format_restores_language_formats_without_clearing_other_choices()
    {
        var service = CreateService();
        service.Initialize();
        service.SetLanguage(AppLanguages.English);
        service.SetFormattingCulture("de-DE");
        service.SetDigits(DigitStyle.Persian);
        service.SetRegion("DE");
        service.SetFirstDayOfWeek(DayOfWeek.Tuesday);
        service.SetFormattingCulture(null);

        Assert.Null(service.FormattingCultureName);
        Assert.Equal("1,234.56", 1234.56m.ToString("N2", service.CurrentCulture));
        Assert.Equal(DigitStyle.Persian, service.CurrentDigits);
        Assert.Equal("DE", service.CurrentRegion);
        Assert.Equal(DayOfWeek.Tuesday, service.FirstDayOfWeek);
        Assert.Throws<ArgumentException>(() => service.SetFormattingCulture("en"));
        Assert.Throws<ArgumentException>(() => service.SetFormattingCulture("unknown-culture"));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.SetDigits((DigitStyle)99));
    }

    [Fact]
    public void Damaged_saved_display_choices_fall_back_to_existing_language_defaults()
    {
        _settings.Set(LocalizationService.FormattingKey, "invalid");
        _settings.Set(LocalizationService.DigitsKey, "77");
        var service = CreateService();
        service.Initialize();
        Assert.Null(service.FormattingCultureName);
        Assert.Equal(DigitStyle.LanguageDefault, service.CurrentDigits);
    }

    [Fact]
    [Trait("AT", "AT-74")]
    public void Regional_order_applies_to_converted_calendar_parts_and_alternate_calendar_labels()
    {
        var service = CreateService();
        service.Initialize();
        service.SetLanguage(AppLanguages.English);
        service.SetFormattingCulture("de-DE");
        service.SetCalendar(CalendarSystem.Persian);
        var date = new DateOnly(2026, 10, 8);
        var formatter = new Vafadar.Localization.Formatting.DateFormatter(service);
        Assert.Equal("16.07.1405", formatter.Format(date));
        Assert.Equal("08.10.2026", formatter.Format(date, Vafadar.Localization.Formatting.DateFormatStyle.Short, CalendarSystem.Gregorian));
        Assert.Equal([DatePart.Day, DatePart.Month, DatePart.Year], CalendarDates.InputOrder(service.CurrentCulture, CalendarSystem.Gregorian));
    }

    [Theory]
    [InlineData("en", CalendarSystem.Persian, "Day,Month,Year")]
    [InlineData("fa", CalendarSystem.Gregorian, "Year,Month,Day")]
    [InlineData("fa", CalendarSystem.Persian, "Year,Month,Day")]
    public void Regional_date_fields_follow_the_numeric_pattern_in_both_directions(string language, CalendarSystem calendar, string expected)
    {
        var service = CreateService();
        service.Initialize();
        service.SetLanguage(service.SupportedLanguages.First(l => l.CultureName == language));
        service.SetFormattingCulture("de-DE");
        service.SetCalendar(calendar);
        Assert.Equal(expected, string.Join(',', CalendarDates.InputOrder(service.CurrentCulture, calendar, useRegionalPattern: true)));
    }

    private LocalizationService CreateService(Action<LocalizationOptions>? configure = null)
    {
        var options = new LocalizationOptions();
        configure?.Invoke(options);
        return new LocalizationService(options, _settings, _translator);
    }
}
