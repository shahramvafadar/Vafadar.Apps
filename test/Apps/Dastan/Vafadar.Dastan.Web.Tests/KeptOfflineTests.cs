using System.Net;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// What a device may keep for a time without a connection (step 17.5, D-064): public pages a visitor who is
/// not signed in was given – and nothing of an account or a private space. The site marks such an answer; the
/// service worker keeps only what is marked.
/// </summary>
public sealed class KeptOfflineTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Story = "story-rostam-sohrab";
    private readonly PhotosSite _site = new();

    public void Dispose() => _site.Dispose();

    private static string? Kept(Page page) => page.Response.Headers.TryGetValues("Dastan-Keep", out var values) ? string.Join(',', values) : null;

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fa/")]
    [InlineData("/en/story-rostam-sohrab")]
    [InlineData("/fa/story-rostam-sohrab/timeline")]
    [InlineData("/en/compare/iran-china-rome")]
    public async Task A_public_page_may_be_kept_by_the_device_of_who_read_it(string address)
    {
        var page = await _site.Visitor().Get(address);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal("page", Kept(page));
    }

    [Theory]
    [InlineData("/en/account")]
    [InlineData("/en/search?q=rostam")]
    [InlineData("/en/saved")]
    [InlineData("/en/no-such-page")]
    [InlineData("/en/story-rostam-sohrab/entity/no-such-entity")]
    [InlineData("/health")]
    [InlineData("/preview.png")]
    [InlineData("/robots.txt")]
    [InlineData("/service-worker.js")]
    [InlineData("/app.css")]
    public async Task What_is_no_public_page_that_was_found_is_not_kept(string address)
    {
        var page = await _site.Visitor().Get(address);

        Assert.Null(Kept(page));
    }

    [Fact]
    public async Task Nothing_is_kept_for_somebody_who_is_signed_in_and_nothing_of_their_private_spaces()
    {
        var mina = _site.Visitor();
        await mina.Post("/preferences/account/register", "/en/account", ("name", "Mina"), ("age", "on"), ("password", Password), ("again", Password), ("returnUrl", "/en/account"));
        var id = System.Text.RegularExpressions.Regex.Match((await mina.Get("/en/account/people")).Html, "href=\"people/([a-f0-9]{32})\"").Groups[1].Value;

        // Her name stands on every page she is given: none of them is kept, public or not.
        Assert.Null(Kept(await mina.Get($"/en/{Story}")));
        Assert.Null(Kept(await mina.Get("/en/")));
        foreach (var address in new[] { "/en/account", "/en/account/people", "/en/account/trees", "/en/account/photos", $"/en/people/{id}" })
        {
            Assert.Equal(HttpStatusCode.OK, (await mina.Get(address)).Status);
            Assert.Null(Kept(await mina.Get(address)));
        }

        // The page of a person is not kept for a stranger either.
        var stranger = _site.Visitor();
        Assert.Equal(HttpStatusCode.OK, (await stranger.Get($"/en/people/{id}")).Status);
        Assert.Null(Kept(await stranger.Get($"/en/people/{id}")));
        Assert.Equal("page", Kept(await stranger.Get($"/en/{Story}")));
    }

    [Fact]
    public async Task A_form_is_not_kept()
    {
        var visitor = _site.Visitor();

        var answer = await visitor.ReadTo(1);

        Assert.Null(Kept(answer));
    }
}
