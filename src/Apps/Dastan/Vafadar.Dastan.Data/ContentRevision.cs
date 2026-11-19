using System.Collections.Immutable;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Data;

/// <summary>What the editors ask for when they publish: which file, as which revision, on which day.</summary>
/// <param name="SampleFile">The working file of the editors – an editorial sample.</param>
/// <param name="Number">The number of the revision: 1 for the first, otherwise one more than the revision before.</param>
/// <param name="PublishedOn">The day the revision is published.</param>
/// <param name="PreviousFile">The file of the revision before; required from the second revision on.</param>
/// <param name="Note">What the editors say about the revision, if anything.</param>
public sealed record RevisionRequest(string SampleFile, int Number, DateOnly PublishedOn, string? PreviousFile = null, string? Note = null);

/// <summary>A published revision as a file, or the reasons there is none.</summary>
/// <param name="Json">The file of the revision; <see langword="null"/> when it cannot be made.</param>
/// <param name="Problems">Why the revision cannot be made, each with the record it is about.</param>
/// <param name="Changes">How many records the revision adds, changes and removes against the revision before.</param>
public sealed record RevisionResult(string? Json, ImmutableArray<string> Problems, (int Added, int Changed, int Removed) Changes = default);

/// <summary>
/// Makes a published revision out of the working file of the editors (ADR 0009): the same records, marked as
/// the revision they are, with the history of the revisions before and what this one changes against the last.
/// It decides nothing – a statement that is not reviewed, a review that is not recorded, a number out of turn
/// are reasons to make no revision. Publishing stays what the editors do with the file it writes.
/// </summary>
public static class ContentRevision
{
    // The groups of records of a collection and what a record of each is called in a history.
    private static readonly (string Group, string Kind)[] Groups =
    [
        ("sources", "source"),
        ("entities", "entity"),
        ("relations", "relation"),
        ("events", "event"),
        ("media", "media"),
    ];

    private static readonly JsonSerializerOptions Written = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Makes the revision, or says why it cannot be made.</summary>
    public static RevisionResult Make(RevisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The working file has to be one the site reads as it stands.
        var checkedSample = ContentCheck.File(request.SampleFile);
        if (!checkedSample.IsUsable)
        {
            return new RevisionResult(null, checkedSample.Problems);
        }

        if (checkedSample.Format != PilotSample.SchemaVersion || Path.GetFullPath(request.SampleFile) != checkedSample.File)
        {
            return Refused($"{Path.GetFileName(request.SampleFile)}: a revision is made from the file of a collection.");
        }

        var sample = JsonNode.Parse(File.ReadAllText(checkedSample.File))!.AsObject();
        JsonObject? previous = null;
        if (request.PreviousFile is not null)
        {
            var checkedPrevious = ContentCheck.File(request.PreviousFile);
            if (!checkedPrevious.IsUsable)
            {
                return new RevisionResult(null, checkedPrevious.Problems);
            }

            previous = JsonNode.Parse(File.ReadAllText(checkedPrevious.File))!.AsObject();
        }

        // The revision is read as the site will read it: with the translations beside the working file and its files.
        var directory = Path.GetDirectoryName(checkedSample.File)!;
        var content = Path.GetDirectoryName(directory) ?? directory;
        return Make(
            sample,
            Path.GetFileName(request.SampleFile),
            previous,
            request.PreviousFile is null ? null : Path.GetFileName(request.PreviousFile),
            request.Number,
            request.PublishedOn,
            request.Note,
            json => Problems(json, new PilotSampleOptions
            {
                SampleDirectory = directory,
                ContentRoot = content,
                ReviewRoot = Path.GetDirectoryName(content) ?? content,
                Translations =
                [
                    .. Directory.EnumerateFiles(directory, Path.GetFileNameWithoutExtension(checkedSample.File) + ".*.json").Order(StringComparer.Ordinal).Select(file => Translation(sample, File.ReadAllText(file))),
                ],
            }));
    }

    /// <summary>Returns what the readers of the site say about a collection; empty when they accept it.</summary>
    public static ImmutableArray<string> Problems(string json, PilotSampleOptions options)
    {
        try
        {
            PilotSample.Read(json, options);
            return [];
        }
        catch (SampleFormatException refused)
        {
            return [.. refused.Problems];
        }
    }

    /// <summary>
    /// Makes the revision out of a working collection that is not a file – the store of the editors holds it as
    /// records. The rules are those of <see cref="Make(RevisionRequest)"/>; nothing is read from a disk here.
    /// </summary>
    /// <param name="working">The working collection; it is not changed.</param>
    /// <param name="workingName">What to call it when it is refused.</param>
    /// <param name="previous">The revision before; nothing for the first revision.</param>
    /// <param name="previousName">What to call the revision before when it is refused.</param>
    /// <param name="number">The number of the revision.</param>
    /// <param name="publishedOn">The day it is published.</param>
    /// <param name="note">What the editors say about it, if anything.</param>
    /// <param name="problemsOf">Reads the revision as the site will read it and returns what is wrong with it.</param>
    public static RevisionResult Make(
        JsonObject working,
        string workingName,
        JsonObject? previous,
        string? previousName,
        int number,
        DateOnly publishedOn,
        string? note,
        Func<string, ImmutableArray<string>> problemsOf)
    {
        ArgumentNullException.ThrowIfNull(working);
        ArgumentNullException.ThrowIfNull(problemsOf);

        var sample = (JsonObject)working.DeepClone();
        var publication = sample["publication"]!.AsObject();
        if ((string?)publication["state"] != "editorial_sample")
        {
            return Refused($"{workingName}: a revision is made from the working file of the editors, an editorial_sample; this file is a revision already.");
        }

        if (number < 1)
        {
            return Refused("the number of a revision starts at 1.");
        }

        // The revisions before this one, and the records of the last of them.
        var history = new JsonArray();
        if (previous is null)
        {
            if (number != 1)
            {
                return Refused($"revision {Number(number)} follows revision {Number(number - 1)}: name the file of that revision.");
            }
        }
        else
        {
            var before = previous["publication"]!.AsObject();
            if ((string?)before["state"] != "published_revision")
            {
                return Refused($"{previousName}: the file before is not a published revision.");
            }

            if ((string?)previous["datasetId"] != (string?)sample["datasetId"])
            {
                return Refused($"{previousName}: it is a revision of {(string?)previous["datasetId"]}, not of {(string?)sample["datasetId"]}.");
            }

            var last = before["revision"]!.AsObject();
            if ((int)last["number"]! != number - 1)
            {
                return Refused($"revision {Number(number)} does not follow revision {Number((int)last["number"]!)} of {previousName}.");
            }

            // A revision written before histories were kept has none: its own entry is all that is known of it.
            if (before["history"] is JsonArray kept)
            {
                foreach (var entry in kept)
                {
                    history.Add(entry!.DeepClone());
                }
            }
            else
            {
                history.Add(new JsonObject { ["number"] = last["number"]!.DeepClone(), ["publishedAt"] = last["publishedAt"]!.DeepClone(), ["changes"] = new JsonArray() });
            }
        }

        LeaveOutReviewerDocuments(sample);
        var withdrawn = Withdraw(sample);
        var changes = previous is null ? [] : Compare(previous, sample, withdrawn);
        var day = publishedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var entered = new JsonObject { ["number"] = number, ["publishedAt"] = day };
        if (!string.IsNullOrWhiteSpace(note))
        {
            entered["note"] = note.Trim();
        }

        entered["changes"] = new JsonArray([.. changes.Select(change => (JsonNode)change)]);
        history.Add(entered);

        publication["state"] = "published_revision";
        publication["publicReleaseApproved"] = true;
        publication["revision"] = new JsonObject { ["number"] = number, ["publishedAt"] = day };
        publication["history"] = history;
        var json = sample.ToJsonString(Written);

        // The rule of what everybody may see is asked here: what is not ready is named now, not found out by a
        // public site.
        var problems = problemsOf(json);
        if (!problems.IsEmpty)
        {
            return new RevisionResult(null, problems);
        }

        int Count(string kind) => changes.Count(change => (string?)change["kind"] == kind);
        return new RevisionResult(json, [], (Count("added"), Count("changed"), Count("removed") + Count("withdrawn")));
    }

    // A translation beside the working file, as the revision takes it: without the texts of statements that
    // were withdrawn, which the revision does not hold. A file that is no document is left to the reader, which
    // says what is wrong with it.
    private static string Translation(JsonObject working, string text)
    {
        try
        {
            return JsonNode.Parse(text) is JsonObject translation ? WithoutWithdrawn(working, translation).ToJsonString() : text;
        }
        catch (System.Text.Json.JsonException)
        {
            return text;
        }
    }

    /// <summary>
    /// Returns the texts of a translation without those of statements the editors withdrew: a revision does not
    /// hold a withdrawn statement, and a translation names only what its collection has.
    /// </summary>
    /// <param name="working">The working collection the translation belongs to.</param>
    /// <param name="translation">The translation document; it is not changed.</param>
    public static JsonObject WithoutWithdrawn(JsonObject working, JsonObject translation)
    {
        ArgumentNullException.ThrowIfNull(working);
        ArgumentNullException.ThrowIfNull(translation);

        static bool IsWithdrawn(JsonNode? record) => (string?)record!["reviewStatus"] == "withdrawn";

        var kept = (JsonObject)translation.DeepClone();
        foreach (var group in new[] { "relations", "events" })
        {
            foreach (var record in working[group]!.AsArray().Where(IsWithdrawn))
            {
                (kept[group] as JsonObject)?.Remove((string)record!["id"]!);
            }
        }

        foreach (var dated in working["entities"]!.AsArray().SelectMany(entity => entity!["dated"] as JsonArray ?? []).Where(IsWithdrawn))
        {
            (kept["dated"] as JsonObject)?.Remove((string)dated!["key"]!);
        }

        return kept;
    }

    private static RevisionResult Refused(string problem) => new(null, [problem]);

    private static string Number(int number) => number.ToString(CultureInfo.InvariantCulture);

    // What the new file changes against the last revision, record by record: a record is the same when it is
    // written the same. The changes keep the name and the reading stage of their record, so that a page can
    // show a change to those who may see the record and to nobody else.
    private static List<JsonObject> Compare(JsonObject before, JsonObject after, Dictionary<string, string> withdrawn)
    {
        var changes = new List<JsonObject>();
        foreach (var (group, kind) in Groups)
        {
            var earlier = Records(before, group);
            var later = Records(after, group);
            foreach (var (id, record) in later)
            {
                if (!earlier.TryGetValue(id, out var was))
                {
                    changes.Add(Change("added", id, kind, record));
                }
                else if (!JsonNode.DeepEquals(was, record))
                {
                    changes.Add(Change("changed", id, kind, record));
                }
            }

            foreach (var (id, record) in earlier.Where(entry => !later.ContainsKey(entry.Key)))
            {
                // A statement the editors withdrew is not just gone: the history says so, and why.
                var change = Change(withdrawn.ContainsKey(id) ? "withdrawn" : "removed", id, kind, record);
                if (withdrawn.TryGetValue(id, out var reason))
                {
                    change["reason"] = reason;
                }

                changes.Add(change);
            }
        }

        return changes;
    }

    // A revision is what a site is given: the folder of content and nothing beside it. The working documents of
    // reviewers a media item points to stay with the editors – the readers never show them, and a site should
    // not need them to start.
    private static void LeaveOutReviewerDocuments(JsonObject sample)
    {
        foreach (var item in sample["media"]!.AsArray())
        {
            item!.AsObject().Remove("reviewerProvenancePath");
            item.AsObject().Remove("reviewerProvenanceAudience");
        }
    }

    // Takes the statements the editors withdrew out of the file that becomes the revision, and returns why each
    // relation and event was withdrawn. A dated statement is part of its entity: withdrawing it changes the entity.
    private static Dictionary<string, string> Withdraw(JsonObject sample)
    {
        static bool IsWithdrawn(JsonNode? record) => (string?)record!["reviewStatus"] == "withdrawn";

        var reasons = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var group in new[] { "relations", "events" })
        {
            var records = sample[group]!.AsArray();
            foreach (var record in records.Where(IsWithdrawn).ToList())
            {
                reasons[(string)record!["id"]!] = (string)record["review"]!["note"]!;
                records.Remove(record);
            }
        }

        foreach (var entity in sample["entities"]!.AsArray())
        {
            if (entity!["dated"] is JsonArray dated)
            {
                foreach (var statement in dated.Where(IsWithdrawn).ToList())
                {
                    dated.Remove(statement);
                }
            }
        }

        return reasons;
    }

    private static Dictionary<string, JsonObject> Records(JsonObject collection, string group) =>
        collection[group]!.AsArray().Select(node => node!.AsObject()).ToDictionary(record => (string)record["id"]!, StringComparer.Ordinal);

    private static JsonObject Change(string kind, string id, string recordKind, JsonObject record) => new()
    {
        ["kind"] = kind,
        ["record"] = id,
        ["recordKind"] = recordKind,
        ["label"] = (string?)record["label"] ?? (string)record["title"]!,
        ["spoilerLevel"] = (int)record["spoilerLevel"]!,
    };
}
