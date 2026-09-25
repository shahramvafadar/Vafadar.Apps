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

    private static DateFormatter CreateFormatter(AppLanguage language, CalendarSystem calendar)
    {
        var service = new LocalizationService(new LocalizationOptions(), new InMemorySettingsStore(), new Translator());
        service.Initialize();
        service.SetLanguage(language);
        service.SetCalendar(calendar);
        return new DateFormatter(service);
    }
}
