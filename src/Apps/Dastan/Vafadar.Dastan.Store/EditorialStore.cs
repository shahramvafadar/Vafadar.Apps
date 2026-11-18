using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store;

/// <summary>What bringing a file into the store did, or why it was not brought in.</summary>
/// <param name="CollectionId">The collection; nothing when the file was refused.</param>
/// <param name="Problems">Why the file was refused, each with the record it is about.</param>
/// <param name="Added">How many records and translations came in.</param>
/// <param name="Changed">How many read differently than before.</param>
/// <param name="Removed">How many were taken out.</param>
public sealed record ImportResult(string? CollectionId, ImmutableArray<string> Problems, int Added = 0, int Changed = 0, int Removed = 0);

/// <summary>A collection written out of the store: its file and the files of its translations.</summary>
/// <param name="Json">The file of the collection.</param>
/// <param name="Translations">The translation documents by their language.</param>
public sealed record ExportedCollection(string Json, ImmutableSortedDictionary<string, string> Translations);

/// <summary>
/// The store of the editors (ADR 0011). A collection lives in it as its records – each as it is written in the
/// file of a collection, with who changed it last – and every change of a record is kept with the record before
/// and after. The file of a collection is what goes in and what comes out; the readers of the site decide what a
/// usable collection is, here as everywhere. The store reviews nothing and publishes nothing.
/// </summary>
public sealed partial class EditorialStore : IDisposable
{
    /// <summary>The groups of records of a collection, in the order of its file.</summary>
    public static readonly ImmutableArray<string> Groups = ["sources", "entities", "relations", "events", "media", "threads"];

    /// <summary>
    /// The groups a collection need not have. A file without such a group is a collection without records of it,
    /// and a collection without records of it is written without the group.
    /// </summary>
    public static readonly ImmutableArray<string> OptionalGroups = ["threads"];

    /// <summary>The group a change of a translation document is recorded under.</summary>
    public const string TranslationGroup = "translation";

    /// <summary>
    /// The group under which a change of what is not a record is kept: the reading stages, the edition, what
    /// the collection is about. Its one "record" is the collection itself.
    /// </summary>
    public const string CollectionGroup = "collection";

    // The places of a translation document that are not named by a group of records.
    private const string DatedGroup = "dated";
    private const string StagesGroup = "readingStages";

    private static readonly JsonSerializerOptions Written = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly StoreContext _context;
    private readonly TimeProvider _time;
    private readonly string? _samplesFolder;

    private EditorialStore(StoreContext context, TimeProvider time, string? samplesFolder)
    {
        _context = context;
        _time = time;
        _samplesFolder = samplesFolder;
    }

    /// <summary>Opens the store in a file, and brings its database to the form this program knows.</summary>
    /// <param name="file">The file of the store; it is made when it does not exist.</param>
    /// <param name="time">The clock; the system's when nothing is given.</param>
    /// <param name="samplesFolder">
    /// The folder in which the files of the collections lie (<c>content/samples</c>): the files a media item names
    /// are looked for from there when an act of an editor is tried. Not given, files are not looked for.
    /// </param>
    /// <param name="migrate">
    /// Whether the database is brought to the form this program knows. A tool that opens the store for every
    /// request does it once, when it starts.
    /// </param>
    public static EditorialStore Open(string file, TimeProvider? time = null, string? samplesFolder = null, bool migrate = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        var path = Path.GetFullPath(file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Pooling is off: the file of a store is released when the store is closed, not when the process ends.
        var options = new DbContextOptionsBuilder<StoreContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        var context = new StoreContext(options);
        if (migrate)
        {
            context.Database.Migrate();
        }

        return new EditorialStore(context, time ?? TimeProvider.System, samplesFolder is null ? null : Path.GetFullPath(samplesFolder));
    }

    /// <summary>
    /// Writes a copy of the whole store – collections, history, revisions and accounts – into a file, as the store
    /// is at this moment. The store can be in use meanwhile: the copy is made by the database itself and is
    /// complete. Never over a file.
    /// </summary>
    /// <returns>Nothing when the copy was written; else why it was not.</returns>
    public string? CopyTo(string file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        var path = Path.GetFullPath(file);
        if (File.Exists(path) || Directory.Exists(path))
        {
            return $"{path}: there already is a file of that name; a copy is never written over a file.";
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _context.Database.ExecuteSql($"VACUUM INTO {path}");
        return null;
    }

    /// <summary>Gets the identifiers of the collections in the store.</summary>
    public ImmutableArray<string> Collections => [.. _context.Collections.AsNoTracking().OrderBy(collection => collection.Id).Select(collection => collection.Id)];

    /// <summary>
    /// Brings the working file of a collection into the store, with the translations beside it. The first time
    /// every record comes in; afterwards a record that reads differently is a change, one that is new is added
    /// and one the file no longer has is taken out – each with who brought the file and why. A file the site
    /// cannot use, and a published revision, are refused and change nothing.
    /// </summary>
    /// <param name="sampleFile">The file of the collection.</param>
    /// <param name="by">Who brings it in.</param>
    /// <param name="reason">Why, in their words.</param>
    public ImportResult Import(string sampleFile, string by, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);

        var report = ContentCheck.File(sampleFile);
        if (!report.IsUsable)
        {
            return new ImportResult(null, report.Problems);
        }

        var name = Path.GetFileName(sampleFile);
        if (report.Format != PilotSample.SchemaVersion || Path.GetFullPath(sampleFile) != report.File)
        {
            return new ImportResult(null, [$"{name}: the store takes the file of a collection."]);
        }

        var root = JsonNode.Parse(File.ReadAllText(report.File))!.AsObject();
        if ((string?)root["publication"]?["state"] != "editorial_sample")
        {
            return new ImportResult(null, [$"{name}: the store holds the working state of the editors; a published revision is made from it, not brought into it."]);
        }

        var translations = TranslationFiles(report.File)
            .Select(file => JsonNode.Parse(File.ReadAllText(file))!.AsObject())
            .ToDictionary(document => (string)document["language"]!, StringComparer.Ordinal);
        var (added, changed, removed) = Keep(root, translations, by, reason);
        return new ImportResult((string)root["datasetId"]!, [], added, changed, removed);
    }

    // Keeps a collection as it is given: every record and translation that differs from what the store holds
    // becomes a change. The collection was read by the readers before it gets here.
    private (int Added, int Changed, int Removed) Keep(JsonObject root, Dictionary<string, JsonObject> documents, string by, string? reason)
    {
        var id = (string)root["datasetId"]!;
        var now = _time.GetUtcNow().UtcDateTime;
        var (added, changed, removed) = (0, 0, 0);

        using var transaction = _context.Database.BeginTransaction();
        var collection = _context.Collections.Find(id);
        var isNew = collection is null;

        // The records, group by group, against what the store holds.
        var held = _context.Records.Where(record => record.CollectionId == id).ToDictionary(record => (record.Group, record.RecordId));
        foreach (var group in Groups)
        {
            var records = root[group] as JsonArray ?? [];
            for (var position = 0; position < records.Count; position++)
            {
                var record = records[position]!.AsObject();
                var recordId = (string)record["id"]!;
                var body = record.ToJsonString();
                var review = (string?)record["reviewStatus"];
                if (!held.Remove((group, recordId), out var stored))
                {
                    _context.Records.Add(new StoredRecord
                    {
                        CollectionId = id, Group = group, RecordId = recordId, Position = position, Body = body,
                        ReviewStatus = review, Version = 1, UpdatedAt = now, UpdatedBy = by,
                    });
                    Record(id, group, recordId, RecordChangeKind.Added, 1, by, now, reason, null, body);
                    added++;
                }
                else
                {
                    stored.Position = position;
                    if (!JsonNode.DeepEquals(JsonNode.Parse(stored.Body), record))
                    {
                        Record(id, group, recordId, RecordChangeKind.Changed, stored.Version + 1, by, now, reason, stored.Body, body);
                        stored.Body = body;
                        stored.ReviewStatus = review;
                        stored.Version++;
                        stored.UpdatedAt = now;
                        stored.UpdatedBy = by;
                        changed++;
                    }
                }
            }
        }

        // What the collection no longer has.
        foreach (var gone in held.Values)
        {
            Record(id, gone.Group, gone.RecordId, RecordChangeKind.Removed, gone.Version + 1, by, now, reason, gone.Body, null);
            _context.Records.Remove(gone);
            removed++;
        }

        // The translations, by their language.
        var translations = _context.Translations.Where(translation => translation.CollectionId == id).ToDictionary(translation => translation.Language);
        foreach (var (language, document) in documents)
        {
            var body = document.ToJsonString();
            if (!translations.Remove(language, out var stored))
            {
                _context.Translations.Add(new StoredTranslation { CollectionId = id, Language = language, Body = body, UpdatedAt = now, UpdatedBy = by });
                Record(id, TranslationGroup, language, RecordChangeKind.Added, 1, by, now, reason, null, body);
                added++;
            }
            else if (!JsonNode.DeepEquals(JsonNode.Parse(stored.Body), document))
            {
                var version = _context.Changes.Count(change => change.CollectionId == id && change.Group == TranslationGroup && change.RecordId == language) + 1;
                Record(id, TranslationGroup, language, RecordChangeKind.Changed, version, by, now, reason, stored.Body, body);
                stored.Body = body;
                stored.UpdatedAt = now;
                stored.UpdatedBy = by;
                changed++;
            }
        }

        foreach (var gone in translations.Values)
        {
            var version = _context.Changes.Count(change => change.CollectionId == id && change.Group == TranslationGroup && change.RecordId == gone.Language) + 1;
            Record(id, TranslationGroup, gone.Language, RecordChangeKind.Removed, version, by, now, reason, gone.Body, null);
            _context.Translations.Remove(gone);
            removed++;
        }

        // Everything of the collection that is not a record: the groups stay in it as empty lists and keep their place.
        var header = (JsonObject)root.DeepClone();
        foreach (var group in Groups)
        {
            if (OptionalGroups.Contains(group))
            {
                header.Remove(group);
            }
            else
            {
                header[group] = new JsonArray();
            }
        }

        var written = header.ToJsonString();
        if (isNew)
        {
            _context.Collections.Add(new StoredCollection { Id = id, Header = written, CreatedAt = now, UpdatedAt = now });
        }
        else if (collection!.Header != written || added + changed + removed > 0)
        {
            if (collection.Header != written)
            {
                // What is not a record changed: it is kept with who, when and why like any record.
                var version = _context.Changes.Count(change => change.CollectionId == id && change.Group == CollectionGroup) + 1;
                Record(id, CollectionGroup, id, RecordChangeKind.Changed, version, by, now, reason, collection.Header, written);
                changed++;
            }

            collection.Header = written;
            collection.UpdatedAt = now;
        }

        _context.SaveChanges();
        transaction.Commit();
        return (added, changed, removed);
    }

    /// <summary>
    /// Writes a collection out of the store as its file and the files of its translations, or returns nothing
    /// for a collection the store does not hold. The file reads as the one that was brought in.
    /// </summary>
    public ExportedCollection? Export(string collectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);
        var collection = _context.Collections.AsNoTracking().SingleOrDefault(item => item.Id == collectionId);
        if (collection is null)
        {
            return null;
        }

        var root = JsonNode.Parse(collection.Header)!.AsObject();
        var records = _context.Records.AsNoTracking().Where(record => record.CollectionId == collectionId).ToList();
        foreach (var group in Groups)
        {
            var written = new JsonArray([.. records.Where(record => record.Group == group).OrderBy(record => record.Position).Select(record => JsonNode.Parse(record.Body))]);
            if (written.Count > 0 || !OptionalGroups.Contains(group))
            {
                root[group] = written;
            }
        }

        var translations = _context.Translations.AsNoTracking().Where(translation => translation.CollectionId == collectionId).ToList()
            .ToImmutableSortedDictionary(translation => translation.Language, translation => JsonNode.Parse(translation.Body)!.ToJsonString(Written), StringComparer.Ordinal);
        return new ExportedCollection(root.ToJsonString(Written), translations);
    }

    /// <summary>Returns the history of a collection, the oldest change first – of one record when it is named.</summary>
    public ImmutableArray<RecordChange> History(string collectionId, string? group = null, string? recordId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);
        var changes = _context.Changes.AsNoTracking().Where(change => change.CollectionId == collectionId);
        if (group is not null)
        {
            changes = changes.Where(change => change.Group == group);
        }

        if (recordId is not null)
        {
            changes = changes.Where(change => change.RecordId == recordId);
        }

        return [.. changes.OrderBy(change => change.Id)];
    }

    /// <inheritdoc />
    public void Dispose() => _context.Dispose();

    // The translation documents beside a collection: <name>.<language>.json.
    private static IEnumerable<string> TranslationFiles(string sampleFile) =>
        Directory.EnumerateFiles(Path.GetDirectoryName(sampleFile)!, Path.GetFileNameWithoutExtension(sampleFile) + ".*.json").Order(StringComparer.Ordinal);

    private void Record(string collection, string group, string record, RecordChangeKind kind, int version, string by, DateTime at, string? reason, string? before, string? after) =>
        _context.Changes.Add(new RecordChange
        {
            CollectionId = collection, Group = group, RecordId = record, Kind = kind, Version = version,
            By = by, At = at, Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), Before = before, After = after,
        });
}
