namespace Vafadar.Temporal;

/// <summary>
/// A year, a month of a year or a day, in a stated era and – when the source names it – a stated calendar. A year
/// alone stays a year: no month or day is added, and "the year 1980" never means the first of January.
/// </summary>
public sealed record CalendarPosition : TimePosition
{
    /// <summary>The largest year of an era that can be stated. Older times are ages (<see cref="AgePosition"/>).</summary>
    public const int MaxYear = 1_000_000;

    private CalendarPosition(CalendarSystem calendar, Era era, int year, int? month, int? day, bool isApproximate)
    {
        if (!Enum.IsDefined(calendar))
        {
            throw new ArgumentOutOfRangeException(nameof(calendar), calendar, "Unknown calendar.");
        }

        if (!Enum.IsDefined(era))
        {
            throw new ArgumentOutOfRangeException(nameof(era), era, "Unknown era.");
        }

        if (!CalendarRules.Accepts(calendar, era))
        {
            throw new ArgumentException($"The {calendar} calendar does not count years in the era {era}.", nameof(era));
        }

        if (year is < 1 or > MaxYear)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, $"A year of an era is between 1 and {MaxYear}; no era has a year zero.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "A month is between 1 and 12.");
        }

        var prolepticYear = CalendarRules.IsBefore(era) ? 1 - year : year;
        if (day is not null && (month is null || day < 1 || day > CalendarRules.MaxDay(calendar, prolepticYear, month.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, "The day does not exist in this month.");
        }

        Calendar = calendar;
        Era = era;
        Year = year;
        Month = month;
        Day = day;
        IsApproximate = isApproximate;
    }

    /// <summary>Gets the calendar, or <see cref="CalendarSystem.Unspecified"/> when the source does not name one.</summary>
    public CalendarSystem Calendar { get; }

    /// <summary>Gets the era the year is counted in.</summary>
    public Era Era { get; }

    /// <summary>Gets the year of the era, 1 or greater – as people write it: "500" in "500 BCE".</summary>
    public int Year { get; }

    /// <summary>Gets the month, 1 to 12, or <see langword="null"/> when it is not known.</summary>
    public int? Month { get; }

    /// <summary>Gets the day of the month, or <see langword="null"/> when it is not known.</summary>
    public int? Day { get; }

    /// <summary>Gets whether the source gives the position as approximate ("around 1450").</summary>
    /// <remarks>Approximate says nothing about how far off the date may be; no range is derived from it.</remarks>
    public bool IsApproximate { get; }

    /// <summary>Gets how much of the date is known.</summary>
    public CalendarPrecision Precision =>
        Day is not null ? CalendarPrecision.Day : Month is not null ? CalendarPrecision.Month : CalendarPrecision.Year;

    /// <summary>
    /// Gets the year in continuous numbering, for calculating only: year 1 of the era is 1, the year before it is 0,
    /// the one before that -1. So "1 BCE" is 0 and "500 BCE" is -499 – not the negated year.
    /// </summary>
    public int ProlepticYear => CalendarRules.IsBefore(Era) ? 1 - Year : Year;

    /// <summary>Creates a position that is known to the year.</summary>
    public static CalendarPosition OfYear(CalendarSystem calendar, Era era, int year) =>
        new(calendar, era, year, null, null, isApproximate: false);

    /// <summary>Creates a position that is known to the month.</summary>
    public static CalendarPosition OfMonth(CalendarSystem calendar, Era era, int year, int month) =>
        new(calendar, era, year, month, null, isApproximate: false);

    /// <summary>Creates a position that is known to the day.</summary>
    public static CalendarPosition OfDay(CalendarSystem calendar, Era era, int year, int month, int day) =>
        new(calendar, era, year, month, day, isApproximate: false);

    /// <summary>Returns this position marked as approximate.</summary>
    public CalendarPosition Approximately() => IsApproximate ? this : new(Calendar, Era, Year, Month, Day, isApproximate: true);
}
