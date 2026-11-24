using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>The site on the library of the repository: the collections that are written to be published.</summary>
public sealed class LibraryBrowserSite : BrowserSite
{
    /// <summary>Gets the folder of the library in the repository.</summary>
    public static string Library { get; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan", "library"));

    /// <summary>Gets a value indicating whether the library holds a collection.</summary>
    public static bool HasCollections { get; } = Directory.Exists(Path.Combine(Library, "samples")) && Directory.EnumerateFiles(Path.Combine(Library, "samples"), "*.json").Any();

    /// <inheritdoc />
    protected override void Settings(IWebHostBuilder builder)
    {
        if (HasCollections)
        {
            builder.UseSetting("Dastan:Content", Library);
            builder.UseSetting("Dastan:Sample", string.Empty);
        }
    }
}

/// <summary>
/// The chart of time of a collection of many names in a browser (D-099): where many names share a lane, no name
/// lies over another name, over its own mark or over the mark of another – at the width of a phone least of all.
/// </summary>
public sealed class LibraryTimelineBrowserTests(LibraryBrowserSite site) : IClassFixture<LibraryBrowserSite>
{
    // What lies over what among the marks of the chart and their names; a name written inside its bar is meant to.
    private const string Counted =
        """
        host => {
          const hit = (a, b) => a.left < b.right - 1 && b.left < a.right - 1 && a.top < b.bottom - 1 && b.top < a.bottom - 1;
          const links = [...host.querySelectorAll('svg a.tlv-mark')];
          const shapes = links.map(link => [...link.querySelectorAll('rect.tlv-bar, circle.tlv-point, rect.tlv-frame-of-window')].map(shape => shape.getBoundingClientRect()));
          const names = links.map(link => { const name = link.querySelector('.tlv-name'); return name && name.textContent && !name.classList.contains('tlv-name-inside') ? name.getBoundingClientRect() : null; });
          let overName = 0, overOwn = 0, overOther = 0;
          names.forEach((name, i) => {
            if (!name) return;
            names.forEach((other, j) => { if (other && j > i && hit(name, other)) overName++; });
            shapes.forEach((marks, j) => marks.forEach(mark => { if (hit(name, mark)) { if (i === j) overOwn++; else overOther++; } }));
          });
          return { marks: links.length, overName, overOwn, overOther };
        }
        """;

    [Theory]
    [InlineData("fa", 375)]
    [InlineData("en", 375)]
    [InlineData("fa", 1280)]
    public async Task Where_many_names_share_a_lane_no_name_lies_over_a_name_or_a_mark(string language, int width)
    {
        if (!LibraryBrowserSite.HasCollections)
        {
            return;
        }

        // The collection with the most names that have a time.
        var most = site.Services.GetRequiredService<DastanLibrary>().Collections
            .Select(collection => (collection.Collection, Names: collection.At(new ReadingLevel(0))?.Timeline().Subjects.Length ?? 0))
            .MaxBy(collection => collection.Names);
        if (most.Names <= 12)
        {
            return;
        }

        var page = await site.Visitor(width, graph: true);
        await page.GotoAsync($"/{language}/{most.Collection}/timeline?open=spans,people,events");
        var host = page.Locator("dastan-timeline");
        await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "live", new() { Timeout = 30_000 });

        var counted = await host.EvaluateAsync<JsonElement>(Counted);
        Assert.True(counted.GetProperty("marks").GetInt32() > 12, "The chart of the collection with the most names has few marks.");
        Assert.Equal(0, counted.GetProperty("overName").GetInt32());
        Assert.Equal(0, counted.GetProperty("overOwn").GetInt32());
        Assert.Equal(0, counted.GetProperty("overOther").GetInt32());
        Assert.False(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > window.innerWidth"));
    }
}

/// <summary>
/// The map of a collection at the width of a phone: its stage is as high as the map, and what it shows first is
/// where the places are – not the western edge of the map.
/// </summary>
public sealed class LibraryPlacesBrowserTests(LibraryBrowserSite site) : IClassFixture<LibraryBrowserSite>
{
    [Theory]
    [InlineData("fa")]
    [InlineData("en")]
    public async Task On_a_phone_a_map_is_as_high_as_the_map_and_begins_where_its_places_are(string language)
    {
        if (!LibraryBrowserSite.HasCollections)
        {
            return;
        }

        var seen = 0;
        foreach (var collection in site.Services.GetRequiredService<DastanLibrary>().Collections)
        {
            if (collection.At(new ReadingLevel(0))?.Places().IsEmpty != false)
            {
                continue;
            }

            var page = await site.Visitor(375);
            await page.GotoAsync($"/{language}/{collection.Collection}/places");
            var frame = page.Locator(".stage-frame");
            if (await frame.Locator(".map-mark").CountAsync() == 0)
            {
                await page.CloseAsync();
                continue;
            }

            seen++;
            var measured = await frame.EvaluateAsync<JsonElement>(
                """
                frame => {
                  const shown = frame.getBoundingClientRect();
                  const middles = [...frame.querySelectorAll('.map-mark')].map(mark => { const box = mark.getBoundingClientRect(); return box.left + box.width / 2; }).sort((a, b) => a - b);
                  const middle = middles[Math.floor(middles.length / 2)];
                  const room = frame.querySelector('.places-room').getBoundingClientRect();
                  return { inSight: middle >= shown.left && middle <= shown.right, spare: Math.round(shown.height - room.height), wide: document.documentElement.scrollWidth > window.innerWidth };
                }
                """);
            Assert.True(measured.GetProperty("inSight").GetBoolean(), $"The map of {collection.Collection} begins away from its places.");
            Assert.True(measured.GetProperty("spare").GetInt32() <= 24, $"The stage of the map of {collection.Collection} is higher than the map.");
            Assert.False(measured.GetProperty("wide").GetBoolean());
            await page.CloseAsync();
        }

        Assert.True(seen > 0, "No collection of the library has a map.");
    }
}
