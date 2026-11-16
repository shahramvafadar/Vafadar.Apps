using System.Globalization;
using static Vafadar.Temporal.Tests.Times;

namespace Vafadar.Temporal.Tests;

public sealed class PositionTests
{
    [Fact]
    public void A_year_stays_a_year_without_month_or_day()
    {
        var birth = Year(1980);

        Assert.Equal(CalendarPrecision.Year, birth.Precision);
        Assert.Null(birth.Month);
        Assert.Null(birth.Day);
    }

    [Fact]
    public void A_month_and_a_day_are_as_precise_as_stated()
    {
        Assert.Equal(CalendarPrecision.Month, CalendarPosition.OfMonth(CalendarSystem.Gregorian, Era.CommonEra, 1980, 5).Precision);
        Assert.Equal(CalendarPrecision.Day, Day(1980, 5, 3).Precision);
    }

    [Fact]
    public void The_year_before_year_one_is_one_before_the_era_not_zero()
    {
        Assert.Equal(1, Year(1).ProlepticYear);
        Assert.Equal(0, Bce(1).ProlepticYear);
        Assert.Equal(-499, Bce(500).ProlepticYear);
        Assert.Equal(1, Year(1).ProlepticYear - Bce(1).ProlepticYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(CalendarPosition.MaxYear + 1)]
    public void A_year_outside_the_era_is_refused(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Year(year));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bce(year));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void A_month_that_does_not_exist_is_refused(int month) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarPosition.OfMonth(CalendarSystem.Gregorian, Era.CommonEra, 1980, month));

    [Theory]
    [InlineData(CalendarSystem.Gregorian, 2023, 2, 29)]
    [InlineData(CalendarSystem.Gregorian, 1900, 2, 29)]
    [InlineData(CalendarSystem.Gregorian, 2024, 4, 31)]
    [InlineData(CalendarSystem.Gregorian, 2024, 1, 0)]
    [InlineData(CalendarSystem.Julian, 1901, 2, 29)]
    public void A_day_that_does_not_exist_is_refused(CalendarSystem calendar, int year, int month, int day) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarPosition.OfDay(calendar, Era.CommonEra, year, month, day));

    [Theory]
    [InlineData(CalendarSystem.Gregorian, Era.CommonEra, 2024, 2, 29)]
    [InlineData(CalendarSystem.Gregorian, Era.CommonEra, 2000, 2, 29)]
    [InlineData(CalendarSystem.Julian, Era.CommonEra, 1900, 2, 29)]
    [InlineData(CalendarSystem.Gregorian, Era.BeforeCommonEra, 1, 2, 29)]
    [InlineData(CalendarSystem.Julian, Era.BeforeCommonEra, 5, 2, 29)]
    [InlineData(CalendarSystem.SolarHijri, Era.SolarHijri, 1380, 6, 31)]
    [InlineData(CalendarSystem.Unspecified, Era.CommonEra, 1450, 3, 31)]
    public void Leap_days_follow_the_calendar_and_the_continuous_year(CalendarSystem calendar, Era era, int year, int month, int day) =>
        Assert.Equal(day, CalendarPosition.OfDay(calendar, era, year, month, day).Day);

    [Theory]
    [InlineData(CalendarSystem.Gregorian, Era.SolarHijri)]
    [InlineData(CalendarSystem.Julian, Era.LunarHijri)]
    [InlineData(CalendarSystem.SolarHijri, Era.CommonEra)]
    [InlineData(CalendarSystem.LunarHijri, Era.SolarHijri)]
    public void A_calendar_counts_years_only_in_its_own_era(CalendarSystem calendar, Era era) =>
        Assert.Throws<ArgumentException>(() => CalendarPosition.OfYear(calendar, era, 1380));

    [Theory]
    [InlineData(Era.CommonEra)]
    [InlineData(Era.BeforeCommonEra)]
    [InlineData(Era.SolarHijri)]
    [InlineData(Era.LunarHijri)]
    public void A_year_can_be_stated_without_naming_the_calendar(Era era)
    {
        var position = CalendarPosition.OfYear(CalendarSystem.Unspecified, era, 1380);

        Assert.Equal(CalendarSystem.Unspecified, position.Calendar);
        Assert.Equal(era, position.Era);
    }

    [Fact]
    public void Approximate_is_a_statement_of_its_own()
    {
        var circa = UnnamedCalendarYear(1450).Approximately();

        Assert.True(circa.IsApproximate);
        Assert.NotEqual(UnnamedCalendarYear(1450), circa);
        Assert.Same(circa, circa.Approximately());
        Assert.Equal(1450, circa.Year);
    }

    [Fact]
    public void An_age_keeps_its_value_unit_and_reference()
    {
        var age = AgePosition.Of(55, AgeUnit.MillionYears, AgeReference.Year(2000));

        Assert.Equal(55m, age.Value);
        Assert.Equal(AgeUnit.MillionYears, age.Unit);
        Assert.Equal(2000, age.Reference.CommonEraYear);
        Assert.Equal(55_000_000m, age.Years);
    }

    [Fact]
    public void An_age_keeps_the_digits_of_its_source()
    {
        var careful = AgePosition.Of(55.0m, AgeUnit.MillionYears, AgeReference.Year(2000));

        Assert.Equal("55.0", careful.Value.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(AgeUnit.Years, 1)]
    [InlineData(AgeUnit.ThousandYears, 1_000)]
    [InlineData(AgeUnit.MillionYears, 1_000_000)]
    [InlineData(AgeUnit.BillionYears, 1_000_000_000)]
    public void Each_unit_has_its_number_of_years(AgeUnit unit, long years) =>
        Assert.Equal(years, AgePosition.Of(1, unit, AgeReference.Unspecified).Years);

    [Fact]
    public void An_age_without_a_reference_says_so()
    {
        var age = AgePosition.Of(12, AgeUnit.ThousandYears, AgeReference.Unspecified);

        Assert.False(age.Reference.IsSpecified);
        Assert.Null(age.Reference.CommonEraYear);
        Assert.Equal(1950, AgeReference.BeforePresent.CommonEraYear);
    }

    [Fact]
    public void An_age_outside_its_limits_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AgePosition.Of(-1, AgeUnit.Years, AgeReference.Unspecified));
        Assert.Throws<ArgumentOutOfRangeException>(() => AgePosition.Of(200, AgeUnit.BillionYears, AgeReference.Unspecified));
        Assert.Throws<ArgumentOutOfRangeException>(() => AgePosition.Of(decimal.MaxValue, AgeUnit.BillionYears, AgeReference.Unspecified));
        Assert.Throws<ArgumentOutOfRangeException>(() => AgeReference.Year(0));
    }

    [Fact]
    public void A_radiocarbon_age_is_not_a_calendar_age()
    {
        var raw = AgePosition.Of(3000, AgeUnit.Years, AgeReference.BeforePresent, AgeBasis.RadiocarbonYears);
        var calendar = AgePosition.Of(3000, AgeUnit.Years, AgeReference.BeforePresent);

        Assert.NotEqual(calendar, raw);
    }

    [Fact]
    public void A_story_position_names_its_sequence_order_and_place()
    {
        var step = Step(2);

        Assert.Equal(Story, step.Sequence);
        Assert.Equal(NarrativeOrder.Presentation, step.Order);
        Assert.Equal(2, step.Ordinal);
    }

    [Fact]
    public void A_story_position_without_a_place_or_sequence_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Step(0));
        Assert.Throws<ArgumentException>(() => Step(1, sequence: " "));
    }
}
