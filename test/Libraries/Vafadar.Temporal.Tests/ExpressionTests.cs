using static Vafadar.Temporal.Tests.Times;

namespace Vafadar.Temporal.Tests;

public sealed class ExpressionTests
{
    [Fact]
    public void An_occurrence_in_a_window_is_not_a_period_with_the_same_positions()
    {
        TemporalExpression somewhen = new OccurrenceWindow(Ma(60), Ma(50));
        TemporalExpression throughout = Lasting(Ma(60), Ma(50));

        Assert.NotEqual(somewhen, throughout);
    }

    [Fact]
    public void A_window_that_ends_before_it_begins_is_refused_not_swapped()
    {
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(Year(2010), Year(2004)));
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(Ka(10), Ka(20)));
        Assert.Throws<ArgumentException>(() => PeriodStart.Between(Year(2010), Year(2004)));
        Assert.Throws<ArgumentException>(() => PeriodEnd.Between(Ka(10), Ka(12)));
        Assert.Throws<ArgumentException>(() => TemporalWindow.Between(Step(5), Step(2)));
    }

    [Fact]
    public void A_window_of_two_kinds_of_time_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(Year(1980), Ka(10)));
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(Step(1), Year(1980)));
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(Step(1), Step(2, sequence: "another-story")));
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(Year(1380), SolarYear(1381)));
    }

    [Fact]
    public void One_statement_shares_what_it_leaves_unsaid()
    {
        var yearsAgo = new OccurrenceWindow(
            AgePosition.Of(20, AgeUnit.ThousandYears, AgeReference.Unspecified),
            AgePosition.Of(10, AgeUnit.ThousandYears, AgeReference.Unspecified));
        var unnamedCalendar = new OccurrenceWindow(
            CalendarPosition.OfMonth(CalendarSystem.Unspecified, Era.CommonEra, 1450, 3),
            CalendarPosition.OfMonth(CalendarSystem.Unspecified, Era.CommonEra, 1450, 5));

        Assert.False(((AgePosition)yearsAgo.Earliest).Reference.IsSpecified);
        Assert.Equal(5, ((CalendarPosition)unnamedCalendar.Latest).Month);
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(
            CalendarPosition.OfMonth(CalendarSystem.Unspecified, Era.CommonEra, 1450, 5),
            CalendarPosition.OfMonth(CalendarSystem.Unspecified, Era.CommonEra, 1450, 3)));
    }

    [Fact]
    public void A_stated_and_an_unstated_reference_do_not_mix_in_one_statement() =>
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(AgePosition.Of(20, AgeUnit.ThousandYears, AgeReference.Unspecified), Ka(10)));

    [Fact]
    public void Alternatives_stay_separate_possibilities()
    {
        var either = new AlternativeOccurrences([At(Year(1980)), At(Year(1990))]);

        Assert.Equal(2, either.Options.Length);
        Assert.Equal(At(Year(1980)), either.Options[0]);
        Assert.Equal(At(Year(1990)), either.Options[1]);
    }

    [Fact]
    public void Alternatives_with_the_same_options_are_equal()
    {
        var first = new AlternativeOccurrences([At(Year(1980)), At(Year(1990))]);
        var second = new AlternativeOccurrences([At(Year(1980)), At(Year(1990))]);
        var reversed = new AlternativeOccurrences([At(Year(1990)), At(Year(1980))]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, reversed);
    }

    [Fact]
    public void Alternatives_are_two_or_more_occurrences()
    {
        Assert.Throws<ArgumentException>(() => new AlternativeOccurrences([At(Year(1980))]));
        Assert.Throws<ArgumentException>(() => new AlternativeOccurrences([At(Year(1980)), Lasting(Year(1990), Year(1995))]));
        Assert.Throws<ArgumentException>(() => new AlternativeOccurrences([At(Year(1980)), new UnknownTime()]));
    }

    [Fact]
    public void The_four_open_ends_of_a_period_are_different_statements()
    {
        var start = PeriodStart.At(Year(2012));
        var ends = new[] { PeriodEnd.Ongoing, PeriodEnd.EndedAtUnknownTime, PeriodEnd.Unknown, PeriodEnd.At(Year(2014)) };

        var periods = ends.Select(end => new Period(start, end)).ToList();

        Assert.Equal(4, periods.Distinct().Count());
        Assert.Equal(4, ends.Select(end => end.State).Distinct().Count());
    }

    [Fact]
    public void An_unknown_end_has_no_position_and_is_not_ongoing()
    {
        var period = new Period(PeriodStart.At(Year(2012)), PeriodEnd.Unknown, lastConfirmed: Year(2014));

        Assert.Null(period.End.Earliest);
        Assert.Null(period.End.Latest);
        Assert.NotEqual(PeriodEndState.Ongoing, period.End.State);
        Assert.Equal(Year(2014), period.LastConfirmed);
    }

    [Fact]
    public void The_bounds_of_a_period_are_uncertain_independently()
    {
        var period = new Period(PeriodStart.Between(Ka(20), Ka(18)), PeriodEnd.Between(Ka(12), Ka(10)), PeriodContinuity.Continuous);

        Assert.Equal(PeriodStartState.Between, period.Start.State);
        Assert.Equal(Ka(20), period.Start.Earliest);
        Assert.Equal(Ka(18), period.Start.Latest);
        Assert.Equal(Ka(12), period.End.Earliest);
        Assert.Equal(Ka(10), period.End.Latest);
    }

    [Fact]
    public void A_period_that_ends_before_it_starts_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Lasting(Year(2010), Year(2004)));
        Assert.Throws<ArgumentException>(() => new Period(PeriodStart.At(Year(2012)), PeriodEnd.Unknown, lastConfirmed: Year(2010)));
        Assert.Throws<ArgumentException>(() => new Period(PeriodStart.At(Year(2012)), PeriodEnd.At(Year(2014)), lastConfirmed: Year(2016)));
    }

    [Fact]
    public void A_period_may_start_and_end_in_the_same_year() =>
        Assert.Equal(Year(2014), Lasting(Year(2014), Year(2014)).End.Latest);

    [Fact]
    public void Continuity_is_asserted_not_assumed() =>
        Assert.Equal(PeriodContinuity.NotAsserted, new Period(PeriodStart.At(Year(2004)), PeriodEnd.At(Year(2010))).Continuity);

    [Fact]
    public void An_elapsed_time_has_an_amount_and_no_position()
    {
        var about = new ElapsedTime(30, ElapsedUnit.Years, isApproximate: true);

        Assert.Equal(30m, about.Value);
        Assert.True(about.IsApproximate);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ElapsedTime(-1, ElapsedUnit.Years));
    }

    [Fact]
    public void Unknown_and_inapplicable_are_different()
    {
        TemporalExpression unknown = new UnknownTime();
        TemporalExpression inapplicable = new InapplicableTime();

        Assert.NotEqual(unknown, inapplicable);
        Assert.Equal(new UnknownTime(), unknown);
    }

    [Fact]
    public void The_wording_of_the_source_is_kept_and_does_not_change_the_value()
    {
        var circa = At(UnnamedCalendarYear(1450).Approximately()) with { SourceText = "circa 1450 CE" };

        Assert.Equal("circa 1450 CE", circa.SourceText);
        Assert.Equal(1450, ((CalendarPosition)circa.At).Year);
        Assert.NotEqual(At(UnnamedCalendarYear(1450).Approximately()), circa);
    }

    [Fact]
    public void An_event_time_names_its_event()
    {
        Assert.Equal("fx-family-school-1970", new EventTime("fx-family-school-1970").EventId);
        Assert.Throws<ArgumentException>(() => new EventTime(" "));
    }
}
