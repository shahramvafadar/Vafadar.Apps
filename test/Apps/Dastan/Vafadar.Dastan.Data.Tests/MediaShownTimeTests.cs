using System.Text.Json.Nodes;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The time an item shows, as a collection writes it (step 19.9, ADR 0021, R-MED-04): a time of the world, apart
/// from the time the item was made, never guessed from it, and never a place in a telling.
/// </summary>
public sealed class MediaShownTimeTests
{
    private const string Item = "media-met-452419";

    [Fact]
    public void An_item_that_says_nothing_of_the_time_it_shows_shows_no_stated_time()
    {
        Assert.All(SampleFiles.Read().Media, item => Assert.Null(item.Shows));
    }

    [Fact]
    public void The_time_an_item_shows_is_read_as_it_is_written_and_leaves_the_time_it_was_made_as_it_was()
    {
        var made = SampleFiles.Read().Media.Single(item => item.Id.Value == Item).Created;
        var catalogue = SampleFiles.Read(sample => sample.Record("media", Item)["depictedTime"] = JsonNode.Parse(
            """
            { "kind": "occurrenceWindow",
              "earliest": { "frame": "calendar", "era": "beforeCommonEra", "year": 600 },
              "latest": { "frame": "calendar", "era": "beforeCommonEra", "year": 500 } }
            """));

        var item = catalogue.Media.Single(item => item.Id.Value == Item);
        var shows = Assert.IsType<OccurrenceWindow>(item.Shows);
        Assert.Equal(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, 600), shows.Earliest);
        Assert.Equal(made, item.Created);
    }

    [Fact]
    public void An_item_may_show_a_depth_of_time()
    {
        var catalogue = SampleFiles.Read(sample => sample.Record("media", Item)["depictedTime"] = JsonNode.Parse(
            """{ "kind": "occurrence", "at": { "frame": "age", "value": 55, "unit": "millionYears", "reference": 1950 } }"""));

        var shows = Assert.IsType<Occurrence>(catalogue.Media.Single(item => item.Id.Value == Item).Shows);
        Assert.Equal(AgePosition.Of(55, AgeUnit.MillionYears, AgeReference.BeforePresent), shows.At);
    }

    [Fact]
    public void A_step_of_a_telling_is_no_time_an_item_shows()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("media", Item)["depictedTime"] = JsonNode.Parse(
            """{ "kind": "occurrence", "at": { "frame": "narrative", "sequence": "story", "order": "presentation", "ordinal": 3 } }"""));

        Assert.Equal([$"{Item}: depictedTime is a place in a telling: the steps an item shows are its depictedEventIds."], problems);
    }

    [Fact]
    public void A_time_that_cannot_be_read_is_refused_and_not_left_out()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("media", Item)["depictedTime"] = "about the year 1020");

        Assert.StartsWith($"{Item}: depictedTime cannot be read:", Assert.Single(problems), StringComparison.Ordinal);
    }
}
