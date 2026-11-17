using System.Text.Json;
using System.Text.Json.Nodes;
using Vafadar.Dastan.Content;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The tools of the writers of content (D-042): the check that reads a file as the site does, the list of the
/// values a field may have, the templates to start from, and the command that gives all three to a person or an
/// assistant.
/// </summary>
public sealed class ContentToolTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 17);

    // A folder of a collection for one test: samples and comparisons below it, as in the repository.
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-content-" + Guid.NewGuid().ToString("N"));

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Write(string relative, string text)
    {
        var file = Path.Combine(_folder, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
        return file;
    }

    private static (int Exit, string Output) Run(params string[] arguments)
    {
        using var output = new StringWriter();
        return (ContentCommands.Run(arguments, output, Today), output.ToString());
    }

    public ContentToolTests() => Directory.CreateDirectory(_folder);

    // --- the check ---

    [Fact]
    public void The_content_of_the_repository_passes_the_check_and_is_described()
    {
        var collection = ContentCheck.File(SampleFiles.PilotPath);
        var comparison = ContentCheck.File(Path.Combine(SampleFiles.Content, "comparisons", "iran-china-rome.json"));

        Assert.True(collection.IsUsable, string.Join('\n', collection.Problems));
        Assert.Equal(PilotSample.SchemaVersion, collection.Format);
        Assert.Contains(("translations", "fa"), collection.Summary);
        Assert.Contains(("relations", "20"), collection.Summary);
        Assert.Contains(collection.Summary, entry => entry.Name == "entities" && entry.Value.StartsWith("14 (Work 1, NarrativeArc 1, Character 7", StringComparison.Ordinal));
        Assert.Empty(collection.Notices);

        Assert.True(comparison.IsUsable, string.Join('\n', comparison.Problems));
        Assert.Equal(ComparisonSample.FormatVersion, comparison.Format);
        Assert.Contains(("lanes", "3"), comparison.Summary);
        Assert.Contains(("released to", "the local demonstration only"), comparison.Summary);
    }

    [Fact]
    public void A_translation_is_checked_with_the_file_it_translates()
    {
        var report = ContentCheck.File(Path.Combine(SampleFiles.Samples, "shahnameh-pilot.fa.json"));

        Assert.True(report.IsUsable, string.Join('\n', report.Problems));
        Assert.Equal(SampleFiles.PilotPath, report.File);
        Assert.Equal(PilotSample.SchemaVersion, report.Format);
    }

    [Fact]
    public void Every_problem_of_a_file_is_named_with_its_record_and_none_is_hidden_by_the_first()
    {
        var sample = JsonNode.Parse(ContentTemplates.Collection("small", Today))!.AsObject();
        sample["entities"]![2]!["type"] = "Hero";
        sample["relations"]![0]!["to"] = "work-missing";
        sample["events"]![0]!["temporalExpression"]!["kind"] = "calendar_date";
        var file = Write("samples/small.json", sample.ToJsonString());

        var report = ContentCheck.File(file);

        Assert.False(report.IsUsable);
        Assert.Empty(report.Summary);
        Assert.Contains(report.Problems, problem => problem.StartsWith("character-first: type has the unknown value \"Hero\"", StringComparison.Ordinal));
        Assert.Contains(report.Problems, problem => problem.StartsWith("event-01", StringComparison.Ordinal) && problem.Contains("narrative_position", StringComparison.Ordinal));
        Assert.True(report.Problems.Length >= 2);
    }

    [Fact]
    public void A_collection_that_contradicts_itself_is_refused_like_one_that_is_badly_written()
    {
        var sample = JsonNode.Parse(ContentTemplates.Collection("small", Today))!.AsObject();
        sample["relations"]![0]!["to"] = "work-missing";
        var file = Write("samples/small.json", sample.ToJsonString());

        var report = ContentCheck.File(file);

        Assert.False(report.IsUsable);
        Assert.Contains(report.Problems, problem => problem.Contains("work-missing does not exist", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("{ \"schemaVersion\": \"dastan-pilot-9.9\" }", "dastan-pilot-9.9")]
    [InlineData("{ \"formatVersion\": \"something-else-1.0\" }", "something-else-1.0")]
    public void A_file_of_a_format_the_site_does_not_read_is_refused_with_the_format_it_states(string text, string format)
    {
        var report = ContentCheck.File(Write("samples/other.json", text));

        Assert.False(report.IsUsable);
        Assert.Equal(format, report.Format);
        Assert.Contains(format, Assert.Single(report.Problems), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not json at all", "not valid JSON")]
    [InlineData("[1, 2]", "not a JSON object")]
    [InlineData("{ \"title\": \"A file\" }", "states no format")]
    public void What_is_not_a_content_file_is_refused_and_the_check_says_why(string text, string reason)
    {
        var report = ContentCheck.File(Write("samples/other.json", text));

        Assert.False(report.IsUsable);
        Assert.Null(report.Format);
        Assert.Contains(reason, Assert.Single(report.Problems), StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_that_does_not_exist_and_a_translation_without_its_file_are_refused()
    {
        var missing = ContentCheck.File(Path.Combine(_folder, "samples", "nothing.json"));
        var alone = ContentCheck.File(Write("samples/story.fa.json", "{ \"formatVersion\": \"dastan-translation-0.1\" }"));

        Assert.Contains("does not exist", Assert.Single(missing.Problems), StringComparison.Ordinal);
        Assert.Contains("lies beside the <file>.json it translates", Assert.Single(alone.Problems), StringComparison.Ordinal);
    }

    [Fact]
    public void The_content_files_of_a_folder_are_its_files_without_their_translations()
    {
        Write("samples/b.json", "{}");
        Write("samples/b.fa.json", "{}");
        Write("comparisons/a.json", "{}");
        Write("comparisons/notes.txt", "not content");

        Assert.Equal(["a.json", "b.json"], ContentCheck.FilesOf(_folder).Select(Path.GetFileName));
    }

    // --- the templates ---

    [Theory]
    [InlineData("collection", "samples")]
    [InlineData("subject", "samples")]
    [InlineData("comparison", "comparisons")]
    public void A_template_is_a_file_the_site_can_read_and_the_check_says_what_is_left_to_fill(string kind, string folder)
    {
        var text = kind switch
        {
            "collection" => ContentTemplates.Collection("my-story", Today),
            "subject" => ContentTemplates.Subject("my-story", Today),
            _ => ContentTemplates.Comparison("my-story", Today),
        };
        var report = ContentCheck.File(Write($"{folder}/my-story.json", text));

        Assert.True(report.IsUsable, string.Join('\n', report.Problems));
        Assert.Contains(report.Summary, entry => entry.Value == "my-story");
        Assert.Contains(report.Notices, notice => notice.Contains("still begin with REPLACE", StringComparison.Ordinal));
        Assert.Contains(report.Notices, notice => notice.Contains("example.org", StringComparison.Ordinal));
        Assert.Contains("\"2026-11-17\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_template_never_approves_itself_for_everybody()
    {
        foreach (var text in new[] { ContentTemplates.Collection("x", Today), ContentTemplates.Subject("x", Today), ContentTemplates.Comparison("x", Today) })
        {
            Assert.False(JsonNode.Parse(text)!["publication"]!["publicReleaseApproved"]!.GetValue<bool>());
        }
    }

    [Theory]
    [InlineData("My Story")]
    [InlineData("story_one")]
    [InlineData("-story")]
    [InlineData("")]
    public void A_template_needs_an_identifier_that_can_stand_in_an_address(string id)
    {
        Assert.Throws<ArgumentException>(() => ContentTemplates.Collection(id, Today));
        Assert.Throws<ArgumentException>(() => ContentTemplates.Comparison(id, Today));
    }

    // --- the vocabulary ---

    [Fact]
    public void The_vocabulary_names_every_fixed_set_with_the_values_the_readers_accept()
    {
        var types = ContentVocabulary.Collection.Single(choices => choices.Field == "relations[].type");
        var presence = ContentVocabulary.Collection.Single(choices => choices.Field == "events[].entityAssociations[].presence");
        var review = ContentVocabulary.Comparison.Single(choices => choices.Field.StartsWith("lanes[].periods[].reviewStatus", StringComparison.Ordinal));

        Assert.Equal(Core.RelationTypes.All.Select(type => type.Key).Order(StringComparer.Ordinal), types.Values.Select(value => value.Name));
        Assert.Equal("directed, from → to; a parent and a child in a family tree", types.Values.Single(value => value.Name == "parentOf").Note);
        Assert.Equal("symmetric; partners in a family tree", types.Values.Single(value => value.Name == "spouseOf").Note);
        Assert.Contains(presence.Values, value => value.Name == "mentioned_only");
        Assert.Equal(["proposed", "sourcedReviewPending", "reviewed", "disputed", "withdrawn"], review.Values.Select(value => value.Name));
        Assert.All(ContentVocabulary.Collection.Concat(ContentVocabulary.Comparison), choices =>
        {
            Assert.NotEmpty(choices.Values);
            Assert.False(string.IsNullOrWhiteSpace(choices.Meaning));
        });
    }

    [Fact]
    public void Every_value_of_the_vocabulary_of_a_collection_is_one_the_reader_accepts_in_its_field()
    {
        // The list is made from the reader; the test asks the reader the other way round, for the fields a
        // template has: each listed value in the place of the value the template has is not an unknown value.
        var places = new (string Field, Func<JsonObject, JsonNode> Parent, string Name)[]
        {
            ("entities[].type", sample => sample["entities"]![2]!, "type"),
            ("relations[].scope.kind", sample => sample["relations"]![0]!["scope"]!, "kind"),
            ("events[].entityAssociations[].presence", sample => sample["events"]![0]!["entityAssociations"]![0]!, "presence"),
            ("sources[].kind", sample => sample["sources"]![0]!, "kind"),
        };
        foreach (var (field, parent, name) in places)
        {
            foreach (var value in ContentVocabulary.Collection.Single(choices => choices.Field == field).Values)
            {
                var sample = JsonNode.Parse(ContentTemplates.Collection("small", Today))!.AsObject();
                parent(sample)[name] = value.Name;
                var report = ContentCheck.File(Write($"samples/{Guid.NewGuid():N}.json", sample.ToJsonString()));

                Assert.DoesNotContain(report.Problems, problem => problem.Contains("unknown value", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void The_pages_of_the_formats_name_every_value_the_readers_accept_and_the_guide_names_the_command()
    {
        // A value that is added to a reader is added to the page of its format in the same commit.
        var docs = Path.Combine(SampleFiles.App, "docs");
        var collection = File.ReadAllText(Path.Combine(docs, "collection-format.md"));
        var comparison = File.ReadAllText(Path.Combine(docs, "comparison-format.md"));
        var guide = File.ReadAllText(Path.Combine(docs, "content-guide.md"));

        Assert.All(ContentVocabulary.Collection.SelectMany(choices => choices.Values), value => Assert.Contains($"`{value.Name}`", collection, StringComparison.Ordinal));
        Assert.All(ContentVocabulary.Comparison.SelectMany(choices => choices.Values), value => Assert.Contains($"`{value.Name}`", comparison, StringComparison.Ordinal));
        Assert.All(new[] { "-- vocabulary", "-- template collection", "-- template subject", "-- template comparison", "-- check" }, command => Assert.Contains(command, guide, StringComparison.Ordinal));
        Assert.Contains(PilotSample.SchemaVersion, guide, StringComparison.Ordinal);
        Assert.Contains(ComparisonSample.FormatVersion, guide, StringComparison.Ordinal);
    }

    // --- the command ---

    [Fact]
    public void The_command_checks_a_folder_and_answers_with_its_exit_code()
    {
        Write("samples/good.json", ContentTemplates.Collection("good", Today));
        var (fine, text) = Run("check", _folder);
        Write("comparisons/bad.json", "{ \"formatVersion\": \"dastan-comparison-0.1\" }");
        var (refused, problems) = Run("check", _folder);

        Assert.Equal(ContentCommands.Done, fine);
        Assert.Contains("In order: the site can use this file.", text, StringComparison.Ordinal);
        Assert.Contains("Notice: good.json:", text, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUsable, refused);
        Assert.Contains("Not usable:", problems, StringComparison.Ordinal);
        Assert.Contains("In order: the site can use this file.", problems, StringComparison.Ordinal);
    }

    [Fact]
    public void Asked_for_data_the_command_answers_with_the_same_facts_as_JSON()
    {
        var good = Write("samples/good.json", ContentTemplates.Collection("good", Today));
        var bad = Write("samples/bad.json", "{ \"schemaVersion\": \"dastan-pilot-0.2\" }");

        var (exit, text) = Run("check", good, bad, "--json");
        using var answer = JsonDocument.Parse(text);
        var files = answer.RootElement.GetProperty("files");

        Assert.Equal(ContentCommands.NotUsable, exit);
        Assert.False(answer.RootElement.GetProperty("usable").GetBoolean());
        Assert.True(files[0].GetProperty("usable").GetBoolean());
        Assert.Equal("good", files[0].GetProperty("summary").GetProperty("collection").GetString());
        Assert.Equal(2, files[0].GetProperty("notices").GetArrayLength());
        Assert.False(files[1].GetProperty("usable").GetBoolean());
        Assert.True(files[1].GetProperty("problems").GetArrayLength() > 3);
    }

    [Fact]
    public void The_command_lists_the_vocabulary_for_reading_and_as_data()
    {
        var (exit, text) = Run("vocabulary", "collection");
        var (_, data) = Run("vocabulary", "--json");
        using var answer = JsonDocument.Parse(data);

        Assert.Equal(ContentCommands.Done, exit);
        Assert.Contains("relations[].type", text, StringComparison.Ordinal);
        Assert.Contains("parentOf  – directed, from → to; a parent and a child in a family tree", text, StringComparison.Ordinal);
        Assert.DoesNotContain("The file of a comparison", text, StringComparison.Ordinal);
        Assert.Equal(PilotSample.SchemaVersion, answer.RootElement.GetProperty("collection").GetProperty("format").GetString());
        Assert.Equal(ComparisonSample.FormatVersion, answer.RootElement.GetProperty("comparison").GetProperty("format").GetString());
        Assert.Equal(ContentVocabulary.Collection.Length, answer.RootElement.GetProperty("collection").GetProperty("fields").GetArrayLength());
    }

    [Fact]
    public void The_command_writes_a_template_to_a_new_file_and_never_over_one()
    {
        var file = Path.Combine(_folder, "comparisons", "two-lands.json");

        var (written, _) = Run("template", "comparison", "two-lands", "--out", file);
        var (again, refusal) = Run("template", "comparison", "two-lands", "--out", file);
        var (shown, text) = Run("template", "collection", "my-story");
        var (badName, _) = Run("template", "collection", "My Story");

        Assert.Equal(ContentCommands.Done, written);
        Assert.True(ContentCheck.File(file).IsUsable);
        Assert.Equal(ContentCommands.NotUnderstood, again);
        Assert.Contains("never written over a file", refusal, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, shown);
        Assert.Equal("my-story", JsonNode.Parse(text)!["datasetId"]!.GetValue<string>());
        Assert.Equal(ContentCommands.NotUnderstood, badName);
    }

    [Theory]
    [InlineData]
    [InlineData("check")]
    [InlineData("vocabulary", "media")]
    [InlineData("template", "collection")]
    [InlineData("publish", "everything")]
    public void A_command_that_is_not_understood_says_what_the_commands_are(params string[] arguments)
    {
        var (exit, text) = Run(arguments);

        Assert.Equal(ContentCommands.NotUnderstood, exit);
        Assert.Contains("check <file or folder>", text, StringComparison.Ordinal);
        Assert.Contains("content-guide.md", text, StringComparison.Ordinal);
    }
}
