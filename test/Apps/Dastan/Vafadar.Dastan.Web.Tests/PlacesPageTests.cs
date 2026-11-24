using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The page of the places of a collection (step 14.1): the real places on a grid of degrees, the list that says
/// the same, and nothing loaded from anybody. The places are test data.
/// </summary>
public sealed class PlacesPageTests(WorldSite world, DemoSite demo) : IClassFixture<WorldSite>, IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "On the map", "on the map</h1>", "about there", "Places without a position", "36.2972° N, 59.6067° E")]
    [InlineData("fa", "روی نقشه", "روی نقشه</h1>", "کم‌وبیش همان‌جا", "جاهای بدون موقعیت", "شمالی")]
    public async Task A_collection_with_real_places_offers_their_page_in_the_language_of_the_visitor(string language, string link, string title, string about, string unmapped, string position)
    {
        var visitor = world.Visitor();
        var collection = await visitor.Get($"/{language}/{Story}");
        var page = await visitor.Get($"/{language}/{Story}/places");

        Assert.Contains($"href=\"{Story}/places\"", collection.Html, StringComparison.Ordinal);
        Assert.Contains(link, collection.Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(about, page.Main, StringComparison.Ordinal);
        Assert.Contains(unmapped, page.Main, StringComparison.Ordinal);
        Assert.Contains(position, page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_drawing_and_the_list_say_the_same()
    {
        var page = await world.Visitor().Get($"/en/{Story}/places");

        // The mark of the city leads to its row; the row leads to its page and says who was born there.
        Assert.Contains("class=\"places-map\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/places#place-place-city\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<title>1: A city of the tests</title>", page.Html, StringComparison.Ordinal);
        Assert.Contains("id=\"place-place-city\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/place-city\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("A poet of the tests", page.Main, StringComparison.Ordinal);
        Assert.Contains("href=\"geo:36.2972,59.6067\"", page.Html, StringComparison.Ordinal);

        // A position that is about there is drawn open; the land that states none is listed apart.
        Assert.Contains("map-mark map-about", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/region-land\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"place-region-land\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_coasts_come_with_the_site_and_lie_under_the_places()
    {
        // The coasts of the earth are in the program: thousands of lines, the Caspian Sea among them.
        var coasts = Vafadar.Dastan.Web.Site.CoastData.OfTheEarth;
        Assert.True(coasts.Count > 1000);
        var caspian = coasts.Within(46, 56, 36, 48, longitude => longitude, latitude => latitude);
        Assert.NotEmpty(caspian);
        Assert.All(caspian, path => Assert.Matches("^M[-0-9. ]+(L[-0-9. ]+)+$", path));

        // Far from the sea a river tells where the places are: the Amu Darya is among the waters.
        Assert.True(Vafadar.Dastan.Web.Site.CoastData.Waters.Count > 1000);
        Assert.NotEmpty(Vafadar.Dastan.Web.Site.CoastData.Waters.Within(58, 70, 33, 40, longitude => longitude, latitude => latitude));

        // What lies under the places says nothing a reader would have to hear.
        var page = await world.Visitor().Get($"/en/{Story}/places");
        Assert.Contains("coasts come with the site", page.Main, StringComparison.Ordinal);
        Assert.All(Regex.Matches(page.Html, "<svg class=\"map-coasts\"[^>]*>").Select(found => found.Value), tag => Assert.Contains("aria-hidden=\"true\"", tag, StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_page_loads_nothing_from_anybody()
    {
        var page = await world.Visitor().Get($"/en/{Story}/places");

        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"http", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("openstreetmap", page.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("en", "No real place can be shown here.")]
    [InlineData("fa", "هیچ جای واقعی‌ای را این‌جا نمی‌توان نشان داد.")]
    public async Task A_story_alone_offers_no_map_and_its_page_says_so(string language, string none)
    {
        var visitor = demo.Visitor();
        var collection = await visitor.Get($"/{language}/{Story}");
        var page = await visitor.Get($"/{language}/{Story}/places");

        Assert.DoesNotContain("/places\"", collection.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(none, page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("places-map", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Another_collection_has_no_such_page()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await world.Visitor().Get("/en/another-story/places")).Status);
    }
}
