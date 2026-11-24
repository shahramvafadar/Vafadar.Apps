using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The site started on the library of the repository, as a local demonstration.</summary>
public sealed class LibrarySite : DemoSite
{
    /// <summary>Gets the folder of the library in the repository.</summary>
    public static string Library { get; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan", "library"));

    /// <summary>Gets the files of the collections of the library, in the order of their names.</summary>
    public static IReadOnlyList<string> Collections { get; } = Directory.Exists(Path.Combine(Library, "samples"))
        ? [.. ContentCheck.FilesOf(Path.Combine(Library, "samples"))]
        : [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", Library);
        builder.UseSetting("Dastan:Sample", string.Empty);
    }
}

/// <summary>A site that is told a folder and no collection to begin with. Test data.</summary>
public sealed class UnnamedFirstSite : DemoSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-unnamed-" + Guid.NewGuid().ToString("N"), "content");

    /// <summary>Gets or sets a value indicating whether the folder holds collections.</summary>
    public bool Written { get; init; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", _content);
        builder.UseSetting("Dastan:Sample", string.Empty);
        if (Directory.Exists(_content))
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(_content, "samples"));
        foreach (var name in Written ? new[] { "b-second", "a-first" } : [])
        {
            File.WriteAllText(Path.Combine(_content, "samples", name + ".json"), ContentTemplates.Subject(name, new DateOnly(2026, 11, 23)));
        }
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
/// The site on the library (delivery plan, 25.1): a file that passes the check and breaks a page is found
/// here. With a library that holds no collection yet there is nothing to ask for.
/// </summary>
public sealed class LibrarySiteTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task EveryCollectionOfTheLibraryHasItsPages(string language)
    {
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        using var site = new LibrarySite();
        var visitor = site.Visitor();
        var library = site.Services.GetRequiredService<DastanLibrary>();
        Assert.Equal(LibrarySite.Collections.Count, library.Collections.Length);

        foreach (var collection in library.Collections.Select(each => each.Collection))
        {
            foreach (var address in new[] { $"/{language}/{collection}", $"/{language}/{collection}/entity/{collection}", $"/{language}/{collection}/timeline", $"/{language}/{collection}/gallery" })
            {
                var page = await visitor.Get(address);
                Assert.True(page.Status == HttpStatusCode.OK, $"{address} answered {(int)page.Status}.");
            }
        }

        // The comparisons of the library, and what the collections of the site share.
        foreach (var address in library.Comparisons.Select(comparison => $"/{language}/compare/{comparison.Id}").Append($"/{language}/themes").Append($"/{language}/search?q=a"))
        {
            var page = await visitor.Get(address);
            Assert.True(page.Status == HttpStatusCode.OK, $"{address} answered {(int)page.Status}.");
        }
    }

    [Fact]
    public async Task ACollectionOfTheLibraryPlacesItsNamesInTimeByWhatTheirRecordsState()
    {
        // The collections of the library say when somebody lived and when a battle was fought as facts of the
        // record (D-098): every such name stands in the chart of time, and a year names who lived in it.
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        using var site = new LibrarySite();
        var visitor = site.Visitor();
        var library = site.Services.GetRequiredService<DastanLibrary>();
        var placed = 0;
        foreach (var collection in library.Collections)
        {
            var read = collection.At(new ReadingLevel(0))!;
            var stated = read.Entities.Where(RecordTimes.Any).ToList();
            var subjects = read.Timeline().Subjects.Select(subject => subject.Id).ToHashSet();
            Assert.All(stated, entity => Assert.Contains(entity.Id, subjects));
            placed += stated.Count;

            // The chart lays its lanes as the time went: no lane begins before the one above it.
            var view = read.Timeline();
            var begins = view.Subjects.Select(subject => TimelineLanes.Earliest(subject, view)).OfType<decimal>().Order().ToList();
            var chartOf = await visitor.Get($"/en/{collection.Collection}/timeline?by=names");
            var places = view.Subjects.Where(subject => TimelineLanes.Earliest(subject, view) is not null)
                .OrderBy(subject => TimelineLanes.Earliest(subject, view)).Select(subject => chartOf.Html.IndexOf($"\"{subject.Id.Value}\"", StringComparison.Ordinal)).Where(place => place >= 0).ToList();
            Assert.Equal(places.Order(), places);
            Assert.Equal(begins.Count, view.Subjects.Count(subject => TimelineLanes.Earliest(subject, view) is not null));

            if (stated.FirstOrDefault(entity => RecordTimes.Of(entity).Any(time => time.IsSpan)) is not { } lived)
            {
                continue;
            }

            // The middle of the first stretch the collection states: the page of that year names its owner.
            var reach = YearCover.Reach(RecordTimes.Of(lived).First(time => time.IsSpan).Time)!.Value;
            var year = YearOfEra.At((reach.From + reach.To) / 2);
            Assert.Contains(read.At(year).Records, record => record.Entity.Id == lived.Id);
            var page = await visitor.Get($"/en/{collection.Collection}/at?year={year.Number}{(year.Era == Vafadar.Temporal.Era.BeforeCommonEra ? "&era=bce" : null)}");
            Assert.Equal(HttpStatusCode.OK, page.Status);
            Assert.Contains("id=\"records\"", page.Html, StringComparison.Ordinal);

            // And the chart of time of the collection draws that stretch in the lane of the name.
            var chart = await visitor.Get($"/en/{collection.Collection}/timeline?by=names");
            Assert.Contains(RecordTimes.Of(lived).First(time => time.IsSpan).Key, chart.Html, StringComparison.Ordinal);
            Assert.Contains($"{collection.Collection}/entity/{lived.Id.Value}\"", page.Html, StringComparison.Ordinal);
        }

        Assert.True(placed > 0, "No collection of the library states a time among its facts.");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task AListOfAPageOfACollectionShowsAFewAndNoStoryLeadsToAnEmptyPage(string language)
    {
        // A work of seventy stories and a hundred names: a list shows its first twelve cards and folds the others
        // into the same page; a story the collection only names leads to what is known of it, and its page of
        // steps – an address somebody may still hold – leads there too.
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        using var site = new LibrarySite();
        var visitor = site.Visitor();
        var library = site.Services.GetRequiredService<DastanLibrary>();
        foreach (var collection in library.Collections)
        {
            var projection = collection.At(new ReadingLevel(0))!;
            var overview = projection.Overview()!;
            var page = (await visitor.Get($"/{language}/{collection.Collection}")).Html;

            // Outside the folds no list has more than twelve cards.
            var open = System.Text.RegularExpressions.Regex.Replace(page, "<details class=\"fold more-cards\">.*?</details>", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);
            foreach (System.Text.RegularExpressions.Match list in System.Text.RegularExpressions.Regex.Matches(open, "<ul class=\"cards\" role=\"list\">.*?</ul>", System.Text.RegularExpressions.RegexOptions.Singleline))
            {
                Assert.True(System.Text.RegularExpressions.Regex.Matches(list.Value, "<li class=\"card\">").Count <= 12, $"A list of {collection.Collection} shows more than twelve cards.");
            }

            Assert.Equal(overview.Subjects.Length > 12 || overview.Stories.Length > 12, page.Contains("more-cards", StringComparison.Ordinal));
            foreach (var subject in overview.Subjects)
            {
                Assert.Contains($"href=\"{collection.Collection}/entity/{subject.Id}\"", page, StringComparison.Ordinal);
            }

            if (overview.Stories.Length <= 1)
            {
                continue;
            }

            foreach (var story in overview.Stories)
            {
                var steps = projection.Story(story.Id)!.Steps;
                var told = story.Id == overview.Stories[0].Id ? $"{collection.Collection}/story" : $"{collection.Collection}/story/{story.Id}";
                if (!steps.IsEmpty)
                {
                    Assert.Contains($"href=\"{told}\"", page, StringComparison.Ordinal);
                    continue;
                }

                Assert.DoesNotContain($"href=\"{told}\"", page, StringComparison.Ordinal);
            }

            var named = overview.Stories.FirstOrDefault(story => projection.Story(story.Id)!.Steps.IsEmpty);
            if (named is not null)
            {
                var empty = (await visitor.Get($"/{language}/{collection.Collection}/story/{named.Id}")).Html;
                Assert.Contains($"class=\"action\" href=\"{collection.Collection}/entity/{named.Id}\"", empty, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task TheViewsOfACollectionSayWhatTheyAreAndEveryCollectionOfTheStartPageHasItsSign(string language)
    {
        // A page somebody is sent – the story, the chart of time, the map, a year, the pictures – says in its
        // head what it is; on the start page every collection has the drawing of its kind, not the first alone.
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        using var site = new LibrarySite();
        var visitor = site.Visitor();
        var library = site.Services.GetRequiredService<DastanLibrary>();
        foreach (var collection in library.Collections)
        {
            var projection = collection.At(new ReadingLevel(0))!;
            var views = new List<string> { "timeline", "gallery" };
            if (projection.Story() is not null)
            {
                views.Add("story");
            }

            if (projection.Years() is not null)
            {
                views.Add("at");
            }

            if (!projection.Places().IsEmpty)
            {
                views.Add("places");
            }

            foreach (var view in views)
            {
                var page = (await visitor.Get($"/{language}/{collection.Collection}/{view}")).Html;
                var described = System.Text.RegularExpressions.Regex.Match(page, "<meta name=\"description\" content=\"([^\"]*)\"");
                Assert.True(described.Success && described.Groups[1].Value.Length > 20, $"The page {view} of {collection.Collection} does not say what it is.");
            }
        }

        var start = (await visitor.Get($"/{language}/")).Html;
        var cards = System.Text.RegularExpressions.Regex.Matches(start, "<article class=\"card feature\">(.*?)</article>", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.Equal(library.Collections.Count(), cards.Count);
        Assert.All(cards, card => Assert.Contains("class=\"profile-picture", card.Value, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AYearAskedOfACollectionLeadsToItsAnswerPastTheForms()
    {
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        using var site = new LibrarySite();
        var visitor = site.Visitor();
        var library = site.Services.GetRequiredService<DastanLibrary>();
        foreach (var collection in library.Collections)
        {
            if (collection.At(new ReadingLevel(0))!.Years() is not { } years)
            {
                continue;
            }

            var asked = $"/en/{collection.Collection}/at?year={years.First.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)}{(years.First.Era == Vafadar.Temporal.Era.BeforeCommonEra ? "&era=bce" : null)}";
            var page = (await visitor.Get(asked)).Html;
            Assert.Contains($"action=\"{collection.Collection}/at#answer\"", page, StringComparison.Ordinal);
            Assert.Contains("id=\"answer\"", page, StringComparison.Ordinal);
            Assert.DoesNotContain("id=\"answer\"", (await visitor.Get($"/en/{collection.Collection}/at")).Html, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task ACollectionOfManyNamesDrawsItsChartByKindAndEveryNameWhenAsked(string language)
    {
        // D-099: more than a dozen names with a time stand by kind, each kind one lane; a kind is put in and
        // taken out by a link, and a lane for each name is one link away.
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        using var site = new LibrarySite();
        var visitor = site.Visitor();
        var library = site.Services.GetRequiredService<DastanLibrary>();
        var many = 0;
        foreach (var collection in library.Collections)
        {
            var subjects = collection.At(new ReadingLevel(0))!.Timeline().Subjects;
            var chart = $"/{language}/{collection.Collection}/timeline";
            var page = (await visitor.Get(chart)).Html;
            if (subjects.Length <= 12)
            {
                Assert.DoesNotContain("id=\"kinds\"", page, StringComparison.Ordinal);
                continue;
            }

            many++;
            Assert.Contains("id=\"kinds\"", page, StringComparison.Ordinal);
            int LanesOf(string html) => System.Text.RegularExpressions.Regex.Matches(html, "<table class=\"chart-table\">").Count;

            // One kind is in the chart to begin with – far fewer lanes than names – and every name is one link away.
            Assert.Equal(1, LanesOf(page));
            Assert.Contains("by=names", page, StringComparison.Ordinal);

            // Every kind the collection has, asked for together: the page answers and names each of them once.
            var all = (await visitor.Get($"{chart}?open=spans,people,events,works,places,other")).Html;
            Assert.InRange(LanesOf(all), 2, 6);

            // What is asked for that is no kind is not kept by the links of the chart: the chart is the one without it.
            var odd = await visitor.Get($"{chart}?open=nothing,%22%3E");
            Assert.Equal(HttpStatusCode.OK, odd.Status);
            Assert.Equal(1, LanesOf(odd.Html));
            Assert.DoesNotContain("nothing", odd.Html.Split("id=\"kinds\"")[1].Split("<h2 id=\"as-table\"")[0], StringComparison.Ordinal);

            // A lane for each name: as many lanes as names, and the way back to the kinds.
            var names = (await visitor.Get($"{chart}?by=names")).Html;
            Assert.Equal(subjects.Length, LanesOf(names));
            Assert.Contains("id=\"kinds\"", names, StringComparison.Ordinal);
            Assert.DoesNotContain("open=", names.Split("id=\"kinds\"")[1].Split("</nav>")[0], StringComparison.Ordinal);
        }

        Assert.True(many > 0, "No collection of the library has many names with a time.");
    }

    [Fact]
    public void ASiteToldNoCollectionBeginsWithTheFirstFileOfTheFolder()
    {
        using var site = new UnnamedFirstSite();

        var library = site.Services.GetRequiredService<DastanLibrary>();

        Assert.Equal(["subject-a-first", "subject-b-second"], library.Collections.Select(each => each.Collection));
    }

    [Fact]
    public void ASiteToldNoCollectionAndAnEmptyFolderSaysWhatIsMissing()
    {
        using var site = new UnnamedFirstSite { Written = false };

        var refusal = Assert.ThrowsAny<Exception>(() => site.Services.GetRequiredService<DastanLibrary>());

        Assert.Contains("holds no collection", refusal.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheSiteHasAWayToStartOnTheLibrary()
    {
        var settings = File.ReadAllText(Path.Combine(LibrarySite.Library, "..", "Vafadar.Dastan.Web", "Properties", "launchSettings.json"));

        Assert.Contains("\"library\"", settings, StringComparison.Ordinal);
        Assert.Contains("../library", settings, StringComparison.Ordinal);
    }
}
