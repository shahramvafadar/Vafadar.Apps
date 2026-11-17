using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site with a collection that tells no story: the template of the writers for a collection about an
/// organisation, written in Persian. Test data.
/// </summary>
public sealed class SubjectSite : DemoSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-subject-" + Guid.NewGuid().ToString("N"), "content");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", _content);
        builder.UseSetting("Dastan:Sample", "a-company.json");
        if (Directory.Exists(_content))
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(_content, "samples"));
        var sample = JsonNode.Parse(ContentTemplates.Subject("a-company", new DateOnly(2026, 11, 17)))!.AsObject();
        sample["language"] = "fa";
        sample["entities"]![0]!["label"] = "شرکت آزمون";
        sample["entities"]![1]!["label"] = "بنیان‌گذار آزمون";
        File.WriteAllText(Path.Combine(_content, "samples", "a-company.json"), sample.ToJsonString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_content))
        {
            Directory.Delete(Path.GetDirectoryName(_content)!, recursive: true);
        }
    }
}

/// <summary>The site with a copy of the collection that tells a second story of two steps. Test data.</summary>
public sealed class StoriesSite : DemoSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-stories-" + Guid.NewGuid().ToString("N"), "content");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", _content);
        if (Directory.Exists(_content))
        {
            return;
        }

        var app = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan"));
        var copies = Path.GetDirectoryName(_content)!;
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(copies, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy, overwrite: true);
            }
        }

        var path = Path.Combine(_content, "samples", "shahnameh-pilot.json");
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        sample["subjectId"] = "story-rostam-sohrab";
        sample["entities"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "story-second", ["type"] = "NarrativeArc", ["label"] = "A second story of the tests", ["summary"] = "Test data.",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
        });
        foreach (var storyEvent in sample["events"]!.AsArray())
        {
            storyEvent!["storyId"] = "story-rostam-sohrab";
        }

        for (var order = 1; order <= 2; order++)
        {
            sample["events"]!.AsArray().Add(new JsonObject
            {
                ["id"] = $"second-0{order}", ["storyId"] = "story-second", ["label"] = $"Second story, step {order}", ["summary"] = "Test data.",
                ["order"] = order, ["orderBasis"] = "narrative", ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
                ["temporalExpression"] = new JsonObject { ["kind"] = "narrative_position" },
                ["reviewStatus"] = "source_backed_editorial_review_pending",
                ["entityAssociations"] = new JsonArray(new JsonObject { ["entityId"] = "character-zal", ["role"] = "Test data", ["presence"] = "acts_in_selected_scenes" }),
            });
        }

        File.WriteAllText(path, sample.ToJsonString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_content))
        {
            Directory.Delete(Path.GetDirectoryName(_content)!, recursive: true);
        }
    }
}

/// <summary>
/// What a collection is about, on the pages of the site (step 12.4): a collection without a story, written in
/// Persian; and a collection with two stories.
/// </summary>
public sealed class CollectionSubjectPagesTests(SubjectSite company, StoriesSite stories) : IClassFixture<SubjectSite>, IClassFixture<StoriesSite>
{
    private const string Company = "subject-a-company";
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "Organisation")]
    [InlineData("fa", "سازمان")]
    public async Task A_collection_about_an_organisation_opens_with_it_and_offers_no_story(string language, string kind)
    {
        var page = await company.Visitor().Get($"/{language}/{Company}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("شرکت آزمون", page.Main, StringComparison.Ordinal);
        Assert.Contains(kind, page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain($"{Company}/story", page.Html, StringComparison.Ordinal);
        Assert.Contains($"{Company}/timeline", page.Html, StringComparison.Ordinal);
        Assert.Contains("بنیان‌گذار آزمون", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_collection_without_a_story_has_no_page_of_a_story_and_all_its_other_pages()
    {
        var visitor = company.Visitor();

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/{Company}/story")).Status);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get("/en/")).Status);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Company}/entity/person-first")).Status);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Company}/entity/person-first/connections")).Status);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Company}/evidence/relation/relation-01")).Status);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Company}/timeline")).Status);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Company}/gallery")).Status);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Company}/search?q=x")).Status);
    }

    [Fact]
    public async Task A_text_of_a_Persian_collection_is_marked_as_Persian_on_an_English_page_and_not_on_a_Persian_one()
    {
        var english = await company.Visitor().Get($"/en/{Company}/entity/person-first");
        var persian = await company.Visitor().Get($"/fa/{Company}/entity/person-first");

        // The name exists in Persian only; an English page shows it and says in which language it is.
        Assert.Contains("بنیان‌گذار آزمون", english.Main, StringComparison.Ordinal);
        Assert.Contains("lang=\"fa\"", english.Html[english.Html.IndexOf("<main", StringComparison.Ordinal)..], StringComparison.Ordinal);
        Assert.Contains("بنیان‌گذار آزمون", persian.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_collection_with_two_stories_names_both_and_each_has_its_page_and_its_steps()
    {
        var visitor = stories.Visitor();
        var collection = await visitor.Get($"/en/{Story}");
        var first = await visitor.Get($"/en/{Story}/story");
        var second = await visitor.Get($"/en/{Story}/story/story-second");

        Assert.Contains("The stories of this collection", collection.Main, StringComparison.Ordinal);
        Assert.Contains($"{Story}/story/story-second", collection.Links);
        Assert.Contains($"{Story}/story", collection.Links);

        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.DoesNotContain("Second story, step 1", first.Text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, second.Status);
        Assert.Contains("Second story, step 1", second.Text, StringComparison.Ordinal);
        Assert.Contains("Second story, step 2", second.Text, StringComparison.Ordinal);
        Assert.Contains("id=\"step-2\"", second.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"step-3\"", second.Html, StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/{Story}/story/character-zal")).Status);
    }

    [Fact]
    public async Task A_step_of_the_second_story_is_reached_from_who_takes_part_in_it()
    {
        var zal = await stories.Visitor().Get($"/en/{Story}/entity/character-zal");

        Assert.Contains($"{Story}/story/story-second#step-1", zal.Links);
        Assert.Equal(HttpStatusCode.OK, (await stories.Visitor().Get($"/en/{Story}/evidence/event/second-01")).Status);
    }
}
