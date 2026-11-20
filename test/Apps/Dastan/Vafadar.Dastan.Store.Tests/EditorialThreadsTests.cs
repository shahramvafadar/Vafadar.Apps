using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// Threads in the store of the editors (step 14.9): a group of records a collection need not have. A thread is
/// put, changed and taken out like any record, tried by the readers; a collection without threads is written
/// without the group; and two entities that are one leave a thread with one stop.
/// </summary>
public sealed class EditorialThreadsTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private static JsonObject Thread(params (string Field, string Id)[] stops) => new()
    {
        ["id"] = "thread-test", ["label"] = "A thread of the tests", ["summary"] = "Test data.", ["spoilerLevel"] = 0,
        ["stops"] = new JsonArray([.. stops.Select((stop, index) => new JsonObject { [stop.Field] = stop.Id, ["note"] = $"Reason {index + 1}." })]),
    };

    [Fact]
    public void A_collection_without_threads_is_kept_and_written_without_the_group()
    {
        using var store = _copy.Filled();

        Assert.Contains("threads", EditorialStore.Groups);
        Assert.Empty(store.Records(Collection, "threads"));
        Assert.Null(JsonNode.Parse(store.Export(Collection)!.Json)!["threads"]);
    }

    [Fact]
    public void A_thread_is_put_like_any_record_and_comes_out_in_the_file()
    {
        using var store = _copy.Filled();

        var put = store.Put(Collection, "threads", Thread(("entityId", "character-rostam"), ("eventId", "event-01")), "a writer", "A first thread.");

        Assert.True(put.Done, string.Join('\n', put.Problems));
        Assert.Equal("A thread of the tests", Assert.Single(store.Records(Collection, "threads")).Label);
        var written = JsonNode.Parse(store.Export(Collection)!.Json)!["threads"]!.AsArray();
        Assert.Equal("thread-test", (string?)Assert.Single(written)!["id"]);
        Assert.Equal(("a writer", "A first thread."), (store.History(Collection, "threads", "thread-test")[^1].By, store.History(Collection, "threads", "thread-test")[^1].Reason));

        // Taken out again, the collection is written without the group as before.
        Assert.True(store.Remove(Collection, "thread-test", "a writer", "Not needed.").Done);
        Assert.Null(JsonNode.Parse(store.Export(Collection)!.Json)!["threads"]);
    }

    [Fact]
    public void A_file_with_threads_comes_out_of_the_store_as_it_went_in()
    {
        var sample = _copy.Working();
        sample["threads"] = new JsonArray(Thread(("entityId", "character-rostam"), ("eventId", "event-01")));
        File.WriteAllText(_copy.Sample, sample.ToJsonString());
        using var store = _copy.Open();

        var result = store.Import(_copy.Sample, "an editor", "With a thread.");

        Assert.Empty(result.Problems);
        Assert.True(JsonNode.DeepEquals(sample, JsonNode.Parse(store.Export(Collection)!.Json)));
    }

    [Fact]
    public void A_thread_the_readers_refuse_is_not_kept_and_a_stop_cannot_lose_what_it_names()
    {
        using var store = _copy.Filled();

        var alone = store.Put(Collection, "threads", Thread(("entityId", "character-rostam")), "a writer", "One stop.");
        var unknown = store.Put(Collection, "threads", Thread(("entityId", "character-rostam"), ("entityId", "nobody-here")), "a writer", "A stop that is not there.");

        Assert.Contains("thread-test: a thread has at least two stops.", alone.Problems);
        Assert.Contains("thread-test, stop 2: nobody-here is not in the collection.", unknown.Problems);
        Assert.Empty(store.Records(Collection, "threads"));

        // An event a thread stops at cannot be taken out from under it.
        Assert.True(store.Put(Collection, "threads", Thread(("entityId", "character-rostam"), ("eventId", "event-01")), "a writer", "A thread.").Done);
        var removed = store.Remove(Collection, "event-01", "a writer", "Away with it.");
        Assert.False(removed.Done);
        Assert.Contains(removed.Problems, problem => problem.Contains("thread-test, stop 2: event-01 is not in the collection.", StringComparison.Ordinal));
    }

    [Fact]
    public void Two_entities_that_are_one_leave_a_thread_with_one_stop_at_the_kept_one()
    {
        using var store = _copy.Filled();
        var twin = WorkingCopy.Stored(store, "entities", "character-rostam");
        twin["id"] = "character-rostam-again";
        twin["label"] = "Rostam, written again";

        // The picture of the first record says that it depicts that record, not this one.
        twin.Remove("picture");
        Assert.True(store.Put(Collection, "entities", twin, "a writer", "A second record of the same man.").Done);
        Assert.True(store.Put(Collection, "threads", Thread(("entityId", "character-rostam-again"), ("eventId", "event-01"), ("entityId", "character-rostam")), "a writer", "A thread over both.").Done);

        var merged = store.MergeEntities(Collection, "character-rostam", "character-rostam-again", "an editor", "The same man.");

        Assert.True(merged.Done, string.Join('\n', merged.Problems));
        var stops = WorkingCopy.Stored(store, "threads", "thread-test")["stops"]!.AsArray();
        Assert.Equal(["character-rostam", "event-01"], stops.Select(stop => (string?)stop!["entityId"] ?? (string?)stop["eventId"]));
        Assert.Equal("Reason 1.", (string?)stops[0]!["note"]);
    }
}
