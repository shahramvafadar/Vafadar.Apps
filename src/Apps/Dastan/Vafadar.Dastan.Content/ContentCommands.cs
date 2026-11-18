using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Content;

/// <summary>
/// The commands of the writers of content (D-042). A person reads the plain answers; an assistant asks for
/// <c>--json</c> and gets the same answer as data. The answer of a command is also its exit code: 0 when all is
/// well, 1 when a content file cannot be used, 2 when the command itself was not understood.
/// </summary>
public static class ContentCommands
{
    /// <summary>The exit code when everything asked for is in order.</summary>
    public const int Done = 0;

    /// <summary>The exit code when a content file cannot be used.</summary>
    public const int NotUsable = 1;

    /// <summary>The exit code when the command was not understood.</summary>
    public const int NotUnderstood = 2;

    private const string Usage = """
        The command of the writers of Dastan's content. The guide is src/Apps/Dastan/docs/content-guide.md.

          check <file or folder>… [--json]
              Reads each file exactly as the site reads it and says whether the site can use it. A folder
              stands for every content file in it; a translation is checked with the file it translates.

          vocabulary [collection|comparison] [--json]
              Lists every field that takes one of a fixed set of values, with the values.

          template collection|subject|comparison <id> [--out <file>]
              Writes the smallest file of that kind the site accepts, to start from. Never over a file.
              "collection" tells a story; "subject" is about a company, a club, a person and tells none.

          revision <file> --number <n> --date <yyyy-MM-dd> --out <file> [--previous <file>] [--note <text>]
              For the editors: makes a published revision out of their working file – numbered, with the
              history of the revisions before and what this one changes. Refuses a file in which a statement
              is not reviewed or a review is not recorded. Never over a file.

          reports forget <folder> [--kept-days <n> | --looked-at-before <yyyy-MM-dd>]
              For whoever runs the site: deletes the reports of visitors that an editor looked at more than
              90 days ago – or another number of days, or before a day that is named. A report nobody has
              looked at is never deleted. Says how many were deleted. Nothing runs it by itself.

          photos sizes <folder>
              For whoever runs the site: makes the smaller size of every photograph of the visitors that has
              none yet – those that were brought in before the site made sizes. The folder is the folder
              "media" inside the folder of the photographs. Says how many were made; changes no photograph.

        """ + StoreCommands.Usage + EditorCommands.Usage + """

        Exit code: 0 in order, 1 a content file cannot be used, 2 the command was not understood.
        """;

    private static readonly JsonWriterOptions Json = new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Runs a command.</summary>
    /// <param name="arguments">The words of the command.</param>
    /// <param name="output">Where the answer is written.</param>
    /// <param name="today">The day a template is written on.</param>
    /// <returns>The exit code.</returns>
    /// <param name="time">The clock of the store; the system's when nothing is given.</param>
    /// <param name="secret">Asks for a text that is not shown while it is typed – a password; nothing when nobody can be asked.</param>
    public static int Run(IReadOnlyList<string> arguments, TextWriter output, DateOnly today, TimeProvider? time = null, Func<string, string?>? secret = null)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);

        var json = arguments.Contains("--json", StringComparer.Ordinal);
        var words = arguments.Where(argument => argument != "--json").ToList();
        return words switch
        {
            ["check", _, ..] => Check(words[1..], output, json),
            ["vocabulary"] => Vocabulary(null, output, json),
            ["vocabulary", "collection" or "comparison"] => Vocabulary(words[1], output, json),
            ["template", "collection" or "subject" or "comparison", _] => Template(words[1], words[2], null, output, today),
            ["template", "collection" or "subject" or "comparison", _, "--out", _] => Template(words[1], words[2], words[4], output, today),
            ["revision", _, ..] => Revision(words[1], words[2..], output),
            ["reports", "forget", _, "--looked-at-before", _] => ForgetReports(words[2], words[4], output),
            ["photos", "sizes", _] => PhotoSizesOf(words[2], output),
            ["reports", "forget", _] => ForgetReports(words[2], today.AddDays(-ReportsKeptDays).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), output),
            ["reports", "forget", _, "--kept-days", _] => int.TryParse(words[4], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var days) && days <= 36500
                ? ForgetReports(words[2], today.AddDays(-days).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), output)
                : NotAsked(output),
            ["store", "editor" or "editors" or "copy", ..] => EditorCommands.Run(words[1..], output, time ?? TimeProvider.System, secret) ?? NotAsked(output),
            ["store", ..] => StoreCommands.Run(words[1..], output, time ?? TimeProvider.System) ?? NotAsked(output),
            _ => NotAsked(output),
        };
    }

    /// <summary>
    /// How many days a report of a visitor is kept after an editor looked at it, when whoever runs the site
    /// names no other number (step 16.11): long enough to come back to it, not longer than it is of use.
    /// </summary>
    public const int ReportsKeptDays = 90;

    private static int ForgetReports(string folder, string day, TextWriter output)
    {
        if (!DateOnly.TryParseExact(day, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var before))
        {
            output.WriteLine($"{day} is no day; write it as 2026-11-18.");
            return NotUnderstood;
        }

        if (!Directory.Exists(folder))
        {
            output.WriteLine($"{folder}: there is no such folder.");
            return NotUsable;
        }

        var forgotten = new ReportBox(folder).Forget(before);
        output.WriteLine($"Deleted {forgotten} report(s) that were looked at before {day}.");
        return 0;
    }

    private static int PhotoSizesOf(string folder, TextWriter output)
    {
        if (!Directory.Exists(folder))
        {
            output.WriteLine($"{folder}: there is no such folder.");
            return NotUsable;
        }

        output.WriteLine($"Made {PhotoSizes.MakeMissing(folder)} smaller size(s).");
        return 0;
    }

    private static int NotAsked(TextWriter output)
    {
        output.WriteLine(Usage);
        return NotUnderstood;
    }

    private static int Check(IReadOnlyList<string> paths, TextWriter output, bool json)
    {
        var reports = new List<ContentReport>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var files = ContentCheck.FilesOf(path);
                if (files.IsEmpty)
                {
                    reports.Add(new ContentReport(Path.GetFullPath(path), null, [$"{path}: the folder holds no content file."], []));
                }

                reports.AddRange(files.Select(ContentCheck.File));
            }
            else
            {
                reports.Add(ContentCheck.File(path));
            }
        }

        // A translation and the file it translates are one check.
        reports = [.. reports.DistinctBy(report => (report.File, string.Join('\n', report.Problems)))];
        if (json)
        {
            Write(output, writer =>
            {
                writer.WriteStartObject();
                writer.WriteBoolean("usable", reports.TrueForAll(report => report.IsUsable));
                writer.WriteStartArray("files");
                foreach (var report in reports)
                {
                    writer.WriteStartObject();
                    writer.WriteString("file", report.File);
                    writer.WriteString("format", report.Format);
                    writer.WriteBoolean("usable", report.IsUsable);
                    writer.WriteStartArray("problems");
                    foreach (var problem in report.Problems)
                    {
                        writer.WriteStringValue(problem);
                    }

                    writer.WriteEndArray();
                    writer.WriteStartArray("notices");
                    foreach (var notice in report.Notices)
                    {
                        writer.WriteStringValue(notice);
                    }

                    writer.WriteEndArray();
                    writer.WriteStartObject("summary");
                    foreach (var (name, value) in report.Summary)
                    {
                        writer.WriteString(name, value);
                    }

                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            });
        }
        else
        {
            foreach (var report in reports)
            {
                output.WriteLine($"{report.File}{(report.Format is null ? null : $"  ({report.Format})")}");
                if (report.IsUsable)
                {
                    output.WriteLine("  In order: the site can use this file.");
                    foreach (var (name, value) in report.Summary)
                    {
                        output.WriteLine($"    {name}: {value}");
                    }

                    foreach (var notice in report.Notices)
                    {
                        output.WriteLine($"  Notice: {notice}");
                    }
                }
                else
                {
                    output.WriteLine($"  Not usable: {report.Problems.Length} {(report.Problems.Length == 1 ? "problem" : "problems")}.");
                    for (var index = 0; index < report.Problems.Length; index++)
                    {
                        output.WriteLine($"    {index + 1}. {report.Problems[index]}");
                    }
                }

                output.WriteLine();
            }
        }

        return reports.TrueForAll(report => report.IsUsable) ? Done : NotUsable;
    }

    private static int Vocabulary(string? kind, TextWriter output, bool json)
    {
        var kinds = new List<(string Name, string Format, ImmutableArray<ContentChoices> Fields)>();
        if (kind is null or "collection")
        {
            kinds.Add(("collection", PilotSample.SchemaVersion, ContentVocabulary.Collection));
        }

        if (kind is null or "comparison")
        {
            kinds.Add(("comparison", ComparisonSample.FormatVersion, ContentVocabulary.Comparison));
        }

        if (json)
        {
            Write(output, writer =>
            {
                writer.WriteStartObject();
                foreach (var (name, format, fields) in kinds)
                {
                    writer.WriteStartObject(name);
                    writer.WriteString("format", format);
                    writer.WriteStartArray("fields");
                    foreach (var field in fields)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("field", field.Field);
                        writer.WriteString("meaning", field.Meaning);
                        writer.WriteStartArray("values");
                        foreach (var value in field.Values)
                        {
                            writer.WriteStartObject();
                            writer.WriteString("name", value.Name);
                            if (value.Note is not null)
                            {
                                writer.WriteString("note", value.Note);
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            });
        }
        else
        {
            foreach (var (name, format, fields) in kinds)
            {
                output.WriteLine($"The file of a {name} ({format})");
                output.WriteLine();
                foreach (var field in fields)
                {
                    output.WriteLine($"  {field.Field}");
                    output.WriteLine($"    {field.Meaning}");
                    foreach (var value in field.Values)
                    {
                        output.WriteLine($"      {value.Name}{(value.Note is null ? null : $"  – {value.Note}")}");
                    }

                    output.WriteLine();
                }
            }
        }

        return Done;
    }

    private static int Template(string kind, string id, string? file, TextWriter output, DateOnly today)
    {
        string text;
        try
        {
            text = kind switch
            {
                "collection" => ContentTemplates.Collection(id, today),
                "subject" => ContentTemplates.Subject(id, today),
                _ => ContentTemplates.Comparison(id, today),
            };
        }
        catch (ArgumentException refused)
        {
            output.WriteLine(refused.Message);
            return NotUnderstood;
        }

        if (file is null)
        {
            output.WriteLine(text);
            return Done;
        }

        if (File.Exists(file))
        {
            output.WriteLine($"{file} exists; a template is never written over a file.");
            return NotUnderstood;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        File.WriteAllText(file, text + Environment.NewLine);
        output.WriteLine($"Written: {Path.GetFullPath(file)}");
        output.WriteLine("Every text that begins with REPLACE is a place for content. Check the file when it is filled.");
        return Done;
    }

    private static int Revision(string file, IReadOnlyList<string> options, TextWriter output)
    {
        // Every option is a name and a value; each is given once.
        var given = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < options.Count; index += 2)
        {
            if (index + 1 >= options.Count || options[index] is not ("--number" or "--date" or "--out" or "--previous" or "--note") || !given.TryAdd(options[index], options[index + 1]))
            {
                return NotAsked(output);
            }
        }

        if (!given.TryGetValue("--number", out var numbered) || !int.TryParse(numbered, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number)
            || !given.TryGetValue("--date", out var dated) || !DateOnly.TryParseExact(dated, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var day)
            || !given.TryGetValue("--out", out var target))
        {
            return NotAsked(output);
        }

        if (File.Exists(target))
        {
            output.WriteLine($"{target} exists; a revision is never written over a file.");
            return NotUnderstood;
        }

        var result = ContentRevision.Make(new RevisionRequest(file, number, day, given.GetValueOrDefault("--previous"), given.GetValueOrDefault("--note")));
        if (result.Json is null)
        {
            output.WriteLine($"No revision: {result.Problems.Length} {(result.Problems.Length == 1 ? "reason" : "reasons")}.");
            for (var index = 0; index < result.Problems.Length; index++)
            {
                output.WriteLine($"  {index + 1}. {result.Problems[index]}");
            }

            return NotUsable;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
        File.WriteAllText(target, result.Json + Environment.NewLine);
        output.WriteLine($"Written: {Path.GetFullPath(target)}");
        output.WriteLine($"Revision {number} of {dated}: {result.Changes.Added} added, {result.Changes.Changed} changed, {result.Changes.Removed} removed against the revision before.");
        output.WriteLine("A translation is a file of its own: put the reviewed ones beside the revision, under its name. Check the revision where it will lie.");
        return Done;
    }

    private static void Write(TextWriter output, Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, Json))
        {
            write(writer);
        }

        output.WriteLine(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }
}
