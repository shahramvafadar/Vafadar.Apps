using System.Globalization;

namespace Vafadar.Localization.Formatting;

/// <summary>
/// Default <see cref="IDateFormatter"/>.
/// </summary>
/// <remarks>
/// When the culture supports the selected calendar (e.g. Persian language with Persian or Gregorian calendar) the
/// culture formats the date. Otherwise (e.g. English or German with the Persian calendar) the date is converted with
/// <see cref="PersianCalendar"/> and formatted with transliterated month names.
/// </remarks>
public sealed class DateFormatter(ILocalizationService localization) : IDateFormatter
{
    private static readonly PersianCalendar Persian = new();

    private static readonly string[] PersianMonthNamesLatin =
    [
        "Farvardin", "Ordibehesht", "Khordad", "Tir", "Mordad", "Shahrivar",
        "Mehr", "Aban", "Azar", "Dey", "Bahman", "Esfand",
    ];

    /// <inheritdoc />
    public string Format(DateOnly date, DateFormatStyle style = DateFormatStyle.Short)
    {
        var culture = localization.CurrentCulture;
        var dateTime = date.ToDateTime(TimeOnly.MinValue);

        if (localization.CurrentCalendar == CalendarSystem.Persian && culture.DateTimeFormat.Calendar is not PersianCalendar)
        {
            return FormatPersianWithLatinNames(dateTime, style, culture);
        }

        var format = style switch
        {
            DateFormatStyle.Long => "D",
            DateFormatStyle.MonthYear => "Y",
            _ => "d",
        };

        return dateTime.ToString(format, culture);
    }

    /// <inheritdoc />
    public string Format(DateTimeOffset value, DateFormatStyle style = DateFormatStyle.Short) =>
        Format(DateOnly.FromDateTime(value.ToLocalTime().DateTime), style);

    private static string FormatPersianWithLatinNames(DateTime dateTime, DateFormatStyle style, CultureInfo culture)
    {
        var year = Persian.GetYear(dateTime);
        var month = Persian.GetMonth(dateTime);
        var day = Persian.GetDayOfMonth(dateTime);
        var monthName = PersianMonthNamesLatin[month - 1];

        return style switch
        {
            DateFormatStyle.Long => string.Create(
                CultureInfo.InvariantCulture,
                $"{culture.DateTimeFormat.GetDayName(dateTime.DayOfWeek)}, {day} {monthName} {year}"),
            DateFormatStyle.MonthYear => string.Create(CultureInfo.InvariantCulture, $"{monthName} {year}"),
            _ => string.Create(CultureInfo.InvariantCulture, $"{year:0000}/{month:00}/{day:00}"),
        };
    }
}
