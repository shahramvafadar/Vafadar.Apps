using Vafadar.Dastan.Core;
using Vafadar.Dastan.Testing;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The pilot sample with its Persian text, seen by a visitor at each of its five reading stages. No web host is
/// involved: these are the answers the pages of the later phases show.
/// </summary>
public sealed class SampleProjectionTests
{
    private static readonly Lazy<Catalogue> Loaded = new(() => PilotSample.Load(SampleFiles.PilotPath));

    private static readonly EntityId Rostam = new("character-rostam");
    private static readonly EntityId Zal = new("character-zal");
    private static readonly EntityId Tahmineh = new("character-tahmineh");
    private static readonly EntityId Sohrab = new("character-sohrab");

    private static Catalogue Catalogue => Loaded.Value;

    private static Projection At(int level) => Projection.Of(Catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

    // --- Nothing of a later stage ---

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void No_answer_discloses_anything_of_a_later_stage_or_of_an_item_that_may_not_be_shown(int level)
    {
        Assert.Empty(Disclosure.Leaks(At(level), Catalogue));
    }

    [Fact]
    public void The_check_for_disclosure_covers_the_ending_and_the_museum_items()
    {
        var secrets = Disclosure.Secrets(At(0), Catalogue);

        Assert.Contains("character-sohrab", secrets);
        Assert.Contains("Sohrab", secrets);
        Assert.Contains("سهراب", secrets);
        Assert.Contains("event-08", secrets);
        Assert.Contains("Mourning in Zabulistan", secrets);
        Assert.Contains("relation-04", secrets);
        Assert.Contains("media/dastan-family-connections.png", secrets);
        // A museum work of a later stage is a secret as a whole; of one the visitor may know as a reference, the
        // address of the image is.
        Assert.Contains("media-met-453265", secrets);
        Assert.Contains("https://www.metmuseum.org/art/collection/search/453265", secrets);
        Assert.DoesNotContain("media-met-452419", secrets);
        Assert.Contains("https://collectionapi.metmuseum.org/api/collection/v1/iiif/452419/904434/main-image", secrets);
        Assert.Contains("src-ganjoor-s21", secrets);

        // At the last stage only what a reference leaves out of an item that may not be shown stays secret.
        var last = Disclosure.Secrets(At(4), Catalogue);
        Assert.Contains("https://collectionapi.metmuseum.org/api/collection/v1/iiif/453265/904428/main-image", last);
        Assert.DoesNotContain("media-umma-1963-1-46", last);
        Assert.DoesNotContain("character-sohrab", last);
    }

    [Fact]
    public void The_sample_is_not_shown_to_the_public()
    {
        Assert.Null(Projection.Of(Catalogue, ReadingLevel.Opening, Audience.Public));
    }

    // --- What each stage holds ---

    [Theory]
    [InlineData(0, 8, 6, 1, 0)]
    [InlineData(1, 10, 11, 2, 0)]
    [InlineData(2, 14, 15, 4, 2)]
    [InlineData(3, 14, 16, 5, 2)]
    [InlineData(4, 14, 20, 8, 2)]
    public void Each_stage_holds_what_the_editors_assigned_to_it(int level, int entities, int relations, int steps, int media)
    {
        var projection = At(level);

        Assert.Equal(entities, projection.Entities.Length);
        Assert.Equal(relations, projection.Relations.Length);
        Assert.Equal(steps, projection.Steps.Length);
        Assert.Equal(media, projection.Media.Length);
        Assert.Equal(Enumerable.Range(1, steps), projection.Steps.Select(step => step.Number));
    }

    [Fact]
    public void A_visitor_at_the_opening_meets_Rostam_and_not_yet_Sohrab()
    {
        var opening = At(0);

        Assert.NotNull(opening.Entity(Rostam));
        Assert.Null(opening.Entity(Tahmineh));
        Assert.Null(opening.Entity(Sohrab));

        // The title of the story names Sohrab from the start – the editors put the arc at the opening. The
        // character himself is not found.
        Assert.Equal(["story-rostam-sohrab"], opening.Search("Sohrab").Select(hit => hit.Entity.Id.Value));
        Assert.Equal(["story-rostam-sohrab"], opening.Search("سهراب").Select(hit => hit.Entity.Id.Value));
        Assert.Empty(opening.Search("Tahmineh"));
        Assert.Equal("A missing horse and a welcome", opening.Story().Steps.Single().Event.Label.Resolve("en").Value);
        Assert.True(opening.Story().Continues);
    }

    [Fact]
    public void The_entry_offers_the_beings_of_the_opening_in_the_order_they_appear()
    {
        var overview = At(0).Overview();

        Assert.Equal("Rostam and Sohrab", overview.Story.Label.Resolve("en").Value);
        Assert.Equal("شاهنامه", overview.Work.Label.Resolve("fa").Value);
        Assert.Equal(5, overview.Stages.Length);
        Assert.Equal(
            ["character-rostam", "character-rakhsh", "character-king-samangan", "character-zal"],
            overview.Subjects.Select(entity => entity.Id.Value));
        Assert.Equal("event-01", overview.FirstStep!.Event.Id.Value);
    }

    // --- Connections ---

    [Fact]
    public void Rostam_has_more_connections_with_every_stage()
    {
        string[] Others(int level) => [.. At(level).Entity(Rostam)!.Connections.Select(connection => connection.Other.Id.Value)];

        Assert.Equal(["character-zal", "character-rakhsh", "place-samangan", "place-turan"], Others(0));
        Assert.Contains("character-tahmineh", Others(1));
        Assert.Contains("object-token", Others(1));
        Assert.DoesNotContain("character-sohrab", Others(1));
        Assert.Contains("character-sohrab", Others(2));
        Assert.Contains("place-zabul", Others(4));
    }

    [Fact]
    public void A_parent_reads_towards_the_child_and_a_marriage_reads_both_ways()
    {
        var rostam = At(2).Entity(Rostam)!.Connections;

        var father = rostam.Single(connection => connection.Other.Id == Zal);
        var son = rostam.Single(connection => connection.Other.Id == Sohrab);
        var wife = rostam.Single(connection => connection.Other.Id == Tahmineh);

        Assert.Equal(ConnectionDirection.Incoming, father.Direction);
        Assert.Equal(ConnectionDirection.Outgoing, son.Direction);
        Assert.Equal(ConnectionDirection.Mutual, wife.Direction);
        Assert.Equal("ازدواج در سمنگان", wife.Relation.Label.Resolve("fa").Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void The_graph_of_every_entity_and_its_list_of_relations_show_the_same_relations(int level)
    {
        var projection = At(level);

        foreach (var entity in projection.Entities)
        {
            var view = projection.Connections(entity.Id)!;
            Assert.Equal(projection.Entity(entity.Id)!.Connections.Select(connection => connection.Relation.Id), view.Edges.Select(edge => edge.Relation.Id));
            Assert.Equal(projection.Relations.Where(relation => relation.From == entity.Id || relation.To == entity.Id), view.Edges.Select(edge => edge.Relation));
            Assert.Equal(view.Nodes.Length, view.Nodes.Distinct().Count());
            Assert.All(view.Edges, edge => Assert.Contains(edge.From, view.Nodes));
            Assert.All(view.Edges, edge => Assert.Contains(edge.To, view.Nodes));
        }
    }

    [Fact]
    public void Expanding_every_node_shows_every_connected_relation_and_nothing_else()
    {
        var projection = At(4);

        var view = projection.Connections(Rostam, projection.Entities.Select(entity => entity.Id))!;

        // The relation between the story and its work is not connected to any character.
        Assert.Equal(projection.Relations.Where(relation => relation.Id.Value != "relation-01"), view.Edges.Select(edge => edge.Relation));
        Assert.Empty(view.Expandable);
    }

    // --- Family ---

    [Fact]
    public void The_family_grows_with_the_reading_stage()
    {
        Assert.Equal(["character-zal", "character-rostam"], At(0).Family().Members.Select(member => member.Entity.Id.Value));
        Assert.Equal(
            [("character-zal", 0), ("character-king-samangan", 0), ("character-rostam", 1), ("character-tahmineh", 1)],
            At(1).Family().Members.Select(member => (member.Entity.Id.Value, member.Generation)));
        Assert.Equal(
            [("character-zal", 0), ("character-king-samangan", 0), ("character-rostam", 1), ("character-tahmineh", 1), ("character-sohrab", 2)],
            At(2).Family().Members.Select(member => (member.Entity.Id.Value, member.Generation)));
    }

    [Fact]
    public void The_family_view_of_the_lineage_stage_is_what_the_original_diagram_draws()
    {
        var family = At(2).Family(Sohrab)!;
        var diagram = At(2).MediaItem(new MediaId("media-dastan-family-illustration"))!;

        Assert.Equal(["relation-02", "relation-04", "relation-05", "relation-06"], family.Parentage.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(["relation-03"], family.Partnerships.Select(edge => edge.Relation.Id.Value));
        Assert.All(diagram.Relations, edge => Assert.Contains(edge, family.Parentage));
        Assert.All(diagram.Depicted, entity => Assert.Contains(family.Members, member => member.Entity == entity));
    }

    // --- Story and lanes ---

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void The_story_and_the_lanes_agree_on_the_order_and_on_how_somebody_takes_part(int level)
    {
        var projection = At(level);
        var story = projection.Story();
        var lanes = projection.Lanes();

        Assert.Equal(story.Steps, lanes.Steps);
        Assert.Equal(level < 4, story.Continues);
        foreach (var lane in lanes.Lanes)
        {
            Assert.Equal(story.Steps.Length, lane.Cells.Length);
            for (var index = 0; index < story.Steps.Length; index++)
            {
                Assert.Same(
                    story.Steps[index].Event.Associations.SingleOrDefault(association => association.Entity == lane.Entity.Id),
                    lane.Cells[index]);
            }
        }

        // Everybody who has to do with a visible event has a lane, and nobody else.
        Assert.Equal(
            story.Steps.SelectMany(step => step.Event.Associations).Select(association => association.Entity.Value).Distinct().Order(),
            lanes.Lanes.Select(lane => lane.Entity.Id.Value).Order());
    }

    [Fact]
    public void In_the_lanes_Rostam_is_named_and_not_present_when_Sohrab_learns_his_lineage()
    {
        var lanes = At(2).Lanes();
        var rostam = lanes.Lanes.Single(lane => lane.Entity.Id == Rostam);
        var lineage = lanes.Steps.Single(step => step.Event.Id.Value == "event-03");

        Assert.Equal(Presence.MentionedOnly, rostam.Cells[lineage.Number - 1]!.Presence);
        Assert.Equal(Presence.Acts, rostam.Cells[0]!.Presence);
    }

    [Fact]
    public void In_the_lanes_a_place_is_a_setting_and_the_token_an_object()
    {
        foreach (var lane in At(4).Lanes().Lanes)
        {
            var presences = lane.Cells.OfType<EventAssociation>().Select(cell => cell.Presence).Distinct().ToList();
            switch (lane.Entity.Type.Nature)
            {
                case EntityNature.Place:
                    Assert.Equal([Presence.Setting], presences);
                    break;
                case EntityNature.Object:
                    Assert.Equal([Presence.Object], presences);
                    break;
                default:
                    Assert.DoesNotContain(Presence.Setting, presences);
                    Assert.DoesNotContain(Presence.Object, presences);
                    break;
            }
        }
    }

    [Fact]
    public void At_the_end_Sohrab_is_reported_and_mourned_and_never_shown_as_acting()
    {
        var sohrab = At(4).Entity(Sohrab)!.Appearances;

        Assert.Equal(
            [Presence.Acts, Presence.Acts, Presence.Acts, Presence.Acts, Presence.SubjectOfReport, Presence.DeceasedSubject],
            sohrab.Select(appearance => appearance.Association.Presence));
    }

    // --- Gallery and media ---

    [Fact]
    public void Before_the_lineage_stage_the_gallery_is_empty_and_from_it_on_both_original_items_are_there()
    {
        Assert.Empty(At(0).Gallery().Items);
        Assert.Empty(At(1).Gallery().Items);
        Assert.Empty(At(1).Gallery(Rostam)!.Items);
        Assert.Equal(
            ["media-dastan-parentage-video", "media-dastan-family-illustration"],
            At(2).Gallery().Items.Select(item => item.Id.Value));
        Assert.Equal(2, At(2).Gallery(Rostam)!.Items.Length);
        Assert.Equal(2, At(2).Entity(Zal)!.Media.Length);
        Assert.Empty(At(2).Gallery(new EntityId("character-rakhsh"))!.Items);
    }

    [Fact]
    public void A_museum_painting_is_not_shown_at_any_stage()
    {
        var last = At(4);

        Assert.Null(last.Find(new MediaId("media-met-452419")));
        Assert.Null(last.MediaItem(new MediaId("media-met-453265")));
        Assert.Null(last.Evidence(new MediaId("media-umma-1963-1-46")));
        Assert.Empty(last.Gallery(new EventId("event-06"))!.Items);
        Assert.DoesNotContain(last.Sources, source => source.Kind is SourceKind.MuseumRecord or SourceKind.RightsPolicy);
    }

    [Fact]
    public void The_video_and_the_diagram_lead_to_the_four_family_members()
    {
        var video = At(2).MediaItem(new MediaId("media-dastan-parentage-video"))!;

        Assert.Equal(MediaKind.Video, video.Item.Kind);
        Assert.Equal(
            ["character-zal", "character-rostam", "character-tahmineh", "character-sohrab"],
            video.Depicted.Select(entity => entity.Id.Value));
        Assert.Equal("خانواده‌ای به‌هم‌پیوسته در شاهنامه", video.Item.Label.Resolve("fa").Value);
        Assert.Equal(MediaRepresentation.PresentDayExplanatoryDiagram, video.Item.Representation);
    }

    // --- Evidence ---

    [Fact]
    public void The_evidence_of_Sohrabs_parentage_is_the_passage_that_tells_it()
    {
        var evidence = At(2).Evidence(new RelationId("relation-04"))!;

        Assert.Equal(["src-ganjoor-s05"], evidence.Sources.Select(source => source.Id.Value));
        Assert.Equal((Rostam, Sohrab), (evidence.Relation!.From.Id, evidence.Relation.To.Id));
        Assert.Equal(RelationScopeKind.RevealedInEvent, evidence.Relation.Relation.Scope.Kind);
        Assert.NotNull(evidence.PassageLocator);
        Assert.Equal(ReviewStatus.SourcedReviewPending, evidence.Review);
        Assert.Equal("edition-ganjoor-sohrab-observed-2026-11-15", evidence.Edition.Id);
        Assert.True(evidence.SourcesMayRevealMore);
        Assert.Null(At(1).Evidence(new RelationId("relation-04")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Every_visible_statement_has_evidence_and_only_its_own(int level)
    {
        var projection = At(level);

        foreach (var entity in projection.Entities)
        {
            Assert.Equal(entity.Sources, projection.Evidence(entity.Id)!.Sources.Select(source => source.Id));
        }

        foreach (var relation in projection.Relations)
        {
            Assert.Equal(relation.Sources, projection.Evidence(relation.Id)!.Sources.Select(source => source.Id));
        }

        foreach (var step in projection.Steps)
        {
            Assert.Equal(step.Event.Sources, projection.Evidence(step.Event.Id)!.Sources.Select(source => source.Id));
            foreach (var association in step.Event.Associations)
            {
                Assert.Same(association, projection.Evidence(step.Event.Id, association.Entity)!.Association);
            }
        }

        foreach (var item in projection.Media)
        {
            Assert.Equal(item.Sources, projection.Evidence(item.Id)!.Sources.Select(source => source.Id));
        }
    }

    // --- Search ---

    [Theory]
    [InlineData("rostam", "character-rostam")]
    [InlineData("ROST", "character-rostam")]
    [InlineData("رستم", "character-rostam")]
    [InlineData("کی کاووس", "character-kay-kavus")]
    [InlineData("كي كاووس", "character-kay-kavus")]
    [InlineData("kavus", "character-kay-kavus")]
    [InlineData("zabul", "place-zabul")]
    [InlineData("زابلستان", "place-zabul")]
    public void A_name_is_found_in_English_and_in_Persian(string text, string expected)
    {
        Assert.Contains(At(4).Search(text), hit => hit.Entity.Id.Value == expected);
    }

    [Fact]
    public void A_search_for_Rostam_finds_him_before_the_story_and_the_keepsake_named_after_him()
    {
        var hits = At(4).Search("Rostam");

        Assert.Equal(
            ["character-rostam", "story-rostam-sohrab", "object-token"],
            hits.Select(hit => hit.Entity.Id.Value));
    }

    // --- Lowering the boundary ---

    [Fact]
    public void Lowering_the_boundary_takes_the_later_stages_away_again()
    {
        var before = At(4);
        var after = At(1);

        Assert.NotNull(before.Entity(Sohrab));
        Assert.Null(after.Entity(Sohrab));
        Assert.Null(after.Connections(Sohrab));
        Assert.Null(after.Evidence(new RelationId("relation-14")));
        Assert.Null(after.Find(new EventId("event-06")));
        Assert.Empty(after.Gallery().Items);
        Assert.Empty(Disclosure.Leaks(after, Catalogue));
    }
}
