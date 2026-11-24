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
            var chartOf = await visitor.Get($"/en/{collection.Collection}/timeline");
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
            var chart = await visitor.Get($"/en/{collection.Collection}/timeline");
            Assert.Contains(RecordTimes.Of(lived).First(time => time.IsSpan).Key, chart.Html, StringComparison.Ordinal);
            Assert.Contains($"{collection.Collection}/entity/{lived.Id.Value}\"", page.Html, StringComparison.Ordinal);
        }

        Assert.True(placed > 0, "No collection of the library states a time among its facts.");
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
