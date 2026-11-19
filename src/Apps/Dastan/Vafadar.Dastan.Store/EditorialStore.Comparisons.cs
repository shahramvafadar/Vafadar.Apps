using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store;

/// <summary>A comparison as the store holds it, for a list.</summary>
/// <param name="Id">The identifier of the comparison.</param>
/// <param name="Title">Its title, in the language of its file.</param>
/// <param name="Languages">The languages it is translated into.</param>
/// <param name="Version">How many times its file was changed, the first time counted.</param>
/// <param name="UpdatedAt">When anything of it was last changed.</param>
/// <param name="UpdatedBy">Who changed it last.</param>
public sealed record ComparisonSummary(string Id, string Title, ImmutableArray<string> Languages, int Version, DateTime UpdatedAt, string UpdatedBy);

/// <summary>
/// One word about a comparison on the site (ADR 0015): a release keeps the comparison as it was at that
/// moment – its file, its translations and the hash of both – and a withdrawal keeps nothing of it. Words are
/// only added; the last one about a comparison counts.
/// </summary>
public sealed class ReleasedComparison
{
    /// <summary>Gets or sets the number of the word, given by the database.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the identifier of the comparison.</summary>
    public required string ComparisonId { get; set; }

    /// <summary>Gets or sets the file of the comparison as it was released; nothing for a withdrawal.</summary>
    public string? Json { get; set; }

    /// <summary>Gets or sets its translations as they were released, as one JSON object by language; nothing for a withdrawal.</summary>
    public string? Translations { get; set; }

    /// <summary>Gets or sets the hash of both (<see cref="PublishedRevision.HashOf"/>); nothing for a withdrawal.</summary>
    public string? Hash { get; set; }

    /// <summary>Gets or sets who said it.</summary>
    public required string By { get; set; }

    /// <summary>Gets or sets when.</summary>
    public DateTime At { get; set; }
}

/// <summary>What the site shows of a comparison.</summary>
public enum ComparisonOnSite
{
    /// <summary>The site does not show it.</summary>
    No,

    /// <summary>The site shows it as the store holds it now.</summary>
    AsHeld,

    /// <summary>The site shows it as it was when it was released; the store holds something else since.</summary>
    Earlier,
}

/// <summary>
/// The comparisons in the store (step 12.6). A comparison is one document – its lanes with their periods and
/// events are parts of it, not records of their own – with a document for every language it is translated into.
/// It is kept like a record: tried by the readers of the site before it is kept, and every change with who,
/// when, why, before and after.
/// </summary>
public sealed partial class EditorialStore
{
    /// <summary>The kind of document a comparison is.</summary>
    public const string ComparisonKind = "comparison";

    /// <summary>Gets the comparisons in the store, by their identifier.</summary>
    public ImmutableArray<ComparisonSummary> Comparisons()
    {
        var documents = _context.Documents.AsNoTracking().Where(document => document.Kind == ComparisonKind).ToList();
        return
        [
            .. documents.Where(document => document.Language.Length == 0).OrderBy(document => document.DocumentId, StringComparer.Ordinal).Select(document =>
            {
                var all = documents.Where(other => other.DocumentId == document.DocumentId).ToList();
                var last = all.MaxBy(other => other.UpdatedAt)!;
                return new ComparisonSummary(
                    document.DocumentId,
                    (string?)JsonNode.Parse(document.Body)!["title"] ?? document.DocumentId,
                    [.. all.Where(other => other.Language.Length > 0).Select(other => other.Language).Order(StringComparer.Ordinal)],
                    document.Version,
                    last.UpdatedAt,
                    last.UpdatedBy);
            }),
        ];
    }

    /// <summary>Returns a comparison as its file and the files of its translations, or nothing when the store holds none of that name.</summary>
    public ExportedCollection? Comparison(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var documents = _context.Documents.AsNoTracking().Where(document => document.Kind == ComparisonKind && document.DocumentId == id).ToList();
        return documents.Find(document => document.Language.Length == 0) is not { } file
            ? null
            : new ExportedCollection(
                JsonNode.Parse(file.Body)!.ToJsonString(Written),
                documents.Where(document => document.Language.Length > 0)
                    .ToImmutableSortedDictionary(document => document.Language, document => JsonNode.Parse(document.Body)!.ToJsonString(Written), StringComparer.Ordinal));
    }

    /// <summary>
    /// Says that the site shows a comparison from its next start on, as the store holds it at this moment. What
    /// is changed afterwards is shown when it is released again. The site still decides by what the comparison
    /// says about itself whether its audience may see it.
    /// </summary>
    /// <returns>Nothing when it was said; else why not.</returns>
    public string? ReleaseComparison(string id, string by)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        using var step = _context.Database.BeginTransaction();
        Lock("comparisons");
        if (Comparison(id) is not { } held)
        {
            return $"{id}: the store holds no comparison of that name.";
        }

        var (translations, hash) = Text(held);
        if (LastWord(id)?.Hash == hash)
        {
            return $"{id}: the site shows it already as the store holds it.";
        }

        _context.ReleasedComparisons.Add(new ReleasedComparison { ComparisonId = id, Json = held.Json, Translations = translations, Hash = hash, By = by, At = _time.GetUtcNow().UtcDateTime });
        _context.SaveChanges();
        step.Commit();
        return null;
    }

    /// <summary>Says that the site does not show a comparison any more.</summary>
    /// <returns>Nothing when it was said; else why not.</returns>
    public string? WithdrawComparison(string id, string by)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        using var step = _context.Database.BeginTransaction();
        Lock("comparisons");
        if (LastWord(id)?.Hash is null)
        {
            return $"{id}: the site does not show it.";
        }

        _context.ReleasedComparisons.Add(new ReleasedComparison { ComparisonId = id, By = by, At = _time.GetUtcNow().UtcDateTime });
        _context.SaveChanges();
        step.Commit();
        return null;
    }

    /// <summary>Returns what the site shows of a comparison.</summary>
    public ComparisonOnSite ComparisonShown(string id) =>
        LastWord(id)?.Hash is not { } shown ? ComparisonOnSite.No
        : Comparison(id) is { } held && Text(held).Hash == shown ? ComparisonOnSite.AsHeld
        : ComparisonOnSite.Earlier;

    private ReleasedComparison? LastWord(string id) =>
        _context.ReleasedComparisons.AsNoTracking().Where(word => word.ComparisonId == id).OrderByDescending(word => word.Number).FirstOrDefault();

    // A comparison as the site is given it: its translations as one object by language, and the hash of both.
    private static (string Translations, string Hash) Text(ExportedCollection comparison)
    {
        var translations = new JsonObject(comparison.Translations.Select(translation => KeyValuePair.Create(translation.Key, JsonNode.Parse(translation.Value)))).ToJsonString();
        return (translations, PublishedRevision.HashOf(comparison.Json, translations));
    }

    /// <summary>Returns what was changed of a comparison – its file and its translations – the oldest change first.</summary>
    public ImmutableArray<DocumentChange> ComparisonHistory(string id) =>
    [
        .. _context.DocumentChanges.AsNoTracking().Where(change => change.Kind == ComparisonKind && change.DocumentId == id).OrderBy(change => change.Number),
    ];

    /// <summary>
    /// Keeps a comparison: new, or in place of the one with its identifier. The translations given are all its
    /// translations – a language that is not among them is taken out. What the site could not read is refused
    /// with the reasons and changes nothing.
    /// </summary>
    /// <param name="json">The file of the comparison.</param>
    /// <param name="translations">The translation documents, by their language.</param>
    /// <param name="by">Who keeps it.</param>
    /// <param name="reason">Why.</param>
    public ActResult PutComparison(string json, IReadOnlyDictionary<string, string> translations, string by, string? reason)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(translations);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);

        JsonObject? file;
        var documents = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        try
        {
            file = JsonNode.Parse(json) as JsonObject;
            foreach (var (language, text) in translations)
            {
                if (JsonNode.Parse(text) is not JsonObject translation)
                {
                    return Refused($"{language}: the translation is not a document.");
                }

                if ((string?)translation["language"] != language)
                {
                    return Refused($"{language}: the translation says it is in {(string?)translation["language"] ?? "no language"}.");
                }

                documents[language] = translation;
            }
        }
        catch (System.Text.Json.JsonException refused)
        {
            return Refused($"the comparison cannot be read: {refused.Message}");
        }

        if (file is null || (file["id"] as JsonValue)?.TryGetValue<string>(out var id) != true || string.IsNullOrWhiteSpace(id))
        {
            return Refused("a comparison names itself: it has an id.");
        }

        try
        {
            _ = ComparisonSample.Read(json, [.. translations.OrderBy(translation => translation.Key, StringComparer.Ordinal).Select(translation => translation.Value)]);
        }
        catch (SampleFormatException refused)
        {
            return new ActResult(refused.Problems.ToImmutableArray());
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var changed = 0;
        using var transaction = _context.Database.BeginTransaction();
        Lock("comparisons");
        _context.ChangeTracker.Clear();
        var held = _context.Documents.Where(document => document.Kind == ComparisonKind && document.DocumentId == id).ToDictionary(document => document.Language, StringComparer.Ordinal);
        if (!WritesReviews && ReviewWritten(held.TryGetValue(string.Empty, out var before) ? JsonNode.Parse(before.Body) : null, file) is { } reviewed)
        {
            return Refused(reviewed);
        }

        foreach (var (language, document) in documents.Prepend(KeyValuePair.Create(string.Empty, file)))
        {
            var body = document.ToJsonString();
            if (!held.Remove(language, out var stored))
            {
                _context.Documents.Add(new StoredDocument { Kind = ComparisonKind, DocumentId = id!, Language = language, Body = body, Version = 1, UpdatedAt = now, UpdatedBy = by });
                Changed(id!, language, RecordChangeKind.Added, 1, by, now, reason, null, body);
                changed++;
            }
            else if (stored.Body != body)
            {
                stored.Version++;
                Changed(id!, language, RecordChangeKind.Changed, stored.Version, by, now, reason, stored.Body, body);
                stored.Body = body;
                stored.UpdatedAt = now;
                stored.UpdatedBy = by;
                changed++;
            }
        }

        // A language that was not given is a translation the comparison no longer has.
        foreach (var gone in held.Values.Where(document => document.Language.Length > 0))
        {
            Changed(id!, gone.Language, RecordChangeKind.Removed, gone.Version + 1, by, now, reason, gone.Body, null);
            _context.Documents.Remove(gone);
            changed++;
        }

        _context.SaveChanges();
        transaction.Commit();
        return new ActResult([], changed);
    }

    /// <summary>Brings the file of a comparison into the store, with the translations that lie beside it.</summary>
    public ActResult ImportComparison(string file, string by, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        var path = Path.GetFullPath(file);
        if (!File.Exists(path))
        {
            return Refused($"{Path.GetFileName(path)}: there is no such file.");
        }

        var translations = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var beside in TranslationFiles(path))
        {
            var language = Path.GetFileNameWithoutExtension(beside).Split('.')[^1];
            translations[language] = File.ReadAllText(beside);
        }

        return PutComparison(File.ReadAllText(path), translations, by, reason);
    }

    /// <summary>Takes a comparison out of the store, with its translations. Its history stays.</summary>
    public ActResult RemoveComparison(string id, string by, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var held = _context.Documents.Where(document => document.Kind == ComparisonKind && document.DocumentId == id).ToList();
        if (held.Count == 0)
        {
            return Refused($"{id}: the store holds no comparison of that name.");
        }

        var now = _time.GetUtcNow().UtcDateTime;
        foreach (var gone in held)
        {
            Changed(id, gone.Language, RecordChangeKind.Removed, gone.Version + 1, by, now, reason, gone.Body, null);
            _context.Documents.Remove(gone);
        }

        _context.SaveChanges();
        return new ActResult([], held.Count);
    }

    private void Changed(string id, string language, RecordChangeKind kind, int version, string by, DateTime at, string? reason, string? before, string? after) =>
        _context.DocumentChanges.Add(new DocumentChange
        {
            Kind = ComparisonKind, DocumentId = id, Language = language, Change = kind, Version = version,
            By = by, At = at, Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), Before = before, After = after,
        });
}
