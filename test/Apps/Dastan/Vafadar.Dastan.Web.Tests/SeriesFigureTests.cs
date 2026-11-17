using System.Net;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// A series of quantities on the page of its entity (step 14.3): the people of a city stated for two years are
/// drawn, and what is stated once is not. Test data.
/// </summary>
public sealed class SeriesFigureTests(WorldSite world) : IClassFixture<WorldSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "2 numbers, from 1000 CE to 1100 CE", "about 1,234,567 people", "2,000,000")]
    [InlineData("fa", "۲ عدد، از", "۱٬۲۳۴٬۵۶۷", "۲٬۰۰۰٬۰۰۰")]
    public async Task The_same_quantity_stated_for_several_years_is_drawn_under_its_list(string language, string caption, string first, string scale)
    {
        var page = await world.Visitor().Get($"/{language}/{Story}/entity/place-city");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("class=\"series-plot\"", page.Html, StringComparison.Ordinal);
        Assert.Contains(caption, page.Main, StringComparison.Ordinal);
        Assert.Contains(first, page.Main, StringComparison.Ordinal);
        Assert.Contains(scale, page.Main, StringComparison.Ordinal);

        // Each mark leads to the row of its statement; the one the source calls "about" is drawn open.
        Assert.Contains($"href=\"{Story}/entity/place-city#stated-city-people\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/place-city#stated-city-people-later\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("series-mark series-about", page.Html, StringComparison.Ordinal);
        Assert.Contains("id=\"stated-city-people-later\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task One_drawing_for_one_series_and_none_for_what_is_stated_once()
    {
        var city = await world.Visitor().Get($"/en/{Story}/entity/place-city");
        var poet = await world.Visitor().Get($"/en/{Story}/entity/person-poet");

        // The area and the tax of the city are stated once each: only the people are a series.
        Assert.Single(city.Html.Split("class=\"series-plot\"").Skip(1));
        Assert.Equal(2, city.Html.Split("class=\"series-mark").Length - 1);
        Assert.DoesNotContain("series-plot", poet.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", city.Own, StringComparison.Ordinal);
    }
}
