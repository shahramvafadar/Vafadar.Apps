using Vafadar.Core.Settings;
using Vafadar.Localization.Formatting;

namespace Vafadar.Localization.Tests.Formatting;

public sealed class DateFormatterTests : IDisposable
{
    // 25 September 2026 (Gregorian) = 3 Mehr 1405 (Persian), a Friday.
    private static readonly DateOnly Date = new(2026, 9, 25);

    private readonly CultureScope _cultures = new();

    public void Dispose() => _cultures.Dispose();

    [Theory]
    [InlineData(DateFormatStyle.Short, "1405/07/03")]
    [InlineData(DateFormatStyle.Long, "Friday, 3 Mehr 1405")]
    [InlineData(DateFormatStyle.MonthYear, "Mehr 1405")]
    [InlineData(DateFormatStyle.DayMonth, "3 Mehr")]
    public void English_with_Persian_calendar_uses_transliterated_month_names(DateFormatStyle style, string expected)
    {
        var formatter = CreateFormatter(AppLanguages.English, CalendarSystem.Persian);

        Assert.Equal(expected, formatter.Format(Date, style));
    }

    [Theory]
    [InlineData(DateFormatStyle.Short, "1405/07/03")]
    [InlineData(DateFormatStyle.Long, "جمعه 3 مهر 1405")]
    [InlineData(DateFormatStyle.MonthYear, "مهر 1405")]
    [InlineData(DateFormatStyle.DayMonth, "3 مهر")]
    public void Persian_with_Persian_calendar_uses_conventional_Persian_patterns(DateFormatStyle style, string expected)
    {
        var formatter = CreateFormatter(AppLanguages.Persian, CalendarSystem.Persian);

        Assert.Equal(expected, formatter.Format(Date, style));
    }

    [Fact]
    public void Persian_with_Gregorian_calendar_shows_Gregorian_dates()
    {
        var formatter = CreateFormatter(AppLanguages.Persian, CalendarSystem.Gregorian);

        Assert.Equal("2026/09/25", formatter.Format(Date));
    }

    [Theory]
    [InlineData("en", "9/25/2026")]
    [InlineData("de", "25.09.2026")]
    public void Gregorian_dates_use_the_language_conventions(string language, string expected)
    {
        var formatter = CreateFormatter(AppLanguages.All.Single(l => l.CultureName == language), CalendarSystem.Gregorian);

        Assert.Equal(expected, formatter.Format(Date));
    }

    [Fact]
    public void German_with_Persian_calendar_uses_German_day_names()
    {
        var formatter = CreateFormatter(AppLanguages.German, CalendarSystem.Persian);

        Assert.Equal("Freitag, 3 Mehr 1405", formatter.Format(Date, DateFormatStyle.Long));
    }

    [Fact]
    public void Dates_outside_the_Persian_range_do_not_throw()
    {
        var formatter = CreateFormatter(AppLanguages.Persian, CalendarSystem.Persian);

        Assert.Equal("0001-01-01", formatter.Format(DateOnly.MinValue, DateFormatStyle.Long));
    }

    [Theory]
    [InlineData(DateFormatStyle.Short, "1448/04/14")]
    [InlineData(DateFormatStyle.Long, "Friday, 14 Rabi al-Thani 1448")]
    [InlineData(DateFormatStyle.MonthYear, "Rabi al-Thani 1448")]
    [InlineData(DateFormatStyle.DayMonth, "14 Rabi al-Thani")]
    public void English_with_the_lunar_Hijri_calendar_uses_Umm_al_Qura_dates(DateFormatStyle style, string expected)
    {
        // 25 September 2026 = 14 Rabi al-Thani 1448 in the Umm al-Qura calendar.
        var formatter = CreateFormatter(AppLanguages.English, CalendarSystem.Hijri);

        Assert.Equal(expected, formatter.Format(Date, style));
    }

    [Fact]
    public void Persian_and_German_with_the_lunar_Hijri_calendar_use_their_month_names_and_day_names()
    {
        Assert.Equal("جمعه 14 ربیع‌الثانی 1448", CreateFormatter(AppLanguages.Persian, CalendarSystem.Hijri).Format(Date, DateFormatStyle.Long));
        Assert.Equal("Freitag, 14 Rabi al-Thani 1448", CreateFormatter(AppLanguages.German, CalendarSystem.Hijri).Format(Date, DateFormatStyle.Long));
    }

    [Fact]
    public void Lunar_Hijri_dates_after_the_Umm_al_Qura_years_are_still_shown()
    {
        // Umm al-Qura ends in 2077 (1500 AH); later dates come from the tabular Hijri calendar instead of failing.
        var formatter = CreateFormatter(AppLanguages.English, CalendarSystem.Hijri);

        Assert.Equal("1523/10/20", formatter.Format(new DateOnly(2100, 1, 1)));
    }

    [Fact]
    public void Short_month_names_for_date_tiles_stay_distinct()
    {
        // Cutting the names to three letters would show "Rab" for both Rabi months.
        var hijri = CreateFormatter(AppLanguages.English, CalendarSystem.Hijri);
        Assert.Equal("14 Rab II", hijri.Format(Date, DateFormatStyle.DayMonthShort));
        Assert.Equal("1 Rab I", hijri.Format(Vafadar.Core.Dates.LunarHijri.ToDate(1448, 3, 1), DateFormatStyle.DayMonthShort));

        Assert.Equal("3 Mehr", CreateFormatter(AppLanguages.English, CalendarSystem.Persian).Format(Date, DateFormatStyle.DayMonthShort));
        Assert.Equal("Sep 25", CreateFormatter(AppLanguages.English, CalendarSystem.Gregorian).Format(Date, DateFormatStyle.DayMonthShort));
        Assert.Equal("14 ربیع‌الثانی", CreateFormatter(AppLanguages.Persian, CalendarSystem.Hijri).Format(Date, DateFormatStyle.DayMonthShort));
    }

    private static DateFormatter CreateFormatter(AppLanguage language, CalendarSystem calendar)
    {
        var service = new LocalizationService(new LocalizationOptions(), new InMemorySettingsStore(), new Translator());
        service.Initialize();
        service.SetLanguage(language);
        service.SetCalendar(calendar);
        return new DateFormatter(service);
    }
}
