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

    /// <summary>Day and month without year, e.g. <c>September 25</c> or <c>3 Mehr</c>.</summary>
    DayMonth = 3,

    /// <summary>
    /// Day and short month name for narrow places such as date tiles, e.g. <c>Sep 25</c>, <c>25 Rab II</c> or <c>3 مهر</c>;
    /// the short names stay distinct (Rabi al-Awwal and Rabi al-Thani are <c>Rab I</c> and <c>Rab II</c>).
    /// </summary>
    DayMonthShort = 4,

    /// <summary>The month name alone, e.g. <c>February</c>, <c>Esfand</c> or <c>رمضان</c>.</summary>
    Month = 5,
}
