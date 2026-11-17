using System.Net;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The answer to "is the site alive?" (step 10.5, docs/operations.md): an address without a language that says
/// that the site answers and nothing else. What a host is sure of from outside the process is asked by
/// <c>eng/Check-Site.ps1</c> against a running container.
/// </summary>
public sealed class SiteHealthTests(DemoSite demonstration, PublicSite unpublished) : IClassFixture<DemoSite>, IClassFixture<PublicSite>
{
    [Fact]
    public async Task The_site_says_that_it_is_alive_at_an_address_without_a_language()
    {
        var answer = await demonstration.Visitor().Get("/health");

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("ok", answer.Html);
        Assert.Equal("text/plain", answer.Response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task The_answer_is_asked_again_every_time_is_not_for_a_search_engine_and_sets_no_cookie()
    {
        var answer = await demonstration.Visitor().Get("/health");

        Assert.Equal("no-store", answer.Response.Headers.CacheControl?.ToString());
        Assert.Equal("noindex, nofollow", string.Join(", ", answer.Response.Headers.GetValues("X-Robots-Tag").Distinct()));
        Assert.False(answer.Response.Headers.Contains("Set-Cookie"));
        Assert.Contains("default-src 'self'", string.Join(' ', answer.Response.Headers.GetValues("Content-Security-Policy")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_with_nothing_published_is_alive_too_and_says_nothing_of_its_collection()
    {
        var answer = await unpublished.Visitor().Get("/health");

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("ok", answer.Html);
    }

    [Fact]
    public async Task Nothing_below_the_address_answers()
    {
        var answer = await demonstration.Visitor().Get("/health/more");

        Assert.Equal(HttpStatusCode.NotFound, answer.Status);
    }
}
