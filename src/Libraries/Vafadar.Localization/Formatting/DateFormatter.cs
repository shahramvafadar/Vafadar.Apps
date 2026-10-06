using System.Globalization;
using System.Text.RegularExpressions;
using Vafadar.Core.Dates;

namespace Vafadar.Localization.Formatting;

/// <summary>
/// Default <see cref="IDateFormatter"/>.
/// </summary>
/// <remarks>
/// When the culture supports the selected calendar (e.g. Persian language with Persian or Gregorian calendar, or an
/// Arabic culture with Umm al-Qura) the culture formats the date. Otherwise (e.g. English with the Persian calendar, or
/// Persian with the lunar Hijri calendar) the date is converted here and formatted with the month names of
/// <see cref="PersianMonthNamesLatin"/>, <see cref="HijriMonthNamesPersian"/> or <see cref="HijriMonthNamesLatin"/>.
/// </remarks>
public sealed partial class DateFormatter(ILocalizationService localization) : IDateFormatter
{
    private static readonly PersianCalendar Persian = new();

    private static readonly string[] PersianMonthNamesLatin =
    [
        "Farvardin", "Ordibehesht", "Khordad", "Tir", "Mordad", "Shahrivar",
        "Mehr", "Aban", "Azar", "Dey", "Bahman", "Esfand",
    ];

    private static readonly string[] PersianMonthNamesLatinShort =
    [
        "Far", "Ord", "Kho", "Tir", "Mor", "Sha",
        "Mehr", "Aban", "Azar", "Dey", "Bah", "Esf",
    ];

    // The lunar Hijri months as they are written in Persian (short enough for date tiles as they are).
    private static readonly string[] HijriMonthNamesPersian =
    [
        "محرم", "صفر", "ربیع‌الاول", "ربیع‌الثانی", "جمادی‌الاول", "جمادی‌الثانی",
        "رجب", "شعبان", "رمضان", "شوال", "ذی‌القعده", "ذی‌الحجه",
    ];

    // The common transliteration, used in English and German.
    private static readonly string[] HijriMonthNamesLatin =
    [
        "Muharram", "Safar", "Rabi al-Awwal", "Rabi al-Thani", "Jumada al-Ula", "Jumada al-Akhirah",
        "Rajab", "Shaban", "Ramadan", "Shawwal", "Dhu al-Qadah", "Dhu al-Hijjah",
    ];

    // Three letters would make the two Rabi, Jumada and Dhu months look the same.
    private static readonly string[] HijriMonthNamesLatinShort =
    [
        "Muh", "Saf", "Rab I", "Rab II", "Jum I", "Jum II",
        "Raj", "Sha", "Ram", "Shaw", "Dhu Q", "Dhu H",
    ];

    /// <inheritdoc />
    public string Format(DateOnly date, DateFormatStyle style = DateFormatStyle.Short) =>
        FormatCore(date, style, localization.CurrentCulture, localization.CurrentCalendar);

    /// <inheritdoc />
    /// <remarks>Another calendar gets its own culture of the current language (e.g. Persian with Gregorian months).</remarks>
    public string Format(DateOnly date, DateFormatStyle style, CalendarSystem calendar) =>
        FormatCore(
            date,
            style,
            calendar == localization.CurrentCalendar ? localization.CurrentCulture : CultureFactory.Create(localization.CurrentLanguage, calendar),
            calendar);

    private static string FormatCore(DateOnly date, DateFormatStyle style, CultureInfo culture, CalendarSystem calendar)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);

        // Dates outside the calendar's range (e.g. an unset default value) must never crash the UI.
        if (dateTime < Persian.MinSupportedDateTime || dateTime > Persian.MaxSupportedDateTime)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var shortNames = style == DateFormatStyle.DayMonthShort;
        switch (calendar)
        {
            case CalendarSystem.Persian when culture.DateTimeFormat.Calendar is not PersianCalendar:
                return FormatOwn(
                    dateTime, style, culture, Persian.GetYear(dateTime), Persian.GetMonth(dateTime), Persian.GetDayOfMonth(dateTime),
                    shortNames ? PersianMonthNamesLatinShort : PersianMonthNamesLatin);

            // The culture's Umm al-Qura calendar covers 1900–2077 only; LunarHijri covers every other year as well.
            case CalendarSystem.Hijri when culture.DateTimeFormat.Calendar is not UmAlQuraCalendar umAlQura
                                           || dateTime < umAlQura.MinSupportedDateTime || dateTime > umAlQura.MaxSupportedDateTime:
                var (year, month, day) = LunarHijri.Parts(date);
                return FormatOwn(dateTime, style, culture, year, month, day, HijriMonthNames(culture, shortNames));
        }

        var format = style switch
        {
            DateFormatStyle.Long => "D",
            DateFormatStyle.MonthYear => "Y",
            DateFormatStyle.DayMonth => "M",
            // The culture's day-month pattern with its short month names, e.g. "Sep 25", "25. Sept." or "25 sept": a quoted
            // literal goes (Spanish "d 'de' MMMM"), so a date tile never reads "de" as part of the month.
            DateFormatStyle.DayMonthShort => QuotedLiteral().Replace(culture.DateTimeFormat.MonthDayPattern, " ")
                .Replace("MMMM", "MMM", StringComparison.Ordinal).Trim(),
            DateFormatStyle.Month => "MMMM",
            _ => "d",
        };

        return dateTime.ToString(format, culture);
    }

    /// <inheritdoc />
    public string Format(DateTimeOffset value, DateFormatStyle style = DateFormatStyle.Short) =>
        Format(DateOnly.FromDateTime(value.ToLocalTime().DateTime), style);

    // A culture with a Hijri calendar of its own (Arabic) has the names in its language; Persian has its own spelling.
    private static string[] HijriMonthNames(CultureInfo culture, bool shortNames) => culture.DateTimeFormat.Calendar switch
    {
        UmAlQuraCalendar or HijriCalendar => (shortNames ? culture.DateTimeFormat.AbbreviatedMonthNames : culture.DateTimeFormat.MonthNames)[..12],
        _ when culture.TwoLetterISOLanguageName == "fa" => HijriMonthNamesPersian,
        _ => shortNames ? HijriMonthNamesLatinShort : HijriMonthNamesLatin,
    };

    // The same layout for every converted calendar: weekday, day, month name and year; numeric dates year first. The
    // words between day, month and year are the culture's own: Spanish writes "3 de Mehr de 1405" ("d 'de' MMMM",
    // "MMMM 'de' yyyy"); English, German and Persian have no such word. The short form for date tiles has none.
    private static string FormatOwn(DateTime dateTime, DateFormatStyle style, CultureInfo culture, int year, int month, int day, string[] monthNames)
    {
        var monthName = monthNames[month - 1];
        var separator = culture.TextInfo.IsRightToLeft ? " " : ", ";
        var dayMonth = Joiner(culture.DateTimeFormat.MonthDayPattern);
        var monthYear = Joiner(culture.DateTimeFormat.YearMonthPattern);
        return style switch
        {
            DateFormatStyle.Long => string.Create(
                CultureInfo.InvariantCulture,
                $"{culture.DateTimeFormat.GetDayName(dateTime.DayOfWeek)}{separator}{day}{dayMonth}{monthName}{monthYear}{year}"),
            DateFormatStyle.MonthYear => string.Create(CultureInfo.InvariantCulture, $"{monthName}{monthYear}{year}"),
            DateFormatStyle.DayMonth => string.Create(CultureInfo.InvariantCulture, $"{day}{dayMonth}{monthName}"),
            DateFormatStyle.DayMonthShort => string.Create(CultureInfo.InvariantCulture, $"{day} {monthName}"),
            DateFormatStyle.Month => monthName,
            _ => string.Create(CultureInfo.InvariantCulture, $"{year:0000}/{month:00}/{day:00}"),
        };
    }

    // " de " for a pattern with the quoted word 'de' (Spanish), otherwise a plain space.
    private static string Joiner(string pattern) => QuotedLiteral().Match(pattern) is { Success: true } literal
        ? $" {literal.Value.Trim().Trim('\'').Trim()} "
        : " ";

    [GeneratedRegex(@"\s*'[^']*'\s*")]
    private static partial Regex QuotedLiteral();
}
