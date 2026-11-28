using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The times the record of a name states among its facts (D-098): they place the name in the chart of time of
/// its collection and among what is said of a year, as the dated statements and the relations with a time do.
/// </summary>
public sealed class RecordTimesTests
{
    private static JsonObject Year(int year, bool approximate = false)
    {
        var at = new JsonObject { ["frame"] = "calendar", ["era"] = "beforeCommonEra", ["year"] = year };
        if (approximate)
        {
            at["approximate"] = true;
        }

        return at;
    }

    private static JsonObject At(string fact, int year, bool approximate = false) =>
        new() { ["fact"] = fact, ["time"] = new JsonObject { ["kind"] = "occurrence", ["at"] = Year(year, approximate) } };

    private static JsonObject Between(string fact, int earliest, int latest) =>
        new() { ["fact"] = fact, ["time"] = new JsonObject { ["kind"] = "occurrenceWindow", ["earliest"] = Year(earliest), ["latest"] = Year(latest) } };

    private static JsonObject Unknown(string fact) => new() { ["fact"] = fact, ["time"] = new JsonObject { ["kind"] = "unknown" } };

    private static Projection Read(int level) => Projection.Of(
        SampleFiles.Read(sample =>
        {
            var entities = sample["entities"]!.AsArray();
            void Add(string id, string type, int stage, params JsonObject[] facts) => entities.Add(new JsonObject
            {
                ["id"] = id,
                ["type"] = type,
                ["label"] = id,
                ["summary"] = "Made up for a test.",
                ["sourceIds"] = new JsonArray("src-ganjoor-index"),
                ["spoilerLevel"] = stage,
                ["facts"] = new JsonArray([.. facts]),
            });

            Add("person-whole-life", "Person", 0, At("born", 600), At("died", 559));
            Add("person-birth-only", "Person", 0, At("born", 590), Unknown("died"));
            Add("person-no-time", "Person", 0, Unknown("born"), Unknown("died"));
            Add("event-sack", "Event", 0, Between("happened", 646, 640));
            Add("period-reign", "Period", 0, At("began", 550), At("ended", 530));
            Add("person-later", "Person", 3, At("born", 580), At("died", 570));
            var cited = At("happened", 620);
            cited["sourceIds"] = new JsonArray("src-ganjoor-s02");
            Add("event-cited", "Event", 0, cited);
        }),
        new ReadingLevel(level),
        Audience.LocalDemonstration)!;

    private static YearOfEra Bce(int year) => new(Era.BeforeCommonEra, year);

    [Fact]
    public void A_record_with_both_ends_states_a_stretch_and_one_with_one_end_states_that_one_time()
    {
        var read = Read(0);
        RecordTime[] Of(string id) => [.. RecordTimes.Of(read.Entities.Single(entity => entity.Id.Value == id))];

        var life = Assert.Single(Of("person-whole-life"));
        Assert.True(life.IsSpan);
        Assert.IsType<Period>(life.Time);
        Assert.Equal("person-whole-life~born~died", life.Key);

        var birth = Assert.Single(Of("person-birth-only"));
        Assert.False(birth.IsSpan);
        Assert.Equal(ProfileFacts.Born.Key, birth.From.Key);
        Assert.IsType<Occurrence>(birth.Time);

        Assert.Empty(Of("person-no-time"));
        Assert.Equal(ProfileFacts.Happened.Key, Assert.Single(Of("event-sack")).From.Key);
        Assert.True(Assert.Single(Of("period-reign")).IsSpan);
    }

    [Fact]
    public void A_time_whose_facts_name_no_source_rests_on_the_sources_of_its_record()
    {
        var read = Read(0);
        RecordTime[] Of(string id) => [.. RecordTimes.Of(read.Entities.Single(entity => entity.Id.Value == id))];

        Assert.Equal(["src-ganjoor-index"], Assert.Single(Of("event-sack")).Sources.Select(source => source.Value));
        Assert.Equal(["src-ganjoor-index"], Assert.Single(Of("person-whole-life")).Sources.Select(source => source.Value));
        Assert.Equal(["src-ganjoor-s02"], Assert.Single(Of("event-cited")).Sources.Select(source => source.Value));
    }

    [Fact]
    public void A_name_whose_record_states_a_time_stands_in_the_chart_of_time_and_one_that_states_none_does_not()
    {
        var subjects = Read(0).Timeline().Subjects.Select(subject => subject.Id.Value).ToHashSet();

        Assert.Contains("person-whole-life", subjects);
        Assert.Contains("person-birth-only", subjects);
        Assert.Contains("event-sack", subjects);
        Assert.Contains("period-reign", subjects);
        Assert.DoesNotContain("person-no-time", subjects);
    }

    [Fact]
    public void The_years_of_a_collection_reach_what_its_records_state()
    {
        var years = Read(0).Years();

        Assert.NotNull(years);
        Assert.True(years.First.Place <= Bce(646).Place);
        Assert.Contains(Bce(600), years.Turns);
        Assert.Contains(Bce(559), years.Turns);
        Assert.True(Read(0).SaysWhen(2026));
    }

    [Fact]
    public void A_year_names_who_lived_and_what_happened_in_it_and_nobody_whose_end_is_not_stated()
    {
        var read = Read(0);
        string[] In(int year) => [.. read.At(Bce(year)).Records.Select(record => record.Entity.Id.Value).Order(StringComparer.Ordinal)];

        // Inside a life; the year of a birth of which no death is stated; no year after it.
        Assert.Equal(["person-whole-life"], In(570));
        Assert.Equal(["person-birth-only", "person-whole-life"], In(590));
        Assert.Equal(["period-reign"], In(540));
        Assert.Empty(In(500));

        // A window of years: possibly in each of them, not for certain.
        var sack = Assert.Single(read.At(Bce(643)).Records);
        Assert.Equal("event-sack", sack.Entity.Id.Value);
        Assert.Equal(Holding.Possibly, sack.Holding);

        // A stretch of years that is asked for gives the same.
        var held = read.Held(AskedTime.Read("600 BCE to 560 BCE")!, 2026);
        Assert.Contains(held.Records, record => record.Entity.Id.Value == "person-whole-life");
        Assert.DoesNotContain(held.Records, record => record.Entity.Id.Value == "event-sack");
        Assert.False(held.IsEmpty);
    }

    [Fact]
    public void What_belongs_to_a_later_reading_stage_is_in_no_view_of_time()
    {
        var early = Read(0);

        Assert.DoesNotContain(early.Timeline().Subjects, subject => subject.Id.Value == "person-later");
        Assert.Equal(["person-whole-life"], early.At(Bce(575)).Records.Select(record => record.Entity.Id.Value));
        Assert.DoesNotContain(Bce(580), early.Years()!.Turns);

        var late = Read(4);
        Assert.Contains(late.Timeline().Subjects, subject => subject.Id.Value == "person-later");
        Assert.Contains(late.At(Bce(575)).Records, record => record.Entity.Id.Value == "person-later");
    }
}
