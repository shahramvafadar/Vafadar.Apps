namespace Vafadar.Temporal.Tests;

/// <summary>Short names for the positions and expressions the tests state again and again.</summary>
internal static class Times
{
    public const string Story = "story-rostam-sohrab";

    public static CalendarPosition Year(int year) => CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, year);

    public static CalendarPosition Bce(int year) => CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.BeforeCommonEra, year);

    public static CalendarPosition Day(int year, int month, int day) =>
        CalendarPosition.OfDay(CalendarSystem.Gregorian, Era.CommonEra, year, month, day);

    public static CalendarPosition SolarYear(int year) => CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, year);

    public static CalendarPosition UnnamedCalendarYear(int year) =>
        CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, year);

    /// <summary>Thousands of years before the year 2000.</summary>
    public static AgePosition Ka(decimal value) => AgePosition.Of(value, AgeUnit.ThousandYears, AgeReference.Year(2000));

    /// <summary>Millions of years before the year 2000.</summary>
    public static AgePosition Ma(decimal value) => AgePosition.Of(value, AgeUnit.MillionYears, AgeReference.Year(2000));

    public static NarrativePosition Step(int ordinal, string sequence = Story, NarrativeOrder order = NarrativeOrder.Presentation) =>
        NarrativePosition.Of(sequence, order, ordinal);

    public static Occurrence At(TimePosition position) => new(position);

    /// <summary>A period with stated ends that is asserted to have lasted without a break.</summary>
    public static Period Lasting(TimePosition from, TimePosition to) =>
        new(PeriodStart.At(from), PeriodEnd.At(to), PeriodContinuity.Continuous);

    public static TemporalWindow During(TimePosition position) => TemporalWindow.Of(position);

    public static TemporalWindow During(TimePosition from, TimePosition to) => TemporalWindow.Between(from, to);
}
