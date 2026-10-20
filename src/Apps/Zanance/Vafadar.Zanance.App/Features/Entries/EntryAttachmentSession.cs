using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>The actual detail attachment owner, retained across native picking and confirmation awaits.</summary>
public sealed class EntryAttachmentSession(ZananceStore store)
{
    private EntryAttachmentReview? _review;
    private long _revision;
    private bool _busy;

    /// <summary>Gets original displayed metadata; a failed or retired load grants no action authority.</summary>
    public EntryAttachmentReview? Review => _review;

    /// <summary>Gets whether this operation is known to have committed, including a failed notification.</summary>
    public bool Accepted { get; private set; }

    /// <summary>Retires original work before a query or navigation can adopt another detail.</summary>
    public void Retire() { _revision++; _review = null; }

    /// <summary>Loads only metadata belonging to the exact displayed original entry.</summary>
    public async Task LoadAsync(LedgerEntry entry, QuickResourceReview? displayed = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Retire(); var revision = _revision; var original = entry.Copy();
        var review = await store.ReadAttachmentReviewAsync(original.Id, displayed: displayed);
        if (revision != _revision || !review.Matches(original) || !review.Matches(entry))
            throw new InvalidOperationException("The original attachment detail changed during loading.");
        _review = review;
    }

    /// <summary>Returns only the original reviewed document while its detail still owns the read.</summary>
    public async Task<EntryAttachment> ReadAsync(Guid id)
    {
        var review = _review ?? throw new InvalidOperationException("Reopen the original attachment detail.");
        var revision = _revision;
        var value = await store.ReadReviewedAttachmentAsync(review, id);
        if (!Current(review, revision)) throw new InvalidOperationException("The original attachment detail retired.");
        return value;
    }

    /// <summary>Picks once and saves only into the original entry, never the later query or selected profile.</summary>
    public Task AddAsync(Func<Task<EntryAttachment?>> pick) => RunAsync(async (review, revision) =>
    {
        var value = await pick();
        if (value is null) return;
        if (!Current(review, revision)) throw new InvalidOperationException("The original file picker owner retired.");
        value.EntryId = review.Inputs.Entry.Id;
        await store.AddReviewedAttachmentAsync(review, value, () => Current(review, revision));
        Accepted = true;
    });

    /// <summary>Confirms one original document and refuses changed ownership, content or a departed detail.</summary>
    public Task DeleteAsync(Guid id, Func<string, Task<bool>> confirm) => RunAsync(async (review, revision) =>
    {
        var value = await store.ReadReviewedAttachmentAsync(review, id);
        if (!Current(review, revision)) throw new InvalidOperationException("The original attachment confirmation retired.");
        if (!await confirm(value.FileName)) return;
        await store.DeleteReviewedAttachmentAsync(review, value, () => Current(review, revision));
        Accepted = true;
    });

    private bool Current(EntryAttachmentReview review, long revision) => revision == _revision && ReferenceEquals(_review, review);

    private async Task RunAsync(Func<EntryAttachmentReview, long, Task> action)
    {
        if (_busy) return;
        Accepted = false;
        var review = _review ?? throw new InvalidOperationException("Reopen the original attachment detail.");
        var revision = _revision; _busy = true;
        try { await action(review, revision); }
        catch (CommittedWriteNotificationException) { Accepted = true; throw; }
        finally
        {
            // Consume a known commit before any fallible native refresh; it can never reopen a picker
            // or repeat a delete. A newer query's review remains owned by that newer query.
            if (Accepted && Current(review, revision)) Retire();
            _busy = false;
        }
    }
}
