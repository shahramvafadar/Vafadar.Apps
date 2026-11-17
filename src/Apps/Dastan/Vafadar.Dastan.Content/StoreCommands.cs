using System.Globalization;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Content;

/// <summary>
/// The commands of the editors' store (ADR 0011): a working file goes into the store, comes out of it, and the
/// store says what was changed, by whom and why. They are part of the command of the writers because the file
/// of a collection is what both work with.
/// </summary>
internal static class StoreCommands
{
    public const string Usage = """
          store import <file> --store <database> --by <name> [--reason <text>]
              For the editors: brings their working file into the store, with the translations beside it. A
              record that reads differently than before becomes a change with who, when and why.

          store export <collection> --store <database> --out <file>
              Writes a collection out of the store as its file, with its translations beside it, and checks
              it where it lies. Never over a file.

          store history <collection> --store <database> [--record <id>]
              Lists what was changed in a collection, the oldest change first.

          store waiting <collection> --store <database>
              Lists the statements that still wait for a review.

          store review <collection> <statement> --store <database> --by <name> --as reviewed|disputed|withdrawn
                       [--on <yyyy-MM-dd>] [--note <text>] [--files <samples folder>]
              Records the review of a relation, an event or a statement with a date. A disputed statement
              needs a note that says what the sources disagree about, a withdrawn one why it is taken back.

          store put <collection> <group> --from <file> --store <database> --by <name> --reason <text> [--files <samples folder>]
              Puts the one record of the file into the collection: in place of the record with its id, or new.

          store remove <collection> <record> --store <database> --by <name> --reason <text> [--files <samples folder>]
              Takes a record out of the collection, with the texts that translate it.

              review, put and remove are tried on the whole collection first: what the site could not use
              afterwards is refused with the reasons, and nothing is changed.

          store publish <collection> --store <database> --by <name> --date <yyyy-MM-dd> --out <file>
                        [--note <text>] [--files <samples folder>]
              Makes the next published revision out of the store – numbered after the last one, with what
              it changes – keeps it, and writes it with its translations beside it. Refuses a collection in
              which a statement is not reviewed. Never over a file.

          store revisions <collection> --store <database>
              Lists the revisions the store published.

          store revision <collection> <number> --store <database> --out <file>
              Writes a published revision again, as it was published. Never over a file.
        """;

    /// <summary>Runs a command of the store; <see langword="null"/> when it was not understood.</summary>
    public static int? Run(IReadOnlyList<string> words, TextWriter output, TimeProvider time)
    {
        if (words.Count < 2)
        {
            return null;
        }

        if (words[0] is "review" or "put" or "remove")
        {
            return Acts(words, time, output);
        }

        if (words[0] == "revision")
        {
            return words.Count >= 3 && int.TryParse(words[2], NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                && Options(words.Skip(3).ToList()) is { } asked && asked.TryGetValue("--store", out var held) && asked.TryGetValue("--out", out var file) && Only(asked, "--store", "--out")
                ? Published(words[1], number, held, file, output, time)
                : null;
        }

        if (Options(words.Skip(2).ToList()) is not { } given || !given.TryGetValue("--store", out var database))
        {
            return null;
        }

        return words[0] switch
        {
            "import" when given.TryGetValue("--by", out var by) && Only(given, "--store", "--by", "--reason") => Import(words[1], database, by, given.GetValueOrDefault("--reason"), output, time),
            "export" when given.TryGetValue("--out", out var target) && Only(given, "--store", "--out") => Export(words[1], database, target, output, time),
            "history" when Only(given, "--store", "--record") => History(words[1], database, given.GetValueOrDefault("--record"), output, time),
            "waiting" when Only(given, "--store") => Waiting(words[1], database, output, time),
            "revisions" when Only(given, "--store") => Revisions(words[1], database, output, time),
            "publish" when given.TryGetValue("--by", out var publisher) && given.TryGetValue("--out", out var revision) && given.TryGetValue("--date", out var dated)
                && DateOnly.TryParseExact(dated, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                && Only(given, "--store", "--by", "--date", "--out", "--note", "--files")
                => Publish(words[1], database, publisher, day, revision, given.GetValueOrDefault("--note"), given.GetValueOrDefault("--files"), output, time),
            _ => null,
        };
    }

    // The acts name a second thing after the collection – a statement, a group, a record – before their options.
    private static int? Acts(IReadOnlyList<string> words, TimeProvider time, TextWriter output)
    {
        if (words.Count < 3 || Options(words.Skip(3).ToList()) is not { } given || !given.TryGetValue("--by", out var by) || !given.TryGetValue("--store", out var database))
        {
            return null;
        }

        var files = given.GetValueOrDefault("--files");
        switch (words[0])
        {
            case "review" when given.TryGetValue("--as", out var found) && Only(given, "--store", "--by", "--as", "--on", "--note", "--files"):
                var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                if (Enum.TryParse<ReviewAct>(found, ignoreCase: true, out var act) && found.All(char.IsLetter)
                    && (!given.TryGetValue("--on", out var dated) || DateOnly.TryParseExact(dated, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day)))
                {
                    return Said(output, $"{words[2]}: {found.ToLowerInvariant()}.", store => store.Review(words[1], words[2], act, by, day, given.GetValueOrDefault("--note")), database, time, files);
                }

                return null;

            case "put" when given.TryGetValue("--from", out var source) && given.TryGetValue("--reason", out var why) && Only(given, "--store", "--by", "--from", "--reason", "--files"):
                if (!File.Exists(source) || Read(source) is not { } record)
                {
                    output.WriteLine($"{source}: the file does not hold one record – a JSON object with an id.");
                    return ContentCommands.NotUsable;
                }

                return Said(output, $"{(string?)record["id"]}: put into {words[2]}.", store => store.Put(words[1], words[2], record, by, why), database, time, files);

            case "remove" when given.TryGetValue("--reason", out var reason) && Only(given, "--store", "--by", "--reason", "--files"):
                return Said(output, $"{words[2]}: taken out.", store => store.Remove(words[1], words[2], by, reason), database, time, files);

            default:
                return null;
        }
    }

    private static System.Text.Json.Nodes.JsonObject? Read(string file)
    {
        try
        {
            return System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file)) as System.Text.Json.Nodes.JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    // Does an act and says what came of it.
    private static int Said(TextWriter output, string done, Func<EditorialStore, ActResult> act, string database, TimeProvider time, string? files)
    {
        using var store = EditorialStore.Open(database, time, files);
        var result = act(store);
        if (result.Done)
        {
            output.WriteLine(result.Changed == 0 ? $"{done} Nothing had to be changed." : done);
            return ContentCommands.Done;
        }

        output.WriteLine($"Not done: {result.Problems.Length} {(result.Problems.Length == 1 ? "reason" : "reasons")}. Nothing was changed.");
        for (var index = 0; index < result.Problems.Length; index++)
        {
            output.WriteLine($"  {index + 1}. {result.Problems[index]}");
        }

        return ContentCommands.NotUsable;
    }

    private static int Waiting(string collection, string database, TextWriter output, TimeProvider time)
    {
        using var store = EditorialStore.Open(database, time);
        if (!store.Collections.Contains(collection))
        {
            output.WriteLine($"{collection}: the store holds no collection of that name.");
            return ContentCommands.NotUsable;
        }

        var all = store.Statements(collection);
        var waiting = store.Statements(collection, waitingOnly: true);
        output.WriteLine($"{collection}: {waiting.Length} of {all.Length} statements wait for a review.");
        foreach (var statement in waiting)
        {
            output.WriteLine($"  {statement.Statement}  ({statement.Group}{(statement.Statement == statement.Record ? null : $", {statement.Record}")})  {statement.Label}  – {statement.ReviewStatus}");
        }

        return ContentCommands.Done;
    }

    // Every option is a name and a value; each is given once.
    private static Dictionary<string, string>? Options(List<string> options)
    {
        var given = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < options.Count; index += 2)
        {
            if (index + 1 >= options.Count || !options[index].StartsWith("--", StringComparison.Ordinal) || !given.TryAdd(options[index], options[index + 1]))
            {
                return null;
            }
        }

        return given;
    }

    private static bool Only(Dictionary<string, string> given, params string[] names) => given.Keys.All(names.Contains);

    private static int Import(string file, string database, string by, string? reason, TextWriter output, TimeProvider time)
    {
        using var store = EditorialStore.Open(database, time);
        var result = store.Import(file, by, reason);
        if (result.CollectionId is null)
        {
            output.WriteLine($"Not brought in: {result.Problems.Length} {(result.Problems.Length == 1 ? "reason" : "reasons")}.");
            for (var index = 0; index < result.Problems.Length; index++)
            {
                output.WriteLine($"  {index + 1}. {result.Problems[index]}");
            }

            return ContentCommands.NotUsable;
        }

        output.WriteLine($"{result.CollectionId}: {result.Added} added, {result.Changed} changed, {result.Removed} removed.");
        return ContentCommands.Done;
    }

    private static int Export(string collection, string database, string target, TextWriter output, TimeProvider time)
    {
        using var store = EditorialStore.Open(database, time);
        if (store.Export(collection) is not { } exported)
        {
            output.WriteLine($"{collection}: the store holds no collection of that name. It holds: {(store.Collections.IsEmpty ? "none" : string.Join(", ", store.Collections))}.");
            return ContentCommands.NotUsable;
        }

        return WriteOut(exported, target, output);
    }

    private static int Publish(string collection, string database, string by, DateOnly day, string target, string? note, string? files, TextWriter output, TimeProvider time)
    {
        if (Existing(target) is { } existing)
        {
            output.WriteLine($"{existing} exists; the store never writes over a file.");
            return ContentCommands.NotUnderstood;
        }

        using var store = EditorialStore.Open(database, time, files);
        var result = store.Publish(collection, day, by, note);
        if (result.Revision is null)
        {
            output.WriteLine($"No revision: {result.Problems.Length} {(result.Problems.Length == 1 ? "reason" : "reasons")}. Nothing was published.");
            for (var index = 0; index < result.Problems.Length; index++)
            {
                output.WriteLine($"  {index + 1}. {result.Problems[index]}");
            }

            return ContentCommands.NotUsable;
        }

        output.WriteLine($"Revision {result.Number} of {day:yyyy-MM-dd}: {result.Changes.Added} added, {result.Changes.Changed} changed, {result.Changes.Removed} removed against the revision before. The store keeps it.");
        return WriteOut(result.Revision, target, output);
    }

    private static int Revisions(string collection, string database, TextWriter output, TimeProvider time)
    {
        using var store = EditorialStore.Open(database, time);
        if (!store.Collections.Contains(collection))
        {
            output.WriteLine($"{collection}: the store holds no collection of that name.");
            return ContentCommands.NotUsable;
        }

        var revisions = store.Revisions(collection);
        output.WriteLine($"{collection}: {revisions.Length} {(revisions.Length == 1 ? "revision" : "revisions")} published.");
        foreach (var revision in revisions)
        {
            output.WriteLine($"  {revision.Number}  {revision.PublishedOn:yyyy-MM-dd}  by {revision.By}{(revision.Note is null ? null : $"  – {revision.Note}")}");
        }

        return ContentCommands.Done;
    }

    private static int Published(string collection, int number, string database, string target, TextWriter output, TimeProvider time)
    {
        using var store = EditorialStore.Open(database, time);
        if (store.Revision(collection, number) is not { } revision)
        {
            output.WriteLine($"{collection}: the store did not publish a revision {number}.");
            return ContentCommands.NotUsable;
        }

        return WriteOut(revision, target, output);
    }

    // The file a collection would be written to, or one of its translations beside it, when it is there already.
    private static string? Existing(string target)
    {
        var path = Path.GetFullPath(target);
        var folder = Path.GetDirectoryName(path)!;
        return File.Exists(path) ? path
            : Directory.Exists(folder) ? Directory.EnumerateFiles(folder, Path.GetFileNameWithoutExtension(path) + ".*.json").FirstOrDefault()
            : null;
    }

    // Writes a collection and its translations beside it – never over a file – and checks it where it lies.
    private static int WriteOut(ExportedCollection exported, string target, TextWriter output)
    {
        var path = Path.GetFullPath(target);
        var beside = exported.Translations.ToDictionary(
            translation => Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}.{translation.Key}.json"),
            translation => translation.Value);
        if (beside.Keys.Append(path).FirstOrDefault(File.Exists) is { } existing)
        {
            output.WriteLine($"{existing} exists; the store never writes over a file.");
            return ContentCommands.NotUnderstood;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, exported.Json + Environment.NewLine);
        foreach (var (file, json) in beside)
        {
            File.WriteAllText(file, json + Environment.NewLine);
        }

        output.WriteLine($"Written: {path}{(beside.Count == 0 ? null : $" and {beside.Count} {(beside.Count == 1 ? "translation" : "translations")} beside it")}");

        // The file is read where it lies, as the site reads it: its media are named by paths relative to it.
        var report = ContentCheck.File(path);
        if (report.IsUsable)
        {
            output.WriteLine("The site can use it.");
            return ContentCommands.Done;
        }

        output.WriteLine($"The site cannot use it where it lies: {report.Problems.Length} {(report.Problems.Length == 1 ? "problem" : "problems")}.");
        for (var index = 0; index < report.Problems.Length; index++)
        {
            output.WriteLine($"  {index + 1}. {report.Problems[index]}");
        }

        return ContentCommands.NotUsable;
    }

    private static int History(string collection, string database, string? record, TextWriter output, TimeProvider time)
    {
        using var store = EditorialStore.Open(database, time);
        if (!store.Collections.Contains(collection))
        {
            output.WriteLine($"{collection}: the store holds no collection of that name.");
            return ContentCommands.NotUsable;
        }

        var changes = store.History(collection, recordId: record);
        output.WriteLine($"{collection}{(record is null ? null : $", {record}")}: {changes.Length} {(changes.Length == 1 ? "change" : "changes")}.");
        foreach (var change in changes)
        {
            var at = change.At.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            output.WriteLine($"  {at}  {change.Kind,-7}  {change.Group}/{change.RecordId}  version {change.Version}  by {change.By}{(change.Reason is null ? null : $"  – {change.Reason}")}");
        }

        return ContentCommands.Done;
    }
}
