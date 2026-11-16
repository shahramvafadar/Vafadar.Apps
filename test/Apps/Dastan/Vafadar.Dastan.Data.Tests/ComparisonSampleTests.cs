using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The comparison of the repository and the strictness of its reader. Tests read the file as it is, or change one
/// thing in a copy in memory and read that; the file itself is never written.
/// </summary>
public sealed class ComparisonSampleTests
{
    private static string Folder => Path.Combine(SampleFiles.Content, "comparisons");

    private static string File(string name = "iran-china-rome.json") => Path.Combine(Folder, name);

    private static JsonObject Sample() => JsonNode.Parse(System.IO.File.ReadAllText(File()))!.AsObject();

    private static JsonObject Translation() => JsonNode.Parse(System.IO.File.ReadAllText(File("iran-china-rome.fa.json")))!.AsObject();

    private static Comparison Read(Action<JsonObject>? change = null, Action<JsonObject>? translate = null)
    {
        var sample = Sample();
        change?.Invoke(sample);
        var translation = Translation();
        translate?.Invoke(translation);
        return ComparisonSample.Read(sample.ToJsonString(), [translation.ToJsonString()]);
    }

    private static IReadOnlyList<string> Problems(Action<JsonObject>? change = null, Action<JsonObject>? translate = null) =>
        Assert.Throws<SampleFormatException>(() => Read(change, translate)).Problems;

    private static JsonObject Lane(JsonObject sample, string key) =>
        sample["lanes"]!.AsArray().Select(node => node!.AsObject()).Single(lane => (string?)lane["key"] == key);

    private static JsonObject Item(JsonObject sample, string lane, string group, string key) =>
        Lane(sample, lane)[group]!.AsArray().Select(node => node!.AsObject()).Single(item => (string?)item["key"] == key);

    private static string Text(LocalizedText text, string language) => text.Resolve(language, "en").Value;

    [Fact]
    public void The_comparison_of_the_repository_is_read_with_its_translation()
    {
        var comparison = ComparisonSample.Load(File());

        Assert.Equal("iran-china-rome", comparison.Id.Value);
        Assert.Equal(ComparisonFrame.CommonEraYears, comparison.Frame);
        Assert.Equal("Iran, China and Rome side by side", Text(comparison.Title, "en"));
        Assert.Equal("ایران، چین و روم در کنار هم", Text(comparison.Title, "fa"));
        Assert.Equal(["china", "iran", "rome"], comparison.Lanes.Select(lane => lane.Key));
        Assert.Equal(["چین", "ایران", "روم"], comparison.Lanes.Select(lane => Text(lane.Label, "fa")));
        Assert.Equal([(6, 2), (7, 3), (5, 1)], comparison.Lanes.Select(lane => (lane.Periods.Length, lane.Events.Length)));
        Assert.Equal(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, 600), comparison.From);
        Assert.Equal(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, 700), comparison.To);
    }

    [Fact]
    public void The_comparison_of_the_repository_is_a_draft_and_says_so_in_every_part()
    {
        var comparison = ComparisonSample.Load(File());

        // Nothing of it is reviewed or sourced yet: it is shown in a demonstration only, and every statement and
        // every translated text carries that state. The day this changes, this test is changed with it.
        Assert.True(comparison.MayBeShownTo(Audience.LocalDemonstration));
        Assert.False(comparison.MayBeShownTo(Audience.Public));
        Assert.Empty(comparison.Sources);
        Assert.All(comparison.Lanes.SelectMany(lane => lane.Periods), period => Assert.Equal(ReviewStatus.Proposed, period.Review));
        Assert.All(comparison.Lanes.SelectMany(lane => lane.Events), single => Assert.Equal(ReviewStatus.Proposed, single.Review));
        Assert.Equal(TextReview.Draft, comparison.Title.Resolve("fa", "en").Review);
        Assert.All(
            comparison.Lanes.SelectMany(lane => lane.Periods.Select(period => period.Label).Concat(lane.Events.Select(single => single.Label)).Append(lane.Label)),
            label => Assert.Equal("fa", label.Resolve("fa", "en").Language));
    }

    [Fact]
    public void A_time_is_read_as_it_is_stated()
    {
        var comparison = ComparisonSample.Load(File());
        var iran = comparison.Lanes.Single(lane => lane.Key == "iran");
        var medes = iran.Periods.Single(period => period.Key == "medes");
        var seleucid = iran.Periods.Single(period => period.Key == "seleucid");

        // "About 678 BCE" stays "about"; an end that is only known to lie between two years stays between them.
        Assert.True(Assert.IsType<CalendarPosition>(medes.Time.Start.Earliest).IsApproximate);
        Assert.Equal(PeriodEndState.Between, seleucid.Time.End.State);
        Assert.Equal(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, 247), seleucid.Time.End.Earliest);
        Assert.Equal(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, 141), seleucid.Time.End.Latest);
        Assert.Equal(
            new Occurrence(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, 331)),
            iran.Events.Single(single => single.Key == "gaugamela").Time);
        Assert.NotNull(seleucid.Summary);
        Assert.Null(medes.Summary);
    }

    [Fact]
    public void Everything_of_the_comparison_can_be_placed_on_its_axis()
    {
        var chart = ComparisonCharts.Of(ComparisonSample.Load(File()), text => text.Resolve("en", "en").Value);

        Assert.Empty(chart.Unplaced);
        Assert.Equal([6, 7, 5], chart.Lanes.Select(lane => lane.Bands.Length));
        Assert.Equal([2, 3, 1], chart.Lanes.Select(lane => lane.Marks.Length));
    }

    [Fact]
    public void A_folder_holds_its_comparisons_and_a_folder_that_does_not_exist_holds_none()
    {
        // The translation beside the comparison is not a comparison of its own.
        Assert.Equal(["iran-china-rome"], ComparisonSample.LoadAll(Folder).Select(comparison => comparison.Id.Value));
        Assert.Empty(ComparisonSample.LoadAll(Path.Combine(Folder, "none")));
    }

    [Fact]
    public void A_comparison_without_a_translation_is_read_in_its_own_language()
    {
        var comparison = ComparisonSample.Read(Sample().ToJsonString());

        Assert.Equal("en", comparison.Title.Resolve("fa", "en").Language);
        Assert.Equal("China", Text(comparison.Lanes[0].Label, "fa"));
    }

    [Fact]
    public void A_source_of_a_comparison_is_read_and_can_be_cited()
    {
        var comparison = Read(sample =>
        {
            sample["sources"]!.AsArray().Add(new JsonObject
            {
                ["id"] = "source-reference",
                ["title"] = "A reference work",
                ["url"] = "https://example.org/reference",
                ["kind"] = "scholarlyReference",
                ["accessedAt"] = "2026-11-16",
                ["passageLocator"] = "chapter 3",
            });
            Item(sample, "iran", "periods", "sasanian")["sourceIds"] = new JsonArray("source-reference");
            Item(sample, "iran", "periods", "sasanian")["reviewStatus"] = "sourcedReviewPending";
        });

        var source = Assert.Single(comparison.Sources);
        var sasanian = comparison.Lanes[1].Periods.Single(period => period.Key == "sasanian");
        Assert.Equal(("A reference work", SourceKind.ScholarlyReference, new DateOnly(2026, 11, 16), "chapter 3"), (source.Title, source.Kind, source.AccessedOn, source.PassageLocator));
        Assert.Equal([source.Id], sasanian.Sources);
        Assert.Equal(ReviewStatus.SourcedReviewPending, sasanian.Review);
    }

    [Fact]
    public void A_file_of_another_format_is_refused()
    {
        Assert.Contains("formatVersion is \"dastan-comparison-9\"", Assert.Single(Problems(sample => sample["formatVersion"] = "dastan-comparison-9")), StringComparison.Ordinal);
        Assert.Contains("this reader understands \"dastan-comparison-translation-0.1\"", Assert.Single(Problems(translate: translation => translation["formatVersion"] = "0.2")), StringComparison.Ordinal);
    }

    [Fact]
    public void A_field_the_reader_does_not_know_is_refused()
    {
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => sample["colour"] = "red")), StringComparison.Ordinal);
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => Lane(sample, "iran")["capital"] = "Ctesiphon")), StringComparison.Ordinal);
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => Item(sample, "iran", "events", "carrhae")["certain"] = true)), StringComparison.Ordinal);
        Assert.StartsWith("A translation cannot be read", Assert.Single(Problems(translate: translation => translation["translator"] = "somebody")), StringComparison.Ordinal);
    }

    [Fact]
    public void A_field_that_is_missing_is_refused()
    {
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => sample.Remove("publication"))), StringComparison.Ordinal);
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => Item(sample, "rome", "periods", "roman-republic").Remove("reviewStatus"))), StringComparison.Ordinal);
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => Item(sample, "rome", "periods", "roman-republic").Remove("sourceIds"))), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("frame", "solarHijriYears", "frame has the unknown value \"solarHijriYears\".")]
    [InlineData("frame", "0", "frame has the unknown value \"0\".")]
    [InlineData("status", "published", "status has the unknown value \"published\".")]
    public void A_name_the_reader_has_no_meaning_for_is_refused(string field, string value, string problem)
    {
        Assert.Equal([problem], Problems(sample => sample[field] = value));
    }

    [Fact]
    public void A_review_state_the_reader_does_not_know_is_refused()
    {
        Assert.Equal(
            ["reviewStatus of \"han\" has the unknown value \"certain\".", "reviewStatus of \"trajan\" has the unknown value \"2\"."],
            Problems(sample =>
            {
                Item(sample, "china", "periods", "han")["reviewStatus"] = "certain";
                Item(sample, "rome", "events", "trajan")["reviewStatus"] = "2";
            }));
    }

    [Fact]
    public void A_time_that_cannot_be_read_is_refused_and_a_period_is_a_period()
    {
        // A year zero does not exist; a state of an end that nobody defined is not one.
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => Item(sample, "iran", "events", "carrhae")["time"]!["at"]!["year"] = 0)), StringComparison.Ordinal);
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => Item(sample, "china", "periods", "tang")["time"]!["end"]!["state"] = "sometime")), StringComparison.Ordinal);
        Assert.StartsWith("The comparison cannot be read", Assert.Single(Problems(sample => sample["show"]!["from"]!["era"] = "BCE")), StringComparison.Ordinal);

        // An event may be a single time; a period lasts.
        Assert.Equal(
            ["The time of the period \"qin\" is not a period."],
            Problems(sample => Item(sample, "china", "periods", "qin")["time"] = Item(sample, "china", "events", "gan-ying")["time"]!.DeepClone()));
    }

    [Fact]
    public void A_source_needs_a_protected_address_a_day_and_a_kind()
    {
        static JsonObject Source(string url = "https://example.org/a", string kind = "museumRecord", string accessed = "2026-11-16") => new()
        {
            ["id"] = "source-a",
            ["title"] = "A",
            ["url"] = url,
            ["kind"] = kind,
            ["accessedAt"] = accessed,
        };

        Assert.Equal(["url of the source \"source-a\" is not an https address."], Problems(sample => sample["sources"]!.AsArray().Add(Source(url: "http://example.org/a"))));
        Assert.Equal(["url of the source \"source-a\" is not an https address."], Problems(sample => sample["sources"]!.AsArray().Add(Source(url: "javascript:alert(1)"))));
        Assert.Equal(["accessedAt of the source \"source-a\" is not a day written as yyyy-MM-dd."], Problems(sample => sample["sources"]!.AsArray().Add(Source(accessed: "16.11.2026"))));
        Assert.Equal(["kind of the source \"source-a\" has the unknown value \"blog\"."], Problems(sample => sample["sources"]!.AsArray().Add(Source(kind: "blog"))));
    }

    [Fact]
    public void What_does_not_hold_together_is_refused()
    {
        // A key that is used twice – read without the translation, which would miss the key that was changed.
        var twice = Sample();
        Item(twice, "iran", "periods", "medes")["key"] = "qin";
        Assert.Contains("\"qin\"", Assert.Single(Assert.Throws<SampleFormatException>(() => ComparisonSample.Read(twice.ToJsonString())).Problems), StringComparison.Ordinal);

        Assert.Contains("source-nowhere", Assert.Single(Problems(sample => Item(sample, "iran", "periods", "medes")["sourceIds"] = new JsonArray("source-nowhere"))), StringComparison.Ordinal);
        var empty = Sample();
        empty["lanes"] = new JsonArray();
        Assert.Contains("at least one lane", Assert.Single(Assert.Throws<SampleFormatException>(() => ComparisonSample.Read(empty.ToJsonString())).Problems), StringComparison.Ordinal);
        Assert.Contains("ends after it begins", Assert.Single(Problems(sample => sample["show"]!["to"] = sample["show"]!["from"]!.DeepClone())), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Iran-China")]
    [InlineData("iran china")]
    [InlineData("iran/china")]
    [InlineData("iran--china")]
    [InlineData("-iran")]
    [InlineData("ایران")]
    public void An_identifier_and_a_key_are_names_for_an_address(string name)
    {
        var problem = $"\"{name}\" is not a name for an address: lower-case letters, digits and single hyphens between them.";

        var named = Sample();
        named["id"] = name;
        Assert.Equal([problem], Assert.Throws<SampleFormatException>(() => ComparisonSample.Read(named.ToJsonString())).Problems);

        var keyed = Sample();
        Item(keyed, "rome", "events", "trajan")["key"] = name;
        Assert.Equal([problem], Assert.Throws<SampleFormatException>(() => ComparisonSample.Read(keyed.ToJsonString())).Problems);

        var laned = Sample();
        Lane(laned, "rome")["key"] = name;
        Assert.Equal([problem], Assert.Throws<SampleFormatException>(() => ComparisonSample.Read(laned.ToJsonString())).Problems);
    }

    [Fact]
    public void A_translation_belongs_to_its_comparison_and_translates_what_exists()
    {
        Assert.Equal(["The translation \"fa\" is for \"another\", not for \"iran-china-rome\"."], Problems(translate: translation => translation["id"] = "another"));
        Assert.Equal(["The translation \"fa\" names the lane \"india\", which the comparison does not have."], Problems(translate: translation => translation["lanes"]!["india"] = "هند"));
        Assert.Equal(
            ["The translation \"fa\" names \"maurya\", which the comparison does not have."],
            Problems(translate: translation => translation["items"]!["maurya"] = new JsonObject { ["label"] = "موریا" }));
        Assert.Equal(["The translation \"en\": language is not a language tag of a translation."], Problems(translate: translation => translation["language"] = "en"));
        Assert.Equal(["The translation \"fa\": review has the unknown value \"final\"."], Problems(translate: translation => translation["review"] = "final"));
    }

    [Fact]
    public void A_reviewed_translation_says_so_and_a_text_without_a_translation_keeps_its_language()
    {
        var comparison = Read(translate: translation =>
        {
            translation["review"] = "reviewed";
            translation["items"]!.AsObject().Remove("qin");
        });

        Assert.Equal(TextReview.Reviewed, comparison.Title.Resolve("fa", "en").Review);
        Assert.Equal(("en", "Qin"), (comparison.Lanes[0].Periods[1].Label.Resolve("fa", "en").Language, comparison.Lanes[0].Periods[1].Label.Resolve("fa", "en").Value));
    }

    [Fact]
    public void A_refusal_of_a_file_names_the_file()
    {
        var folder = Directory.CreateTempSubdirectory("dastan-comparison-");
        try
        {
            var sample = Sample();
            sample["status"] = "final";
            System.IO.File.WriteAllText(Path.Combine(folder.FullName, "broken.json"), sample.ToJsonString());

            var refused = Assert.Throws<SampleFormatException>(() => ComparisonSample.LoadAll(folder.FullName));

            Assert.Equal(["broken.json: status has the unknown value \"final\"."], refused.Problems);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
