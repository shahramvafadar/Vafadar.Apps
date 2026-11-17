using Vafadar.Dastan.Testing;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The model is not about persons. The same catalogue and the same projection carry a collection whose subjects
/// are a club, its teams, a sponsor and a season; the only person in it is somebody who is merely mentioned.
/// </summary>
public sealed class OtherSubjectsTests
{
    private static readonly EntityId Club = new("club");
    private static readonly EntityId Seniors = new("seniors");

    private static SmallStory Season()
    {
        var story = new SmallStory();
        story.Entities.RemoveAll(entity => entity.Type.Nature is EntityNature.Being or EntityNature.Place or EntityNature.Object);
        story.Entities.AddRange(
        [
            SmallStory.Entity("club", EntityTypes.SportsClub),
            SmallStory.Entity("seniors", EntityTypes.SportsTeam),
            SmallStory.Entity("rivals", EntityTypes.SportsTeam),
            SmallStory.Entity("sponsor", EntityTypes.Company, level: 1, source: "passage-1"),
            SmallStory.Entity("season", EntityTypes.CompetitionSeason),
            SmallStory.Entity("coach", EntityTypes.Person, level: 2, source: "passage-2"),
        ]);
        story.Relations.Clear();
        story.Relations.AddRange(
        [
            SmallStory.Relation("has-team", "seniors", "club", "partOf"),
            SmallStory.Relation("derby", "seniors", "rivals", "opponentOf", events: ["kick-off"]),
            SmallStory.Relation("backing", "sponsor", "seniors", "supportsExpeditionOf", level: 1, source: "passage-1", events: ["deal"]),
        ]);
        story.Events.Clear();
        story.Events.AddRange(
        [
            SmallStory.Event("kick-off", 1, 0, "passage-0", ("seniors", Presence.Acts), ("rivals", Presence.Acts), ("season", Presence.Involved)),
            SmallStory.Event("deal", 2, 1, "passage-1", ("sponsor", Presence.Acts), ("club", Presence.Acts)),
            SmallStory.Event("title", 3, 2, "passage-2", ("seniors", Presence.Acts), ("season", Presence.Involved), ("coach", Presence.MentionedOnly)),
        ]);
        story.Media.Clear();
        story.Media.Add(SmallStory.Diagram("crest", depicted: ["club", "seniors"], relations: []));
        return story;
    }

    private static Projection At(int level) =>
        Projection.Of(Season().Build(), new ReadingLevel(level), Audience.LocalDemonstration)!;

    [Fact]
    public void A_collection_without_a_single_character_is_a_consistent_collection()
    {
        var catalogue = Season().Build();

        Assert.DoesNotContain(catalogue.Entities, entity => entity.Type == EntityTypes.Character);
        Assert.Equal(8, catalogue.Entities.Length);
    }

    [Fact]
    public void A_visitor_starts_from_teams_and_companies_as_from_characters()
    {
        Assert.Equal(["seniors", "rivals", "club"], At(0).Overview().Subjects.Select(entity => entity.Id.Value));
        Assert.Equal(["seniors", "rivals", "sponsor", "club", "coach"], At(2).Overview().Subjects.Select(entity => entity.Id.Value));
    }

    [Fact]
    public void A_team_has_connections_a_lane_and_appearances_like_anybody_else()
    {
        var projection = At(2);
        var seniors = projection.Entity(Seniors)!;

        Assert.Equal(
            [("has-team", "club", ConnectionDirection.Outgoing), ("derby", "rivals", ConnectionDirection.Mutual), ("backing", "sponsor", ConnectionDirection.Incoming)],
            seniors.Connections.Select(connection => (connection.Relation.Id.Value, connection.Other.Id.Value, connection.Direction)));
        Assert.Equal(["kick-off", "title"], seniors.Appearances.Select(appearance => appearance.Step.Event.Id.Value));

        var lane = projection.Lanes()!.Lanes.Single(row => row.Entity.Id == Seniors);
        Assert.Equal([Presence.Acts, null, Presence.Acts], lane.Cells.Select(cell => cell?.Presence));
    }

    [Fact]
    public void A_season_takes_part_in_events_without_being_a_place_or_an_actor()
    {
        var season = At(2).Lanes()!.Lanes.Single(lane => lane.Entity.Type == EntityTypes.CompetitionSeason);

        Assert.Equal(EntityNature.Occasion, season.Entity.Type.Nature);
        Assert.Equal([Presence.Involved, null, Presence.Involved], season.Cells.Select(cell => cell?.Presence));
    }

    [Fact]
    public void A_collection_without_parents_and_partners_has_an_empty_family()
    {
        Assert.Empty(At(2).Family().Members);
        Assert.Empty(At(2).Family(Club)!.Members);
    }

    [Fact]
    public void A_club_is_found_by_its_name_and_has_its_gallery_and_its_evidence()
    {
        var projection = At(2);

        Assert.Equal("club", Assert.Single(projection.Search("CLUB")).Entity.Id.Value);
        Assert.Equal(["crest"], projection.Gallery(Club)!.Items.Select(item => item.Id.Value));
        Assert.Equal(EvidenceSubject.Entity, projection.Evidence(Club)!.Subject);
        Assert.Equal(["seniors", "club", "rivals", "sponsor"], projection.Connections(Seniors)!.Nodes.Select(node => node.Id.Value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void The_reading_boundary_holds_for_teams_and_companies_too(int level)
    {
        var catalogue = Season().Build();
        var projection = Projection.Of(catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

        Assert.Empty(Disclosure.Leaks(projection, catalogue));
        Assert.Equal(level >= 1, projection.Find(new EntityId("sponsor")) is not null);
        Assert.Equal(level >= 2, projection.Find(new EntityId("coach")) is not null);
    }
}
