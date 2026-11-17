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
        entities[^1]!["names"] = JsonNode.Parse("""
            [
              { "name": "The sage of the tests", "language": "en", "kind": "title" },
              { "name": "حکیم آزمون", "language": "fa", "kind": "original_script" },
              { "name": "A younger poet", "language": "en", "kind": "former", "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 980 } } }
            ]
            """);
        entities[^1]!["identifiers"] = JsonNode.Parse("""
            [
              { "issuer": "A catalogue of the tests", "value": "T-1", "url": "https://catalogue.example/T-1", "match": "exact" },
              { "issuer": "Another catalogue", "value": "X 9", "match": "uncertain" }
            ]
            """);
        entities.Add(Entity("place-city", "Place", "A city of the tests"));

        entities[^1]!["position"] = JsonNode.Parse("""{ "latitude": 36.2972, "longitude": 59.6067, "precision": "approximate" }""");

        // What is stated of the city: how many lived there in a year, and how far it reaches, said for no year (step 12.7).
        entities[^1]!["dated"] = JsonNode.Parse("""
            [
              { "key": "city-people", "label": "People of the city", "summary": "Test data of the tests.",
                "quantity": { "value": 1234567, "unit": "people", "approximate": true },
                "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1000 } },
                "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0, "reviewStatus": "proposed" },
              { "key": "city-people-later", "label": "People of the city",
                "quantity": { "value": 2000000, "unit": "people" },
                "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1100 } },
                "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0, "reviewStatus": "proposed" },
              { "key": "city-area", "label": "Area of the city", "quantity": { "value": 12.5, "unit": "square_kilometres" },
                "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0, "reviewStatus": "disputed",
                "review": { "by": "an editor", "on": "2026-11-17", "note": "Two sources, two numbers." } },
              { "key": "city-tax", "label": "Tax of the city", "quantity": { "value": 5000, "unit": "money", "currency": "EUR" },
                "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0, "reviewStatus": "proposed" }
            ]
            """);
        entities.Add(Entity("region-land", "Region", "A land of the tests"));
        entities.Add(Entity("organisation-court", "Organisation", "A court of the tests"));
        var relations = sample["relations"]!.AsArray();
        relations.Add(Relation("world-01", "person-poet", "work-shahnameh", "authorOf", "Author of"));
        relations.Add(Relation("world-02", "person-poet", "place-city", "bornIn", "Born in"));
        relations.Add(Relation("world-03", "place-city", "region-land", "locatedIn", "Lies in"));
        relations.Add(Relation("world-04", "person-poet", "organisation-court", "memberOf", "Poet at"));

        // Two tenures at the court, each with its time, and the year the work was finished (step 12.2).
        relations[^1]!["time"] = JsonNode.Parse("""{ "kind": "period", "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 994 } }, "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 999 } }, "continuity": "notAsserted" }""");
        relations.Add(Relation("world-05", "person-poet", "organisation-court", "memberOf", "Poet again at"));
        relations[^1]!["time"] = JsonNode.Parse("""{ "kind": "period", "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 1004 } }, "end": { "state": "unknown" }, "continuity": "notAsserted" }""");
        relations.Single(relation => (string?)relation!["id"] == "world-01")!["time"] = JsonNode.Parse("""{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } }""");
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

    [Theory]
    [InlineData("en", "994 CE", "999 CE", "1010 CE")]
    [InlineData("fa", "۹۹۴", "۹۹۹", "۱۰۱۰")]
    public async Task A_relation_that_says_when_it_held_says_so_where_it_is_shown(string language, string from, string to, string once)
    {
        var poet = await site.Visitor().Get($"/{language}/{Story}/entity/person-poet/connections");
        var evidence = await site.Visitor().Get($"/{language}/{Story}/evidence/relation/world-04");

        Assert.Contains(from, poet.Main, StringComparison.Ordinal);
        Assert.Contains(to, poet.Main, StringComparison.Ordinal);
        Assert.Contains(once, poet.Main, StringComparison.Ordinal);
        Assert.Contains("class=\"when\"", poet.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, evidence.Status);
        Assert.Contains(from, evidence.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_timeline_has_a_lane_for_who_a_dated_relation_reads_from_with_a_bar_for_every_tenure()
    {
        var page = await site.Visitor().Get($"/en/{Story}/timeline");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("A poet of the tests", page.Text, StringComparison.Ordinal);
        Assert.Contains("Poet at A court of the tests", page.Text, StringComparison.Ordinal);
        Assert.Contains("Poet again at A court of the tests", page.Text, StringComparison.Ordinal);
        Assert.Contains("Author of", page.Text, StringComparison.Ordinal);

        // What has no time is not on the timeline: where the poet was born is said without a year.
        Assert.DoesNotContain("Born in", page.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Other names", "title", "earlier name", "980 CE", "In other catalogues", "not established that it is the same")]
    [InlineData("fa", "نام‌های دیگر", "لقب", "نام پیشین", "۹۸۰", "در فهرست‌های دیگر", "یکی‌بودنش ثابت نشده است")]
    public async Task The_page_of_an_entity_names_its_other_names_and_what_others_call_it(string language, string heading, string title, string former, string year, string catalogues, string uncertain)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}/entity/person-poet");

        Assert.Contains(heading, page.Main, StringComparison.Ordinal);
        Assert.Contains("The sage of the tests", page.Main, StringComparison.Ordinal);
        Assert.Contains("<span lang=\"fa\" dir=\"auto\">", page.Html, StringComparison.Ordinal);
        Assert.Contains("حکیم آزمون", page.Main, StringComparison.Ordinal);
        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(former, page.Main, StringComparison.Ordinal);
        Assert.Contains(year, page.Main, StringComparison.Ordinal);
        Assert.Contains(catalogues, page.Main, StringComparison.Ordinal);
        Assert.Contains("<a href=\"https://catalogue.example/T-1\" rel=\"external noopener noreferrer\">", page.Html, StringComparison.Ordinal);
        Assert.Contains(uncertain, page.Main, StringComparison.Ordinal);

        // An entity without other names says nothing about names.
        var city = await site.Visitor().Get($"/{language}/{Story}/entity/place-city");
        Assert.DoesNotContain(heading, city.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(catalogues, city.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("sage", "The sage of the tests")]
    [InlineData("حکیم", "حکیم آزمون")]
    public async Task The_search_of_the_site_finds_an_entity_by_another_name_and_says_by_which(string typed, string name)
    {
        var search = await site.Visitor().Get($"/en/{Story}/search?q={Uri.EscapeDataString(typed)}");

        Assert.Contains("A poet of the tests", search.Text, StringComparison.Ordinal);
        Assert.Contains(name, search.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "What is stated", "about 1,234,567 people", "1000 CE", "12.5 km²", "5,000 EUR")]
    [InlineData("fa", "آنچه گفته شده است", "حدود ۱٬۲۳۴٬۵۶۷ نفر", "۱۰۰۰", "۱۲٫۵ کیلومتر مربع", "۵٬۰۰۰ EUR")]
    public async Task The_page_of_an_entity_says_what_is_stated_about_it_with_every_digit(string language, string heading, string people, string year, string area, string tax)
    {
        var city = await site.Visitor().Get($"/{language}/{Story}/entity/place-city");
        var poet = await site.Visitor().Get($"/{language}/{Story}/entity/person-poet");
        var timeline = await site.Visitor().Get($"/{language}/{Story}/timeline");

        Assert.Contains(heading, city.Main, StringComparison.Ordinal);
        Assert.Contains("People of the city", city.Main, StringComparison.Ordinal);
        Assert.Contains(people, city.Main, StringComparison.Ordinal);
        Assert.Contains(year, city.Main, StringComparison.Ordinal);
        Assert.Contains(area, city.Main, StringComparison.Ordinal);
        Assert.Contains(tax, city.Main, StringComparison.Ordinal);

        // Somebody nothing is stated about has no such section; what has no time is not on the timeline.
        Assert.DoesNotContain(heading, poet.Main, StringComparison.Ordinal);
        Assert.Contains("People of the city", timeline.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Area of the city", timeline.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Where it is", "36.2972° N, 59.6067° E", "about there")]
    [InlineData("fa", "جای آن", "۳۶٫۲۹۷۲° شمالی، ۵۹٫۶۰۶۷° شرقی", "کم‌وبیش همان‌جا")]
    public async Task The_page_of_a_real_place_says_where_it_is_and_loads_no_map(string language, string title, string position, string precision)
    {
        var city = await site.Visitor().Get($"/{language}/{Story}/entity/place-city");
        var land = await site.Visitor().Get($"/{language}/{Story}/entity/region-land");

        Assert.Contains(title, city.Main, StringComparison.Ordinal);
        Assert.Contains(position, city.Main, StringComparison.Ordinal);
        Assert.Contains(precision, city.Main, StringComparison.Ordinal);
        Assert.Contains("href=\"geo:36.2972,59.6067\"", city.Html, StringComparison.Ordinal);

        // No map of anybody else is loaded: the page names no other site for it.
        Assert.DoesNotContain("openstreetmap", city.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("google", city.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(title, land.Main, StringComparison.Ordinal);
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
