using static Vafadar.Temporal.Tests.Times;

namespace Vafadar.Temporal.Tests;

/// <summary>
/// The comparisons of the product specification (time and uncertainty, TIME-012 and TIME-017; reference scenarios
/// SC-03 to SC-08 and SC-12). All data is made up for the tests.
/// </summary>
public sealed class ComparerTests
{
    // ---- An occurrence somewhere in a window (TIME-012, SC-05) ----

    [Fact]
    public void An_occurrence_that_may_lie_outside_a_search_window_matches_possibly()
    {
        var sometime = new OccurrenceWindow(Year(1998), Year(2003));

        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(sometime, During(Year(2000), Year(2005))).Overlap);
    }

    [Fact]
    public void An_occurrence_whose_every_possible_time_is_inside_a_search_window_matches_definitely()
    {
        var sometime = new OccurrenceWindow(Year(1998), Year(2003));

        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(sometime, During(Year(1990), Year(2010))).Overlap);
    }

    [Fact]
    public void An_occurrence_that_cannot_lie_in_a_search_window_does_not_match()
    {
        var sometime = new OccurrenceWindow(Year(1998), Year(2003));

        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(sometime, During(Year(2004), Year(2005))).Overlap);
    }

    [Fact]
    public void An_event_between_twenty_and_ten_thousand_years_ago_is_possibly_in_a_smaller_interval()
    {
        var sometime = new OccurrenceWindow(Ka(20), Ka(10));

        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(sometime, During(Ka(15), Ka(12))).Overlap);
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(sometime, During(Ka(25), Ka(5))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(sometime, During(Ka(9), Ka(5))).Overlap);
    }

    [Fact]
    public void Two_occurrences_that_may_fall_together_are_not_shown_to_be_simultaneous()
    {
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(At(Year(1980)), At(Year(1980))).Overlap);
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(At(Day(1980, 5, 3)), At(Day(1980, 5, 3))).Overlap);
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(new OccurrenceWindow(Year(1998), Year(2003)), new OccurrenceWindow(Year(2001), Year(2008))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(At(Year(1980)), At(Year(1981))).Overlap);
    }

    [Fact]
    public void A_year_is_inside_itself_and_a_day_is_inside_its_year()
    {
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(At(Year(1980)), During(Year(1980))).Overlap);
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(At(Day(1980, 5, 3)), During(Year(1980))).Overlap);
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(At(Year(1980)), During(Day(1980, 5, 3))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(At(Day(1980, 12, 31)), During(Year(1981))).Overlap);
    }

    // ---- Alternatives (TIME-004) ----

    [Fact]
    public void Alternatives_are_neither_averaged_nor_joined_into_a_range()
    {
        var either = new AlternativeOccurrences([At(Year(1980)), At(Year(1990))]);

        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(either, During(Year(1985))).Overlap);
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(either, During(Year(1980))).Overlap);
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(either, During(Year(1980), Year(1990))).Overlap);
    }

    // ---- Periods with uncertain bounds (SC-06) ----

    private static Period UncertainPeriod(PeriodContinuity continuity = PeriodContinuity.Continuous) =>
        new(PeriodStart.Between(Ka(20), Ka(18)), PeriodEnd.Between(Ka(12), Ka(10)), continuity);

    [Fact]
    public void The_part_inside_every_reading_of_a_continuous_period_overlaps_definitely()
    {
        var result = TemporalComparer.Overlap(UncertainPeriod(), During(Ka(17), Ka(13)));

        Assert.Equal(TemporalOverlap.Definite, result.Overlap);
        Assert.True(result.Basis.HasFlag(ComparisonBasis.ContinuityAssertion));
    }

    [Fact]
    public void The_parts_inside_only_some_readings_overlap_possibly()
    {
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(UncertainPeriod(), During(Ka(19), Ka(18.5m))).Overlap);
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(UncertainPeriod(), During(Ka(11), Ka(10.5m))).Overlap);
    }

    [Fact]
    public void A_time_outside_every_reading_does_not_overlap()
    {
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(UncertainPeriod(), During(Ka(30), Ka(25))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(UncertainPeriod(), During(Ka(9), Ka(5))).Overlap);
    }

    [Fact]
    public void Without_asserted_continuity_nothing_inside_a_period_is_certain()
    {
        var result = TemporalComparer.Overlap(UncertainPeriod(PeriodContinuity.NotAsserted), During(Ka(17), Ka(13)));

        Assert.Equal(TemporalOverlap.Possible, result.Overlap);
        Assert.False(result.Basis.HasFlag(ComparisonBasis.ContinuityAssertion));
    }

    [Fact]
    public void An_occurrence_is_compared_with_a_period_by_where_it_can_lie()
    {
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(At(Ka(15)), UncertainPeriod()).Overlap);
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(UncertainPeriod(), At(Ka(19))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(At(Ka(25)), UncertainPeriod()).Overlap);
    }

    [Fact]
    public void Two_periods_overlap_definitely_only_where_both_certainly_run()
    {
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(UncertainPeriod(), Lasting(Ka(15), Ka(11))).Overlap);
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(UncertainPeriod(), Lasting(Ka(25), Ka(19))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(UncertainPeriod(), Lasting(Ka(30), Ka(25))).Overlap);
    }

    // ---- Deep time (SC-07) ----

    [Fact]
    public void An_event_somewhere_in_a_range_and_a_period_across_it_answer_differently()
    {
        var search = During(Ma(58), Ma(52));

        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(new OccurrenceWindow(Ma(60), Ma(50)), search).Overlap);
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(Lasting(Ma(60), Ma(50)), search).Overlap);
    }

    [Fact]
    public void Larger_ages_are_further_in_the_past()
    {
        Assert.Equal(TemporalOrder.Before, TemporalComparer.Order(At(Ma(60)), At(Ma(50))).Order);
        Assert.Equal(TemporalOrder.After, TemporalComparer.Order(At(Ka(10)), At(Ma(50))).Order);
    }

    [Fact]
    public void Ages_with_different_stated_references_are_brought_together()
    {
        var fiftyBefore2000 = At(AgePosition.Of(50, AgeUnit.Years, AgeReference.Year(2000)));
        var tenBefore1950 = At(AgePosition.Of(10, AgeUnit.Years, AgeReference.BeforePresent));

        Assert.Equal(TemporalOrder.After, TemporalComparer.Order(fiftyBefore2000, tenBefore1950).Order);
    }

    [Fact]
    public void An_age_without_a_reference_is_not_compared_with_another_statement()
    {
        var yearsAgo = At(AgePosition.Of(12, AgeUnit.ThousandYears, AgeReference.Unspecified));

        Assert.Equal(TemporalOverlap.InsufficientInformation, TemporalComparer.Overlap(yearsAgo, During(Ka(15), Ka(10))).Overlap);
        Assert.Equal(TemporalOrder.InsufficientInformation, TemporalComparer.Order(yearsAgo, At(Ka(15))).Order);
    }

    [Fact]
    public void A_radiocarbon_age_is_not_compared_with_calendar_years()
    {
        var raw = At(AgePosition.Of(3000, AgeUnit.Years, AgeReference.BeforePresent, AgeBasis.RadiocarbonYears));
        var calendar = At(AgePosition.Of(3000, AgeUnit.Years, AgeReference.BeforePresent));

        Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(raw, calendar).Order);
        Assert.Equal(TemporalOrder.Undetermined, TemporalComparer.Order(raw, raw).Order);
    }

    // ---- Before the common era, year precision, an unknown end (SC-08) ----

    [Fact]
    public void Years_before_and_of_the_common_era_are_ordered_without_a_year_zero()
    {
        Assert.Equal(TemporalOrder.Before, TemporalComparer.Order(At(Bce(500)), At(Bce(1))).Order);
        Assert.Equal(TemporalOrder.Before, TemporalComparer.Order(At(Bce(1)), At(Year(1))).Order);
        Assert.Equal(TemporalOrder.After, TemporalComparer.Order(At(Year(1)), At(Bce(500))).Order);
    }

    [Fact]
    public void Two_events_of_the_same_year_are_not_ordered()
    {
        var result = TemporalComparer.Order(At(Year(1980)), At(Year(1980)));

        Assert.Equal(TemporalOrder.Undetermined, result.Order);
        Assert.Equal(ComparisonBasis.PrecisionRange, result.Basis);
    }

    [Fact]
    public void Stated_days_are_compared_as_they_are()
    {
        var result = TemporalComparer.Order(At(Day(1980, 5, 3)), At(Day(1980, 5, 4)));

        Assert.Equal(TemporalOrder.Before, result.Order);
        Assert.Equal(ComparisonBasis.None, result.Basis);
    }

    private static Period Organization(PeriodEnd end, TimePosition? lastConfirmed = null) =>
        new(PeriodStart.At(Year(2012)), end, PeriodContinuity.Continuous, lastConfirmed);

    [Fact]
    public void An_unknown_end_does_not_make_an_organization_active_later()
    {
        var result = TemporalComparer.Overlap(Organization(PeriodEnd.Unknown, lastConfirmed: Year(2014)), During(Year(2020)));

        Assert.Equal(TemporalOverlap.Possible, result.Overlap);
        Assert.True(result.Basis.HasFlag(ComparisonBasis.OpenBound));
    }

    [Fact]
    public void A_confirmation_supports_the_time_up_to_it()
    {
        var organization = Organization(PeriodEnd.Unknown, lastConfirmed: Year(2014));

        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(organization, During(Year(2013))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(organization, During(Year(2010))).Overlap);
    }

    [Theory]
    [InlineData(PeriodEndState.Ongoing)]
    [InlineData(PeriodEndState.EndedAtUnknownTime)]
    [InlineData(PeriodEndState.Unknown)]
    public void No_open_end_proves_a_later_time(PeriodEndState state)
    {
        var end = state switch
        {
            PeriodEndState.Ongoing => PeriodEnd.Ongoing,
            PeriodEndState.EndedAtUnknownTime => PeriodEnd.EndedAtUnknownTime,
            _ => PeriodEnd.Unknown,
        };

        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(Organization(end), During(Year(2020))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(Organization(end), During(Year(2010))).Overlap);
    }

    [Fact]
    public void An_unknown_start_is_limited_by_the_end()
    {
        var period = new Period(PeriodStart.Unknown, PeriodEnd.At(Year(2010)), PeriodContinuity.Continuous);

        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(period, During(Year(1990))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(period, During(Year(2011))).Overlap);
    }

    // ---- University years in the Solar Hijri calendar (SC-12) ----

    [Fact]
    public void Attendance_in_a_year_is_definite_or_possible_as_the_records_allow()
    {
        var a = Lasting(SolarYear(1380), SolarYear(1384));
        var b = Lasting(SolarYear(1382), SolarYear(1386));
        var c = new Period(PeriodStart.At(SolarYear(1380)), PeriodEnd.Unknown, PeriodContinuity.Continuous);

        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(a, During(SolarYear(1383))).Overlap);
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(b, During(SolarYear(1383))).Overlap);
        Assert.Equal(TemporalOverlap.Possible, TemporalComparer.Overlap(c, During(SolarYear(1383))).Overlap);
    }

    [Fact]
    public void A_period_that_starts_in_a_year_overlaps_that_year()
    {
        var b = Lasting(SolarYear(1382), SolarYear(1386));

        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(b, During(SolarYear(1382))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(b, During(SolarYear(1381))).Overlap);
        Assert.Equal(TemporalOverlap.None, TemporalComparer.Overlap(b, During(SolarYear(1387))).Overlap);
    }

    // ---- Story order (SC-03, TIME-010) ----

    [Fact]
    public void Events_of_one_story_are_ordered_by_their_place()
    {
        var result = TemporalComparer.Order(At(Step(2)), At(Step(5)));

        Assert.Equal(TemporalOrder.Before, result.Order);
        Assert.Equal(ComparisonBasis.None, result.Basis);
        Assert.Equal(TemporalOverlap.Definite, TemporalComparer.Overlap(At(Step(2)), During(Step(1), Step(3))).Overlap);
    }

    [Fact]
    public void A_place_in_a_story_is_never_a_date()
    {
        Assert.Equal(TemporalOverlap.Incompatible, TemporalComparer.Overlap(At(Step(2)), At(Year(1010))).Overlap);
        Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(At(Step(2)), At(Bce(500))).Order);
        Assert.Equal(TemporalOverlap.Incompatible, TemporalComparer.Overlap(At(Step(2)), During(Ka(15), Ka(10))).Overlap);
    }

    [Fact]
    public void The_same_number_in_two_stories_is_not_the_same_time() =>
        Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(At(Step(5)), At(Step(5, sequence: "another-story"))).Order);

    [Fact]
    public void The_order_of_telling_is_not_the_order_of_events() =>
        Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(At(Step(3)), At(Step(5, order: NarrativeOrder.InWorld))).Order);

    // ---- Different kinds of time and calendars (TIME-006) ----

    [Fact]
    public void An_age_and_a_date_have_no_common_scale() =>
        Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(At(Ka(12)), At(Bce(500))).Order);

    [Fact]
    public void Years_of_different_eras_have_no_common_scale()
    {
        Assert.Equal(TemporalOverlap.Incompatible, TemporalComparer.Overlap(Lasting(SolarYear(1380), SolarYear(1384)), During(Year(2003))).Overlap);
        Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(At(SolarYear(1380)), At(Year(2001))).Order);
    }

    [Fact]
    public void Dates_without_a_named_calendar_are_ordered_by_their_years_when_far_enough_apart()
    {
        var result = TemporalComparer.Order(At(UnnamedCalendarYear(1450)), At(UnnamedCalendarYear(1605)));

        Assert.Equal(TemporalOrder.Before, result.Order);
        Assert.True(result.Basis.HasFlag(ComparisonBasis.YearCountOnly));
    }

    [Theory]
    [InlineData(1450, 1450, TemporalOrder.Undetermined)]
    [InlineData(1450, 1452, TemporalOrder.Undetermined)]
    [InlineData(1450, 1453, TemporalOrder.Before)]
    [InlineData(1453, 1450, TemporalOrder.After)]
    public void Years_of_unnamed_calendars_need_three_years_between_them(int first, int second, TemporalOrder expected) =>
        Assert.Equal(expected, TemporalComparer.Order(At(UnnamedCalendarYear(first)), At(UnnamedCalendarYear(second))).Order);

    [Fact]
    public void The_same_year_in_two_calendars_is_not_the_same_stretch_of_time()
    {
        var julian = At(CalendarPosition.OfDay(CalendarSystem.Julian, Era.CommonEra, 1500, 12, 28));
        var gregorian = At(Day(1500, 1, 5));

        var result = TemporalComparer.Order(gregorian, julian);

        Assert.Equal(TemporalOrder.Undetermined, result.Order);
        Assert.True(result.Basis.HasFlag(ComparisonBasis.YearCountOnly));
    }

    // ---- What cannot be answered ----

    public static TheoryData<TemporalExpression> ExpressionsWithoutAPlace() =>
    [
        new UnknownTime(),
        new InapplicableTime(),
        new ElapsedTime(30, ElapsedUnit.Years, isApproximate: true),
        new EventTime("fx-family-school-1970"),
        new Period(PeriodStart.Unknown, PeriodEnd.Unknown),
    ];

    [Theory]
    [MemberData(nameof(ExpressionsWithoutAPlace))]
    public void A_time_without_a_place_gives_no_answer_instead_of_a_guess(TemporalExpression expression)
    {
        Assert.Equal(TemporalOverlap.InsufficientInformation, TemporalComparer.Overlap(expression, During(Year(1980))).Overlap);
        Assert.Equal(TemporalOverlap.InsufficientInformation, TemporalComparer.Overlap(At(Year(1980)), expression).Overlap);
        Assert.Equal(TemporalOrder.InsufficientInformation, TemporalComparer.Order(expression, At(Year(1980))).Order);
    }

    [Fact]
    public void An_approximate_position_is_compared_at_its_stated_value_and_says_so()
    {
        var result = TemporalComparer.Order(At(Year(1450).Approximately()), At(Year(1605)));

        Assert.Equal(TemporalOrder.Before, result.Order);
        Assert.True(result.Basis.HasFlag(ComparisonBasis.ApproximateValue));
    }

    [Fact]
    public void An_answer_that_is_not_one_rests_on_nothing()
    {
        Assert.Equal(ComparisonBasis.None, TemporalComparer.Overlap(At(Step(2)), At(Year(1010).Approximately())).Basis);
        Assert.Equal(ComparisonBasis.None, TemporalComparer.Order(new UnknownTime(), At(Year(1010))).Basis);
    }
}
