using System.Net;
using Microsoft.AspNetCore.Http;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// What a review of the site made firmer (step 18.7): what a device may keep is a plain page of public content
/// and nothing a visitor asked; an address is sent on as an address is written; the address a form names as
/// its page carries an invitation and nothing else.
/// </summary>
public sealed class FirmerSiteTests(DemoSite demo, AccountsSite accounts) : IClassFixture<DemoSite>, IClassFixture<AccountsSite>
{
    private static string? Kept(Page page) => page.Response.Headers.TryGetValues(KeptOffline.Header, out var values) ? string.Join(',', values) : null;

    [Theory]
    [InlineData("/", true)]
    [InlineData("/shahnameh", true)]
    [InlineData("/shahnameh/story-rostam-sohrab", true)]
    [InlineData("/compare/two-houses", true)]
    [InlineData("/Compare/two-houses", true)]
    [InlineData("/shahnameh/search", false)]
    [InlineData("/shahnameh/Search", false)]
    [InlineData("/search", false)]
    [InlineData("/Search", false)]
    [InlineData("/people/abc", false)]
    [InlineData("/People/abc", false)]
    [InlineData("/Trees/abc", false)]
    [InlineData("/Saved", false)]
    [InlineData("/ACCOUNT", false)]
    [InlineData("/_framework/x", false)]
    [InlineData("/app.css", false)]
    public void What_a_device_may_keep_is_a_page_of_public_content_in_whatever_case_an_address_is_written(string path, bool kept) =>
        Assert.Equal(kept, KeptOffline.IsPublicContent(new PathString(path)));

    [Fact]
    public async Task An_address_with_a_question_is_a_moment_of_one_visitor_and_is_not_kept()
    {
        var visitor = demo.Visitor();
        await visitor.Post("/preferences/offline", "/en/cookies", ("answer", "on"), ("returnUrl", "/en/cookies"));

        Assert.Equal(KeptOffline.Page, Kept(await visitor.Get("/en/")));
        var asked = await visitor.Get("/en/?reported=thanks");
        Assert.Equal(HttpStatusCode.OK, asked.Status);
        Assert.Null(Kept(asked));
        Assert.Null(Kept(await visitor.Get("/en/search?q=rostam")));
    }

    [Theory]
    [InlineData("/%D8%B4%D8%A7%D9%87", "/%D8%B4%D8%A7%D9%87")]
    [InlineData("/FA/%D8%B4", "/fa/%D8%B4")]
    [InlineData("/a%3Fb", "/a%3Fb")]
    public async Task An_address_without_its_language_is_sent_on_as_an_address_is_written(string address, string ends)
    {
        var sent = await demo.Visitor().Get(address);

        Assert.True(sent.Status is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently, sent.Status.ToString());
        var location = sent.Response.Headers.Location!.OriginalString;
        Assert.EndsWith(ends, location, StringComparison.Ordinal);

        // A header holds letters of the Latin alphabet only: anything else would not leave the server.
        Assert.All(location, letter => Assert.InRange(letter, '!', '~'));
    }

    [Fact]
    public async Task The_page_a_form_names_carries_an_invitation_and_nothing_hung_on_it()
    {
        var visitor = accounts.Visitor();
        const string from = "/en/account";

        // A form that is refused (no age) leads back to the form, with the invitation the visitor came by.
        var plain = await visitor.Post("/preferences/account/register", from, ("name", "Somebody New"), ("password", "a long pass phrase"), ("again", "a long pass phrase"), ("returnUrl", "/en/account?invitation=ABC123"));
        Assert.Equal("/en/account?form=register&problem=age&invitation=ABC123#register", plain.Response.Headers.Location?.OriginalString);

        var hung = await visitor.Post("/preferences/account/register", from, ("name", "Somebody New"), ("password", "a long pass phrase"), ("again", "a long pass phrase"), ("returnUrl", "/en/account?invitation=ABC123&said=welcome"));
        Assert.Equal("/en/account?form=register&problem=age&invitation=ABC123#register", hung.Response.Headers.Location?.OriginalString);

        var noCode = await visitor.Post("/preferences/account/register", from, ("name", "Somebody New"), ("password", "a long pass phrase"), ("again", "a long pass phrase"), ("returnUrl", "/en/account?invitation=a b%0d%0ac"));
        Assert.Equal("/en/account?form=register&problem=age#register", noCode.Response.Headers.Location?.OriginalString);
    }
}
