using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site with a copy of the collection through which a thread is laid: from Rostam to the first step of the
/// story, to Sohrab – who is of a later part – and on. The thread is test data: the collection of the
/// repository has none.
/// </summary>
public sealed class ThreadSite : DemoSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-thread-" + Guid.NewGuid().ToString("N"), "content");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", _content);
        if (Directory.Exists(_content))
        {
            return;
        }

        var app = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan"));
        var copies = Path.GetDirectoryName(_content)!;
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(copies, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy, overwrite: true);
            }
        }

        var path = Path.Combine(_content, "samples", "shahnameh-pilot.json");
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        sample["threads"] = JsonNode.Parse("""
            [
              { "id": "thread-father-and-son", "label": "A father and a son", "summary": "From the hero to the one he does not know.", "spoilerLevel": 0,
                "stops": [
                  { "entityId": "character-rostam", "note": "Start with the hero of the story." },
                  { "eventId": "event-01", "note": "Then what sets him on his way." },
                  { "entityId": "character-sohrab", "note": "Then the son – a reason that tells too much." },
                  { "entityId": "character-tahmineh", "note": "And his mother." }
                ] },
              { "id": "thread-of-the-end", "label": "A thread of the end", "summary": "It tells how it ends.", "spoilerLevel": 4,
                "stops": [
                  { "entityId": "character-rostam", "note": "From the hero." },
                  { "entityId": "character-sohrab", "note": "To the end." }
                ] }
            ]
            """);
        File.WriteAllText(path, sample.ToJsonString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_content))
        {
            Directory.Delete(Path.GetDirectoryName(_content)!, recursive: true);
        }
    }
}

/// <summary>
/// Threads on the pages of the site (step 14.9, R-EXP-04, R-PUB-06): the list on the page of the collection,
/// the page of a thread as far as the visitor has read, and where to go on from a name – with nothing of a
/// later part named or counted.
/// </summary>
public sealed class ThreadPagesTests(ThreadSite site, DemoSite plain) : IClassFixture<ThreadSite>, IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Thread = $"{Story}/thread/thread-father-and-son";

    [Theory]
    [InlineData("en", "Threads to follow", "A father and a son")]
    [InlineData("fa", "رشته‌هایی برای دنبال‌کردن", "A father and a son")]
    public async Task The_page_of_a_collection_lists_the_threads_a_visitor_may_follow(string language, string title, string thread)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}");

        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(thread, page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Thread}\"", page.Html, StringComparison.Ordinal);

        // A thread of a later part is not offered and not named.
        Assert.DoesNotContain("A thread of the end", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("thread-of-the-end", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_thread_is_shown_as_far_as_the_visitor_has_read_and_says_only_that_it_goes_on()
    {
        var page = await site.Visitor().Get($"/en/{Thread}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Stop 1", page.Main, StringComparison.Ordinal);
        Assert.Contains("Start with the hero of the story.", page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("Stop 2", page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/story#step-1\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("The thread goes on after what you have read.", page.Main, StringComparison.Ordinal);

        // Neither the stop that is not reached nor the one after it: no name, no reason, no number.
        Assert.DoesNotContain("Stop 3", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("character-sohrab", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("the son", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("tells too much", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("character-tahmineh", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("The thread ends here.", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_visitor_who_has_read_to_the_end_follows_the_whole_thread()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);
        var page = await visitor.Get($"/en/{Thread}");
        var collection = await visitor.Get($"/en/{Story}");

        Assert.Contains("Stop 4", page.Main, StringComparison.Ordinal);
        Assert.Contains("Then the son – a reason that tells too much.", page.Main, StringComparison.Ordinal);
        Assert.Contains("The thread ends here.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("The thread goes on", page.Main, StringComparison.Ordinal);
        Assert.Contains("A thread of the end", collection.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Where to go on from here", "Then what sets him on his way.")]
    [InlineData("fa", "از این‌جا به کجا برویم", "Then what sets him on his way.")]
    public async Task The_page_of_a_name_says_where_a_thread_leads_on_and_why(string language, string title, string reason)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}/entity/character-rostam");

        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(reason, page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/story#step-1\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Thread}#stop-1\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("thread-of-the-end", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_name_no_thread_passes_and_a_collection_without_threads_say_nothing_of_threads()
    {
        var beside = await site.Visitor().Get($"/en/{Story}/entity/character-tahmineh");
        var collection = await plain.Visitor().Get($"/en/{Story}");
        var rostam = await plain.Visitor().Get($"/en/{Story}/entity/character-rostam");

        Assert.DoesNotContain("Where to go on from here", beside.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Threads to follow", collection.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Where to go on from here", rostam.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("thread-of-the-end")]
    [InlineData("thread-nowhere")]
    public async Task A_thread_that_is_not_reached_and_one_that_is_not_there_are_the_same_page(string thread)
    {
        var page = await site.Visitor().Get($"/en/{Story}/thread/{thread}");

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
        Assert.DoesNotContain("It tells how it ends.", page.Text, StringComparison.Ordinal);
    }
}
