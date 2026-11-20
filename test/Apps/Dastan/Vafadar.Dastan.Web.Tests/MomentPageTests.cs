using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Site;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// One year of a collection (step 14.2): what held then, what may have, and what is stated for that year. The
/// records are test data: a poet at a court from 994 to 999 and again from 1004 with no end stated, a work
/// finished in 1010, and the people of a city in the year 1000.
/// </summary>
public sealed class MomentPageTests(WorldSite world, DemoSite demo) : IClassFixture<WorldSite>, IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    [Fact]
    public void The_projection_names_the_years_something_is_stated_for()
    {
        var projection = world.Services.GetRequiredService<DastanLibrary>().Collections[0].At(ReadingLevel.Opening)!;
        var years = projection.Years()!;

        Assert.Equal((994, 1100), (years.First.Number, years.Last.Number));
        Assert.Equal([994, 999, 1000, 1004, 1010, 1100], years.Turns.Select(turn => turn.Number));

        var moment = projection.At(new YearOfEra(Era.CommonEra, 1000));
        Assert.Empty(moment.Relations);
        Assert.Equal("city-people", Assert.Single(moment.Stated).Statement.Key);
        Assert.Equal((999, 1004), (moment.Earlier!.Value.Number, moment.Later!.Value.Number));
        Assert.Null(projection.At(years.First).Earlier);
        Assert.Null(projection.At(years.Last).Later);
    }

    [Theory]
    [InlineData("en", "In a year", "A year of", "No year is chosen yet.", "Begin at 994 CE")]
    [InlineData("fa", "در یک سال", "یک سال از", "هنوز سالی برگزیده نشده است.", "آغاز از")]
    public async Task A_collection_that_states_years_offers_the_page_and_asks_for_a_year(string language, string link, string title, string choose, string first)
    {
        var visitor = world.Visitor();
        var collection = await visitor.Get($"/{language}/{Story}");
        var page = await visitor.Get($"/{language}/{Story}/at");

        Assert.Contains($"href=\"{Story}/at\"", collection.Html, StringComparison.Ordinal);
        Assert.Contains(link, collection.Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(choose, page.Main, StringComparison.Ordinal);
        Assert.Contains(first, page.Main, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Story}/at\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("name=\"year\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/at?year=994\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_lasted_through_a_year_held_in_it()
    {
        var page = await world.Visitor().Get($"/en/{Story}/at?year=996");

        Assert.Contains("in the year 996 CE", page.Main, StringComparison.Ordinal);
        Assert.Contains("Poet at", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Poet again at", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("may have held", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("People of the city", page.Main, StringComparison.Ordinal);

        // The steps lead to the years in which something changes.
        Assert.Contains($"href=\"{Story}/at?year=994\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/at?year=999\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("value=\"996\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_has_no_end_stated_only_may_have_held_later()
    {
        var page = await world.Visitor().Get($"/en/{Story}/at?year=1010");

        Assert.Contains("Poet again at", page.Main, StringComparison.Ordinal);
        Assert.Contains("may have held", page.Main, StringComparison.Ordinal);
        Assert.Contains("Author of", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Poet at", page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "People of the city", "about 1,234,567")]
    [InlineData("fa", "People of the city", "۱٬۲۳۴٬۵۶۷")]
    public async Task What_is_stated_for_a_year_is_named_with_its_quantity(string language, string label, string quantity)
    {
        var page = await world.Visitor().Get($"/{language}/{Story}/at?year=1000");

        Assert.Contains(label, page.Main, StringComparison.Ordinal);
        Assert.Contains(quantity, page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/place-city#stated-city-people\"", page.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Nothing is stated for this year.")]
    [InlineData("fa", "برای این سال چیزی گفته نشده است.")]
    public async Task A_year_nothing_is_stated_for_says_so_and_still_leads_on(string language, string nothing)
    {
        var page = await world.Visitor().Get($"/{language}/{Story}/at?year=500");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(nothing, page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/at?year=994\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_year_before_the_era_is_asked_for_with_its_era()
    {
        var page = await world.Visitor().Get($"/en/{Story}/at?year=500&era=bce");

        Assert.Contains("in the year 500 BCE", page.Main, StringComparison.Ordinal);
        Assert.Contains("Nothing is stated for this year.", page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("990 to 1000", "990 CE – 1000 CE")]
    [InlineData("990-1000", "990 CE – 1000 CE")]
    [InlineData("ago", "years ago")]
    public async Task A_stretch_of_years_and_a_depth_of_time_are_asked_for_as_a_year_is(string typed, string written)
    {
        // The same years as a depth of time, counted back from the year of the asking.
        var now = DateTime.UtcNow.Year;
        if (typed == "ago")
        {
            (typed, written) = ($"{now - 1000} to {now - 990} years ago", $"between {now - 1000} years ago and {now - 990} years ago");
        }

        var page = await world.Visitor().Get($"/en/{Story}/at?time={Uri.EscapeDataString(typed)}");

        // What held in a part of the stretch held in it: the poet at the court from 994 to 999, and the people
        // of the city in the year 1000.
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(written, page.Main, StringComparison.Ordinal);
        Assert.Contains("Poet at", page.Main, StringComparison.Ordinal);
        Assert.Contains("People of the city", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Poet again at", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Nothing is stated", page.Main, StringComparison.Ordinal);

        // The sources of a relation lead back to this view.
        Assert.Contains($"from={Uri.EscapeDataString($"{Story}/at?time=")}", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_time_is_typed_in_Persian_too_and_one_year_of_it_is_the_view_of_that_year()
    {
        var stretch = await world.Visitor().Get($"/fa/{Story}/at?time={Uri.EscapeDataString("۹۹۰ تا ۱۰۰۰")}");
        Assert.Contains("Poet at", stretch.Main, StringComparison.Ordinal);
        Assert.Contains("value=\"990 to 1000\"", stretch.Html, StringComparison.Ordinal);

        var year = await world.Visitor().Get($"/en/{Story}/at?time={Uri.EscapeDataString("996")}");
        Assert.Contains("in the year 996 CE", year.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/at?year=999\"", year.Html, StringComparison.Ordinal);

        var before = await world.Visitor().Get($"/en/{Story}/at?time={Uri.EscapeDataString("۵۰۰ پ.م")}");
        Assert.Contains("in the year 500 BCE", before.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("55 million years ago", "55 million years ago")]
    [InlineData("50-60 Ma", "between 50 million years ago and 60 million years ago")]
    [InlineData("12000 BCE to 11000 BCE", "12000 BCE – 11000 BCE")]
    [InlineData("4.5 Ga", "4.5 billion years ago")]
    public async Task A_time_the_collection_says_nothing_of_is_answered_with_nothing_and_not_with_an_error(string typed, string written)
    {
        var page = await world.Visitor().Get($"/en/{Story}/at?time={Uri.EscapeDataString(typed)}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(written, page.Main, StringComparison.Ordinal);
        Assert.Contains("Nothing is stated for this time.", page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("400 to 300")]
    [InlineData("early")]
    [InlineData("0")]
    [InlineData("-2147483648")]
    [InlineData("99999999999999999999")]
    [InlineData("200 Ga")]
    [InlineData("<script>alert(1)</script>")]
    public async Task What_is_no_time_is_said_to_be_none(string typed)
    {
        var page = await world.Visitor().Get($"/en/{Story}/at?time={Uri.EscapeDataString(typed)}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("That is no time", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("alert(1)", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Poet", page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?year=0")]
    [InlineData("?year=-5")]
    [InlineData("?year=abc")]
    [InlineData("?year=1000001")]
    [InlineData("?year=2147483648")]
    [InlineData("?year=1000&era=other")]
    [InlineData("?year=%3Cscript%3E")]
    public async Task What_is_no_year_is_said_to_be_none_and_nothing_of_it_is_written_back(string query)
    {
        var page = await world.Visitor().Get($"/en/{Story}/at{query}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("That is no year", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("in the year", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("script", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"" + Uri.UnescapeDataString(query[6..].Split("&")[0]).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal) + "\"", page.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Nothing with a year can be shown here.")]
    [InlineData("fa", "چیزی که سال داشته باشد این‌جا نمی‌توان نشان داد.")]
    public async Task A_story_alone_offers_no_year_and_its_page_says_so(string language, string none)
    {
        var visitor = demo.Visitor();
        var collection = await visitor.Get($"/{language}/{Story}");
        var page = await visitor.Get($"/{language}/{Story}/at?year=1000");

        Assert.DoesNotContain("/at\"", collection.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(none, page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"year\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Another_collection_has_no_such_page()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await world.Visitor().Get("/en/another-story/at?year=1000")).Status);
    }
}
