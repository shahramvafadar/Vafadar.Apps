using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

namespace Vafadar.Dastan.Data;

/// <summary>What the check of a content file found.</summary>
/// <param name="File">The file that was checked: the file of a collection or a comparison, also when a translation was named.</param>
/// <param name="Format">The format of the file, or <see langword="null"/> when it states none the readers know.</param>
/// <param name="Problems">Every problem, each with the record it belongs to. Empty when the site can use the file.</param>
/// <param name="Summary">What the file holds, as names and counts. Empty when it has a problem.</param>
/// <param name="Notices">What a writer should look at although the site can use the file: places a template left to be filled.</param>
public sealed record ContentReport(
    string File,
    string? Format,
    ImmutableArray<string> Problems,
    ImmutableArray<(string Name, string Value)> Summary,
    ImmutableArray<string> Notices = default)
{
    /// <summary>Gets what a writer should look at although the site can use the file.</summary>
    public ImmutableArray<string> Notices { get; init; } = Notices.IsDefault ? [] : Notices;

    /// <summary>Gets a value indicating whether the site can use the file.</summary>
    public bool IsUsable => Problems.IsEmpty;
}

/// <summary>
/// Checks a content file exactly as the site reads it (D-042): the same readers, the same refusals. A file that
/// passes is a file the site starts with; what the check cannot know – whether the content is true, whether a
/// source says what is claimed – stays the work of the editors.
/// </summary>
public static class ContentCheck
{
    /// <summary>
    /// Checks a file. A translation is checked together with the file it translates, which lies beside it:
    /// <c>story.fa.json</c> beside <c>story.json</c>.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    public static ContentReport File(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var file = Path.GetFullPath(path);
        if (!System.IO.File.Exists(file))
        {
            return Refused(file, null, "the file does not exist.");
        }

        var format = Format(file, out var problem);
        if (format is null)
        {
            return Refused(file, null, problem!);
        }

        if (format is PilotSample.TranslationVersion or ComparisonSample.TranslationVersion or KindsFile.TranslationVersion)
        {
            // A translation says nothing by itself: it is read with the file it translates.
            var name = Path.GetFileNameWithoutExtension(file);
            var dot = name.LastIndexOf('.');
            var translated = dot <= 0 ? null : Path.Combine(Path.GetDirectoryName(file)!, name[..dot] + ".json");
            if (translated is null || !System.IO.File.Exists(translated))
            {
                return Refused(file, format, "a translation is named <file>.<language>.json and lies beside the <file>.json it translates; that file was not found.");
            }

            var expected = format switch
            {
                PilotSample.TranslationVersion => PilotSample.SchemaVersion,
                KindsFile.TranslationVersion => KindsFile.FormatVersion,
                _ => ComparisonSample.FormatVersion,
            };
            var found = Format(translated, out _);
            if (found != expected)
            {
                return Refused(file, format, $"it translates a file of the format {expected}; {Path.GetFileName(translated)} is {found ?? "not one"}.");
            }

            file = translated;
            format = expected;
        }

        try
        {
            return format switch
            {
                PilotSample.SchemaVersion => new ContentReport(file, format, [], Describe(PilotSample.Load(file), file), Unfilled(file)),
                ComparisonSample.FormatVersion => new ContentReport(file, format, [], Describe(ComparisonSample.Load(file), file), Unfilled(file)),
                KindsFile.FormatVersion => Kinds(file, format),
                _ => Refused(file, format, $"the format {format} is not one the site reads; it reads {PilotSample.SchemaVersion}, {ComparisonSample.FormatVersion} and {KindsFile.FormatVersion}, and their translations."),
            };
        }
        catch (SampleFormatException refused)
        {
            return new ContentReport(file, format, [.. refused.Problems], []);
        }
    }

    /// <summary>Returns the content files of a folder and the folders below it: every <c>.json</c> file that is not a translation.</summary>
    /// <param name="folder">The folder.</param>
    public static ImmutableArray<string> FilesOf(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        return
        [
            .. Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories)
                .Where(file => !Path.GetFileNameWithoutExtension(file).Contains('.', StringComparison.Ordinal))
                .Order(StringComparer.Ordinal),
        ];
    }

    /// <summary>The format a report states for a content root that holds no file yet.</summary>
    public const string EmptyRoot = "an empty content root";

    /// <summary>
    /// Returns whether a folder is a content root: a folder with the three folders the site reads –
    /// <c>samples</c>, <c>comparisons</c> and <c>media</c>. Such a folder may hold no file yet and is in order then.
    /// </summary>
    /// <param name="folder">The folder.</param>
    public static bool IsContentRoot(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        return new[] { "samples", "comparisons", "media" }.All(name => Directory.Exists(Path.Combine(folder, name)));
    }

    // The list of kinds is read where the site reads it: as kinds.json of a content root, beside its translations.
    private static ContentReport Kinds(string file, string format)
    {
        if (!string.Equals(Path.GetFileName(file), KindsFile.FileName, StringComparison.Ordinal))
        {
            return Refused(file, format, $"a list of kinds is read only as {KindsFile.FileName} of a content root; this file is named otherwise.");
        }

        var kinds = KindsFile.Load(Path.GetDirectoryName(file)!);
        return new ContentReport(
            file,
            format,
            [],
            [
                ("kinds", Count(kinds.All.Length)),
                ("retired", Count(kinds.All.Count(kind => kind.Retired))),
                ("translations", Translations(file)),
            ],
            Unfilled(file));
    }

    // How many records have no kind yet (ADR 0029, K4): what is left to classify.
    private static string WithoutKind(int without, int all) => $"{Count(without)} of {Count(all)}";

    private static ContentReport Refused(string file, string? format, string problem) =>
        new(file, format, [$"{Path.GetFileName(file)}: {problem}"], []);

    // The format a file states: schemaVersion for a collection, formatVersion for everything else.
    private static string? Format(string file, out string? problem)
    {
        problem = null;
        try
        {
            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(file));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                problem = "it is not a JSON object.";
                return null;
            }

            foreach (var field in new[] { "schemaVersion", "formatVersion" })
            {
                if (document.RootElement.TryGetProperty(field, out var version) && version.ValueKind == JsonValueKind.String)
                {
                    return version.GetString();
                }
            }

            problem = "it states no format: a collection has schemaVersion, a comparison and a translation have formatVersion.";
            return null;
        }
        catch (JsonException exception)
        {
            problem = $"it is not valid JSON ({exception.Message})";
            return null;
        }
    }

    // What a template left to be filled, in the file and its translations. The readers accept such a file – it
    // is well formed – and no reader of the site should see it.
    private static ImmutableArray<string> Unfilled(string file)
    {
        var notices = ImmutableArray.CreateBuilder<string>();
        var files = Directory.EnumerateFiles(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + ".*.json").Order(StringComparer.Ordinal).Prepend(file);
        foreach (var each in files)
        {
            var text = System.IO.File.ReadAllText(each);
            var places = text.Split("\"REPLACE", StringSplitOptions.None).Length - 1;
            if (places > 0)
            {
                notices.Add($"{Path.GetFileName(each)}: {Count(places)} {(places == 1 ? "text still begins" : "texts still begin")} with REPLACE – the place of a template that is not filled.");
            }

            if (text.Contains("https://example.org/", StringComparison.Ordinal))
            {
                notices.Add($"{Path.GetFileName(each)}: an address still points to example.org – the place of a template that is not filled.");
            }
        }

        return notices.ToImmutable();
    }

    private static string Count(int count) => count.ToString(CultureInfo.InvariantCulture);

    private static string Translations(string file) => string.Join(
        ", ",
        Directory.EnumerateFiles(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + ".*.json")
            .Select(translation => Path.GetFileNameWithoutExtension(translation)[(Path.GetFileNameWithoutExtension(file).Length + 1)..])
            .Order(StringComparer.Ordinal)
            .DefaultIfEmpty("none"));

    private static ImmutableArray<(string Name, string Value)> Describe(Core.Catalogue collection, string file) =>
    [
        ("collection", collection.Info.Id),
        ("translations", Translations(file)),
        ("reading stages", Count(collection.Info.ReadingStages.Length)),
        ("sources", Count(collection.Sources.Length)),
        ("entities", Count(collection.Entities.Length) + " (" + string.Join(", ", collection.Entities.GroupBy(entity => PilotReader.NameOf(entity.Type)).Select(group => $"{group.Key} {Count(group.Count())}")) + ")"),
        ("without a kind", WithoutKind(collection.Entities.Count(entity => entity.Kinds.IsEmpty), collection.Entities.Length)),
        ("relations", Count(collection.Relations.Length)),
        ("events", Count(collection.Events.Length)),
        ("media", Count(collection.Media.Length)),
        ("released to", collection.Info.Publication.PublicReleaseApproved ? "everybody" : collection.Info.Publication.LocalDemoAllowed ? "the local demonstration only" : "nobody"),
    ];

    private static ImmutableArray<(string Name, string Value)> Describe(Core.Comparison comparison, string file) =>
    [
        ("comparison", comparison.Id.Value),
        ("translations", Translations(file)),
        ("lanes", Count(comparison.Lanes.Length)),
        ("periods", Count(comparison.Lanes.Sum(lane => lane.Periods.Length))),
        ("events", Count(comparison.Lanes.Sum(lane => lane.Events.Length))),
        ("without a kind", WithoutKind(
            comparison.Lanes.Sum(lane => lane.Periods.Count(period => period.Kinds.IsEmpty) + lane.Events.Count(single => single.Kinds.IsEmpty)),
            comparison.Lanes.Sum(lane => lane.Periods.Length + lane.Events.Length))),
        ("sources", Count(comparison.Sources.Length)),
        ("released to", comparison.Publication.PublicReleaseApproved ? "everybody" : comparison.Publication.LocalDemoAllowed ? "the local demonstration only" : "nobody"),
    ];
}
