using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The states every view has to answer (document 18, section 5), as far as other tests do not cover them: an
/// address that leads nowhere, a failure, a view that cannot show an entity.
/// </summary>
public sealed class StatesTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task What_is_not_read_yet_and_what_does_not_exist_get_the_same_page_with_the_same_ways_on(string language)
    {
        var visitor = site.Visitor();

        var hidden = await visitor.Get($"/{language}/{Story}/entity/character-sohrab");
        var unknown = await visitor.Get($"/{language}/{Story}/entity/character-nobody");
        var nowhere = await visitor.Get($"/{language}/no/such/page");

        Assert.All(new[] { hidden, unknown, nowhere }, page => Assert.Equal(HttpStatusCode.NotFound, page.Status));
        Assert.Equal(unknown.Main, hidden.Main);
        Assert.Equal(unknown.Main, nowhere.Main);

        // The start, the story and the reading position in general – nothing that hints at a later part.
        Assert.Contains("./", hidden.Links);
        Assert.Contains(Story, hidden.Links);
        Assert.Contains($"{Story}#reading", hidden.Links);
        Assert.DoesNotContain("Sohrab", hidden.Main.Replace("Rostam and Sohrab", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_view_that_has_nothing_to_show_for_an_entity_says_why_and_offers_the_one_that_has()
    {
        var page = await site.Visitor().Get($"/en/{Story}/entity/character-rakhsh/family");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("No family relations to show.", page.Main, StringComparison.Ordinal);
        Assert.Contains("Other connections are in the drawing of all connections.", page.Main, StringComparison.Ordinal);
        Assert.Contains($"{Story}/entity/character-rakhsh/connections", page.Links);
        Assert.Contains($"{Story}/entity/character-rakhsh", page.Links);
    }

    [Fact]
    public async Task The_story_says_that_it_is_not_a_calendar_instead_of_offering_one()
    {
        var english = await site.Visitor().Get($"/en/{Story}/story");
        var persian = await site.Visitor().Get($"/fa/{Story}/story");

        Assert.Contains("This is not a calendar: the story gives no dates", english.Main, StringComparison.Ordinal);
        Assert.Contains("این یک تقویم نیست", persian.Main, StringComparison.Ordinal);
        Assert.DoesNotMatch("\\b(1[0-9]{3}|20[0-9]{2})\\b", english.Main);
    }

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fa/story-rostam-sohrab")]
    [InlineData("/en/story-rostam-sohrab/entity/character-rostam/connections")]
    [InlineData("/en/story-rostam-sohrab/gallery")]
    [InlineData("/en/story-rostam-sohrab/search?q=rostam")]
    [InlineData("/en/story-rostam-sohrab/entity/character-nobody")]
    public async Task A_page_is_not_kept_by_the_browser_so_that_going_back_asks_for_it_again(string address)
    {
        var page = await site.Visitor().Get(address);

        Assert.True(page.Response.Headers.CacheControl is { NoStore: true });
    }

    [Fact]
    public async Task The_page_for_a_failure_opened_by_itself_offers_the_start_and_no_address_to_try_again()
    {
        var page = await site.Visitor().Get("/en/error");

        Assert.Contains("Something went wrong", page.Main, StringComparison.Ordinal);
        Assert.Contains("./", page.Links);
        Assert.DoesNotContain("Try again", page.Main, StringComparison.Ordinal);
    }
}

/// <summary>The site as it runs outside development, with one address that fails.</summary>
public sealed class FailingSite : DemoSite
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Dastan:Content", "../content");
        builder.UseSetting("Dastan:Audience", "LocalDemonstration");
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, Failure>());
    }

    // Stands behind everything the site maps: an address the site does not know reaches it, and one of them fails.
    private sealed class Failure : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Use((context, following) => context.Request.Path.StartsWithSegments("/fails")
                ? throw new InvalidOperationException("The secret reason of the failure.")
                : following(context));
        };
    }
}

/// <summary>What a visitor gets when the site fails.</summary>
public sealed class FailureTests(FailingSite site) : IClassFixture<FailingSite>
{
    [Theory]
    [InlineData("en", "Something went wrong", "Try again")]
    [InlineData("fa", "مشکلی پیش آمد", "تلاش دوباره")]
    public async Task A_failure_is_said_to_be_one_in_the_language_of_the_page_and_offers_the_same_address_again(string language, string title, string again)
    {
        var page = await site.Visitor().Get($"/{language}/fails/here?open=character-zal");

        Assert.Equal(HttpStatusCode.InternalServerError, page.Status);
        Assert.Equal(language, page.Root("lang"));
        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(again, page.Main, StringComparison.Ordinal);
        Assert.Contains("fails/here?open=character-zal", page.Links);
        Assert.Contains("./", page.Links);

        // Not an empty result, not a missing page, and nothing of the failure itself.
        Assert.DoesNotContain("secret reason", page.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Not here", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("There is nothing to show", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task After_a_failure_the_other_pages_still_answer()
    {
        var visitor = site.Visitor();
        await visitor.Get("/en/fails");

        Assert.Equal(HttpStatusCode.OK, (await visitor.Get("/en/story-rostam-sohrab")).Status);
    }
}
