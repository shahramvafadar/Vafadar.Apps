using System.Globalization;

namespace Vafadar.Localization;

/// <summary>
/// Creates the formatting culture for a language and calendar combination.
/// </summary>
internal static class CultureFactory
{
    public static CultureInfo Create(AppLanguage language, CalendarSystem calendar)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(language.CultureName).Clone();
        var format = culture.DateTimeFormat;

        // Only calendars that are valid for the culture can be assigned. For other combinations (e.g. English with
        // the Persian calendar) the culture keeps its default calendar and DateFormatter formats the date itself.
        var requested = calendar switch
        {
            CalendarSystem.Persian => culture.OptionalCalendars.OfType<PersianCalendar>().FirstOrDefault(),
            _ => culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault() as Calendar,
        };

        if (requested is not null && requested.GetType() != format.Calendar.GetType())
        {
            format.Calendar = requested;
        }

        // The built-in Persian patterns are awkward ("1405 مهر 3, جمعه"); use the conventional order instead.
        // Patterns must be set after the calendar, because assigning a calendar resets them.
        if (culture.TwoLetterISOLanguageName == "fa")
        {
            format.ShortDatePattern = "yyyy/MM/dd";
            format.LongDatePattern = "dddd d MMMM yyyy";
            format.YearMonthPattern = "MMMM yyyy";
            format.MonthDayPattern = "d MMMM";

            // The app shows Latin digits; next to them the Arabic separators (U+066B, U+066C) look like commas, so
            // "1٬250٫50" reads as "1,250,50". Use the unambiguous Latin separators instead (input accepts both).
            var numbers = culture.NumberFormat;
            numbers.NumberDecimalSeparator = numbers.CurrencyDecimalSeparator = numbers.PercentDecimalSeparator = ".";
            numbers.NumberGroupSeparator = numbers.CurrencyGroupSeparator = numbers.PercentGroupSeparator = ",";
        }

        return culture;
    }
}
