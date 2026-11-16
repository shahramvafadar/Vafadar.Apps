using System.Text.Json;
using static Vafadar.Temporal.Tests.Times;

namespace Vafadar.Temporal.Tests;

public sealed class JsonTests
{
    public static TheoryData<TemporalExpression> EveryKind() =>
    [
        // An exact day and a year without month or day.
        At(Day(1980, 5, 3)),
        At(Year(1980)),
        At(CalendarPosition.OfMonth(CalendarSystem.Julian, Era.BeforeCommonEra, 44, 3)),

        // The step from 1 BCE to 1 CE.
        new OccurrenceWindow(Bce(1), Year(1)),

        // An approximate year in a calendar the source does not name, with its wording.
        At(UnnamedCalendarYear(1450).Approximately()) with { SourceText = "circa 1450 CE" },

        // An occurrence somewhere in a window and alternatives.
        new OccurrenceWindow(Ka(20), Ka(10)),
        new AlternativeOccurrences([At(Year(1980)), new OccurrenceWindow(Year(1990), Year(1992))]),

        // A period with independently uncertain bounds, and every open end.
        new Period(PeriodStart.Between(Ka(20), Ka(18)), PeriodEnd.Between(Ka(12), Ka(10)), PeriodContinuity.Continuous),
        new Period(PeriodStart.At(Year(2012)), PeriodEnd.Unknown, PeriodContinuity.Continuous, Year(2014)),
        new Period(PeriodStart.At(Year(2012)), PeriodEnd.Ongoing, lastConfirmed: Day(2026, 11, 15)),
        new Period(PeriodStart.Unknown, PeriodEnd.EndedAtUnknownTime),
        new Period(PeriodStart.At(SolarYear(1380)), PeriodEnd.At(SolarYear(1384)), PeriodContinuity.Continuous),

        // A duration without a position.
        new ElapsedTime(30, ElapsedUnit.Years, isApproximate: true),
        new ElapsedTime(2.5m, ElapsedUnit.Months),

        // Deep time with a fixed reference, an unstated reference and a radiocarbon age.
        new OccurrenceWindow(Ma(60), Ma(50)),
        At(AgePosition.Of(55.0m, AgeUnit.MillionYears, AgeReference.Year(2000)).Approximately()),
        At(AgePosition.Of(4.54m, AgeUnit.BillionYears, AgeReference.Unspecified)),
        At(AgePosition.Of(3000, AgeUnit.Years, AgeReference.BeforePresent, AgeBasis.RadiocarbonYears)),

        // A place in a story without any calendar, in both orders.
        At(Step(2)),
        At(Step(7, order: NarrativeOrder.InWorld)),

        // The time of another event, unknown and inapplicable.
        new EventTime("fx-family-school-1970"),
        new UnknownTime(),
        new InapplicableTime(),
    ];

    [Theory]
    [MemberData(nameof(EveryKind))]
    public void An_expression_comes_back_as_it_went_in(TemporalExpression expression)
    {
        var json = TemporalJson.Serialize(expression);

        var back = TemporalJson.Deserialize(json);

        Assert.Equal(expression, back);
        Assert.Equal(json, TemporalJson.Serialize(back));
    }

    [Fact]
    public void A_year_is_written_without_month_or_day() =>
        Assert.Equal(
            """{"kind":"occurrence","at":{"frame":"calendar","calendar":"gregorian","era":"commonEra","year":1980}}""",
            TemporalJson.Serialize(At(Year(1980))));

    [Fact]
    public void A_calendar_that_is_not_named_is_not_written() =>
        Assert.Equal(
            """{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":1450,"approximate":true},"sourceText":"circa 1450 CE"}""",
            TemporalJson.Serialize(At(UnnamedCalendarYear(1450).Approximately()) with { SourceText = "circa 1450 CE" }));

    [Fact]
    public void A_year_before_the_era_is_written_as_people_write_it() =>
        Assert.Equal(
            """{"kind":"occurrence","at":{"frame":"calendar","calendar":"gregorian","era":"beforeCommonEra","year":500}}""",
            TemporalJson.Serialize(At(Bce(500))));

    [Fact]
    public void An_age_is_written_with_its_digits_unit_and_reference()
    {
        var json = TemporalJson.Serialize(At(AgePosition.Of(55.0m, AgeUnit.MillionYears, AgeReference.Year(2000))));

        Assert.Equal("""{"kind":"occurrence","at":{"frame":"age","value":55.0,"unit":"millionYears","reference":2000}}""", json);
        Assert.Equal("55.0", ((AgePosition)((Occurrence)TemporalJson.Deserialize(json)).At).Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void A_place_in_a_story_is_written_without_any_date() =>
        Assert.Equal(
            """{"kind":"occurrence","at":{"frame":"narrative","sequence":"story-rostam-sohrab","order":"presentation","ordinal":2}}""",
            TemporalJson.Serialize(At(Step(2))));

    [Fact]
    public void An_open_end_is_written_as_its_state()
    {
        var json = TemporalJson.Serialize(new Period(PeriodStart.At(Year(2012)), PeriodEnd.Unknown, PeriodContinuity.Continuous, Year(2014)));

        Assert.Contains("""  "end":{"state":"unknown"}  """.Trim(), json, StringComparison.Ordinal);
        Assert.Contains("""  "continuity":"continuous"  """.Trim(), json, StringComparison.Ordinal);
        Assert.Contains("""  "lastConfirmed":{"frame":"calendar"  """.Trim(), json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"kind":"moment"}""")]
    [InlineData("""{"at":{"frame":"calendar","era":"commonEra","year":1980}}""")]
    [InlineData("""{"kind":"occurrence"}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":1980},"month":5}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":1980,"hour":5}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"clock","value":5}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":0}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":1980,"day":3}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":2023,"month":2,"day":30,"calendar":"gregorian"}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"ce","year":1980}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"CommonEra","year":1980}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":"1980"}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":1980.5}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","calendar":"unspecified","era":"commonEra","year":1980}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","calendar":"gregorian","era":"solarHijri","year":1380}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":1980,"approximate":false}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"age","value":5.5e1,"unit":"millionYears"}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"age","value":-5,"unit":"years"}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"age","value":5,"unit":"centuries"}}""")]
    [InlineData("""{"kind":"occurrence","at":{"frame":"narrative","sequence":"s","order":"presentation","ordinal":0}}""")]
    [InlineData("""{"kind":"occurrenceWindow","earliest":{"frame":"calendar","era":"commonEra","year":2010},"latest":{"frame":"calendar","era":"commonEra","year":2004}}""")]
    [InlineData("""{"kind":"alternatives","options":[{"kind":"unknown"},{"kind":"unknown"}]}""")]
    [InlineData("""{"kind":"alternatives","options":{"kind":"unknown"}}""")]
    [InlineData("""{"kind":"period","start":{"state":"unknown"},"end":{"state":"unknown"}}""")]
    [InlineData("""{"kind":"period","start":{"state":"ongoing"},"end":{"state":"unknown"},"continuity":"continuous"}""")]
    [InlineData("""{"kind":"period","start":{"state":"unknown","at":{"frame":"calendar","era":"commonEra","year":1980}},"end":{"state":"unknown"},"continuity":"continuous"}""")]
    [InlineData("""{"kind":"elapsed","value":30,"unit":"years","approximate":"yes"}""")]
    [InlineData("""["occurrence"]""")]
    [InlineData("""null""")]
    public void Text_that_is_not_exactly_an_expression_is_refused(string json) =>
        Assert.Throws<JsonException>(() => TemporalJson.Deserialize(json));

    private sealed record Holder(TemporalExpression When, TimePosition Where, TemporalExpression? Other);

    [Fact]
    public void The_converters_work_inside_the_options_of_an_app()
    {
        var options = new JsonSerializerOptions();
        foreach (var converter in TemporalJson.Converters)
        {
            options.Converters.Add(converter);
        }

        var holder = new Holder(new OccurrenceWindow(Ma(60), Ma(50)), Step(2), null);

        var back = JsonSerializer.Deserialize<Holder>(JsonSerializer.Serialize(holder, options), options);

        Assert.Equal(holder, back);
    }
}
