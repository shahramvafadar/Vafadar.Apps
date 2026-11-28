using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Web.Components.Shared;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site on a copy of a collection and a comparison of the library with the list of kinds of the library
/// (ADR 0029): the library is classified, and a few records are given other kinds here, so that the pages have
/// a kind no other record has (a drought) and records of known kinds. Test data.
/// </summary>
public sealed class KindsSite : DemoSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-kinds-" + Guid.NewGuid().ToString("N"), "content");

    /// <summary>Gets the identifier and the name of the event of the chart that is given the kind battle.</summary>
    public (string Id, string Label) Battle { get; private set; }

    /// <summary>Gets the identifier and the name of the event of the chart that is given the kind drought.</summary>
    public (string Id, string Label) Drought { get; private set; }

    /// <summary>Gets the identifier and the name of the person of the list of names that is given the kind poet.</summary>
    public (string Id, string Label) Poet { get; private set; }

    /// <summary>Gets the identifier of another person of the list of names, given the kind religious.</summary>
    public string Priest { get; private set; } = string.Empty;

    /// <summary>Gets the name of the first period of the comparison, given the kind war, and of the second, given the kind era.</summary>
    public (string War, string Era) Periods { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", _content);
        builder.UseSetting("Dastan:Sample", "ancient-iran.json");
        if (Directory.Exists(_content))
        {
            return;
        }

        var library = LibrarySite.Library;
        foreach (var folder in new[] { "samples", "comparisons", "media" })
        {
            Directory.CreateDirectory(Path.Combine(_content, folder));
        }

        foreach (var file in new[] { "kinds.json", "kinds.fa.json", "samples/ancient-iran.json", "samples/ancient-iran.fa.json", "comparisons/iran-china-rome.json", "comparisons/iran-china-rome.fa.json" })
        {
            File.Copy(Path.Combine(library, file), Path.Combine(_content, file));
        }

        // The records given a kind are found in what the pages show: events of the chart and a person of the list.
        var original = PilotSample.Load(Path.Combine(library, "samples", "ancient-iran.json"));
        var projection = Projection.Of(original, ReadingLevel.Opening, Audience.LocalDemonstration)!;
        var events = projection.Timeline().Subjects.Where(entity => entity.Type == EntityTypes.Event).Take(2).ToList();
        var people = projection.Overview().Subjects.Where(entity => entity.Type == EntityTypes.Person).Take(2).ToList();
        string Label(Entity entity) => entity.Label.Resolve("en").Value;
        Battle = (events[0].Id.Value, Label(events[0]));
        Drought = (events[1].Id.Value, Label(events[1]));
        Poet = (people[0].Id.Value, Label(people[0]));
        Priest = people[1].Id.Value;

        var path = Path.Combine(_content, "samples", "ancient-iran.json");
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        foreach (var (id, kind) in new[] { (Battle.Id, "battle"), (Drought.Id, "drought"), (Poet.Id, "poet"), (Priest, "religious") })
        {
            sample["entities"]!.AsArray().Single(entity => (string?)entity!["id"] == id)!["kinds"] = new JsonArray(kind);
        }

        File.WriteAllText(path, sample.ToJsonString());

        var comparisonPath = Path.Combine(_content, "comparisons", "iran-china-rome.json");
        var comparison = JsonNode.Parse(File.ReadAllText(comparisonPath))!.AsObject();
        var periods = comparison["lanes"]![0]!["periods"]!.AsArray();
        periods[0]!["kinds"] = new JsonArray("war");
        periods[1]!["kinds"] = new JsonArray("era");
        Periods = ((string)periods[0]!["label"]!, (string)periods[1]!["label"]!);
        File.WriteAllText(comparisonPath, comparison.ToJsonString());
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
/// Kinds on the pages (ADR 0029): the page of a record says its kinds with their drawings, and the pages that
/// show many records narrow them by kind – a wider kind taking in those below it, the choice in the address.
/// </summary>
public sealed class KindPagesTests(KindsSite site) : IClassFixture<KindsSite>
{
    private const string Collection = "subject-ancient-iran";

    [Fact]
    public async Task The_page_of_a_record_says_its_kinds_with_their_drawing_in_both_languages()
    {
        _ = site.Server;
        var english = await site.Visitor().Get($"/en/{Collection}/entity/{site.Poet.Id}");
        var persian = await site.Visitor().Get($"/fa/{Collection}/entity/{site.Poet.Id}");

        Assert.Equal(HttpStatusCode.OK, english.Status);
        Assert.Contains("<span class=\"kinds\">", english.Html, StringComparison.Ordinal);
        Assert.Contains("Poet", english.Main, StringComparison.Ordinal);
        Assert.Contains("A person who wrote poems.", english.Main, StringComparison.Ordinal);
        Assert.Contains("شاعر", persian.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_list_of_names_is_narrowed_by_a_wider_kind_and_offers_only_kinds_it_has()
    {
        _ = site.Server;
        var all = await site.Visitor().Get($"/en/{Collection}");
        var narrowed = await site.Visitor().Get($"/en/{Collection}?{KindFilter.Parameter}=literary");

        Assert.Contains(site.Poet.Label, all.Main, StringComparison.Ordinal);
        Assert.Contains("kinds=poet#subject-kinds", all.Html, StringComparison.Ordinal);
        Assert.Contains("kinds=literary#subject-kinds", all.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("kinds=battle", all.Html, StringComparison.Ordinal);

        Assert.Contains(site.Poet.Label, narrowed.Main, StringComparison.Ordinal);
        Assert.Contains($"entity/{site.Priest}\"", all.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"entity/{site.Priest}\"", narrowed.Html, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"true\"", narrowed.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_timeline_is_narrowed_by_kind_and_its_links_keep_the_choice()
    {
        _ = site.Server;
        var wars = await site.Visitor().Get($"/en/{Collection}/timeline?{KindFilter.Parameter}=war");
        var nature = await site.Visitor().Get($"/en/{Collection}/timeline?{KindFilter.Parameter}=naturalEvent");
        var unknown = await site.Visitor().Get($"/en/{Collection}/timeline?{KindFilter.Parameter}=nothingOfTheList");

        Assert.Equal(HttpStatusCode.OK, wars.Status);
        Assert.Contains(site.Battle.Label, wars.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(site.Drought.Label, wars.Main, StringComparison.Ordinal);
        Assert.Contains(site.Drought.Label, nature.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(site.Battle.Label, nature.Main, StringComparison.Ordinal);
        Assert.Contains(wars.Links, link => link.Contains("view=", StringComparison.Ordinal) && link.Contains("kinds=war", StringComparison.Ordinal));

        // A kind that is no kind of the page is not asked for: the whole chart, and its choice says every kind.
        Assert.Contains("kinds=war#timeline-kinds", unknown.Html, StringComparison.Ordinal);
        Assert.Contains("kinds=naturalEvent#timeline-kinds", unknown.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_comparison_offers_only_its_kinds_and_shows_only_what_is_of_them()
    {
        _ = site.Server;
        var all = await site.Visitor().Get($"/en/compare/iran-china-rome");
        var wars = await site.Visitor().Get($"/en/compare/iran-china-rome?{KindFilter.Parameter}=war");

        Assert.Contains("kinds=war#comparison-kinds", all.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("kinds=demon", all.Html, StringComparison.Ordinal);
        Assert.Contains(site.Periods.Era, all.Main, StringComparison.Ordinal);
        Assert.Contains(site.Periods.War, wars.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(site.Periods.Era, wars.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_search_asks_for_kinds_with_boxes_and_finds_what_is_of_them()
    {
        _ = site.Server;
        var form = await site.Visitor().Get($"/en/{Collection}/search");
        var found = await site.Visitor().Get($"/en/{Collection}/search?{KindFilter.Parameter}=political&{KindFilter.Parameter}=literary");

        Assert.Contains("type=\"checkbox\" name=\"kinds\" value=\"poet\"", form.Html, StringComparison.Ordinal);
        Assert.Contains(site.Poet.Label, found.Main, StringComparison.Ordinal);
        Assert.DoesNotContain($"entity/{site.Priest}\"", found.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_kind_of_the_library_names_a_drawing_of_the_site()
    {
        var kinds = KindsFile.Load(LibrarySite.Library);

        Assert.NotEmpty(kinds.All);
        Assert.All(kinds.All, kind => Assert.Contains(kind.Icon, Icon.Names));
    }
}
