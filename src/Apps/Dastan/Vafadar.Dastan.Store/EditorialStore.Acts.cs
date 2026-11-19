using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store;

/// <summary>What an act of an editor did, or why it was not done.</summary>
/// <param name="Problems">Why the act was refused, each with the record it is about; empty when it was done.</param>
/// <param name="Changed">How many records and translations read differently afterwards.</param>
public sealed record ActResult(ImmutableArray<string> Problems, int Changed = 0)
{
    /// <summary>Gets whether the act was done.</summary>
    public bool Done => Problems.IsEmpty;
}

/// <summary>What a review says about a statement.</summary>
public enum ReviewAct
{
    /// <summary>The statement was read against its sources and stands.</summary>
    Reviewed = 0,

    /// <summary>The sources disagree about the statement; it stands as disputed.</summary>
    Disputed = 1,

    /// <summary>The editors take the statement back.</summary>
    Withdrawn = 2,
}

/// <summary>A statement of a collection and how far its review has come.</summary>
/// <param name="Group">The group of the record that holds the statement.</param>
/// <param name="Record">The identifier of that record.</param>
/// <param name="Statement">The name of the statement: the identifier of a relation or an event, the key of a dated statement.</param>
/// <param name="Label">The statement in the editors' words.</param>
/// <param name="ReviewStatus">The state of its review, as the record says it.</param>
public sealed record StatementState(string Group, string Record, string Statement, string Label, string ReviewStatus);

// The acts of an editor on one record (step 11.2). Each act is tried on the whole collection: the collection as
// it would be is read by the readers of the site, and only a collection they accept is kept. The store has no
// rule of its own about what a record may say.
public sealed partial class EditorialStore
{
    private static readonly ImmutableArray<string> StatementGroups = ["relations", "events"];
    private static readonly ImmutableArray<string> Settled = ["reviewed", "disputed", "withdrawn"];

    /// <summary>
    /// Puts a record into a collection: in place of the record with the same identifier, or as a new one at the
    /// end of its group.
    /// </summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="group">The group of the record: <c>sources</c>, <c>entities</c>, <c>relations</c>, <c>events</c> or <c>media</c>.</param>
    /// <param name="record">The record as it is written in the file of a collection.</param>
    /// <param name="by">Who puts it there.</param>
    /// <param name="reason">Why.</param>
    /// <param name="texts">
    /// The texts of the record in the other languages, by the language, as a translation writes them for the
    /// record; they are kept in the same act, so that a record and what translates it never disagree. A language
    /// that is not named keeps its texts; a language named with no texts loses them.
    /// </param>
    /// <param name="dated">
    /// For an entity: the texts of its statements with a date in the other languages, by the language and then
    /// by the key of the statement. A language that is named keeps exactly the texts given for the statements
    /// the record has now; the texts of a statement that was taken out go with it.
    /// </param>
    public ActResult Put(string collectionId, string group, JsonObject record, string by, string reason, IReadOnlyDictionary<string, JsonObject>? texts = null, IReadOnlyDictionary<string, JsonObject>? dated = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (!Groups.Contains(group))
        {
            return Refused($"{group}: a collection has no such group of records; it has {string.Join(", ", Groups)}.");
        }

        if (record["id"] is not JsonValue named || !named.TryGetValue<string>(out var id) || string.IsNullOrWhiteSpace(id))
        {
            return Refused("a record names itself: it has an id.");
        }

        return Act(collectionId, by, reason, (root, translations) =>
        {
            foreach (var (language, translated) in texts ?? new Dictionary<string, JsonObject>())
            {
                if (!translations.TryGetValue(language, out var translation))
                {
                    return $"{language}: the collection has no texts in that language; a language is added by importing its file.";
                }

                if (translated.Count > 0)
                {
                    if (translation[group] is not JsonObject named)
                    {
                        translation[group] = named = [];
                    }

                    named[id] = translated.DeepClone();
                }
                else
                {
                    (translation[group] as JsonObject)?.Remove(id);
                }
            }

            var records = root[group]!.AsArray();
            var place = Enumerable.Range(0, records.Count).Where(index => (string?)records[index]!["id"] == id).Cast<int?>().FirstOrDefault();
            if (!WritesReviews && ReviewWritten(place is { } there ? records[there] : null, record) is { } reviewed)
            {
                return reviewed;
            }

            if (dated is not null)
            {
                static IEnumerable<string> Keys(JsonNode? entity) =>
                    (entity?["dated"] as JsonArray ?? []).Select(statement => (string?)statement?["key"]).OfType<string>();

                var before = Keys(place is { } held ? records[held] : null).ToList();
                var now = Keys(record).ToHashSet(StringComparer.Ordinal);
                foreach (var (language, named) in dated)
                {
                    if (!translations.TryGetValue(language, out var translation))
                    {
                        return $"{language}: the collection has no texts in that language; a language is added by importing its file.";
                    }

                    var kept = translation[DatedGroup] as JsonObject;
                    foreach (var key in before)
                    {
                        kept?.Remove(key);
                    }

                    foreach (var (key, translated) in named)
                    {
                        if (now.Contains(key) && translated is JsonObject { Count: > 0 })
                        {
                            if (kept is null)
                            {
                                translation[DatedGroup] = kept = [];
                            }

                            kept[key] = translated.DeepClone();
                        }
                    }
                }
            }

            if (place is { } index)
            {
                records[index] = record.DeepClone();
            }
            else
            {
                records.Add(record.DeepClone());
            }

            return null;
        });
    }

    /// <summary>
    /// Takes a record out of a collection, with the texts that translate it. What still points to the record
    /// makes the collection unusable, and the act is refused with what points to it.
    /// </summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="recordId">The identifier of the record.</param>
    /// <param name="by">Who takes it out.</param>
    /// <param name="reason">Why.</param>
    /// <param name="group">
    /// The group the record is in. Named, only that group is looked in – a thread may have the identifier of an
    /// entity, and taking the thread out must not take the entity. Not named, the first record of that
    /// identifier in any group.
    /// </param>
    public ActResult Remove(string collectionId, string recordId, string by, string reason, string? group = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (group is not null && !Groups.Contains(group))
        {
            return Refused($"{group}: a collection has no such group of records; it has {string.Join(", ", Groups)}.");
        }

        return Act(collectionId, by, reason, (root, translations) =>
        {
            foreach (var group in group is null ? Groups : [group])
            {
                var records = root[group]!.AsArray();
                if (records.FirstOrDefault(record => (string?)record!["id"] == recordId) is { } found)
                {
                    records.Remove(found);

                    // A translation names only what the collection has.
                    foreach (var translation in translations.Values)
                    {
                        (translation[group] as JsonObject)?.Remove(recordId);
                    }

                    return null;
                }
            }

            return $"{recordId}: the collection holds no record of that name.";
        });
    }

    /// <summary>
    /// Records the review of a statement – a relation, an event, or a statement with a date of an entity: who
    /// reviewed it, on which day, and what the review found. A disputed statement says in the note what the
    /// sources disagree about, a withdrawn one why it is taken back; the readers refuse it otherwise.
    /// </summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="statement">The identifier of the relation or the event, or the key of the dated statement.</param>
    /// <param name="act">What the review found.</param>
    /// <param name="by">Who reviewed.</param>
    /// <param name="on">The day of the review.</param>
    /// <param name="note">What the statement was checked against, what is disputed, or why it is withdrawn.</param>
    public ActResult Review(string collectionId, string statement, ReviewAct act, string by, DateOnly on, string? note = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statement);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        var said = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var status = act switch { ReviewAct.Reviewed => "reviewed", ReviewAct.Disputed => "disputed", _ => "withdrawn" };
        return Act(collectionId, by, said ?? $"Review: {status}.", (root, _) =>
        {
            if (Statement(root, statement) is not { } found)
            {
                return $"{statement}: the collection holds no statement of that name – a relation, an event or a statement with a date.";
            }

            found["reviewStatus"] = status;
            var review = new JsonObject { ["by"] = by, ["on"] = on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
            if (said is not null)
            {
                review["note"] = said;
            }

            found["review"] = review;
            return null;
        });
    }

    /// <summary>
    /// Returns the statements of a collection with the state of their review, in the order of the collection –
    /// only those that still wait for a review when asked so. Empty for a collection the store does not hold.
    /// </summary>
    public ImmutableArray<StatementState> Statements(string collectionId, bool waitingOnly = false)
    {
        if (Working(collectionId) is not var (root, _))
        {
            return [];
        }

        var statements = ImmutableArray.CreateBuilder<StatementState>();
        foreach (var group in StatementGroups)
        {
            foreach (var record in root[group]!.AsArray())
            {
                statements.Add(new StatementState(group, (string)record!["id"]!, (string)record["id"]!, (string?)record["label"] ?? string.Empty, (string?)record["reviewStatus"] ?? string.Empty));
            }
        }

        foreach (var entity in root["entities"]!.AsArray())
        {
            foreach (var dated in entity!["dated"] as JsonArray ?? [])
            {
                statements.Add(new StatementState("entities", (string)entity["id"]!, (string)dated!["key"]!, (string?)dated["label"] ?? string.Empty, (string?)dated["reviewStatus"] ?? string.Empty));
            }
        }

        return waitingOnly ? [.. statements.Where(statement => !Settled.Contains(statement.ReviewStatus))] : statements.ToImmutable();
    }

    // The statement of that name: a relation, an event, or a dated statement of an entity.
    private static JsonObject? Statement(JsonObject root, string name)
    {
        foreach (var group in StatementGroups)
        {
            if (root[group]!.AsArray().FirstOrDefault(record => (string?)record!["id"] == name) is { } record)
            {
                return record.AsObject();
            }
        }

        return root["entities"]!.AsArray()
            .SelectMany(entity => entity!["dated"] as JsonArray ?? [])
            .FirstOrDefault(dated => (string?)dated!["key"] == name)?.AsObject();
    }

    /// <summary>
    /// Adds a language to a collection: an empty translation document, to which the texts of the records are
    /// then added one by one. It begins as a draft – nobody has reviewed a text of it.
    /// </summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="language">The language, as a language tag such as <c>de</c>.</param>
    /// <param name="by">Who adds it.</param>
    /// <param name="reason">Why.</param>
    public ActResult AddLanguage(string collectionId, string language, string by, string reason)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var tag = language.Trim();
        return Act(collectionId, by, reason, (root, translations) =>
        {
            if (translations.ContainsKey(tag))
            {
                return $"{tag}: the collection already has texts in that language.";
            }

            translations[tag] = new JsonObject
            {
                ["formatVersion"] = PilotSample.TranslationVersion,
                ["datasetId"] = (string)root["datasetId"]!,
                ["language"] = tag,
                ["review"] = "draft",
            };
            return null;
        });
    }

    /// <summary>
    /// Changes the reading stages of a collection: their names in the language of the file and in the other
    /// languages. A stage is added at the end; the last one is taken out by leaving it away – which the readers
    /// refuse while a record is still told at it.
    /// </summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="labels">The names of the stages from the opening on, in the language of the file.</param>
    /// <param name="texts">The names in the other languages, by the language; a name left empty is one the language does not have.</param>
    /// <param name="by">Who changes them.</param>
    /// <param name="reason">Why.</param>
    public ActResult PutStages(string collectionId, IReadOnlyList<string> labels, IReadOnlyDictionary<string, IReadOnlyList<string?>> texts, string by, string reason)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (labels.Count == 0 || labels.Any(string.IsNullOrWhiteSpace))
        {
            return Refused("a collection has at least one reading stage, and every stage has a name.");
        }

        return Act(collectionId, by, reason, (root, translations) =>
        {
            var levels = root["spoilerPolicy"]!["levels"]!.AsArray();
            while (levels.Count > labels.Count)
            {
                levels.RemoveAt(levels.Count - 1);
            }

            for (var level = 0; level < labels.Count; level++)
            {
                if (level < levels.Count)
                {
                    levels[level]!["label"] = labels[level].Trim();
                }
                else
                {
                    levels.Add(new JsonObject { ["level"] = level, ["label"] = labels[level].Trim() });
                }
            }

            foreach (var (language, names) in texts)
            {
                if (!translations.TryGetValue(language, out var translation))
                {
                    return $"{language}: the collection has no texts in that language; a language is added by importing its file.";
                }

                var stages = new JsonObject();
                for (var level = 0; level < labels.Count && level < names.Count; level++)
                {
                    if (!string.IsNullOrWhiteSpace(names[level]))
                    {
                        stages[level.ToString(System.Globalization.CultureInfo.InvariantCulture)] = names[level]!.Trim();
                    }
                }

                if (stages.Count > 0)
                {
                    translation[StagesGroup] = stages;
                }
                else
                {
                    translation.Remove(StagesGroup);
                }
            }

            // A stage that is no longer there has no name in any language.
            foreach (var translation in translations.Values)
            {
                if (translation[StagesGroup] is JsonObject named)
                {
                    foreach (var gone in named.Select(name => name.Key).Where(key => !int.TryParse(key, out var level) || level >= labels.Count).ToList())
                    {
                        named.Remove(gone);
                    }
                }
            }

            return null;
        });
    }

    // Tries an act on the collection as the store holds it and keeps the result when the readers accept it.
    private ActResult Act(string collectionId, string by, string reason, Func<JsonObject, Dictionary<string, JsonObject>, string?> change)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);

        // One step of the store from what the act reads to what it keeps, and one act at a time – a lock of
        // the database per collection. An act keeps the whole collection as it read it: if another were kept in
        // between, this one would put that act's records back to what they were, under this editor's name.
        using var step = _context.Database.BeginTransaction();
        Lock("collection:" + collectionId);
        _context.ChangeTracker.Clear();
        if (Working(collectionId) is not var (root, translations))
        {
            return Refused($"{collectionId}: the store holds no collection of that name.");
        }

        if (change(root, translations) is { } refused)
        {
            return Refused(refused);
        }

        var problems = Problems(root, translations);
        if (!problems.IsEmpty)
        {
            return new ActResult(problems);
        }

        var (added, changed, removed) = Keep(root, translations, by, reason);
        step.Commit();
        return new ActResult([], added + changed + removed);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the acts of this store may change what a review found – the
    /// status of a statement and who reviewed it. A tool with accounts says no for an editor who is no
    /// reviewer (GOV-002: writing and reviewing are kept apart): such an editor changes records, and a record
    /// that is changed keeps the review it had or goes back to waiting for one.
    /// </summary>
    public bool WritesReviews { get; set; } = true;

    private static readonly string[] Findings = ["reviewed", "disputed", "withdrawn"];

    // What a record says about reviews, by where it says it: the status and the review of the record itself,
    // of each of its statements with a date, of each period and event of a comparison.
    private static Dictionary<string, (string? Status, string? Review)> Reviews(JsonNode? node)
    {
        var said = new Dictionary<string, (string?, string?)>(StringComparer.Ordinal);
        Walk(node, string.Empty);
        return said;

        void Walk(JsonNode? at, string path)
        {
            switch (at)
            {
                case JsonObject record:
                    if (record.ContainsKey("reviewStatus") || record.ContainsKey("review"))
                    {
                        said[path] = (record["reviewStatus"] is JsonValue status && status.TryGetValue<string>(out var written) ? written : record["reviewStatus"]?.ToJsonString(), record["review"]?.ToJsonString());
                    }

                    foreach (var (name, value) in record)
                    {
                        if (name is not ("reviewStatus" or "review"))
                        {
                            Walk(value, $"{path}/{name}");
                        }
                    }

                    break;

                case JsonArray rows:
                    for (var index = 0; index < rows.Count; index++)
                    {
                        // A row is known by its key or its identifier, so that rows that change places stay themselves.
                        var name = rows[index] is JsonObject row
                            ? (row["key"] as JsonValue)?.ToString() ?? (row["id"] as JsonValue)?.ToString() ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            : index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        Walk(rows[index], $"{path}[{name}]");
                    }

                    break;
            }
        }
    }

    // Whether a record, as it is to be kept, says something about a review that only a reviewer may say: a
    // finding – reviewed, disputed, withdrawn – or who reviewed, that the record did not hold before.
    private static string? ReviewWritten(JsonNode? before, JsonNode after)
    {
        var held = Reviews(before);
        foreach (var (path, (status, review)) in Reviews(after))
        {
            var had = held.GetValueOrDefault(path);
            if ((status is not null && Findings.Contains(status, StringComparer.Ordinal) && status != had.Status) || (review is not null && review != had.Review))
            {
                return "a review is recorded by a reviewer, with the form for it: this record says what a review found, or who reviewed, and did not before.";
            }
        }

        return null;
    }

    private static ActResult Refused(string problem) => new([problem]);

    // The collection as the store holds it, to be changed and tried: its file and its translations.
    private (JsonObject Root, Dictionary<string, JsonObject> Translations)? Working(string collectionId)
    {
        var collection = _context.Collections.AsNoTracking().SingleOrDefault(item => item.Id == collectionId);
        if (collection is null)
        {
            return null;
        }

        var root = JsonNode.Parse(collection.Header)!.AsObject();
        var records = _context.Records.AsNoTracking().Where(record => record.CollectionId == collectionId).ToList();
        foreach (var group in Groups)
        {
            root[group] = new JsonArray([.. records.Where(record => record.Group == group).OrderBy(record => record.Position).Select(record => JsonNode.Parse(record.Body))]);
        }

        var translations = _context.Translations.AsNoTracking().Where(translation => translation.CollectionId == collectionId).ToList()
            .ToDictionary(translation => translation.Language, translation => JsonNode.Parse(translation.Body)!.AsObject(), StringComparer.Ordinal);
        return (root, translations);
    }

    // What the readers of the site say about a collection. The files a media item names are looked for where the
    // store was told the files of its collections lie; told nothing, the store does not look for files.
    private ImmutableArray<string> Problems(JsonObject root, Dictionary<string, JsonObject> translations) => Problems(root.ToJsonString(), translations);

    private ImmutableArray<string> Problems(string json, Dictionary<string, JsonObject> translations)
    {
        var samples = _samplesFolder ?? Directory.GetCurrentDirectory();
        var content = Path.GetDirectoryName(samples) ?? samples;
        return ContentRevision.Problems(json, new PilotSampleOptions
        {
            SampleDirectory = samples,
            ContentRoot = content,
            ReviewRoot = Path.GetDirectoryName(content) ?? content,
            CheckFiles = _samplesFolder is not null,
            Translations = [.. translations.OrderBy(translation => translation.Key, StringComparer.Ordinal).Select(translation => translation.Value.ToJsonString())],
        });
    }
}
