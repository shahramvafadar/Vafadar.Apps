using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

public sealed class CatalogueTests
{
    [Fact]
    public void A_consistent_collection_is_accepted_and_finds_its_records()
    {
        var catalogue = new SmallStory().Build();

        Assert.Equal(6, catalogue.Entities.Length);
        Assert.Equal(EntityTypes.Character, catalogue.Find(new EntityId("hero"))!.Type);
        Assert.Equal(new EntityId("child"), catalogue.Find(new RelationId("parent"))!.To);
        Assert.Equal(2, catalogue.Find(new EventId("lineage"))!.Associations.Length);
        Assert.NotNull(catalogue.Find(new SourceId("passage-1")));
        Assert.NotNull(catalogue.Find(new MediaId("diagram")));
    }

    [Fact]
    public void An_unknown_identifier_finds_nothing()
    {
        var catalogue = new SmallStory().Build();

        Assert.Null(catalogue.Find(new EntityId("nobody")));
        Assert.Null(catalogue.Find(new RelationId("hero")));
        Assert.Null(catalogue.Find(new EventId("hero")));
    }

    [Fact]
    public void The_records_keep_the_order_of_their_source()
    {
        var catalogue = new SmallStory().Build();

        Assert.Equal(["work", "story", "hero", "town", "keepsake", "child"], catalogue.Entities.Select(entity => entity.Id.Value));
        Assert.Equal(["arrival", "gift", "lineage"], catalogue.Events.Select(storyEvent => storyEvent.Id.Value));
    }

    [Fact]
    public void An_identifier_is_used_once()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("hero", EntityTypes.Character));

        Assert.Contains(story.Problems(), problem => problem.StartsWith("hero:", StringComparison.Ordinal));
    }

    [Fact]
    public void An_identifier_does_not_name_records_of_two_kinds()
    {
        var story = new SmallStory();
        story.Sources.Add(SmallStory.Source("hero", 0));

        Assert.Contains("hero: the identifier names records of two kinds.", story.Problems());
    }

    [Fact]
    public void Every_record_has_a_source_that_exists()
    {
        var story = new SmallStory();
        story.Entities[2] = SmallStory.Entity("hero", EntityTypes.Character, source: "lost-page");

        Assert.Contains("hero: the source lost-page does not exist.", story.Problems());
    }

    [Fact]
    public void A_record_without_a_source_is_refused()
    {
        var story = new SmallStory();
        var hero = SmallStory.Entity("hero", EntityTypes.Character);
        story.Entities[2] = new Entity { Id = hero.Id, Type = hero.Type, Label = hero.Label, Summary = hero.Summary, Sources = [], Level = hero.Level };

        Assert.Contains("hero: no source supports the record.", story.Problems());
    }

    [Fact]
    public void A_record_is_not_supported_by_a_source_of_a_later_level()
    {
        var story = new SmallStory();
        story.Entities[2] = SmallStory.Entity("hero", EntityTypes.Character, source: "passage-2");

        Assert.Contains(story.Problems(), problem => problem.StartsWith("hero: its source passage-2 belongs to a later reading level", StringComparison.Ordinal));
    }

    [Fact]
    public void A_relation_needs_both_of_its_entities()
    {
        var story = new SmallStory();
        story.Relations[0] = SmallStory.Relation("visits", "hero", "village", "visits");

        Assert.Contains("visits: the endpoint village does not exist.", story.Problems());
    }

    [Fact]
    public void A_relation_does_not_give_away_an_entity_of_a_later_level()
    {
        var story = new SmallStory();
        story.Relations[1] = SmallStory.Relation("parent", "hero", "child", "parentOf", level: 0);

        Assert.Contains(story.Problems(), problem => problem.StartsWith("parent: its endpoint child belongs to a later reading level", StringComparison.Ordinal));
    }

    [Fact]
    public void A_relation_does_not_give_away_an_event_of_a_later_level()
    {
        var story = new SmallStory();
        story.Relations[0] = SmallStory.Relation("visits", "hero", "town", "visits", events: ["lineage"]);

        Assert.Contains(story.Problems(), problem => problem.StartsWith("visits: its event of the scope lineage belongs to a later reading level", StringComparison.Ordinal));
    }

    [Fact]
    public void An_event_does_not_give_away_an_entity_of_a_later_level()
    {
        var story = new SmallStory();
        story.Events[0] = SmallStory.Event("arrival", 1, 0, "passage-0", ("hero", Presence.Acts), ("child", Presence.MentionedOnly));

        Assert.Contains(story.Problems(), problem => problem.StartsWith("arrival: its associated entity child belongs to a later reading level", StringComparison.Ordinal));
    }

    [Fact]
    public void An_event_says_what_at_least_one_entity_has_to_do_with_it()
    {
        var story = new SmallStory();
        story.Events[0] = SmallStory.Event("arrival", 1, 0, "passage-0");

        Assert.Contains("arrival: an event says what at least one entity has to do with it.", story.Problems());
    }

    [Fact]
    public void An_entity_is_associated_with_an_event_once()
    {
        var story = new SmallStory();
        story.Events[0] = SmallStory.Event("arrival", 1, 0, "passage-0", ("hero", Presence.Acts), ("hero", Presence.MentionedOnly));

        Assert.Contains("arrival: hero is associated twice.", story.Problems());
    }

    [Theory]
    [InlineData(Presence.Acts)]
    [InlineData(Presence.Involved)]
    [InlineData(Presence.MentionedOnly)]
    public void A_place_is_the_setting_of_an_event_and_nothing_else(Presence presence)
    {
        var story = new SmallStory();
        story.Events[0] = SmallStory.Event("arrival", 1, 0, "passage-0", ("hero", Presence.Acts), ("town", presence));

        Assert.Contains($"arrival: the place town is associated as {presence}, not as the setting.", story.Problems());
    }

    [Theory]
    [InlineData(Presence.Acts)]
    [InlineData(Presence.Setting)]
    public void An_object_is_an_object_in_an_event_and_nothing_else(Presence presence)
    {
        var story = new SmallStory();
        story.Events[1] = SmallStory.Event("gift", 2, 1, "passage-1", ("hero", Presence.Acts), ("keepsake", presence));

        Assert.Contains($"gift: the object keepsake is associated as {presence}, not as an object.", story.Problems());
    }

    [Fact]
    public void Someone_who_is_only_mentioned_stays_mentioned()
    {
        var lineage = new SmallStory().Build().Find(new EventId("lineage"))!;

        var hero = Assert.Single(lineage.Associations, association => association.Entity == new EntityId("hero"));

        Assert.Equal(Presence.MentionedOnly, hero.Presence);
    }

    [Fact]
    public void Two_events_do_not_share_a_place_in_the_story_order()
    {
        var story = new SmallStory();
        story.Events[1] = SmallStory.Event("gift", 1, 1, "passage-1", ("hero", Presence.Acts), ("keepsake", Presence.Object));

        Assert.Contains("arrival: gift has the same place in the story order.", story.Problems());
    }

    [Fact]
    public void The_time_of_a_story_event_is_a_place_in_the_story_and_no_date()
    {
        var gift = new SmallStory().Build().Find(new EventId("gift"))!;

        var position = Assert.IsType<NarrativePosition>(Assert.IsType<Occurrence>(gift.Time).At);

        Assert.Equal(2, position.Ordinal);
        Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(gift.Time, new Occurrence(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 1010))).Order);
    }

    [Fact]
    public void A_media_item_does_not_give_away_what_it_shows()
    {
        var story = new SmallStory();
        story.Media[0] = SmallStory.Diagram("diagram", level: 0);

        var problems = story.Problems();

        Assert.Contains(problems, problem => problem.StartsWith("diagram: its depicted entity child belongs to a later reading level", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.StartsWith("diagram: its explained relation parent belongs to a later reading level", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.StartsWith("diagram: its source passage-2 belongs to a later reading level", StringComparison.Ordinal));
    }

    [Fact]
    public void Only_original_content_is_shown_in_the_demonstration()
    {
        var story = new SmallStory();
        story.Media[0] = SmallStory.Diagram("diagram", rights: RightsStatus.OpenAccessVerified);

        Assert.Contains("diagram: only original project content is shown in the demonstration.", story.Problems());
    }

    [Fact]
    public void An_item_without_established_permission_has_no_file_and_is_not_shown()
    {
        var story = new SmallStory();
        story.Media[0] = SmallStory.Diagram("diagram", allowed: false, rights: RightsStatus.PermissionNotVerified);

        Assert.Contains("diagram: an item without established permission has no file and is not shown.", story.Problems());
    }

    [Fact]
    public void A_reference_without_permission_and_without_a_file_is_kept_as_a_reference()
    {
        var story = new SmallStory();
        story.Media.Add(SmallStory.Diagram("painting", allowed: false, rights: RightsStatus.PermissionNotVerified, file: null, altText: AltTextStatus.NotPrepared));

        Assert.False(story.Build().Find(new MediaId("painting"))!.DemoRenderAllowed);
    }

    [Fact]
    public void An_item_that_is_shown_needs_its_file()
    {
        var story = new SmallStory();
        story.Media[0] = SmallStory.Diagram("diagram", file: null);

        Assert.Contains("diagram: an item that is shown needs its file.", story.Problems());
    }

    [Fact]
    public void An_image_that_is_shown_needs_a_checked_text_alternative()
    {
        var story = new SmallStory();
        story.Media[0] = SmallStory.Diagram("diagram", altText: AltTextStatus.DraftPendingReview);

        Assert.Contains("diagram: an image that is shown needs a checked text alternative.", story.Problems());
    }

    [Fact]
    public void A_video_that_is_shown_needs_its_playing_time_captions_and_a_transcript()
    {
        var story = new SmallStory();
        story.Media[0] = SmallStory.Diagram("diagram", kind: MediaKind.Video);

        Assert.Contains("diagram: a video that is shown needs its playing time, captions and a transcript.", story.Problems());
    }

    [Fact]
    public void A_video_with_its_companions_is_accepted()
    {
        var story = new SmallStory();
        story.Media[0] = SmallStory.Diagram(
            "diagram",
            kind: MediaKind.Video,
            file: "media/diagram.mp4",
            captions: [new CaptionTrack("en", "media/diagram.vtt")],
            transcript: "media/diagram.txt",
            seconds: 19.9m);

        Assert.Equal(MediaKind.Video, story.Build().Find(new MediaId("diagram"))!.Kind);
    }

    [Fact]
    public void The_reading_stages_start_at_the_opening_without_gaps()
    {
        var story = new SmallStory { Stages = 0 };

        Assert.Contains("collection: the reading stages start at 0 and have no gaps.", story.Problems());
    }

    [Fact]
    public void A_record_belongs_to_a_stage_of_the_collection()
    {
        var story = new SmallStory { Stages = 2 };

        var problems = story.Problems();

        Assert.Contains("child: the reading level 2 is not a stage of the collection.", problems);
        Assert.Contains("lineage: the reading level 2 is not a stage of the collection.", problems);
    }

    [Fact]
    public void The_starting_boundary_is_a_stage_of_the_collection()
    {
        var story = new SmallStory { DefaultLevel = 7 };

        Assert.Contains("collection: the reading level 7 is not a stage of the collection.", story.Problems());
    }

    [Fact]
    public void The_story_of_the_collection_can_be_shown_at_the_opening()
    {
        var story = new SmallStory();
        story.Entities[1] = SmallStory.Entity("story", EntityTypes.StoryArc, level: 1, source: "passage-1");

        Assert.Contains(story.Problems(), problem => problem.StartsWith("collection: its story story belongs to a later reading level", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_problem_is_reported_not_only_the_first()
    {
        var story = new SmallStory();
        story.Relations[0] = SmallStory.Relation("visits", "hero", "village", "visits");
        story.Events[0] = SmallStory.Event("arrival", 1, 0, "passage-0");
        story.Media[0] = SmallStory.Diagram("diagram", file: null);

        var exception = Assert.Throws<CatalogueException>(() => story.Build());

        Assert.Equal(3, exception.Problems.Count);
        Assert.Contains("3 problems", exception.Message, StringComparison.Ordinal);
    }
}
