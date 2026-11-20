using Vafadar.Temporal;

namespace Vafadar.Dastan.People;

/// <summary>
/// The time of the birth or the death of a person of a private tree (step 19.5, ADR 0017): an expression of
/// time – a year, a month or a day of a calendar, exact or about, a time known only to lie between two, or a
/// time that is stated as not known – and never a bare number from 1 to 9999. Kept as the text
/// <see cref="TemporalJson"/> writes.
/// </summary>
public static class PersonTime
{
    /// <summary>Reads a time as the store keeps it; nothing for nothing.</summary>
    public static TemporalExpression? Read(string? kept) => string.IsNullOrEmpty(kept) ? null : TemporalJson.Deserialize(kept);

    /// <summary>Writes a time as the store keeps it; nothing for nothing.</summary>
    public static string? Write(TemporalExpression? time) => time is null ? null : TemporalJson.Serialize(time);

    /// <summary>
    /// Returns whether a time is one a record of a person can state: a position of a calendar, a window
    /// between two of them, or the statement that it is not known.
    /// </summary>
    public static bool IsOne(TemporalExpression? time) => time switch
    {
        null or UnknownTime => true,
        Occurrence { At: CalendarPosition } => true,
        OccurrenceWindow { Earliest: CalendarPosition, Latest: CalendarPosition } => true,
        _ => false,
    };

    /// <summary>Returns whether a time says no more than years: what a record may say of a living person who was not asked (PRIV-003).</summary>
    public static bool YearsOnly(TemporalExpression? time) => time switch
    {
        null or UnknownTime => true,
        Occurrence { At: CalendarPosition at } => at.Precision == CalendarPrecision.Year,
        OccurrenceWindow { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } => earliest.Precision == CalendarPrecision.Year && latest.Precision == CalendarPrecision.Year,
        _ => false,
    };

    /// <summary>
    /// Returns the years of the common era a time lies between, as nearly as a comparison of two lives needs
    /// them – a year of another calendar lies in two years of the common era, and both are counted; nothing for
    /// a time that names none.
    /// </summary>
    public static (int Earliest, int Latest)? Years(TemporalExpression? time) => time switch
    {
        Occurrence { At: CalendarPosition at } => Common(at),
        OccurrenceWindow { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } => (Common(earliest).Earliest, Common(latest).Latest),
        _ => null,
    };

    /// <summary>
    /// Returns whether somebody born at one time can have died at another: nobody dies before they are born.
    /// Two times of one calendar are compared as far as both say – the year, the month, the day; times of two
    /// calendars by the years of the common era they can lie in.
    /// </summary>
    public static bool Lived(TemporalExpression? born, TemporalExpression? died)
    {
        if (First(born) is { } from && Last(died) is { } until && from.Calendar == until.Calendar)
        {
            return from.ProlepticYear != until.ProlepticYear ? from.ProlepticYear < until.ProlepticYear
                : from.Month is not { } bornIn || until.Month is not { } diedIn || (bornIn != diedIn ? bornIn < diedIn
                : from.Day is not { } bornOn || until.Day is not { } diedOn || bornOn <= diedOn);
        }

        return Years(born) is not var (earliest, _) || Years(died) is not var (_, latest) || earliest <= latest;
    }

    /// <summary>Returns the earliest position of a calendar a time names; nothing for a time that names none.</summary>
    public static CalendarPosition? First(TemporalExpression? time) => time switch
    {
        Occurrence { At: CalendarPosition at } => at,
        OccurrenceWindow { Earliest: CalendarPosition earliest } => earliest,
        _ => null,
    };

    /// <summary>Returns the latest position of a calendar a time names; nothing for a time that names none.</summary>
    public static CalendarPosition? Last(TemporalExpression? time) => time switch
    {
        Occurrence { At: CalendarPosition at } => at,
        OccurrenceWindow { Latest: CalendarPosition latest } => latest,
        _ => null,
    };

    // The years of the common era a year of a calendar can lie in: a year that begins in spring, or one that
    // moves through the seasons, lies in two of them.
    private static (int Earliest, int Latest) Common(CalendarPosition position)
    {
        switch (position.Calendar)
        {
            case CalendarSystem.SolarHijri:
                return (position.ProlepticYear + 621, position.ProlepticYear + 622);
            case CalendarSystem.LunarHijri:
                var begins = (position.ProlepticYear * 0.970224) + 621.5774;
                return ((int)Math.Floor(begins) - 1, (int)Math.Floor(begins) + 1);
            default:
                return (position.ProlepticYear, position.ProlepticYear);
        }
    }

    /// <summary>
    /// Returns the calendar some times are written in, when all that name a position are written in one;
    /// nothing when they are written in two, or name none.
    /// </summary>
    public static CalendarSystem? Calendar(params TemporalExpression?[] times)
    {
        ArgumentNullException.ThrowIfNull(times);
        var calendars = times.SelectMany(time => new[] { First(time), Last(time) }).Where(position => position is not null).Select(position => position!.Calendar).Distinct().ToList();
        return calendars.Count == 1 ? calendars[0] : null;
    }

    /// <summary>
    /// Returns a time as years on an axis of its own calendar: each position as the year it lies in, written
    /// as a year of the common era with the same number. It serves to compare times of one calendar with a
    /// year of that calendar – year against year – by rules that count years of the common era.
    /// </summary>
    public static TemporalExpression? AsYears(TemporalExpression? time) => time switch
    {
        Occurrence { At: CalendarPosition at } => new Occurrence(Axis(at.ProlepticYear)),
        OccurrenceWindow { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } => Between(Axis(earliest.ProlepticYear), Axis(latest.ProlepticYear)),
        _ => time,
    };

    /// <summary>
    /// Returns a time as the common era can say it: a position of the common era as it is, a position of
    /// another calendar as the years of the common era it can lie in – a time known only to lie between two.
    /// </summary>
    public static TemporalExpression? InCommonEra(TemporalExpression? time) => time switch
    {
        Occurrence { At: CalendarPosition { Calendar: not CalendarSystem.Gregorian and not CalendarSystem.Julian } at } => Between(Axis(Common(at).Earliest), Axis(Common(at).Latest)),
        OccurrenceWindow { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } when earliest.Calendar is not (CalendarSystem.Gregorian or CalendarSystem.Julian) || latest.Calendar is not (CalendarSystem.Gregorian or CalendarSystem.Julian) =>
            Between(Axis(Common(earliest).Earliest), Axis(Common(latest).Latest)),
        _ => time,
    };

    private static TemporalExpression Between(CalendarPosition earliest, CalendarPosition latest) =>
        earliest == latest ? new Occurrence(earliest) : new OccurrenceWindow(earliest, latest);

    // A year on the axis of the common era, by its number counted through the year before the first.
    private static CalendarPosition Axis(int proleptic) => proleptic >= 1
        ? CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, proleptic)
        : CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.BeforeCommonEra, 1 - proleptic);
}
