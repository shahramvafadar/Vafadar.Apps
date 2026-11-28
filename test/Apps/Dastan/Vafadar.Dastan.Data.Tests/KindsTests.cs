using System.Text.Json.Nodes;
using Vafadar.Dastan.Content;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The kinds of records (ADR 0029): the list of a content root, what a record may name of it, what the check
/// says of the records without a kind, and how a choice of kinds lets records through.
/// </summary>
public sealed class KindsTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 28);

    private const string List = """
        {
          "formatVersion": "dastan-kinds-0.1",
          "kinds": [
            { "key": "war", "label": "War", "meaning": "A war.", "types": ["event", "period"], "icon": "war" },
            { "key": "battle", "label": "Battle", "meaning": "A battle.", "types": ["event"], "icon": "war", "parent": "war" },
            { "key": "hero", "label": "Hero", "meaning": "A hero.", "types": ["character"], "icon": "hero" },
            { "key": "champion", "label": "Champion", "meaning": "An older word.", "types": ["character"], "icon": "hero", "retired": true },
            { "key": "poem", "label": "Poem", "meaning": "A poem.", "types": ["work"], "icon": "pen" }
          ]
        }
        """;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-kinds-" + Guid.NewGuid().ToString("N"));

    public KindsTests()
    {
        foreach (var name in new[] { "samples", "comparisons", "media" })
        {
            Directory.CreateDirectory(Path.Combine(_folder, name));
        }
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Write(string relative, string text)
    {
        var file = Path.Combine(_folder, relative);
        File.WriteAllText(file, text);
        return file;
    }

    // The template of a collection with kinds given to its records: work-small, story-small, character-first, place-first.
    private string Collection(params (string Id, string[] Kinds)[] kinds)
    {
        var sample = JsonNode.Parse(ContentTemplates.Collection("small", Today))!.AsObject();
        foreach (var (id, given) in kinds)
        {
            sample["entities"]!.AsArray().Single(entity => (string?)entity!["id"] == id)!["kinds"] = new JsonArray([.. given.Select(key => (JsonNode)key)]);
        }

        return Write("samples/small.json", sample.ToJsonString());
    }

    private static string Problems(ContentReport report) => string.Join('\n', report.Problems);

    [Fact]
    public void A_record_names_kinds_of_the_list_and_the_check_refuses_the_records_without_one()
    {
        Write("kinds.json", List);
        var file = Collection(("character-first", ["hero", "champion"]), ("work-small", ["poem"]));

        var report = ContentCheck.File(file);
        var collection = PilotSample.Read(File.ReadAllText(file), new PilotSampleOptions
        {
            SampleDirectory = Path.Combine(_folder, "samples"),
            ContentRoot = _folder,
            ReviewRoot = _folder,
            Kinds = KindsFile.Load(_folder),
            RequireKinds = false,
        });
        var hero = collection.Entities.Single(entity => entity.Id.Value == "character-first");

        // The story and the place of the template name no kind: with the rule of K4 the collection cannot be used.
        Assert.False(report.IsUsable);
        Assert.Equal(2, report.Problems.Count(problem => problem.Contains("kinds names no kind", StringComparison.Ordinal)));
        Assert.Equal(["hero", "champion"], hero.Kinds.Select(kind => kind.Key));
        Assert.True(hero.Kinds[1].Retired);
    }

    [Fact]
    public void A_kind_that_is_not_in_the_list_is_refused()
    {
        Write("kinds.json", List);
        var report = ContentCheck.File(Collection(("character-first", ["villain"])));

        Assert.Contains(report.Problems, problem => problem.Contains("kinds names villain, which is not a kind of the list", StringComparison.Ordinal));
    }

    [Fact]
    public void A_kind_is_refused_for_a_type_it_is_not_given_to()
    {
        Write("kinds.json", List);
        var report = ContentCheck.File(Collection(("character-first", ["poem"])));

        Assert.Contains(report.Problems, problem => problem.Contains("the kind poem is not given to a character; it is given to work", StringComparison.Ordinal));
    }

    [Fact]
    public void Without_a_list_no_record_may_name_a_kind()
    {
        var report = ContentCheck.File(Collection(("character-first", ["hero"])));

        Assert.Contains(report.Problems, problem => problem.Contains("the content root has no list of kinds", StringComparison.Ordinal));
    }

    [Fact]
    public void The_rule_that_every_record_has_a_kind_is_on_since_the_library_is_classified_and_refuses()
    {
        Write("kinds.json", List);
        var file = Collection(("character-first", ["hero"]));
        var options = new PilotSampleOptions
        {
            SampleDirectory = Path.Combine(_folder, "samples"),
            ContentRoot = _folder,
            ReviewRoot = _folder,
            Kinds = KindsFile.Load(_folder),
            RequireKinds = true,
        };

        Assert.True(KindsFile.EveryRecordHasAKind);
        Assert.True(new PilotSampleOptions { SampleDirectory = _folder, ContentRoot = _folder, ReviewRoot = _folder }.RequireKinds);
        var refused = Assert.Throws<SampleFormatException>(() => PilotSample.Read(File.ReadAllText(file), options));
        Assert.Equal(3, refused.Problems.Count(problem => problem.Contains("kinds names no kind", StringComparison.Ordinal)));
    }

    [Fact]
    public void A_wider_kind_takes_in_the_kinds_below_it_and_only_kinds_that_are_there_are_offered()
    {
        var kinds = KindsFile.Read(List);
        var battle = kinds.Find("battle")!;
        var hero = kinds.Find("hero")!;

        Assert.Same(kinds.Find("war"), battle.Parent);
        Assert.True(KindFilter.Passes([battle], new HashSet<string> { "war" }));
        Assert.True(KindFilter.Passes([battle], new HashSet<string>()));
        Assert.False(KindFilter.Passes([hero], new HashSet<string> { "war" }));
        Assert.False(KindFilter.Passes([], new HashSet<string> { "war" }));
        Assert.Equal(["war", "battle", "hero"], KindFilter.Offered([[hero], [battle]]).Select(kind => kind.Key));
        Assert.Equal(["battle", "war"], KindFilter.Read(["battle,war", "nothing"], [kinds.Find("war")!, battle]).Order(StringComparer.Ordinal));
        Assert.Equal("war,battle", KindFilter.Written(["battle", "war"], kinds.All));
    }

    [Theory]
    [InlineData("\"types\": [\"event\"]", "\"types\": [\"battleground\"]", "types names battleground, which is no type of entity")]
    [InlineData("\"parent\": \"war\"", "\"parent\": \"conflict\"", "the parent conflict is not a kind of the list")]
    [InlineData("\"key\": \"poem\"", "\"key\": \"hero\"", "the key is given to two kinds")]
    [InlineData("\"icon\": \"pen\"", "\"icon\": \"pen\", \"colour\": \"red\"", "the field colour is not known to this reader")]
    [InlineData("\"icon\": \"pen\"", "\"icon\": \"Pen Drawing\"", "icon is an identifier and contains a blank")]
    [InlineData("\"key\": \"war\"", "\"key\": \"war\", \"parent\": \"battle\"", "following its parents leads back to it")]
    public void The_list_is_read_strictly(string written, string instead, string problem)
    {
        var refused = Assert.Throws<SampleFormatException>(() => KindsFile.Read(List.Replace(written, instead, StringComparison.Ordinal)));

        Assert.Contains(refused.Problems, found => found.Contains(problem, StringComparison.Ordinal));
    }

    [Fact]
    public void A_translation_names_the_kinds_in_its_language_and_a_draft_is_left_out_for_the_public()
    {
        const string persian = """
            { "formatVersion": "dastan-kinds-translation-0.1", "language": "fa", "review": "draft", "kinds": { "battle": { "label": "نبرد", "meaning": "یک نبرد." } } }
            """;
        const string unknown = """
            { "formatVersion": "dastan-kinds-translation-0.1", "language": "fa", "review": "draft", "kinds": { "skirmish": { "label": "زد و خورد" } } }
            """;

        var read = KindsFile.Read(List, [persian]);
        var forThePublic = KindsFile.Read(List, [persian], reviewedTextsOnly: true);

        Assert.Equal("نبرد", read.Find("battle")!.Label.In("fa")!.Value.Value);
        Assert.Equal(TextReview.Draft, read.Find("battle")!.Label.In("fa")!.Value.Review);
        Assert.Null(forThePublic.Find("battle")!.Label.In("fa"));
        Assert.Contains(Assert.Throws<SampleFormatException>(() => KindsFile.Read(List, [unknown])).Problems, problem => problem.Contains("translates a kind the list does not have", StringComparison.Ordinal));
    }

    [Fact]
    public void The_items_of_a_comparison_name_kinds_of_their_type()
    {
        Write("kinds.json", List);
        var comparison = JsonNode.Parse(ContentTemplates.Comparison("two-lands", Today))!.AsObject();
        var without = ContentCheck.File(Write("comparisons/two-lands.json", comparison.ToJsonString()));
        foreach (var item in comparison["lanes"]!.AsArray().SelectMany(lane => (lane!["periods"]?.AsArray() ?? []).Concat(lane!["events"]?.AsArray() ?? [])))
        {
            item!["kinds"] = new JsonArray("war");
        }

        var good = ContentCheck.File(Write("comparisons/two-lands.json", comparison.ToJsonString()));
        comparison["lanes"]![0]!["periods"]![0]!["kinds"] = new JsonArray("battle");
        var refused = ContentCheck.File(Write("comparisons/two-lands.json", comparison.ToJsonString()));

        Assert.Equal(3, without.Problems.Count(problem => problem.Contains("names no kind", StringComparison.Ordinal)));
        Assert.True(good.IsUsable, Problems(good));
        Assert.Contains(good.Summary, entry => entry.Name == "without a kind" && entry.Value == "0 of 3");
        Assert.Contains(refused.Problems, problem => problem.Contains("the kind battle is not given to a period", StringComparison.Ordinal));
    }

    [Fact]
    public void The_check_reads_the_list_with_its_translation_and_the_command_names_its_vocabulary()
    {
        Write("kinds.json", List);
        Write("kinds.fa.json", """{ "formatVersion": "dastan-kinds-translation-0.1", "language": "fa", "review": "draft", "kinds": { "war": { "label": "جنگ" } } }""");

        var list = ContentCheck.File(Path.Combine(_folder, "kinds.json"));
        var translation = ContentCheck.File(Path.Combine(_folder, "kinds.fa.json"));
        using var output = new StringWriter();
        var exit = ContentCommands.Run(["vocabulary", "kinds"], output, Today);

        Assert.True(list.IsUsable, Problems(list));
        Assert.Equal(KindsFile.FormatVersion, list.Format);
        Assert.Contains(("kinds", "5"), list.Summary);
        Assert.Contains(("retired", "1"), list.Summary);
        Assert.Contains(("translations", "fa"), list.Summary);
        Assert.Equal(list.File, translation.File);
        Assert.Equal(ContentCommands.Done, exit);
        Assert.Contains("kinds[].types", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(EntityTypes.All.Select(type => type.Key), ContentVocabulary.Kinds.Single(choices => choices.Field == "kinds[].types").Values.Select(value => value.Name));
    }

    [Fact]
    public void The_list_of_the_library_has_the_first_kinds_with_their_wider_kinds_and_their_Persian()
    {
        var kinds = KindsFile.Load(Path.Combine(SampleFiles.App, "library"));

        Assert.Equal("war", kinds.Find("battle")!.Parent!.Key);
        Assert.Equal("war", kinds.Find("siege")!.Parent!.Key);
        Assert.All(new[] { "drought", "flood", "earthquake", "frost", "famine", "epidemic" }, key => Assert.Equal("naturalEvent", kinds.Find(key)!.Parent!.Key));
        Assert.All(new[] { "ruler", "statesman", "general" }, key => Assert.Equal("political", kinds.Find(key)!.Parent!.Key));
        Assert.All(new[] { "poet", "historian" }, key => Assert.Equal("literary", kinds.Find(key)!.Parent!.Key));

        // Figures of myth are kinds of characters of a story, never of real persons (K3).
        Assert.All(new[] { "hero", "demon", "divineBeing", "creature" }, key => Assert.DoesNotContain(EntityTypes.Person, kinds.Find(key)!.Types));
        Assert.Equal("نبرد", kinds.Find("battle")!.Label.In("fa")!.Value.Value);
        Assert.All(kinds.All, kind => Assert.NotNull(kind.Label.In("fa")));
    }
}
