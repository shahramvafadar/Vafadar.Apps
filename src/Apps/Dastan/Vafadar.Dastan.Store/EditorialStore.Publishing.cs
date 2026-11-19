using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store;

/// <summary>A published revision as the store keeps it: what was published, unchanged ever after.</summary>
public sealed class StoredRevision
{
    /// <summary>Gets or sets the collection.</summary>
    public required string CollectionId { get; set; }

    /// <summary>Gets or sets the number of the revision.</summary>
    public int Number { get; set; }

    /// <summary>Gets or sets the day it was published.</summary>
    public DateOnly PublishedOn { get; set; }

    /// <summary>Gets or sets the file of the revision, as JSON.</summary>
    public required string Json { get; set; }

    /// <summary>Gets or sets the translation documents published with it, as one JSON object by language.</summary>
    public required string Translations { get; set; }

    /// <summary>Gets or sets what the editors said about the revision.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets who published it.</summary>
    public required string By { get; set; }

    /// <summary>Gets or sets when it was made.</summary>
    public DateTime At { get; set; }
}

/// <summary>A revision the store made, or why it made none.</summary>
/// <param name="Problems">Why there is no revision, each with the record it is about.</param>
/// <param name="Number">The number of the revision.</param>
/// <param name="Revision">The revision: its file and the files of its translations.</param>
/// <param name="Changes">How many records it adds, changes and removes against the revision before.</param>
public sealed record PublishResult(ImmutableArray<string> Problems, int Number = 0, ExportedCollection? Revision = null, (int Added, int Changed, int Removed) Changes = default);

/// <summary>A published revision of a collection, without its content.</summary>
/// <param name="Number">The number.</param>
/// <param name="PublishedOn">The day it was published.</param>
/// <param name="Note">What the editors said about it.</param>
/// <param name="By">Who published it.</param>
public sealed record RevisionInfo(int Number, DateOnly PublishedOn, string? Note, string By);

// Publishing from the store (step 11.3). The rule is the rule of ADR 0009, asked by the same code that makes a
// revision from a file; the store adds that it knows the revision before and keeps the one it makes.
public sealed partial class EditorialStore
{
    /// <summary>
    /// Makes the next published revision of a collection out of what the store holds: numbered after the last
    /// one the store published, with the history of the revisions before and what this one changes. A statement
    /// that is not reviewed, or reviewed without a record, stops it; then nothing is kept. The revision is kept
    /// as it was published and is never changed afterwards.
    /// </summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="publishedOn">The day the revision is published.</param>
    /// <param name="by">Who publishes it.</param>
    /// <param name="note">What the editors say about the revision, if anything.</param>
    public PublishResult Publish(string collectionId, DateOnly publishedOn, string by, string? note = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);

        // One step: what is read, the number and the revision. Two who publish at the same moment get two
        // numbers, one after the other.
        using var step = _context.Database.BeginTransaction();
        _context.ChangeTracker.Clear();
        if (Working(collectionId) is not var (root, working))
        {
            return new PublishResult([$"{collectionId}: the store holds no collection of that name."]);
        }

        var last = _context.Revisions.AsNoTracking().Where(revision => revision.CollectionId == collectionId).OrderByDescending(revision => revision.Number).FirstOrDefault();
        var number = (last?.Number ?? 0) + 1;

        // A revision does not hold a withdrawn statement; the texts that translate one go with it.
        var translations = working.ToDictionary(translation => translation.Key, translation => ContentRevision.WithoutWithdrawn(root, translation.Value), StringComparer.Ordinal);
        var made = ContentRevision.Make(
            root,
            collectionId,
            last is null ? null : JsonNode.Parse(last.Json)!.AsObject(),
            last is null ? null : $"revision {last.Number}",
            number,
            publishedOn,
            note,
            json => Problems(json, translations));
        if (made.Json is null)
        {
            return new PublishResult(made.Problems);
        }

        var written = translations.ToImmutableSortedDictionary(translation => translation.Key, translation => translation.Value.ToJsonString(Written), StringComparer.Ordinal);
        _context.Revisions.Add(new StoredRevision
        {
            CollectionId = collectionId,
            Number = number,
            PublishedOn = publishedOn,
            Json = made.Json,
            Translations = new JsonObject(translations.Select(translation => KeyValuePair.Create(translation.Key, (JsonNode?)translation.Value.DeepClone()))).ToJsonString(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            By = by,
            At = _time.GetUtcNow().UtcDateTime,
        });
        _context.SaveChanges();
        step.Commit();
        return new PublishResult([], number, new ExportedCollection(made.Json, written), made.Changes);
    }

    /// <summary>Returns the revisions the store published of a collection, the first one first.</summary>
    public ImmutableArray<RevisionInfo> Revisions(string collectionId) =>
    [
        .. _context.Revisions.AsNoTracking().Where(revision => revision.CollectionId == collectionId).OrderBy(revision => revision.Number)
            .Select(revision => new RevisionInfo(revision.Number, revision.PublishedOn, revision.Note, revision.By)),
    ];

    /// <summary>Returns a published revision as it was published, or nothing when the store did not publish it.</summary>
    public ExportedCollection? Revision(string collectionId, int number)
    {
        var revision = _context.Revisions.AsNoTracking().SingleOrDefault(item => item.CollectionId == collectionId && item.Number == number);
        return revision is null
            ? null
            : new ExportedCollection(
                revision.Json,
                JsonNode.Parse(revision.Translations)!.AsObject().ToImmutableSortedDictionary(translation => translation.Key, translation => translation.Value!.ToJsonString(Written), StringComparer.Ordinal));
    }
}
