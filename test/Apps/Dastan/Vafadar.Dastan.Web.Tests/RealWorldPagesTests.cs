using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site with a copy of the collection that names, beside the story, a poet, the city of his birth and a
/// court – and says who wrote the work. The records are test data: the collection of the repository names
/// nothing of the world outside its story.
/// </summary>
public sealed class WorldSite : DemoSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-world-" + Guid.NewGuid().ToString("N"), "content");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", _content);
        if (Directory.Exists(_content))
        {
            return;
        }

        // The collection and, beside it, the documents of the editors it points to for reviewers.
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
        var entities = sample["entities"]!.AsArray();
        entities.Add(Entity("person-poet", "Person", "A poet of the tests"));
        entities.Add(Entity("place-city", "Place", "A city of the tests"));
        entities.Add(Entity("region-land", "Region", "A land of the tests"));
        entities.Add(Entity("organisation-court", "Organisation", "A court of the tests"));
        var relations = sample["relations"]!.AsArray();
        relations.Add(Relation("world-01", "person-poet", "work-shahnameh", "authorOf", "Author of"));
        relations.Add(Relation("world-02", "person-poet", "place-city", "bornIn", "Born in"));
        relations.Add(Relation("world-03", "place-city", "region-land", "locatedIn", "Lies in"));
        relations.Add(Relation("world-04", "person-poet", "organisation-court", "memberOf", "Poet at"));
        File.WriteAllText(path, sample.ToJsonString());
    }

    private static JsonObject Entity(string id, string type, string label) => new()
    {
        ["id"] = id, ["type"] = type, ["label"] = label, ["summary"] = "Test data of the tests.",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
    };

    private static JsonObject Relation(string id, string from, string to, string type, string label) => new()
    {
        ["id"] = id, ["from"] = from, ["to"] = to, ["type"] = type, ["label"] = label,
        ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
        ["reviewStatus"] = "source_backed_editorial_review_pending",
        ["scope"] = new JsonObject { ["kind"] = "outside_the_story", ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
        ["directionality"] = "directed",
    };

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
/// More than narrative things on the pages of the site (step 10.6): a real person, place, region and
/// organisation have their page, their kind in the language of the page, and their connections like anything else.
/// </summary>
public sealed class RealWorldPagesTests(WorldSite site) : IClassFixture<WorldSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "person-poet", "Person")]
    [InlineData("en", "place-city", "Place")]
    [InlineData("en", "region-land", "Region")]
    [InlineData("en", "organisation-court", "Organisation")]
    [InlineData("fa", "person-poet", "شخص")]
    [InlineData("fa", "place-city", "مکان")]
    [InlineData("fa", "region-land", "سرزمین")]
    [InlineData("fa", "organisation-court", "سازمان")]
    public async Task A_thing_of_the_world_has_its_page_with_its_kind_in_the_language_of_the_page(string language, string id, string kind)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}/entity/{id}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("of the tests", page.Text, StringComparison.Ordinal);
        Assert.Contains(kind, page.Main, StringComparison.Ordinal);

        // Never the key of the type in place of its name.
        Assert.DoesNotContain(">person<", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(">organisation<", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_is_said_of_a_person_stands_among_the_connections_of_the_person_and_of_the_work()
    {
        var poet = await site.Visitor().Get($"/en/{Story}/entity/person-poet/connections");
        var work = await site.Visitor().Get($"/en/{Story}/entity/work-shahnameh/connections");

        Assert.Equal(HttpStatusCode.OK, poet.Status);
        Assert.Contains("Author of", poet.Text, StringComparison.Ordinal);
        Assert.Contains("Born in", poet.Text, StringComparison.Ordinal);
        Assert.Contains("Poet at", poet.Text, StringComparison.Ordinal);
        Assert.Contains("A poet of the tests", work.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_person_has_no_family_drawing_from_what_is_no_family()
    {
        var family = await site.Visitor().Get($"/en/{Story}/entity/person-poet/family");
        var search = await site.Visitor().Get($"/en/{Story}/search?q=poet");

        // Being born somewhere and writing a work say nothing about a family.
        Assert.DoesNotContain("A city of the tests", family.Main, StringComparison.Ordinal);
        Assert.Contains("A poet of the tests", search.Text, StringComparison.Ordinal);
    }
}
