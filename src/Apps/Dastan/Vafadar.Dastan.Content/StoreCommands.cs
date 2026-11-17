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
        """;

    /// <summary>Runs a command of the store; <see langword="null"/> when it was not understood.</summary>
    public static int? Run(IReadOnlyList<string> words, TextWriter output, TimeProvider time)
    {
        if (words.Count < 2 || Options(words.Skip(2).ToList()) is not { } given || !given.TryGetValue("--store", out var database))
        {
            return null;
        }

        return words[0] switch
        {
            "import" when given.TryGetValue("--by", out var by) && Only(given, "--store", "--by", "--reason") => Import(words[1], database, by, given.GetValueOrDefault("--reason"), output, time),
            "export" when given.TryGetValue("--out", out var target) && Only(given, "--store", "--out") => Export(words[1], database, target, output, time),
            "history" when Only(given, "--store", "--record") => History(words[1], database, given.GetValueOrDefault("--record"), output, time),
            _ => null,
        };
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
