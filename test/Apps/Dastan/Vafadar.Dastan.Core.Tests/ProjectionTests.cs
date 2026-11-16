using Vafadar.Dastan.Testing;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// What a visitor may see of the small story: a hero comes to a town (level 0), a keepsake changes hands (level 1),
/// a child learns who the father is (level 2).
/// </summary>
public sealed class ProjectionTests
{
    private static readonly EntityId Hero = new("hero");
    private static readonly EntityId Town = new("town");
    private static readonly EntityId Keepsake = new("keepsake");
    private static readonly EntityId Child = new("child");

    private static Projection At(int level, SmallStory? story = null) =>
        Projection.Of((story ?? new SmallStory()).Build(), new ReadingLevel(level), Audience.LocalDemonstration)!;

    // --- Who the collection is shown to ---

    [Fact]
    public void A_collection_that_is_not_released_to_the_public_shows_the_public_nothing()
    {
        var catalogue = new SmallStory().Build();

        Assert.Null(Projection.Of(catalogue, new ReadingLevel(2), Audience.Public));
        Assert.NotNull(Projection.Of(catalogue, new ReadingLevel(2), Audience.LocalDemonstration));
    }

    [Fact]
    public void A_collection_that_may_not_be_demonstrated_is_not_demonstrated()
    {
        var story = new SmallStory { Publication = new PublicationState(LocalDemoAllowed: false, PublicReleaseApproved: false) };

        Assert.Null(Projection.Of(story.Build(), ReadingLevel.Opening, Audience.LocalDemonstration));
    }

    [Fact]
    public void The_public_sees_no_media_that_was_only_cleared_for_a_demonstration()
    {
        var story = new SmallStory { Publication = new PublicationState(LocalDemoAllowed: true, PublicReleaseApproved: true) };

        var projection = Projection.Of(story.Build(), new ReadingLevel(2), Audience.Public)!;

        Assert.Empty(projection.Media);
        Assert.NotEmpty(projection.Entities);
    }

    [Fact]
    public void An_item_that_may_not_be_shown_is_not_part_of_any_answer()
    {
        var story = new SmallStory();
        story.Media.Add(SmallStory.Diagram("painting", allowed: false, rights: RightsStatus.PermissionNotVerified, file: null, altText: AltTextStatus.NotPrepared, depicted: ["hero"]));
        var projection = At(2, story);

        Assert.Equal(["diagram"], projection.Media.Select(item => item.Id.Value));
        Assert.Null(projection.Find(new MediaId("painting")));
        Assert.Null(projection.MediaItem(new MediaId("painting")));
        Assert.Null(projection.Evidence(new MediaId("painting")));
        Assert.Equal(["diagram"], projection.Gallery(Hero)!.Items.Select(item => item.Id.Value));
    }

    // --- The reading boundary ---

    [Fact]
    public void At_the_opening_only_the_opening_is_there()
    {
        var projection = At(0);

        Assert.Equal(["work", "story", "hero", "town"], projection.Entities.Select(entity => entity.Id.Value));
        Assert.Equal(["visits"], projection.Relations.Select(relation => relation.Id.Value));
        Assert.Equal(["arrival"], projection.Steps.Select(step => step.Event.Id.Value));
        Assert.Equal(["passage-0"], projection.Sources.Select(source => source.Id.Value));
        Assert.Empty(projection.Media);
    }

    [Fact]
    public void Each_stage_adds_what_belongs_to_it()
    {
        Assert.Equal(["arrival", "gift"], At(1).Steps.Select(step => step.Event.Id.Value));
        Assert.Contains(At(1).Entities, entity => entity.Id == Keepsake);
        Assert.DoesNotContain(At(1).Entities, entity => entity.Id == Child);
        Assert.Equal(6, At(2).Entities.Length);
        Assert.Equal(["visits", "parent"], At(2).Relations.Select(relation => relation.Id.Value));
        Assert.Equal(["diagram"], At(2).Media.Select(item => item.Id.Value));
    }

    [Fact]
    public void A_boundary_beyond_the_last_stage_is_the_last_stage()
    {
        var projection = At(9);

        Assert.Equal(new ReadingLevel(2), projection.Boundary);
        Assert.False(projection.StoryContinues);
    }

    [Fact]
    public void A_visitor_learns_that_the_story_continues_and_nothing_about_how()
    {
        Assert.True(At(0).StoryContinues);
        Assert.True(At(1).StoryContinues);
        Assert.False(At(2).StoryContinues);
        Assert.True(At(0).Story().Continues);
    }

    [Fact]
    public void A_hidden_record_and_a_record_that_does_not_exist_give_the_same_answer()
    {
        var projection = At(0);
        var nobody = new EntityId("nobody");

        Assert.Null(projection.Find(Child));
        Assert.Null(projection.Find(nobody));
        Assert.Null(projection.Entity(Child));
        Assert.Null(projection.Entity(nobody));
        Assert.Null(projection.Connections(Child));
        Assert.Null(projection.Family(Child));
        Assert.Null(projection.Gallery(Child));
        Assert.Null(projection.Evidence(Child));
        Assert.Null(projection.Find(new RelationId("parent")));
        Assert.Null(projection.Evidence(new RelationId("parent")));
        Assert.Null(projection.Find(new EventId("lineage")));
        Assert.Null(projection.Gallery(new EventId("lineage")));
        Assert.Null(projection.Evidence(new EventId("lineage")));
        Assert.Null(projection.Evidence(new EventId("lineage"), Hero));
        Assert.Null(projection.Find(new MediaId("diagram")));
        Assert.Null(projection.MediaItem(new MediaId("diagram")));
        Assert.Null(projection.Evidence(new MediaId("diagram")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void No_answer_discloses_anything_of_a_later_stage(int level)
    {
        var catalogue = new SmallStory().Build();
        var projection = Projection.Of(catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

        Assert.Empty(Disclosure.Leaks(projection, catalogue));
    }

    [Fact]
    public void The_check_for_disclosure_knows_what_is_secret_and_sees_what_is_shown()
    {
        var catalogue = new SmallStory().Build();
        var opening = Projection.Of(catalogue, ReadingLevel.Opening, Audience.LocalDemonstration)!;

        var secrets = Disclosure.Secrets(opening, catalogue);
        var shown = Disclosure.Of(opening, catalogue);

        Assert.Contains("child", secrets);
        Assert.Contains("About child.", secrets);
        Assert.Contains("lineage", secrets);
        Assert.Contains("media/diagram.png", secrets);
        Assert.Contains("passage-2", secrets);
        Assert.DoesNotContain("hero", secrets);
        Assert.Contains("hero", shown);
        Assert.Contains("About hero.", shown);
        Assert.Contains("https://example.org/passage-0", shown);
        Assert.Empty(Disclosure.Secrets(Projection.Of(catalogue, new ReadingLevel(2), Audience.LocalDemonstration)!, catalogue));
    }

    // --- The story ---

    [Fact]
    public void The_steps_are_numbered_as_the_visitor_sees_them()
    {
        var story = new SmallStory();
        story.Events[1] = SmallStory.Event("gift", 2, 2, "passage-2", ("hero", Presence.Acts), ("keepsake", Presence.Object));
        story.Events[2] = SmallStory.Event("lineage", 3, 1, "passage-1", ("hero", Presence.MentionedOnly));

        var steps = At(1, story).Steps;

        // The event between the two is hidden; the numbers do not show a gap.
        Assert.Equal(["arrival", "lineage"], steps.Select(step => step.Event.Id.Value));
        Assert.Equal([1, 2], steps.Select(step => step.Number));
    }

    [Fact]
    public void The_story_is_told_in_the_order_of_the_telling_whatever_the_order_of_the_records()
    {
        var story = new SmallStory();
        story.Events.Reverse();

        Assert.Equal(["arrival", "gift", "lineage"], At(2, story).Story().Steps.Select(step => step.Event.Id.Value));
    }

    [Fact]
    public void The_lanes_have_the_steps_of_the_story_and_keep_how_somebody_takes_part()
    {
        var projection = At(2);
        var lanes = projection.Lanes();

        Assert.Equal(projection.Story().Steps, lanes.Steps);
        Assert.Equal(["hero", "town", "keepsake", "child"], lanes.Lanes.Select(lane => lane.Entity.Id.Value));

        var hero = lanes.Lanes[0];
        Assert.Equal([Presence.Acts, Presence.Acts, Presence.MentionedOnly], hero.Cells.Select(cell => cell!.Presence));

        var child = lanes.Lanes[3];
        Assert.Equal([null, null, Presence.Acts], child.Cells.Select(cell => cell?.Presence));
    }

    [Fact]
    public void A_cell_of_a_lane_is_the_association_of_the_event()
    {
        var lanes = At(2).Lanes();

        foreach (var lane in lanes.Lanes)
        {
            for (var index = 0; index < lanes.Steps.Length; index++)
            {
                var stated = lanes.Steps[index].Event.Associations.SingleOrDefault(association => association.Entity == lane.Entity.Id);
                Assert.Same(stated, lane.Cells[index]);
            }
        }
    }

    // --- The entry ---

    [Fact]
    public void The_entry_names_the_story_its_work_the_stages_and_whom_to_start_with()
    {
        var overview = At(2).Overview();

        Assert.Equal("story", overview.Story.Id.Value);
        Assert.Equal("work", overview.Work.Id.Value);
        Assert.Equal(3, overview.Stages.Length);
        Assert.Equal(new ReadingLevel(2), overview.Boundary);
        Assert.Equal(["hero", "child"], overview.Subjects.Select(entity => entity.Id.Value));
        Assert.Equal("arrival", overview.FirstStep!.Event.Id.Value);
        Assert.Equal(["hero"], At(0).Overview().Subjects.Select(entity => entity.Id.Value));
    }

    // --- One entity ---

    [Fact]
    public void An_entity_shows_its_relations_from_its_own_side()
    {
        var hero = At(2).Entity(Hero)!;
        var child = At(2).Entity(Child)!;

        Assert.Equal(
            [("visits", "town", ConnectionDirection.Outgoing), ("parent", "child", ConnectionDirection.Outgoing)],
            hero.Connections.Select(connection => (connection.Relation.Id.Value, connection.Other.Id.Value, connection.Direction)));
        Assert.Equal(
            [("parent", "hero", ConnectionDirection.Incoming)],
            child.Connections.Select(connection => (connection.Relation.Id.Value, connection.Other.Id.Value, connection.Direction)));
    }

    [Fact]
    public void A_relation_that_is_the_same_from_both_sides_has_no_direction()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("rivals", "hero", "child", "opponentOf", level: 2, source: "passage-2"));

        var hero = At(2, story).Entity(Hero)!.Connections.Single(connection => connection.Relation.Id.Value == "rivals");
        var child = At(2, story).Entity(Child)!.Connections.Single(connection => connection.Relation.Id.Value == "rivals");

        Assert.Equal(ConnectionDirection.Mutual, hero.Direction);
        Assert.Equal(ConnectionDirection.Mutual, child.Direction);
    }

    [Fact]
    public void An_entity_shows_the_events_it_has_to_do_with_and_how()
    {
        var appearances = At(2).Entity(Hero)!.Appearances;

        Assert.Equal(["arrival", "gift", "lineage"], appearances.Select(appearance => appearance.Step.Event.Id.Value));
        Assert.Equal(Presence.MentionedOnly, appearances[2].Association.Presence);
        Assert.Equal(["arrival"], At(0).Entity(Hero)!.Appearances.Select(appearance => appearance.Step.Event.Id.Value));
    }

    [Fact]
    public void An_entity_shows_the_media_that_depict_it_once_they_may_be_shown()
    {
        Assert.Equal(["diagram"], At(2).Entity(Hero)!.Media.Select(item => item.Id.Value));
        Assert.Empty(At(1).Entity(Hero)!.Media);
        Assert.Empty(At(2).Entity(Town)!.Media);
    }

    // --- Connections ---

    [Fact]
    public void The_neighbourhood_of_an_entity_is_its_relations_and_who_they_lead_to()
    {
        var view = At(2).Connections(Hero)!;

        Assert.Equal("hero", view.Focus.Id.Value);
        Assert.Equal(["hero", "town", "child"], view.Nodes.Select(node => node.Id.Value));
        Assert.Equal(["visits", "parent"], view.Edges.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(("hero", "child"), (view.Edges[1].From.Id.Value, view.Edges[1].To.Id.Value));
        Assert.Empty(view.Expandable);
    }

    [Fact]
    public void At_an_earlier_stage_the_neighbourhood_is_smaller_and_does_not_say_so()
    {
        var view = At(0).Connections(Hero)!;

        Assert.Equal(["hero", "town"], view.Nodes.Select(node => node.Id.Value));
        Assert.Equal(["visits"], view.Edges.Select(edge => edge.Relation.Id.Value));
        Assert.Empty(view.Expandable);
    }

    [Fact]
    public void A_node_with_more_relations_can_be_expanded_and_expanding_adds_them()
    {
        var closed = At(2).Connections(Town)!;
        var open = At(2).Connections(Town, [Hero])!;

        Assert.Equal(["visits"], closed.Edges.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(["hero"], closed.Expandable.Select(node => node.Id.Value));
        Assert.Equal(["visits", "parent"], open.Edges.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(["town", "hero", "child"], open.Nodes.Select(node => node.Id.Value));
        Assert.Empty(open.Expandable);
    }

    [Fact]
    public void Expanding_reaches_only_what_is_connected_and_visible()
    {
        var projection = At(1);

        var view = projection.Connections(Town, [Keepsake, Child, new EntityId("nobody"), Hero])!;

        // The keepsake has no relation, the child is hidden, nobody does not exist; only the hero is opened.
        Assert.Equal(["town", "hero"], view.Nodes.Select(node => node.Id.Value));
        Assert.Equal(["visits"], view.Edges.Select(edge => edge.Relation.Id.Value));
    }

    [Fact]
    public void A_chain_of_expansions_is_followed_whatever_its_order()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("friend", EntityTypes.Character));
        story.Relations.Add(SmallStory.Relation("supports", "friend", "child", "supportsExpeditionOf", level: 2, source: "passage-2"));

        var view = At(2, story).Connections(Town, [new EntityId("child"), Hero])!;

        Assert.Equal(["visits", "parent", "supports"], view.Edges.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(["town", "hero", "child", "friend"], view.Nodes.Select(node => node.Id.Value));
        Assert.Empty(view.Expandable);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void The_graph_of_an_entity_and_its_list_of_relations_show_the_same_relations(int level)
    {
        var projection = At(level);

        foreach (var entity in projection.Entities)
        {
            var graph = projection.Connections(entity.Id)!.Edges.Select(edge => edge.Relation.Id);
            var list = projection.Entity(entity.Id)!.Connections.Select(connection => connection.Relation.Id);
            Assert.Equal(list, graph);
        }
    }

    // --- Family ---

    [Fact]
    public void A_family_is_drawn_from_the_relations_that_state_parentage_or_partnership()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("mother", EntityTypes.Character, level: 1, source: "passage-1"));
        story.Entities.Add(SmallStory.Entity("elder", EntityTypes.Character));
        story.Relations.Add(SmallStory.Relation("married", "hero", "mother", "spouseOf", level: 1, source: "passage-1"));
        story.Relations.Add(SmallStory.Relation("mother-of", "mother", "child", "parentOf", level: 2, source: "passage-2"));
        story.Relations.Add(SmallStory.Relation("elder-of", "elder", "hero", "parentOf"));

        var family = At(2, story).Family();

        Assert.Equal(
            [("elder", 0), ("hero", 1), ("mother", 1), ("child", 2)],
            family.Members.Select(member => (member.Entity.Id.Value, member.Generation)));
        Assert.Equal(["parent", "mother-of", "elder-of"], family.Parentage.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(["married"], family.Partnerships.Select(edge => edge.Relation.Id.Value));
        Assert.DoesNotContain(family.Members, member => member.Entity.Id == Town);
    }

    [Fact]
    public void A_family_at_an_earlier_stage_has_only_the_members_known_by_then()
    {
        Assert.Empty(At(1).Family().Members);
        Assert.Equal(["hero", "child"], At(2).Family().Members.Select(member => member.Entity.Id.Value));
    }

    [Fact]
    public void The_family_of_an_entity_is_who_is_connected_to_it_by_family_relations()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("stranger", EntityTypes.Character));
        story.Entities.Add(SmallStory.Entity("heir", EntityTypes.Character));
        story.Relations.Add(SmallStory.Relation("other-line", "stranger", "heir", "parentOf"));
        var projection = At(2, story);

        Assert.Equal(["hero", "child"], projection.Family(Child)!.Members.Select(member => member.Entity.Id.Value));
        Assert.Equal(["stranger", "heir"], projection.Family(new EntityId("heir"))!.Members.Select(member => member.Entity.Id.Value));
        Assert.Empty(projection.Family(Town)!.Members);
        Assert.Equal(4, projection.Family().Members.Length);
    }

    [Fact]
    public void Relations_that_contradict_each_other_do_not_stop_the_family_view()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("impossible", "child", "hero", "parentOf", level: 2, source: "passage-2"));

        Assert.Equal(2, At(2, story).Family().Members.Length);
    }

    // --- Gallery and media ---

    [Fact]
    public void A_gallery_says_why_its_items_are_in_it()
    {
        var projection = At(2);

        var all = projection.Gallery();
        var ofHero = projection.Gallery(Hero)!;
        var ofTown = projection.Gallery(Town)!;
        var ofArrival = projection.Gallery(new EventId("arrival"))!;

        Assert.Equal((GalleryRule.WholeCollection, GalleryOrder.Editorial), (all.Rule, all.Order));
        Assert.Equal(["diagram"], all.Items.Select(item => item.Id.Value));
        Assert.Equal((GalleryRule.DepictsEntity, "hero"), (ofHero.Rule, ofHero.Entity!.Id.Value));
        Assert.Single(ofHero.Items);
        Assert.Empty(ofTown.Items);
        Assert.Equal((GalleryRule.DepictsEvent, "arrival"), (ofArrival.Rule, ofArrival.Step!.Event.Id.Value));
        Assert.Empty(ofArrival.Items);
    }

    [Fact]
    public void A_gallery_is_empty_and_not_missing_when_nothing_may_be_shown_yet()
    {
        Assert.Empty(At(0).Gallery().Items);
        Assert.Empty(At(1).Gallery(Hero)!.Items);
    }

    [Fact]
    public void A_media_item_leads_to_what_it_shows()
    {
        var view = At(2).MediaItem(new MediaId("diagram"))!;

        Assert.Equal(["hero", "child"], view.Depicted.Select(entity => entity.Id.Value));
        Assert.Equal(["parent"], view.Relations.Select(edge => edge.Relation.Id.Value));
        Assert.Empty(view.Steps);
    }

    // --- Evidence ---

    [Fact]
    public void The_evidence_of_a_relation_is_the_evidence_of_that_relation()
    {
        var evidence = At(2).Evidence(new RelationId("parent"))!;

        Assert.Equal(EvidenceSubject.Relation, evidence.Subject);
        Assert.Equal(["passage-2"], evidence.Sources.Select(source => source.Id.Value));
        Assert.Equal(("hero", "child"), (evidence.Relation!.From.Id.Value, evidence.Relation.To.Id.Value));
        Assert.Equal(ReviewStatus.SourcedReviewPending, evidence.Review);
        Assert.Equal("edition", evidence.Edition.Id);
    }

    [Fact]
    public void The_evidence_of_taking_part_keeps_how_somebody_takes_part()
    {
        var evidence = At(2).Evidence(new EventId("lineage"), Hero)!;

        Assert.Equal(EvidenceSubject.Association, evidence.Subject);
        Assert.Equal(Presence.MentionedOnly, evidence.Association!.Presence);
        Assert.Equal("hero", evidence.Entity!.Id.Value);
        Assert.Equal("lineage", evidence.Step!.Event.Id.Value);
        Assert.Equal(["passage-2"], evidence.Sources.Select(source => source.Id.Value));
        Assert.Null(At(2).Evidence(new EventId("lineage"), Town));
    }

    [Fact]
    public void Every_kind_of_statement_has_its_evidence()
    {
        var projection = At(2);

        Assert.Equal(EvidenceSubject.Entity, projection.Evidence(Hero)!.Subject);
        Assert.Equal(EvidenceSubject.Event, projection.Evidence(new EventId("gift"))!.Subject);
        Assert.Equal(["passage-1"], projection.Evidence(new EventId("gift"))!.Sources.Select(source => source.Id.Value));
        Assert.Equal(EvidenceSubject.Media, projection.Evidence(new MediaId("diagram"))!.Subject);
        Assert.Equal("diagram", projection.Evidence(new MediaId("diagram"))!.Media!.Id.Value);
    }

    [Fact]
    public void A_source_outside_the_app_may_tell_more_than_the_visitor_has_read()
    {
        Assert.True(At(0).Evidence(Hero)!.SourcesMayRevealMore);
        Assert.False(At(2).Evidence(Hero)!.SourcesMayRevealMore);
    }

    // --- Search ---

    [Theory]
    [InlineData("hero")]
    [InlineData("HERO")]
    [InlineData("  Héro ")]
    [InlineData("her")]
    public void An_entity_is_found_by_its_name_whatever_the_case_and_the_accents(string text)
    {
        var hit = Assert.Single(At(2).Search(text));

        Assert.Equal("hero", hit.Entity.Id.Value);
        Assert.Equal(("hero", "en"), (hit.Name, hit.Language));
    }

    [Fact]
    public void An_entity_is_found_by_its_name_in_another_language_and_spelling()
    {
        var story = new SmallStory();
        var hero = SmallStory.Entity("hero", EntityTypes.Character);
        story.Entities[2] = new Entity
        {
            Id = hero.Id,
            Type = hero.Type,
            Label = hero.Label.With("fa", "پهلوانِ کي‌کاووس ۲"),
            Summary = hero.Summary,
            Sources = hero.Sources,
            Level = hero.Level,
        };

        // Arabic yeh instead of Persian yeh, no vowel mark, a space instead of the joiner, a Latin digit.
        var hit = Assert.Single(At(0, story).Search("پهلوان کی کاووس 2"));

        Assert.Equal(("hero", "fa"), (hit.Entity.Id.Value, hit.Language));
    }

    [Fact]
    public void A_name_that_starts_with_the_text_comes_before_one_that_contains_it()
    {
        var story = new SmallStory();
        story.Entities.Insert(2, SmallStory.Entity("old town", EntityTypes.NarrativePlace));
        story.Entities.Insert(3, SmallStory.Entity("downtown", EntityTypes.NarrativePlace));

        var hits = At(0, story).Search("town");

        Assert.Equal(["town", "old town", "downtown"], hits.Select(hit => hit.Entity.Id.Value));
    }

    [Fact]
    public void Search_does_not_find_what_the_visitor_has_not_read_yet()
    {
        Assert.Empty(At(0).Search("child"));
        Assert.Empty(At(1).Search("chi"));
        Assert.Single(At(2).Search("child"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("zzz")]
    public void Nothing_to_look_for_finds_nothing(string? text)
    {
        Assert.Empty(At(2).Search(text));
    }

    [Fact]
    public void Search_returns_no_more_hits_than_asked_for()
    {
        Assert.Equal(2, At(2).Search("o", limit: 2).Length);
        Assert.Empty(At(2).Search("o", limit: 0));
    }
}
