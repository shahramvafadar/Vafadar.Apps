namespace Vafadar.Temporal;

/// <summary>
/// The calendar a date is stated in: its months and days. The year count is the <see cref="Era"/>, a separate fact.
/// </summary>
public enum CalendarSystem
{
    /// <summary>The source does not say. A year of an era can still be given; it is never assumed to be Gregorian.</summary>
    Unspecified = 0,

    /// <summary>The Gregorian calendar, also before its introduction (proleptic).</summary>
    Gregorian = 1,

    /// <summary>The Julian calendar.</summary>
    Julian = 2,

    /// <summary>The Solar Hijri (Persian) calendar.</summary>
    SolarHijri = 3,

    /// <summary>The lunar Hijri calendar.</summary>
    LunarHijri = 4,
}

/// <summary>
/// The year count of a date. An era is not a calendar: "1450 CE" names a year without saying which calendar's months
/// and days apply. No era has a year zero – year 1 before an era is directly followed by year 1 of it.
/// </summary>
public enum Era
{
    /// <summary>Common era (CE).</summary>
    CommonEra = 0,

    /// <summary>Before the common era (BCE).</summary>
    BeforeCommonEra = 1,

    /// <summary>Solar Hijri years.</summary>
    SolarHijri = 2,

    /// <summary>Years before the Solar Hijri epoch.</summary>
    BeforeSolarHijri = 3,

    /// <summary>Lunar Hijri years.</summary>
    LunarHijri = 4,

    /// <summary>Years before the lunar Hijri epoch.</summary>
    BeforeLunarHijri = 5,
}

/// <summary>How much of a calendar date is known. Parts that are not known are absent, never filled in.</summary>
public enum CalendarPrecision
{
    /// <summary>Only the year is known.</summary>
    Year = 0,

    /// <summary>Year and month are known.</summary>
    Month = 1,

    /// <summary>Year, month and day are known.</summary>
    Day = 2,
}

/// <summary>The epoch a year count starts from; years of different epochs are not compared without a conversion.</summary>
internal enum Epoch
{
    Common,
    SolarHijri,
    LunarHijri,
}

/// <summary>Rules that connect eras and calendars.</summary>
internal static class CalendarRules
{
    /// <summary>Returns whether <paramref name="era"/> counts backwards from its epoch.</summary>
    public static bool IsBefore(Era era) => era is Era.BeforeCommonEra or Era.BeforeSolarHijri or Era.BeforeLunarHijri;

    /// <summary>Returns the epoch of <paramref name="era"/>.</summary>
    public static Epoch EpochOf(Era era) => era switch
    {
        Era.CommonEra or Era.BeforeCommonEra => Epoch.Common,
        Era.SolarHijri or Era.BeforeSolarHijri => Epoch.SolarHijri,
        Era.LunarHijri or Era.BeforeLunarHijri => Epoch.LunarHijri,
        _ => throw new ArgumentOutOfRangeException(nameof(era), era, "Unknown era."),
    };

    /// <summary>
    /// Returns whether dates of <paramref name="calendar"/> are counted in <paramref name="era"/>. An unspecified
    /// calendar accepts every era.
    /// </summary>
    public static bool Accepts(CalendarSystem calendar, Era era) => calendar switch
    {
        CalendarSystem.Unspecified => true,
        CalendarSystem.Gregorian or CalendarSystem.Julian => EpochOf(era) == Epoch.Common,
        CalendarSystem.SolarHijri => EpochOf(era) == Epoch.SolarHijri,
        CalendarSystem.LunarHijri => EpochOf(era) == Epoch.LunarHijri,
        _ => throw new ArgumentOutOfRangeException(nameof(calendar), calendar, "Unknown calendar."),
    };

    /// <summary>
    /// Returns the largest day number <paramref name="month"/> can have. Exact for the Gregorian and Julian calendars;
    /// for the others it is the upper limit of the month, because their leap rules are not implemented here.
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="prolepticYear">The year in continuous numbering (see <see cref="CalendarPosition.ProlepticYear"/>).</param>
    /// <param name="month">The month, 1 to 12.</param>
    public static int MaxDay(CalendarSystem calendar, int prolepticYear, int month) => calendar switch
    {
        CalendarSystem.Gregorian => WesternMonth(month, IsGregorianLeapYear(prolepticYear)),
        CalendarSystem.Julian => WesternMonth(month, Mod(prolepticYear, 4) == 0),
        CalendarSystem.SolarHijri => month <= 6 ? 31 : 30,
        CalendarSystem.LunarHijri => 30,
        _ => 31,
    };

    private static bool IsGregorianLeapYear(int prolepticYear) =>
        Mod(prolepticYear, 4) == 0 && (Mod(prolepticYear, 100) != 0 || Mod(prolepticYear, 400) == 0);

    private static int WesternMonth(int month, bool leapYear) => month switch
    {
        2 => leapYear ? 29 : 28,
        4 or 6 or 9 or 11 => 30,
        _ => 31,
    };

    // The remainder of C# is negative for negative years; leap rules need the mathematical one.
    private static int Mod(int value, int divisor) => ((value % divisor) + divisor) % divisor;
}
