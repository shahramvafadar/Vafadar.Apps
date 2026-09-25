namespace Vafadar.Localization.Formatting;

/// <summary>
/// How much detail a formatted date shows.
/// </summary>
public enum DateFormatStyle
{
    /// <summary>Numeric date, e.g. <c>9/25/2026</c> or <c>1405/07/03</c>.</summary>
    Short = 0,

    /// <summary>Date with day and month names, e.g. <c>Friday, September 25, 2026</c> or <c>جمعه 3 مهر 1405</c>.</summary>
    Long = 1,

    /// <summary>Month and year, e.g. <c>September 2026</c> or <c>مهر 1405</c>.</summary>
    MonthYear = 2,
}
