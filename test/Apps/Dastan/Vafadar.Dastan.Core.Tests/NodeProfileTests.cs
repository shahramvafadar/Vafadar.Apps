using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The profile of a node (step 19.5, ADR 0017): every kind of the vocabulary has its facts; a profile states
/// what its kind needs or says that it is unknown; a fact and a picture are shown to exactly those who may see
/// them; and a collection whose profiles break the table is refused. The story is made up.
/// </summary>
public sealed class NodeProfileTests
{
    private static readonly TemporalExpression Year1000 = new Occurrence(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 1000));

    private static ProfileFact Fact(FactKind kind, TemporalExpression? time = null, string? node = null, string? words = null, int level = 0, string? source = null) => new()
    {
        Kind = kind,
        Time = time,
        Node = node is null ? null : new EntityId(node),
        Words = words is null ? null : SmallStory.Text(words),
        Sources = source is null ? [] : [new SourceId(source)],
        Level = new ReadingLevel(level),
    };

    // The story with one entity given facts and, perhaps, a picture.
    private static SmallStory With(string id, ProfileFact[] facts, string? picture = null, SmallStory? story = null)
    {
        story ??= new SmallStory();
        var at = story.Entities.FindIndex(entity => entity.Id.Value == id);
        var entity = story.Entities[at];
        story.Entities[at] = new Entity
        {
            Id = entity.Id,
            Type = entity.Type,
            Label = entity.Label,
            Summary = entity.Summary,
            Sources = entity.Sources,
            Level = entity.Level,
            Facts = [.. facts],
            Picture = picture is null ? null : new MediaId(picture),
        };
        return story;
    }

    private static NodeProfile Profile(SmallStory story, string id, int level, Audience audience = Audience.LocalDemonstration) =>
        Projection.Of(story.Build(), new ReadingLevel(level), audience)!.Profile(new EntityId(id))!;

    [Fact]
    public void Every_kind_of_the_vocabulary_has_facts_and_a_key_says_the_same_everywhere()
    {
        foreach (var type in EntityTypes.All)
        {
            var facts = ProfileFacts.Of(type);

            Assert.NotEmpty(facts.Essential);
            Assert.Equal(facts.All.Length, facts.All.Select(fact => fact.Key).Distinct().Count());
            Assert.All(facts.All, fact => Assert.Same(ProfileFacts.Find(fact.Key), fact));
        }

        Assert.Equal(ProfileFacts.All.Length, ProfileFacts.All.Select(fact => fact.Key).Distinct().Count());
        Assert.Null(ProfileFacts.Find("no-such-fact"));
        Assert.Null(ProfileFacts.Of(EntityTypes.Character).Find(ProfileFacts.Born.Key));
    }

    [Fact]
    public void A_profile_says_every_essential_fact_stated_or_unknown_and_an_added_one_only_when_it_is_stated()
    {
        var bare = Profile(new SmallStory(), "hero", 0);

        var role = Assert.Single(bare.Essential);
        Assert.Same(ProfileFacts.Role, role.Kind);
        Assert.True(role.IsUnknown);
        Assert.Empty(bare.More);
        Assert.Empty(bare.Card);
        Assert.Null(bare.Picture);

        var stated = Profile(With("hero", [Fact(ProfileFacts.Origin, node: "town"), Fact(ProfileFacts.Role, words: "A wandering hero", source: "passage-0")]), "hero", 0);

        Assert.Equal("A wandering hero", Assert.Single(Assert.Single(stated.Essential).Stated).Words!.In("en")!.Value.Value);
        Assert.Equal("town", Assert.Single(Assert.Single(stated.More).Stated).Node!.Value.Value);

        // The essential fact comes first on a card, whatever the order of the record.
        Assert.Equal(["role", "origin"], stated.Card.Select(line => line.Kind.Key));
    }

    [Fact]
    public void A_fact_of_a_later_stage_is_shown_like_one_nobody_stated()
    {
        var story = With("hero", [Fact(ProfileFacts.Role, words: "The father of the child", level: 2, source: "passage-2")]);

        Assert.True(Assert.Single(Profile(story, "hero", 0).Essential).IsUnknown);
        Assert.True(Assert.Single(Profile(story, "hero", 1).Essential).IsUnknown);
        Assert.False(Assert.Single(Profile(story, "hero", 2).Essential).IsUnknown);

        // Its source is named only where the fact is.
        Assert.DoesNotContain(Projection.Of(story.Build(), new ReadingLevel(0), Audience.LocalDemonstration)!.Sources, source => source.Id.Value == "passage-2");
    }

    [Fact]
    public void A_fact_that_names_a_node_the_visitor_may_not_see_yet_is_not_told()
    {
        // The keepsake is held by the child, who is told of at stage 2; the keepsake itself at stage 1.
        var story = With("keepsake", [Fact(ProfileFacts.HeldBy, node: "child", level: 1)]);

        Assert.True(Assert.Single(Profile(story, "keepsake", 1).Essential).IsUnknown);
        Assert.Equal("child", Assert.Single(Assert.Single(Profile(story, "keepsake", 2).Essential).Stated).Node!.Value.Value);
        Assert.Null(Projection.Of(story.Build(), new ReadingLevel(0), Audience.LocalDemonstration)!.Profile(new EntityId("keepsake")));
    }

    [Fact]
    public void The_picture_is_shown_to_exactly_those_who_may_see_that_media_item()
    {
        // The diagram is told of from stage 2 on, and cleared for a demonstration only.
        var story = With("hero", [], picture: "diagram");

        Assert.Null(Profile(story, "hero", 0).Picture);
        Assert.Null(Profile(story, "hero", 1).Picture);
        Assert.Equal("diagram", Profile(story, "hero", 2).Picture!.Id.Value);

        var approved = With("hero", [], picture: "diagram", story: new SmallStory().Published());
        Assert.Null(Profile(approved, "hero", 2, Audience.Public).Picture);

        var cleared = new SmallStory().Published();
        cleared.Media[0] = SmallStory.Diagram("diagram", forPublic: true);
        Assert.NotNull(Profile(With("hero", [], picture: "diagram", story: cleared), "hero", 2, Audience.Public).Picture);
    }

    [Fact]
    public void A_card_shows_three_facts_at_most()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("battle", EntityTypes.Event));
        With(
            "battle",
            [
                Fact(ProfileFacts.Happened, time: Year1000),
                Fact(ProfileFacts.Where, node: "town"),
                Fact(ProfileFacts.Participant, node: "hero"),
                Fact(ProfileFacts.Participant, node: "story"),
            ],
            story: story);

        var profile = Profile(story, "battle", 0);

        Assert.Equal(2, profile.Essential.Length);
        Assert.Equal(2, Assert.Single(profile.More).Stated.Length);
        Assert.Equal(NodeProfile.CardFacts, profile.Card.Length);
    }

    [Fact]
    public void A_time_nobody_knows_is_stated_as_unknown_and_is_a_fact()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("poet", EntityTypes.Person));
        With("poet", [Fact(ProfileFacts.Born, time: new UnknownTime()), Fact(ProfileFacts.Died, time: Year1000)], story: story);

        var profile = Profile(story, "poet", 0);

        Assert.Equal(["born", "died", "knownFor"], profile.Essential.Select(line => line.Kind.Key));
        Assert.IsType<UnknownTime>(Assert.Single(profile.Essential[0].Stated).Time);
        Assert.False(profile.Essential[0].IsUnknown);
        Assert.True(profile.Essential[2].IsUnknown);
    }

    public static TheoryData<string, string> Refused() => new()
    {
        { "born", "hero, the fact born: a character has no such fact; it has role, origin." },
        { "words-for-a-place", "hero, the fact origin: it says a place, and nothing else." },
        { "two-things", "hero, the fact role: it says words, and nothing else." },
        { "itself", "hero, the fact origin: it names the entity itself." },
        { "no-place", "hero, the fact origin: it names keepsake, which is no place." },
        { "nobody", "hero, the fact origin: it names nowhere, which is no entity of the collection." },
        { "twice", "hero: the fact role is stated 2 times; it is stated once." },
        { "early", "child, the fact role: a fact about child cannot be shown before child itself." },
        { "no-picture", "hero: its picture nothing is no media item of the collection." },
        { "other-picture", "town: its picture diagram does not say that it depicts town." },
        { "source", "hero, the fact role: the source no-source does not exist." },
    };

    [Theory]
    [MemberData(nameof(Refused))]
    public void A_collection_whose_profile_breaks_the_table_is_refused(string what, string problem)
    {
        var story = what switch
        {
            "born" => With("hero", [Fact(ProfileFacts.Born, time: Year1000)]),
            "words-for-a-place" => With("hero", [Fact(ProfileFacts.Origin, words: "Somewhere")]),
            "two-things" => With("hero", [Fact(ProfileFacts.Role, words: "A hero", node: "town")]),
            "itself" => With("hero", [Fact(ProfileFacts.Origin, node: "hero")]),
            "no-place" => With("hero", [Fact(ProfileFacts.Origin, node: "keepsake")]),
            "nobody" => With("hero", [Fact(ProfileFacts.Origin, node: "nowhere")]),
            "twice" => With("hero", [Fact(ProfileFacts.Role, words: "A hero"), Fact(ProfileFacts.Role, words: "A wanderer")]),
            "early" => With("child", [Fact(ProfileFacts.Role, words: "A child", level: 1)]),
            "no-picture" => With("hero", [], picture: "nothing"),
            "other-picture" => With("town", [], picture: "diagram"),
            _ => With("hero", [Fact(ProfileFacts.Role, words: "A hero", source: "no-source")]),
        };

        Assert.Contains(problem, story.Problems());
    }
}
