namespace Vafadar.Temporal;

/// <summary>
/// A stretch on an axis of years: from <see cref="From"/> up to, but not including, <see cref="To"/>. A position
/// that is stated to the year covers that whole year; nothing says where in it something happened.
/// </summary>
/// <param name="From">The beginning of the stretch.</param>
/// <param name="To">The end of the stretch; it is the beginning of what follows.</param>
public readonly record struct YearSpan(decimal From, decimal To)
{
    /// <summary>Gets the middle of the stretch: the place to draw a position that is known no closer than this.</summary>
    public decimal Middle => (From + To) / 2m;

    /// <summary>Gets the length of the stretch in years.</summary>
    public decimal Length => To - From;
}

/// <summary>
/// The axis of the years of the common era, for drawing: one number per year, running on through the change of the
/// era. The year 1 of the common era begins at 1, the year 1 before it at 0, the year 2 before it at −1 – there is
/// no year zero, so the numbers of the axis are not the numbers of the years before the era.
/// <para>
/// The axis counts years, not days: a Julian and a Gregorian date of the same year stand in the same year, and a
/// position whose calendar is not stated is placed by its year. That is right for looking at centuries beside each
/// other and wrong for deciding which of two days came first – <see cref="TemporalComparer"/> does that. A position
/// that is counted in another era, or is not a calendar position at all, has no place here: placing it would need a
/// mapping that nobody has stated.
/// </para>
/// </summary>
public static class YearAxis
{
    private const decimal MonthsPerYear = 12m;

    // A month is drawn as 31 places wide, like the comparer counts them: enough to keep the days of a month in order.
    private const decimal PlacesPerMonth = 31m;

    /// <summary>
    /// Returns the stretch of the axis a position covers – a year, a month or a day of it – or
    /// <see langword="null"/> when the position cannot be placed on the axis of the common era.
    /// </summary>
    public static YearSpan? Of(TimePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position is not CalendarPosition { Era: Era.CommonEra or Era.BeforeCommonEra } calendar)
        {
            return null;
        }

        var year = (decimal)calendar.ProlepticYear;
        if (calendar.Month is not { } month)
        {
            return new YearSpan(year, year + 1m);
        }

        var from = year + ((month - 1) / MonthsPerYear);
        if (calendar.Day is not { } day)
        {
            return new YearSpan(from, from + (1m / MonthsPerYear));
        }

        var dayLength = 1m / (MonthsPerYear * PlacesPerMonth);
        from += (day - 1) * dayLength;
        return new YearSpan(from, from + dayLength);
    }

    /// <summary>Returns where a year of an era begins on the axis.</summary>
    /// <param name="era">The common era or the time before it.</param>
    /// <param name="year">The year, counted from 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">The era is not one of the axis, or the year is not a year of an era.</exception>
    public static decimal StartOf(Era era, int year)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);
        return era switch
        {
            Era.CommonEra => year,
            Era.BeforeCommonEra => 1 - year,
            _ => throw new ArgumentOutOfRangeException(nameof(era), era, "The axis counts the years of the common era and of the time before it."),
        };
    }

    /// <summary>Returns the year a place on the axis lies in, with its era.</summary>
    /// <param name="place">A place on the axis.</param>
    public static (Era Era, int Year) YearAt(decimal place)
    {
        var proleptic = (int)Math.Floor(place);
        return proleptic >= 1 ? (Era.CommonEra, proleptic) : (Era.BeforeCommonEra, 1 - proleptic);
    }
}
