using System.Net;
using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// Threads in the tool of the editors (step 14.9): the group among the records of a collection, the form a new
/// thread begins with, and the page of a thread that is there.
/// </summary>
public sealed class StudioThreadsTests : IDisposable
{
    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioThreadsTests() => _desk = new Desk(_studio);

    public void Dispose()
    {
        _desk.Dispose();
        _studio.Dispose();
    }

    [Theory]
    [InlineData("en", "Threads")]
    [InlineData("fa", "رشته‌ها")]
    public async Task The_tool_has_the_group_of_the_threads_and_a_form_for_a_new_one(string language, string name)
    {
        var collection = await _desk.Get($"/{language}/{Studio.Collection}");
        var group = await _desk.Get($"/{language}/{Studio.Collection}/records/threads");
        var form = await _desk.Get($"/{language}/{Studio.Collection}/records/threads/new");

        Assert.Contains(name, collection.Text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, group.Status);
        Assert.Contains(name, group.Text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, form.Status);

        // The form begins with two stops, each naming an entity of the collection and giving its reason.
        Assert.Contains("stops[0].entityId", form.Html, StringComparison.Ordinal);
        Assert.Contains("stops[1].note", form.Html, StringComparison.Ordinal);
        Assert.Contains("character-rostam", form.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_thread_that_is_there_has_its_page_with_its_stops()
    {
        using (var store = _studio.Store())
        {
            var thread = new JsonObject
            {
                ["id"] = "thread-test", ["label"] = "A thread of the tests", ["summary"] = "Test data.", ["spoilerLevel"] = 0,
                ["stops"] = new JsonArray(
                    new JsonObject { ["entityId"] = "character-rostam", ["note"] = "Begin here." },
                    new JsonObject { ["eventId"] = "event-01", ["note"] = "Then this." }),
            };
            Assert.True(store.Put(Studio.Collection, "threads", thread, "a writer", "A thread.").Done);
        }

        var group = await _desk.Get($"/en/{Studio.Collection}/records/threads");
        var page = await _desk.Get($"/en/{Studio.Collection}/record/threads/thread-test");

        Assert.Contains("A thread of the tests", group.Text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Begin here.", page.Html, StringComparison.Ordinal);
        Assert.Contains("stops[1].eventId", page.Html, StringComparison.Ordinal);
    }
}
