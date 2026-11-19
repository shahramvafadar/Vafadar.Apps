using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// What a review of the store of the editors made firmer (step 18.7): an editor who is no reviewer cannot write
/// a review into a record, and two acts that are made close together do not put each other's records back.
/// </summary>
public sealed class FirmerStoreTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private static readonly DateOnly Today = new(2026, 11, 17);

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private static JsonObject NewSource() => JsonNode.Parse(
        """
        { "id": "src-new", "title": "A new source", "url": "https://example.org/a-new-source", "kind": "primary_literary_text_transcription",
          "accessedAt": "2026-11-17", "spoilerLevel": 0 }
        """)!.AsObject();

    [Fact]
    public void An_editor_who_is_no_reviewer_cannot_write_what_a_review_found_into_a_record()
    {
        using var store = _copy.Filled();
        store.WritesReviews = false;
        var relation = WorkingCopy.Stored(store, "relations", "relation-02");
        Assert.NotEqual("reviewed", (string?)relation["reviewStatus"]);

        // The text form takes any record: the status of a review, and who reviewed, written by hand.
        var status = relation.DeepClone().AsObject();
        status["reviewStatus"] = "reviewed";
        var reviewer = relation.DeepClone().AsObject();
        reviewer["review"] = new JsonObject { ["by"] = "Somebody Else", ["on"] = "2026-11-01" };

        var first = store.Put(Collection, "relations", status, "a writer", "A try.");
        var second = store.Put(Collection, "relations", reviewer, "a writer", "A try.");

        Assert.False(first.Done);
        Assert.False(second.Done);
        Assert.Contains("a review is recorded by a reviewer", Assert.Single(first.Problems), StringComparison.Ordinal);
        Assert.Equal(relation.ToJsonString(), WorkingCopy.Stored(store, "relations", "relation-02").ToJsonString());
    }

    [Fact]
    public void Such_an_editor_keeps_a_record_with_the_review_it_has_and_a_reviewer_reviews_as_before()
    {
        using var store = _copy.Filled();
        Assert.True(store.Review(Collection, "relation-02", ReviewAct.Reviewed, "a reviewer", Today, "Read against the text.").Done);
        store.WritesReviews = false;

        // The record as it is, with its review, is kept by any editor: nothing about the review is new.
        var kept = WorkingCopy.Stored(store, "relations", "relation-02");
        var same = store.Put(Collection, "relations", kept, "a writer", "The same record once more.");
        Assert.True(same.Done, string.Join(" | ", same.Problems));

        // Another finding is a reviewer's.
        var disputed = WorkingCopy.Stored(store, "relations", "relation-02");
        disputed["reviewStatus"] = "disputed";
        Assert.False(store.Put(Collection, "relations", disputed, "a writer", "A try.").Done);

        store.WritesReviews = true;
        Assert.True(store.Review(Collection, "relation-02", ReviewAct.Disputed, "a reviewer", Today, "Two manuscripts differ.").Done);
    }

    [Fact]
    public void An_act_keeps_what_another_store_kept_while_it_was_at_work()
    {
        using var first = _copy.Filled();
        using var second = _copy.Open();

        // The first editor changes one record; the second, whose store was opened before that, adds another.
        var rostam = WorkingCopy.Stored(first, "entities", "character-rostam");
        rostam["summary"] = "Changed by the first editor.";
        var kept = first.Put(Collection, "entities", rostam, "the first", "A summary.");
        Assert.True(kept.Done, string.Join(" | ", kept.Problems));

        var result = second.Put(Collection, "sources", NewSource(), "the second", "A source.");

        Assert.True(result.Done, string.Join(" | ", result.Problems));
        Assert.Equal(1, result.Changed);
        Assert.Equal("Changed by the first editor.", (string?)WorkingCopy.Stored(second, "entities", "character-rostam")["summary"]);
        Assert.Equal("the first", first.History(Collection, "entities", "character-rostam")[^1].By);
    }

    [Fact]
    public void Two_who_publish_get_two_numbers_one_after_the_other()
    {
        using var first = _copy.Filled(files: true);
        foreach (var statement in first.Statements(Collection))
        {
            Assert.True(first.Review(Collection, statement.Statement, ReviewAct.Reviewed, "a reviewer", Today.AddDays(-1), "Read against the text.").Done);
        }

        using var second = _copy.Open(files: true);

        var one = first.Publish(Collection, Today, "a publisher");
        Assert.Empty(one.Problems);
        var two = second.Publish(Collection, Today, "another publisher");

        Assert.Equal((1, 2), (one.Number, two.Number));
    }
}
