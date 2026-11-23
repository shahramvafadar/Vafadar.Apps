using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The years a visitor chose, kept to by the page of a name and by its family tree as well (D-084, delivery
/// plan 25.3): what is stated about a name is told apart by one rule – in the years, without a time, at another
/// time –, descent is drawn at every time, and a partnership that states another time is left out and counted.
/// The records are test data.
/// </summary>
public sealed class YearsOfEveryViewTests
{
    private const string Poet = "person-poet";
    private const string Partner = "person-partner";
    private const string Child = "person-child";
    private const string Other = "person-other";
    private const string Rostam = "character-rostam";

    private static string Years(int from, int to) => $$"""
        {
          "kind": "period",
          "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": {{from}} } },
          "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": {{to}} } },
          "continuity": "notAsserted"
        }
        """;

    // The world of the years of a name, and: the poet and a partner from 1000 to 1010, with a child of both;
    // and the poet and another partner from 1040 to 1050, with no child.
    private static void World(JsonObject sample)
    {
        YearsOfANameTests.World(sample);
        foreach (var id in new[] { Partner, Child, Other })
        {
            sample["entities"]!.AsArray().Add(new JsonObject
            {
                ["id"] = id, ["type"] = "Person", ["label"] = "Test data " + id, ["summary"] = "Test data.",
                ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
            });
        }

        JsonObject Relation(string id, string from, string to, string type, string? time)
        {
            var relation = new JsonObject
            {
                ["id"] = id, ["from"] = from, ["to"] = to, ["type"] = type, ["label"] = "Test data",
                ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
                ["reviewStatus"] = "source_backed_editorial_review_pending",
                ["scope"] = new JsonObject { ["kind"] = "outside_the_story", ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
                ["directionality"] = type == "spouseOf" ? "symmetric" : "directed",
            };
            if (time is not null)
            {
                relation["time"] = JsonNode.Parse(time);
            }

            return relation;
        }

        var relations = sample["relations"]!.AsArray();
        relations.Add(Relation("relation-partner", Poet, Partner, "spouseOf", Years(1000, 1010)));
        relations.Add(Relation("relation-other", Poet, Other, "spouseOf", Years(1040, 1050)));
        relations.Add(Relation("relation-child-of-poet", Poet, Child, "parentOf", null));
        relations.Add(Relation("relation-child-of-partner", Partner, Child, "parentOf", null));

        // The other partner has a parent of their own: a family beside the tree when the partnership is left out.
        sample["entities"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "person-other-parent", ["type"] = "Person", ["label"] = "Test data person-other-parent", ["summary"] = "Test data.",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
        });
        relations.Add(Relation("relation-parent-of-other", "person-other-parent", Other, "parentOf", null));
    }

    private static Projection Reader() => Projection.Of(SampleFiles.Read(World), new ReadingLevel(4), Audience.LocalDemonstration)!;

    private static YearSpan Span(int from, int to) => Exploration.Read(new EntityId(Poet), ExplorationView.Overview, new ReadingLevel(4),
        [KeyValuePair.Create<string, string?>("from", from.ToString(System.Globalization.CultureInfo.InvariantCulture)), KeyValuePair.Create<string, string?>("to", to.ToString(System.Globalization.CultureInfo.InvariantCulture))]).Span!.Value;

    private static YearParts<Connection> Connected(YearSpan? years) =>
        YearParts.Of(Reader().Entity(new EntityId(Poet))!.Connections, connection => connection.Relation.Time, years, 2026);

    [Fact]
    public void Without_years_everything_stated_about_a_name_is_one_list()
    {
        var parts = Connected(null);

        Assert.Null(parts.Years);
        Assert.Equal(6, parts.In.Length);
        Assert.Empty(parts.WithoutTime);
        Assert.Equal(0, parts.LeftOut);
        Assert.Empty(parts.Possibly);
    }

    [Fact]
    public void In_a_stretch_of_years_what_is_stated_about_a_name_is_told_apart()
    {
        var parts = Connected(Span(990, 999));

        Assert.Equal(["relation-court-dated"], parts.In.Select(connection => connection.Relation.Id.Value));
        Assert.Equal(["relation-child-of-poet", "relation-court-undated"], parts.WithoutTime.Select(connection => connection.Relation.Id.Value).Order());
        Assert.Equal(3, parts.LeftOut);
        Assert.Empty(parts.Possibly);
        Assert.False(parts.IsEmpty);
    }

    [Fact]
    public void What_reaches_into_the_years_held_in_them()
    {
        var parts = Connected(Span(1005, 1025));

        Assert.Equal(["relation-school", "relation-partner"], parts.In.Select(connection => connection.Relation.Id.Value).OrderDescending());

        // A stretch with both ends known that reaches into the years held in them; only a time that is itself
        // not sure – a window, an approximate year – "may have held" (the rule of YearCover, tested with it).
        Assert.Empty(parts.Possibly);
        Assert.Equal(2, parts.LeftOut);
    }

    [Fact]
    public void In_years_nothing_states_only_what_states_no_time_is_left()
    {
        var parts = Connected(Span(1500, 1600));

        Assert.Empty(parts.In);
        Assert.Equal(2, parts.WithoutTime.Length);
        Assert.Equal(4, parts.LeftOut);
    }

    [Fact]
    public void Every_view_of_a_name_keeps_to_the_years()
    {
        foreach (var view in Enum.GetValues<ExplorationView>())
        {
            Assert.True(Exploration.HonoursYears(view), view.ToString());
        }
    }

    [Fact]
    public void A_family_tree_without_years_is_what_it_was()
    {
        var family = Reader().Family(new EntityId(Rostam))!;

        Assert.Same(family, family.Within(new EntityId(Rostam), null, 2026));
        Assert.Null(family.Years);
        Assert.Equal(0, family.LeftOut);
    }

    [Fact]
    public void A_family_of_a_story_states_no_time_and_is_drawn_in_every_stretch_of_years()
    {
        var whole = Reader().Family(new EntityId(Rostam))!;
        var then = whole.Within(new EntityId(Rostam), Span(1100, 1200), 2026);

        Assert.Equal(whole.Members, then.Members);
        Assert.Equal(whole.Partnerships, then.Partnerships);
        Assert.Equal(0, then.LeftOut);
        Assert.Equal(whole.Parentage.Length + whole.Partnerships.Length, then.WithoutTime);
        Assert.NotNull(then.Years);
    }

    [Fact]
    public void Descent_is_drawn_at_every_time_and_a_partnership_of_another_time_is_left_out_and_counted()
    {
        var whole = Reader().Family(new EntityId(Poet))!;
        Assert.Equal([Child, Other, "person-other-parent", Partner, Poet], whole.Members.Select(member => member.Entity.Id.Value).Order());
        Assert.Equal(2, whole.Partnerships.Length);

        var then = whole.Within(new EntityId(Poet), Span(1100, 1200), 2026);

        Assert.Equal(["relation-child-of-partner", "relation-child-of-poet"], then.Parentage.Select(edge => edge.Relation.Id.Value).Order());
        Assert.Empty(then.Partnerships);
        Assert.Equal(2, then.LeftOut);
        Assert.Equal(2, then.WithoutTime);

        // The first partner is a parent of a child that is drawn: drawn, by descent. The other is reached by
        // nothing that is drawn from the poet – and neither is the parent of the other, who would stand beside
        // the tree as a family of their own.
        Assert.Equal([Child, Partner, Poet], then.Members.Select(member => member.Entity.Id.Value).Order());
    }

    [Fact]
    public void A_partnership_that_held_in_the_years_or_reaches_into_them_is_drawn()
    {
        var whole = Reader().Family(new EntityId(Poet))!;

        var then = whole.Within(new EntityId(Poet), Span(1040, 1050), 2026);
        Assert.Equal(["relation-other"], then.Partnerships.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(1, then.LeftOut);
        Assert.Empty(then.Possibly);
        Assert.Equal(5, then.Members.Length);

        var touching = whole.Within(new EntityId(Poet), Span(1005, 1020), 2026);
        Assert.Equal(["relation-partner"], touching.Partnerships.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(1, touching.LeftOut);
    }

    [Fact]
    public void The_one_a_tree_is_about_is_drawn_whatever_the_years()
    {
        var alone = Reader().Family(new EntityId(Other))!.Within(new EntityId(Other), Span(1100, 1200), 2026);

        Assert.Contains(alone.Members, member => member.Entity.Id.Value == Other);
        Assert.DoesNotContain(alone.Partnerships, edge => edge.Relation.Id.Value == "relation-other");
    }
}
