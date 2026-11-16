using System.Globalization;
using System.Text.Json.Nodes;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The cross-domain fixtures (a company, a team, a family) are test data only: they are never loaded by the app and
/// never shown. Their time values check that the time library can state what other domains than a story need – a
/// year without a day, a period with an unknown end, a time that is another event. Their accounts and access
/// policies belong to private spaces, which are not part of the first milestone and are not read here.
/// </summary>
public sealed class FixtureTimeTests
{
    private static readonly Lazy<JsonObject> Fixtures =
        new(() => JsonNode.Parse(File.ReadAllText(SampleFiles.FixturesPath))!.AsObject());

    private static readonly string[] TimeKinds = ["partial_calendar_date", "calendar_date", "period", "event_reference", "exact_instant"];

    [Fact]
    public void The_fixtures_are_marked_as_made_up_and_as_never_to_be_published()
    {
        Assert.True((bool)Fixtures.Value["synthetic"]!);
        Assert.False((bool)Fixtures.Value["productionPublicationAllowed"]!);
        Assert.All(Fixtures.Value["fixtures"]!.AsArray(), fixture => Assert.True((bool)fixture!["synthetic"]!));
    }

    [Fact]
    public void The_fixtures_are_not_among_the_files_of_the_app()
    {
        var path = Path.GetFullPath(SampleFiles.FixturesPath);

        Assert.False(path.StartsWith(Path.GetFullPath(SampleFiles.App) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(Directory.EnumerateFiles(SampleFiles.Content, "*fixture*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Every_time_of_the_fixtures_can_be_stated_without_adding_or_losing_precision()
    {
        var times = Times(Fixtures.Value).ToList();

        Assert.Equal(
            [("calendar_date", 2), ("event_reference", 1), ("exact_instant", 4), ("partial_calendar_date", 17), ("period", 4)],
            times.GroupBy(time => (string)time["kind"]!).Select(group => (group.Key, group.Count())).Order());
        Assert.All(times, time => Assert.NotNull(Read(time)));
    }

    [Fact]
    public void A_year_stays_a_year()
    {
        var founded = Event("FIX-COMPANY-01", "fx-co-founded")["time"]!.AsObject();

        var position = Assert.IsType<CalendarPosition>(Assert.IsType<Occurrence>(Read(founded)).At);

        Assert.Equal((CalendarSystem.Gregorian, Era.CommonEra, 2000), (position.Calendar, position.Era, position.Year));
        Assert.Equal(CalendarPrecision.Year, position.Precision);
        Assert.Null(position.Month);
    }

    [Fact]
    public void A_role_held_from_one_year_to_another_is_a_continuous_period_between_two_years()
    {
        var role = Assert.IsType<Period>(Read(Relationship("FIX-COMPANY-01", "fx-co-manager-role")["time"]!.AsObject()));

        Assert.Equal(PeriodContinuity.Continuous, role.Continuity);
        Assert.Equal(2004, Assert.IsType<CalendarPosition>(role.Start.Earliest).Year);
        Assert.Equal(PeriodEndState.Known, role.End.State);
        Assert.Equal(2010, Assert.IsType<CalendarPosition>(role.End.Latest).Year);
    }

    [Fact]
    public void An_end_nobody_knows_is_neither_an_end_nor_still_going_on()
    {
        var ownership = Assert.IsType<Period>(Read(Relationship("FIX-COMPANY-01", "fx-co-ownership")["time"]!.AsObject()));

        Assert.Equal(PeriodEndState.Unknown, ownership.End.State);
        Assert.Equal(2014, Assert.IsType<CalendarPosition>(ownership.LastConfirmed).Year);

        // Confirmed in 2014 says nothing about 2026: the comparison does not turn "unknown" into "still the owner".
        var today = new Occurrence(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 2026));
        Assert.NotEqual(TemporalOverlap.Definite, TemporalComparer.Overlap(ownership, today).Overlap);
    }

    [Fact]
    public void A_question_about_a_year_inside_a_role_is_answered_and_one_after_it_is_not_a_yes()
    {
        var role = Assert.IsType<Period>(Read(Relationship("FIX-COMPANY-01", "fx-co-manager-role")["time"]!.AsObject()));

        var during = TemporalComparer.Overlap(role, new Occurrence(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 2008)));
        var after = TemporalComparer.Overlap(role, new Occurrence(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 2014)));

        Assert.Equal(TemporalOverlap.Definite, during.Overlap);
        Assert.Equal(TemporalOverlap.None, after.Overlap);
    }

    [Fact]
    public void A_time_that_is_another_event_stays_a_reference_to_that_event()
    {
        var reference = Times(Fixtures.Value).Single(time => (string)time["kind"]! == "event_reference");

        var time = Assert.IsType<EventTime>(Read(reference));

        Assert.Equal((string)reference["eventId"]!, time.EventId);
    }

    [Fact]
    public void The_moment_something_was_shared_is_a_timestamp_and_not_a_time_of_the_subject()
    {
        var shared = Times(Fixtures.Value).First(time => (string)time["kind"]! == "exact_instant");

        var moment = Assert.IsType<DateTimeOffset>(Read(shared));

        Assert.Equal(TimeSpan.Zero, moment.Offset);
    }

    // --- The two invalid copies of the pack's validator that concern the fixtures' times ---

    [Fact]
    public void An_era_cannot_stand_where_a_calendar_belongs()
    {
        var time = (JsonObject)Event("FIX-COMPANY-01", "fx-co-founded")["time"]!.DeepClone();
        time["calendar"] = "CE";

        var refusal = Assert.Throws<FormatException>(() => Read(time));

        Assert.Equal("calendar is CE, which is an era and not a calendar.", refusal.Message);
    }

    [Fact]
    public void A_period_that_ends_before_it_starts_cannot_be_stated()
    {
        var time = (JsonObject)Relationship("FIX-COMPANY-01", "fx-co-manager-role")["time"]!.DeepClone();
        time["end"]!["year"] = 1999;

        Assert.Throws<ArgumentException>(() => Read(time));
    }

    private static JsonObject Fixture(string id) =>
        Fixtures.Value["fixtures"]!.AsArray().Select(node => node!.AsObject()).Single(fixture => (string)fixture["id"]! == id);

    private static JsonObject Event(string fixture, string id) =>
        Fixture(fixture)["events"]!.AsArray().Select(node => node!.AsObject()).Single(record => (string)record["id"]! == id);

    private static JsonObject Relationship(string fixture, string id) =>
        Fixture(fixture)["relationships"]!.AsArray().Select(node => node!.AsObject()).Single(record => (string)record["id"]! == id);

    /// <summary>Returns every time value of the document, wherever it stands.</summary>
    private static IEnumerable<JsonObject> Times(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject record:
                if (record["kind"] is JsonValue kind && kind.TryGetValue<string>(out var name) && TimeKinds.Contains(name))
                {
                    yield return record;
                }

                foreach (var (_, child) in record)
                {
                    foreach (var time in Times(child))
                    {
                        yield return time;
                    }
                }

                break;

            case JsonArray list:
                foreach (var time in list.SelectMany(Times))
                {
                    yield return time;
                }

                break;
        }
    }

    /// <summary>
    /// Reads a time value of the fixtures: an expression of the time library for a time of the subject, a timestamp
    /// for the moment something was done in the app.
    /// </summary>
    private static object Read(JsonObject time) => (string)time["kind"]! switch
    {
        "partial_calendar_date" or "calendar_date" => new Occurrence(Position(time)),
        "period" => new Period(
            PeriodStart.At(Position(time["start"]!.AsObject())),
            End(time["end"]!.AsObject()),
            (string?)time["continuity"] == "continuous_within_asserted_period" ? PeriodContinuity.Continuous : PeriodContinuity.NotAsserted,
            time["lastConfirmedAt"] is JsonObject confirmed ? Position(confirmed) : null),
        "event_reference" => new EventTime((string)time["eventId"]!),
        "exact_instant" => DateTimeOffset.Parse((string)time["value"]!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        var kind => throw new FormatException($"The kind of time {kind} is not known."),
    };

    private static PeriodEnd End(JsonObject end) => (string?)end["state"] switch
    {
        null => PeriodEnd.At(Position(end)),
        "unknown" => PeriodEnd.Unknown,
        var state => throw new FormatException($"The state {state} of an end is not known."),
    };

    private static CalendarPosition Position(JsonObject date)
    {
        var calendar = (string?)date["calendar"] switch
        {
            "Gregorian" => CalendarSystem.Gregorian,
            "CE" or "BCE" or "AD" or "BC" => throw new FormatException($"calendar is {date["calendar"]}, which is an era and not a calendar."),
            var other => throw new FormatException($"The calendar {other} is not known."),
        };
        var era = (string?)date["era"] switch
        {
            "CE" => Era.CommonEra,
            "BCE" => Era.BeforeCommonEra,
            var other => throw new FormatException($"The era {other} is not known."),
        };
        if (date["date"] is { } day)
        {
            var parsed = DateOnly.ParseExact((string)day!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return CalendarPosition.OfDay(calendar, era, parsed.Year, parsed.Month, parsed.Day);
        }

        return (string?)date["precision"] == "year"
            ? CalendarPosition.OfYear(calendar, era, (int)date["year"]!)
            : throw new FormatException($"The precision {date["precision"]} is not known.");
    }
}
