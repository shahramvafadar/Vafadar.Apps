using System.Collections.Concurrent;
using System.Globalization;

namespace Vafadar.Zanance.Core.Plans;

/// <summary>A public holiday.</summary>
/// <param name="Date">The Gregorian date.</param>
/// <param name="Key">A stable key for the name (translated as <c>Holiday_{Key}</c>).</param>
/// <param name="IsApproximate">
/// <see langword="true"/> for holidays of the lunar Hijri calendar: they are calculated with the tabular Hijri calendar and
/// can differ by a day or two from the official announcement, which depends on the sighting of the moon.
/// </param>
public sealed record PublicHoliday(DateOnly Date, string Key, bool IsApproximate);

/// <summary>
/// Nationwide public holidays of the supported regions (F2-CON-05, D-29): Germany (rules around Easter and fixed dates;
/// holidays of single states are not included) and Iran (Solar Hijri dates are exact, lunar Hijri dates approximate).
/// Every date has a stated rule, so a moved due date is always explainable.
/// </summary>
public static class PublicHolidays
{
    private static readonly ConcurrentDictionary<(string Region, int Year), IReadOnlyList<PublicHoliday>> Cache = new();
    private static readonly PersianCalendar Persian = new();
    // No adjustment: the machine's Hijri setting must not move a due date from one device to another.
    private static readonly HijriCalendar Hijri = new() { HijriAdjustment = 0 };

    /// <summary>Gets the regions (ISO 3166 codes) with a holiday calendar.</summary>
    public static IReadOnlyList<string> Regions { get; } = ["DE", "IR"];

    /// <summary>Returns whether a region has a holiday calendar.</summary>
    public static bool IsSupported(string? region) => region is not null && Regions.Contains(region.ToUpperInvariant());

    /// <summary>Returns whether a region's holidays include lunar dates that can differ from the announcement.</summary>
    public static bool HasApproximateDates(string? region) => string.Equals(region, "IR", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns whether only nationwide holidays are known (holidays of single states are left out).</summary>
    public static bool IsNationwideOnly(string? region) => string.Equals(region, "DE", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns whether <paramref name="date"/> is a public holiday in <paramref name="region"/>.</summary>
    public static bool IsHoliday(string? region, DateOnly date) =>
        IsSupported(region) && In(region!, date.Year).Any(h => h.Date == date);

    /// <summary>Returns the holidays of a Gregorian year, ordered by date.</summary>
    public static IReadOnlyList<PublicHoliday> In(string region, int year)
    {
        ArgumentNullException.ThrowIfNull(region);
        var key = (region.ToUpperInvariant(), year);
        return Cache.GetOrAdd(key, k =>
        {
            try
            {
                return k.Region switch
                {
                    "DE" => Germany(k.Year),
                    "IR" => Iran(k.Year),
                    _ => [],
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                // A year outside the range of the Persian or Hijri calendar has no known holidays.
                return [];
            }
        });
    }

    /// <summary>Returns Easter Sunday of a Gregorian year (anonymous Gregorian algorithm).</summary>
    public static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }

    // Nationwide holidays of Germany; holidays of single states are left out.
    private static List<PublicHoliday> Germany(int year)
    {
        var easter = EasterSunday(year);
        return
        [
            new(new DateOnly(year, 1, 1), "NewYear", false),
            new(easter.AddDays(-2), "GoodFriday", false),
            new(easter.AddDays(1), "EasterMonday", false),
            new(new DateOnly(year, 5, 1), "LabourDay", false),
            new(easter.AddDays(39), "Ascension", false),
            new(easter.AddDays(50), "WhitMonday", false),
            new(new DateOnly(year, 10, 3), "GermanUnity", false),
            new(new DateOnly(year, 12, 25), "Christmas", false),
            new(new DateOnly(year, 12, 26), "SecondChristmas", false),
        ];
    }

    // Official holidays of Iran: Solar Hijri dates (exact) and lunar Hijri dates (approximate).
    private static List<PublicHoliday> Iran(int year)
    {
        var holidays = new List<PublicHoliday>();
        var first = new DateTime(year, 1, 1);
        var last = new DateTime(year, 12, 31);

        (int Month, int Day, string Key)[] solar =
        [
            (1, 1, "Nowruz"), (1, 2, "Nowruz"), (1, 3, "Nowruz"), (1, 4, "Nowruz"),
            (1, 12, "RepublicDay"), (1, 13, "NatureDay"),
            (3, 14, "KhomeiniDeath"), (3, 15, "Khordad15"),
            (11, 22, "RevolutionDay"), (12, 29, "OilNationalization"),
        ];
        for (var persianYear = Persian.GetYear(first); persianYear <= Persian.GetYear(last); persianYear++)
        {
            foreach (var (month, day, key) in solar)
            {
                if (month == 12 && day > Persian.GetDaysInMonth(persianYear, 12))
                {
                    continue;
                }

                Add(holidays, Persian.ToDateTime(persianYear, month, day, 0, 0, 0, 0), key, false, year);
            }
        }

        // Day 0 = the last day of the month (e.g. the end of Safar).
        (int Month, int Day, string Key)[] lunar =
        [
            (1, 9, "Tasua"), (1, 10, "Ashura"), (2, 20, "Arbaeen"), (2, 28, "ProphetDeath"), (2, 0, "RezaMartyrdom"),
            (3, 8, "AskariMartyrdom"), (3, 17, "ProphetBirth"), (6, 3, "FatimaMartyrdom"), (7, 13, "AliBirth"),
            (7, 27, "Mabath"), (8, 15, "MahdiBirth"), (9, 21, "AliMartyrdom"), (10, 1, "EidFitr"), (10, 2, "EidFitr"),
            (10, 25, "SadiqMartyrdom"), (12, 10, "EidAdha"), (12, 18, "EidGhadir"),
        ];
        for (var hijriYear = Hijri.GetYear(first); hijriYear <= Hijri.GetYear(last); hijriYear++)
        {
            foreach (var (month, day, key) in lunar)
            {
                var actualDay = day == 0 ? Hijri.GetDaysInMonth(hijriYear, month) : day;
                Add(holidays, Hijri.ToDateTime(hijriYear, month, actualDay, 0, 0, 0, 0), key, true, year);
            }
        }

        return [.. holidays.OrderBy(h => h.Date).DistinctBy(h => h.Date)];
    }

    private static void Add(List<PublicHoliday> holidays, DateTime date, string key, bool approximate, int year)
    {
        if (date.Year == year)
        {
            holidays.Add(new PublicHoliday(DateOnly.FromDateTime(date), key, approximate));
        }
    }
}
