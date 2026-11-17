using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// What is stated about an entity with a time of the calendar (step 9.4): the rules a collection keeps for it,
/// and that a visitor gets no statement of a later reading stage – not in the timeline and not with the entity.
/// </summary>
public sealed class DatedStatementTests
{
    private static readonly EntityId Work = new("work");

    private static DatedStatement Written(string key = "work-written", int level = 0, string source = "passage-0") => new()
    {
        Key = key,
        Label = SmallStory.Text("The work is written"),
        Time = new Period(
            PeriodStart.At(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 977)),
            PeriodEnd.At(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 1010))),
        Sources = [new SourceId(source)],
        Review = ReviewStatus.Proposed,
        Level = new ReadingLevel(level),
    };

    private static DatedStatement Copied(int level) => new()
    {
        Key = "work-copied",
        Label = SmallStory.Text("A famous copy is made"),
        Time = new Occurrence(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 1330)),
        Sources = [new SourceId($"passage-{level}")],
        Review = ReviewStatus.Proposed,
        Level = new ReadingLevel(level),
    };

    private static SmallStory With(params DatedStatement[] dated)
    {
        var story = new SmallStory();
        var index = story.Entities.FindIndex(entity => entity.Id == Work);
        var work = story.Entities[index];
        story.Entities[index] = new Entity { Id = work.Id, Type = work.Type, Label = work.Label, Summary = work.Summary, Sources = work.Sources, Level = work.Level, Dated = [.. dated] };
        return story;
    }

    private static Projection At(SmallStory story, int level) =>
        Projection.Of(story.Build(), new ReadingLevel(level), Audience.LocalDemonstration)!;

    [Fact]
    public void The_timeline_names_the_entities_something_dated_is_stated_about_and_the_media()
    {
        var timeline = At(With(Written(), Copied(0)), 2).Timeline();

        var work = Assert.Single(timeline.Subjects);
        Assert.Equal(Work, work.Id);
        Assert.Equal(["work-written", "work-copied"], work.Dated.Select(dated => dated.Key));
        Assert.Equal(["diagram"], timeline.Made.Select(item => item.Id.Value));
        Assert.False(timeline.IsEmpty);
    }

    [Fact]
    public void A_statement_of_a_later_stage_does_not_exist_for_a_visitor_before_it()
    {
        var story = With(Written(), Copied(2));

        var early = At(story, 1);
        var late = At(story, 2);

        // Neither the timeline nor the entity itself carries the later statement, and its source is not named.
        Assert.Equal(["work-written"], early.Timeline().Subjects.Single().Dated.Select(dated => dated.Key));
        Assert.Equal(["work-written"], early.Find(Work)!.Dated.Select(dated => dated.Key));
        Assert.Equal(["work-written"], early.Entities.Single(entity => entity.Id == Work).Dated.Select(dated => dated.Key));
        Assert.DoesNotContain(early.Sources, source => source.Id.Value == "passage-2");
        Assert.Equal(["work-written", "work-copied"], late.Timeline().Subjects.Single().Dated.Select(dated => dated.Key));
    }

    [Fact]
    public void An_entity_whose_statements_are_all_later_is_not_a_subject_of_the_timeline_yet()
    {
        var timeline = At(With(Copied(2)), 0).Timeline();

        Assert.Empty(timeline.Subjects);
    }

    [Fact]
    public void A_collection_without_anything_dated_and_without_media_to_show_has_an_empty_timeline()
    {
        var story = new SmallStory();
        story.Media.Clear();

        Assert.True(At(story, 2).Timeline().IsEmpty);
    }

    [Fact]
    public void A_source_that_only_a_dated_statement_cites_is_one_of_the_sources_of_the_visitor()
    {
        var story = With(Written(source: "passage-dated"));
        story.Sources.Add(SmallStory.Source("passage-dated", 0));

        Assert.Contains(At(story, 0).Sources, source => source.Id.Value == "passage-dated");
    }

    [Fact]
    public void A_dated_statement_needs_a_source_a_stage_and_a_key_of_its_own()
    {
        var unsupported = With(new DatedStatement { Key = "bare", Label = SmallStory.Text("Bare"), Time = Copied(0).Time, Sources = [], Review = ReviewStatus.Proposed, Level = ReadingLevel.Opening });
        var missing = With(Written(source: "passage-nowhere"));
        var beyond = With(Written(level: 9));
        var twice = With(Written(), Written());
        var taken = With(Written(key: "hero"));

        Assert.Contains("bare: no source supports the record.", unsupported.Problems());
        Assert.Contains("work-written: the source passage-nowhere does not exist.", missing.Problems());
        Assert.Contains("work-written: the reading level 9 is not a stage of the collection.", beyond.Problems());
        Assert.Contains("work-written: the identifier names records of two kinds.", twice.Problems());
        Assert.Contains("hero: the identifier names records of two kinds.", taken.Problems());
    }

    [Fact]
    public void A_dated_statement_is_not_shown_before_its_entity_or_before_its_source()
    {
        var story = new SmallStory();
        var index = story.Entities.FindIndex(entity => entity.Id.Value == "child");
        var child = story.Entities[index];
        story.Entities[index] = new Entity { Id = child.Id, Type = child.Type, Label = child.Label, Summary = child.Summary, Sources = child.Sources, Level = child.Level, Dated = [Written(key: "child-born", level: 1, source: "passage-1")] };
        var early = With(Written(level: 0, source: "passage-2"));

        Assert.Contains("child-born: a statement about child cannot be shown before child itself.", story.Problems());
        Assert.Contains("work-written: its source passage-2 belongs to a later reading level and would give the story away.", early.Problems());
    }
}
