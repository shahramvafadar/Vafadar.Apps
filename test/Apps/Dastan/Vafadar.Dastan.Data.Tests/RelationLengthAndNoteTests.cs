using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// How long a relation held where a source states a length and no years, and what stands behind a relation
/// (D-102): a length is a length of time and no date; a note says whose account a statement is and belongs to
/// the details of the relation, not to its label. The records are test data.
/// </summary>
public sealed class RelationLengthAndNoteTests
{
    private static JsonObject Entity(string id, string type) => new()
    {
        ["id"] = id, ["type"] = type, ["label"] = "Test data " + id, ["summary"] = "Test data.",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
    };

    private static Action<JsonObject> World(string? length, string? note = null, string scope = "outside_the_story") => sample =>
    {
        sample["entities"]!.AsArray().Add(Entity("person-poet", "Person"));
        sample["entities"]!.AsArray().Add(Entity("organisation-court", "Organisation"));
        var relation = new JsonObject
        {
            ["id"] = "world-1", ["from"] = "person-poet", ["to"] = "organisation-court", ["type"] = "memberOf", ["label"] = "Test data",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
            ["reviewStatus"] = "source_backed_editorial_review_pending",
            ["scope"] = new JsonObject { ["kind"] = scope, ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
            ["directionality"] = "directed",
        };
        if (length is not null)
        {
            relation["length"] = JsonNode.Parse(length);
        }

        if (note is not null)
        {
            relation["note"] = note;
        }

        sample["relations"]!.AsArray().Add(relation);
    };

    [Fact]
    public void A_relation_says_how_long_it_held_without_a_year()
    {
        var catalogue = SampleFiles.Read(World("""{ "value": 37, "unit": "years", "approximate": true }""", "By tradition."));
        var relation = catalogue.Find(new RelationId("world-1"))!;

        Assert.Equal(new Quantity(37, "years", true, null), relation.Length);
        Assert.Null(relation.Time);
        Assert.Equal("By tradition.", relation.Note!.In("en")!.Value.Value);

        // A length is no date: the relation stands in no view of time.
        var view = Projection.Of(catalogue, catalogue.Info.DefaultLevel, Audience.LocalDemonstration)!.Timeline();
        Assert.DoesNotContain(view.Held, edge => edge.Relation.Id.Value == "world-1");
    }

    [Fact]
    public void A_relation_without_them_has_neither()
    {
        var relation = SampleFiles.Read(World(null)).Find(new RelationId("world-1"))!;

        Assert.Null(relation.Length);
        Assert.Null(relation.Note);
    }

    [Theory]
    [InlineData("""{ "value": 3, "unit": "months" }""")]
    [InlineData("""{ "value": 40, "unit": "days" }""")]
    public void A_length_is_in_years_months_or_days(string length)
    {
        Assert.NotNull(SampleFiles.Read(World(length)).Find(new RelationId("world-1"))!.Length);
    }

    [Fact]
    public void What_is_no_length_of_time_is_refused()
    {
        Assert.Equal(["world-1: length is a length of time: its unit is years, months or days."], SampleFiles.Problems(World("""{ "value": 37, "unit": "verses" }""")));
        Assert.Equal(["world-1: length is more than nothing."], SampleFiles.Problems(World("""{ "value": 0, "unit": "years" }""")));
    }

    [Fact]
    public void A_relation_of_a_story_may_state_a_length_too()
    {
        // A telling says how long a king reigned; it still dates nothing.
        var relation = SampleFiles.Read(World("""{ "value": 30, "unit": "years" }""", scope: "observed_narrative_context")).Find(new RelationId("world-1"))!;

        Assert.Equal(30, relation.Length!.Value);
    }

    /// <summary>
    /// The rule of the labels (D-102), asked of every collection of the library in both languages: a label says
    /// what the relation is. It names neither of the two it connects – the page writes them beside it – and not
    /// whose account the statement is: that stands in the note.
    /// </summary>
    [Fact]
    public void A_label_of_the_library_names_neither_an_end_of_its_relation_nor_whose_account_it_is()
    {
        var samples = Path.Combine(SampleFiles.App, "library", "samples");
        var attributed = new Regex("according to|by tradition|in the legend|legendary|perhaps|as .+ calls|به گفتهٔ|بر پایهٔ روایت|به روایت|در افسانه|افسانه‌ای|شاید|چنان‌که", RegexOptions.IgnoreCase);
        var wrong = new List<string>();
        foreach (var file in Directory.EnumerateFiles(samples, "*.json").Where(file => !file.EndsWith(".fa.json", StringComparison.Ordinal)))
        {
            var collection = JsonNode.Parse(File.ReadAllText(file))!;
            var persian = Path.ChangeExtension(file, null) + ".fa.json";
            var translated = File.Exists(persian) ? JsonNode.Parse(File.ReadAllText(persian)) : null;
            var names = collection["entities"]!.AsArray().ToDictionary(entity => (string)entity!["id"]!, entity => (string)entity!["label"]!);
            foreach (var relation in collection["relations"]!.AsArray())
            {
                var id = (string)relation!["id"]!;
                var ends = new[] { (string)relation["from"]!, (string)relation["to"]! };
                Check((string)relation["label"]!, ends.Select(end => names[end]), $"{Path.GetFileName(file)}, {id}");
                if (translated?["relations"]?[id]?["label"] is { } label)
                {
                    Check((string)label!, ends.Select(end => (string?)translated["entities"]?[end]?["label"]).OfType<string>(), $"{Path.GetFileName(persian)}, {id}");
                }
            }
        }

        Assert.True(wrong.Count == 0, "Labels of relations that say more than what the relation is:\n" + string.Join('\n', wrong));

        void Check(string label, IEnumerable<string> ends, string where)
        {
            if (attributed.IsMatch(label))
            {
                wrong.Add($"{where}: \"{label}\" says whose account it is – that belongs to the note.");
            }

            foreach (var name in ends)
            {
                // The name as a word of its own: "Successor of" does not name a house called "Su".
                if (Regex.IsMatch(label, $@"(?<![\p{{L}}\p{{M}}]){Regex.Escape(name)}(?![\p{{L}}\p{{M}}])", RegexOptions.IgnoreCase))
                {
                    wrong.Add($"{where}: \"{label}\" repeats the name \"{name}\".");
                }
            }
        }
    }
}
