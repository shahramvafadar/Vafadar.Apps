using System.Net;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The story read one step at a time (D-095): a step is an address, the reader sees where they are among the steps
/// they may read, and a step beyond the reading position is no step.
/// </summary>
public sealed class StoryReaderTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    [Fact]
    public async Task The_list_of_steps_offers_to_read_them_one_at_a_time()
    {
        var page = await site.Visitor().Get($"/en/{Story}/story");

        Assert.Contains("Read step by step", page.Main, StringComparison.Ordinal);
        Assert.Contains($"{Story}/story?step=1#reading", page.Links);
    }

    [Fact]
    public async Task A_step_is_read_on_its_own_with_where_it_stands_and_the_way_on_and_back()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);
        var first = await visitor.Get($"/en/{Story}/story?step=1");
        var second = await visitor.Get($"/en/{Story}/story?step=2");

        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.Contains("id=\"reading\"", first.Html, StringComparison.Ordinal);
        Assert.Matches("Step 1 of \\d+", first.Main);
        Assert.DoesNotContain("Previous step", first.Main, StringComparison.Ordinal);
        Assert.Contains($"{Story}/story?step=2#reading", first.Links);
        Assert.Contains($"{Story}/story?step=1#reading", second.Links);
        Assert.Contains("Next step", second.Main, StringComparison.Ordinal);
        Assert.Contains("All the steps", second.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_step_beyond_the_reading_position_is_no_step_and_the_list_is_shown()
    {
        var page = await site.Visitor().Get($"/en/{Story}/story?step=99");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.DoesNotContain("id=\"reading\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("Read step by step", page.Main, StringComparison.Ordinal);
    }
}
