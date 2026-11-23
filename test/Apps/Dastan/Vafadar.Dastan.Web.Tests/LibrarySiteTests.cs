using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
