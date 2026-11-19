using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// An act that says which version of a record it was decided on (step 19.3, ADR 0015): it is kept when the
/// record is still at that version and refused – with nothing kept – when somebody changed the record or took
/// it out in between. And the versions of a record are counted through, also over a time it was not there.
/// </summary>
public sealed class ReadVersionTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private static readonly DateOnly Today = new(2026, 11, 17);

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private static JsonObject Source(string title) => JsonNode.Parse(
        $$"""
        { "id": "src-read", "title": "{{title}}", "url": "https://example.org/read", "kind": "primary_literary_text_transcription",
          "accessedAt": "2026-11-17", "spoilerLevel": 0 }
        """)!.AsObject();

    [Fact]
    public void An_act_on_a_record_somebody_changed_in_between_is_refused_and_nothing_is_kept()
    {
        using var store = _copy.Filled();
        var first = WorkingCopy.Stored(store, "entities", "character-rostam");
        var second = WorkingCopy.Stored(store, "entities", "character-rostam");
        var read = new ReadRecord("entities", "character-rostam", store.Record(Collection, "entities", "character-rostam")!.Info.Version);

        // Two editors read the same version; the first one's change is kept.
        first["summary"] = "The summary of the first editor.";
        Assert.True(store.Put(Collection, "entities", first, "the first editor", "A correction.", read: read).Done);

        // The second one's is not, and says who came first.
        second["summary"] = "The summary of the second editor.";
        var late = store.Put(Collection, "entities", second, "the second editor", "Another correction.", read: read);
        Assert.False(late.Done);
        Assert.Contains("was changed by the first editor since you read it", Assert.Single(late.Problems), StringComparison.Ordinal);
        Assert.Equal("The summary of the first editor.", (string?)WorkingCopy.Stored(store, "entities", "character-rostam")["summary"]);
        Assert.Equal(2, store.History(Collection, "entities", "character-rostam").Length);

        // Read again, it is kept.
        var again = new ReadRecord("entities", "character-rostam", read.Version + 1);
        Assert.True(store.Put(Collection, "entities", second, "the second editor", "Another correction.", read: again).Done);
    }

    [Fact]
    public void A_review_and_a_removal_are_refused_the_same_way()
    {
        using var store = _copy.Filled();
        var read = new ReadRecord("relations", "relation-02", store.Record(Collection, "relations", "relation-02")!.Info.Version);
        Assert.True(store.Review(Collection, "relation-02", ReviewAct.Reviewed, "a reviewer", Today, "Read against the text.", read).Done);

        var review = store.Review(Collection, "relation-02", ReviewAct.Disputed, "another reviewer", Today, "Two manuscripts differ.", read);
        Assert.False(review.Done);
        Assert.Equal("reviewed", (string?)WorkingCopy.Stored(store, "relations", "relation-02")["reviewStatus"]);

        // A source that was read, taken out by somebody else, and then changed or taken out by who read it.
        Assert.True(store.Put(Collection, "sources", Source("A source"), "a writer", "A source.", read: new ReadRecord("sources", "src-read", 0)).Done);
        var seen = new ReadRecord("sources", "src-read", 1);
        Assert.True(store.Remove(Collection, "src-read", "a writer", "Not needed.", "sources", seen).Done);
        var gone = store.Remove(Collection, "src-read", "another writer", "Not needed either.", "sources", seen);
        Assert.False(gone.Done);
        Assert.Contains("was taken out since you read it", Assert.Single(gone.Problems), StringComparison.Ordinal);
        Assert.False(store.Put(Collection, "sources", Source("Changed"), "another writer", "A change.", read: seen).Done);
    }

    [Fact]
    public void A_new_record_is_not_put_over_one_somebody_else_made_under_the_same_identifier()
    {
        using var store = _copy.Filled();
        var nothing = new ReadRecord("sources", "src-read", 0);

        Assert.True(store.Put(Collection, "sources", Source("The source of the first editor"), "the first editor", "A source.", read: nothing).Done);
        var late = store.Put(Collection, "sources", Source("The source of the second editor"), "the second editor", "A source.", read: nothing);

        Assert.False(late.Done);
        Assert.Equal("The source of the first editor", (string?)WorkingCopy.Stored(store, "sources", "src-read")["title"]);
    }

    [Fact]
    public void The_versions_of_a_record_are_counted_through_also_when_it_was_taken_out_and_came_back()
    {
        using var store = _copy.Filled();
        Assert.True(store.Put(Collection, "sources", Source("A source"), "a writer", "A source.").Done);
        Assert.True(store.Remove(Collection, "src-read", "a writer", "Not needed.", "sources").Done);
        Assert.True(store.Put(Collection, "sources", Source("A source again"), "a writer", "Needed after all.").Done);
        Assert.True(store.Put(Collection, "sources", Source("A source once more"), "a writer", "A better title.").Done);

        var history = store.History(Collection, "sources", "src-read");
        Assert.Equal([1, 2, 3, 4], history.Select(change => change.Version));
        Assert.Equal([RecordChangeKind.Added, RecordChangeKind.Removed, RecordChangeKind.Added, RecordChangeKind.Changed], history.Select(change => change.Kind));
        Assert.Equal(4, store.Record(Collection, "sources", "src-read")!.Info.Version);
    }
}
