using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Site;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// Finding by kind, place and time, and a gallery in another order (step 14.5, R-PUB-04, MED-003). The records
/// of the world are test data: a poet born in a city that lies in a land, at a court from 994 to 999.
/// </summary>
public sealed class FindingTests(WorldSite world, DemoSite demo) : IClassFixture<WorldSite>, IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    private Projection Projection => world.Services.GetRequiredService<DastanLibrary>().Collections[0].At(ReadingLevel.Opening)!;

    [Fact]
    public void A_filter_alone_finds_everything_that_fits_and_nothing_alone_finds_nothing()
    {
        var projection = Projection;

        Assert.Empty(projection.Search(null, new SearchFilter(), 50));
        Assert.Equal(["place-city", "region-land"], projection.Search(null, new SearchFilter(Nature: EntityNature.Place), 50)
            .Where(hit => hit.Entity.Type == EntityTypes.Place || hit.Entity.Type == EntityTypes.Region).Select(hit => hit.Entity.Id.Value));
        Assert.All(projection.Search(null, new SearchFilter(Nature: EntityNature.Place), 50), hit => Assert.Equal(EntityNature.Place, hit.Entity.Type.Nature));
        Assert.All(projection.Search(null, new SearchFilter(Nature: EntityNature.Place), 50), hit => Assert.Empty(hit.Name));
    }

    [Fact]
    public void A_place_finds_what_is_connected_to_it_and_a_year_what_took_part_then()
    {
        var projection = Projection;
        var city = new EntityId("place-city");

        Assert.Equal(["person-poet", "place-city", "region-land"], projection.Search(null, new SearchFilter(Place: city), 50).Select(hit => hit.Entity.Id.Value).Order());
        Assert.Equal(["organisation-court", "person-poet"], projection.Search(null, new SearchFilter(Year: new YearOfEra(Era.CommonEra, 996)), 50).Select(hit => hit.Entity.Id.Value).Order());
        Assert.Equal(["place-city"], projection.Search(null, new SearchFilter(Year: new YearOfEra(Era.CommonEra, 1000)), 50).Select(hit => hit.Entity.Id.Value));
        Assert.Empty(projection.Search(null, new SearchFilter(Year: new YearOfEra(Era.CommonEra, 500)), 50));
    }

    [Fact]
    public void A_name_and_a_filter_must_both_fit()
    {
        var projection = Projection;

        Assert.Equal(["person-poet"], projection.Search("tests", new SearchFilter(Nature: EntityNature.Being), 50).Select(hit => hit.Entity.Id.Value));
        Assert.Equal(["place-city"], projection.Search("tests", new SearchFilter(Place: new EntityId("place-city"), Year: new YearOfEra(Era.CommonEra, 1000)), 50).Select(hit => hit.Entity.Id.Value));
        Assert.Empty(projection.Search("rostam", new SearchFilter(Nature: EntityNature.Place), 50));
    }

    [Theory]
    [InlineData("en", "What kind of thing", "At which place", "When", "People and beings")]
    [InlineData("fa", "چه گونه چیزی", "در کدام جا", "در چه زمانی", "کسان و جانداران")]
    public async Task The_page_of_the_search_asks_for_kind_place_and_year_in_the_language_of_the_page(string language, string kind, string place, string year, string beings)
    {
        var page = await world.Visitor().Get($"/{language}/{Story}/search");

        Assert.Contains(kind, page.Main, StringComparison.Ordinal);
        Assert.Contains(place, page.Main, StringComparison.Ordinal);
        Assert.Contains(year, page.Main, StringComparison.Ordinal);
        Assert.Contains(beings, page.Main, StringComparison.Ordinal);
        Assert.Contains("<option value=\"place-city\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_fits_the_choices_is_listed_without_a_name_and_the_choices_stay_chosen()
    {
        var page = await world.Visitor().Get($"/en/{Story}/search?kind=being&place=place-city");

        Assert.Contains("Found: 1", page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/person-poet\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"href=\"{Story}/entity/region-land\"", page.Html, StringComparison.Ordinal);
        Assert.Matches("<option value=\"being\" selected[^>]*>", page.Html);
        Assert.Matches("<option value=\"place-city\" selected[^>]*>", page.Html);
        Assert.DoesNotContain("Found under the name", page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?year=996", "A court of the tests")]
    [InlineData("?time=996", "A court of the tests")]
    [InlineData("?q=court&year=996", "A court of the tests")]
    public async Task A_year_finds_what_took_part_in_it(string query, string found)
    {
        var page = await world.Visitor().Get($"/en/{Story}/search{query}");

        Assert.Contains(found, page.Main, StringComparison.Ordinal);
        Assert.Contains("value=\"996\"", page.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("990 to 998", "990 to 998")]
    [InlineData("990-998 CE", "990 to 998")]
    [InlineData("۹۹۰ تا ۹۹۸", "990 to 998")]
    [InlineData("ago", null)]
    public async Task A_stretch_of_years_and_a_depth_of_time_find_what_took_part_in_them(string typed, string? kept)
    {
        var now = DateTime.UtcNow.Year;
        if (typed == "ago")
        {
            (typed, kept) = ($"{now - 998} to {now - 990} years ago", $"{now - 998} to {now - 990} years ago");
        }

        var page = await world.Visitor().Get($"/en/{Story}/search?time={Uri.EscapeDataString(typed)}");

        // The poet was at the court from 994 to 999: the court is found, and the time stays in the form as the
        // reader took it.
        Assert.Contains("A court of the tests", page.Main, StringComparison.Ordinal);
        Assert.Contains($"name=\"time\" value=\"{kept}\"", page.Html, StringComparison.Ordinal);

        // A time nothing is said of finds nothing, and what is no time is not asked for.
        Assert.DoesNotContain("A court of the tests", (await world.Visitor().Get($"/en/{Story}/search?time=55+Ma")).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("A court of the tests", (await world.Visitor().Get($"/en/{Story}/search?time=600+to+500+BCE")).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"400 to 300\"", (await world.Visitor().Get($"/en/{Story}/search?time=400+to+300")).Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?kind=%3Cb%3E")]
    [InlineData("?kind=nothing&place=nowhere&year=abc")]
    [InlineData("?place=character-rostam")]
    [InlineData("?year=0")]
    public async Task A_choice_that_is_none_is_not_asked_for_and_not_written_back(string query)
    {
        var page = await world.Visitor().Get($"/en/{Story}/search{query}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.DoesNotContain("Found:", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Nothing fits", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("selected", Regex.Match(page.Html, "<form class=\"search\".*?</form>", RegexOptions.Singleline).Value, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_story_alone_is_asked_for_a_kind_only_and_the_form_of_a_collection_stays_short()
    {
        var search = await demo.Visitor().Get($"/en/{Story}/search");
        var collection = await demo.Visitor().Get($"/en/{Story}");

        Assert.Contains("name=\"kind\"", search.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"place\"", search.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"year\"", search.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"kind\"", collection.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "oldest first", "By when each was made")]
    [InlineData("fa", "کهن‌ترین نخست", "به ترتیب زمان ساخته‌شدن")]
    public async Task A_gallery_is_put_in_the_order_of_the_making_and_says_which_time_that_is(string language, string choice, string basis)
    {
        var visitor = demo.Visitor();
        await visitor.ReadTo(4);
        var editorial = await visitor.Get($"/{language}/{Story}/gallery");
        var oldest = await visitor.Get($"/{language}/{Story}/gallery?order=oldest");
        var newest = await visitor.Get($"/{language}/{Story}/gallery?order=newest");

        Assert.Contains(choice, editorial.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(basis, editorial.Main, StringComparison.Ordinal);
        Assert.Contains(basis, oldest.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/gallery?order=oldest\" aria-current=\"true\"", oldest.Html, StringComparison.Ordinal);

        // The same items, and the two orders by time are each other's reverse where the times differ.
        static List<string> Items(Page page) => [.. Regex.Matches(page.Html, "<li class=\"card media-card\" id=\"([^\"]+)\"").Select(match => match.Groups[1].Value)];
        Assert.Equal(Items(editorial).Order(), Items(oldest).Order());
        Assert.Equal(Items(editorial).Order(), Items(newest).Order());
        Assert.NotEmpty(Items(oldest));
    }

    [Fact]
    public void The_order_of_the_making_puts_the_oldest_first_and_what_has_no_year_last()
    {
        var gallery = demo.Services.GetRequiredService<DastanLibrary>().Collections[0].At(ReadingLevel.Opening)!.Gallery();
        var oldest = gallery.Arranged(GalleryOrder.OldestFirst, null);
        var newest = gallery.Arranged(GalleryOrder.NewestFirst, null);

        var made = oldest.Items.Select(item => YearCover.Reach(item.Created)?.Middle).ToList();
        Assert.Equal(made.Where(year => year is not null).Order(), made.Where(year => year is not null));
        Assert.Equal(made.Count(year => year is not null), made.TakeWhile(year => year is not null).Count());
        Assert.Equal(GalleryOrder.NewestFirst, newest.Order);
        Assert.Equal(gallery.Items.Length, newest.Items.Length);
        Assert.All(gallery.Arranged(GalleryOrder.Editorial, MediaKind.Video).Items, item => Assert.Equal(MediaKind.Video, item.Kind));
        Assert.Equal(gallery.Items, gallery.Arranged(GalleryOrder.Editorial, null).Items);
    }

    [Theory]
    [InlineData("?kind=video", "Video")]
    [InlineData("?kind=image", "Image")]
    public async Task A_gallery_shows_images_or_videos_only_when_it_has_both(string query, string kind)
    {
        var visitor = demo.Visitor();
        await visitor.ReadTo(4);
        var all = await visitor.Get($"/en/{Story}/gallery");
        var page = await visitor.Get($"/en/{Story}/gallery{query}");

        // The gallery of a reader who has reached the end has both kinds, so both are offered.
        Assert.Contains($"href=\"{Story}/gallery?kind=video\"", all.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/gallery?kind=image\"", all.Html, StringComparison.Ordinal);

        var kinds = Regex.Matches(page.Html, "<li class=\"card media-card\" id=\"[^\"]+\">.*?<p class=\"meta\">([^<–]+)", RegexOptions.Singleline).Select(match => match.Groups[1].Value.Trim()).ToList();
        Assert.NotEmpty(kinds);
        Assert.All(kinds, shown => Assert.Equal(kind, shown));
    }

    [Fact]
    public async Task An_order_that_is_none_is_the_order_of_the_editors()
    {
        var page = await demo.Visitor().Get($"/en/{Story}/gallery?order=%3Cscript%3E&kind=other");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains($"href=\"{Story}/gallery\" aria-current=\"true\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("&lt;script", page.Html, StringComparison.Ordinal);
    }
}
