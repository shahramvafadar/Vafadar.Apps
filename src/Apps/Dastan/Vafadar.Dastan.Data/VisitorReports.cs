using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Data;

/// <summary>
/// What a visitor wrote about a statement of the site (R-GOV-03): the statement, the words and the day. Nothing
/// about the visitor is kept – no name, no address, no browser, no time of the day.
/// </summary>
/// <param name="Id">The name of the report, made by the box.</param>
/// <param name="On">The day it was written.</param>
/// <param name="Collection">The collection the statement belongs to.</param>
/// <param name="Revision">The published revision the visitor read, when the site shows one.</param>
/// <param name="Language">The language of the page the visitor read.</param>
/// <param name="Statement">The address of the statement inside its collection, such as <c>evidence/entity/character-rostam</c>.</param>
/// <param name="Text">What the visitor wrote.</param>
/// <param name="SettledBy">The editor who looked at the report, once somebody has.</param>
/// <param name="SettledOn">The day that was.</param>
public sealed record VisitorReport(
    string Id,
    DateOnly On,
    string Collection,
    int? Revision,
    string Language,
    string Statement,
    string Text,
    string? SettledBy = null,
    DateOnly? SettledOn = null);

/// <summary>
/// The folder the reports of visitors lie in: one small file each. The site writes into it and reads nothing
/// back; the tool of the editors reads it and moves a report that was looked at into the folder <c>settled</c>.
/// The two programs share nothing but this folder.
/// </summary>
public sealed partial class ReportBox
{
    /// <summary>The longest text of a report, in characters.</summary>
    public const int LongestText = 1000;

    /// <summary>The largest number of reports that wait: a box that is full takes no more until an editor has looked.</summary>
    public const int MostWaiting = 500;

    private static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _folder;

    /// <summary>Initializes a new instance of the <see cref="ReportBox"/> class for a folder; it is made when the first report comes.</summary>
    public ReportBox(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        _folder = Path.GetFullPath(folder);
    }

    private string Settled => Path.Combine(_folder, "settled");

    /// <summary>Returns whether a text names a statement: the address of the page of its sources inside a collection.</summary>
    public static bool IsStatement(string? statement) => statement is { Length: <= 300 } && StatementForm().IsMatch(statement);

    /// <summary>
    /// Keeps a report. Returns <see langword="false"/> when the box is full; what is no report – no words, too
    /// many, an address that names no statement – is refused with an exception, because a form of the site
    /// cannot send it.
    /// </summary>
    public bool Take(string collection, int? revision, string language, string statement, string text, DateOnly today)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collection);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        var words = Clean(text);
        if (words.Length is 0 or > LongestText || !IsStatement(statement) || !NameForm().IsMatch(collection) || !NameForm().IsMatch(language))
        {
            throw new ArgumentException("That is no report.", nameof(text));
        }

        Directory.CreateDirectory(_folder);
        if (Directory.EnumerateFiles(_folder, "*.json").Take(MostWaiting).Count() >= MostWaiting)
        {
            return false;
        }

        var report = new VisitorReport(Guid.NewGuid().ToString("N"), today, collection, revision, language, statement, words);

        // Written beside its place and then moved there, so that nobody reads half a report.
        var file = Path.Combine(_folder, report.Id + ".json");
        File.WriteAllText(file + ".part", JsonSerializer.Serialize(report, Format));
        File.Move(file + ".part", file);
        return true;
    }

    /// <summary>Returns the reports nobody has looked at yet, the oldest first. A file that is no report is passed over.</summary>
    public IReadOnlyList<VisitorReport> Waiting() => Read(_folder);

    /// <summary>Returns the reports somebody has looked at, the newest first.</summary>
    public IReadOnlyList<VisitorReport> Looked() => [.. Read(Settled).OrderByDescending(report => report.SettledOn).ThenByDescending(report => report.On)];

    /// <summary>Notes that an editor has looked at a report and takes it out of those that wait. Returns <see langword="false"/> when no such report waits.</summary>
    public bool Settle(string id, string by, DateOnly today)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        if (id is not { Length: 32 } || !id.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        var file = Path.Combine(_folder, id + ".json");
        if (!File.Exists(file) || ReadOne(file) is not { } report)
        {
            return false;
        }

        Directory.CreateDirectory(Settled);
        File.WriteAllText(Path.Combine(Settled, id + ".json"), JsonSerializer.Serialize(report with { SettledBy = by, SettledOn = today }, Format));
        File.Delete(file);
        return true;
    }

    private static List<VisitorReport> Read(string folder) => !Directory.Exists(folder)
        ? []
        : [.. Directory.EnumerateFiles(folder, "*.json").Select(ReadOne).OfType<VisitorReport>().OrderBy(report => report.On).ThenBy(report => report.Id, StringComparer.Ordinal)];

    private static VisitorReport? ReadOne(string file)
    {
        try
        {
            var report = JsonSerializer.Deserialize<VisitorReport>(File.ReadAllText(file), Format);
            return report is { Id.Length: 32, Text.Length: > 0 and <= LongestText } && IsStatement(report.Statement) && Path.GetFileNameWithoutExtension(file) == report.Id ? report : null;
        }
        catch (Exception unreadable) when (unreadable is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // What a visitor typed: without control characters, with line ends kept as they are read.
    private static string Clean(string? text) =>
        new string([.. (text ?? string.Empty).ReplaceLineEndings("\n").Where(character => character == '\n' || !char.IsControl(character))]).Trim();

    [GeneratedRegex(@"^evidence/(entity|relation|media)/[A-Za-z0-9._-]{1,120}$|^evidence/event/[A-Za-z0-9._-]{1,120}(/entity/[A-Za-z0-9._-]{1,120})?$")]
    private static partial Regex StatementForm();

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,120}$")]
    private static partial Regex NameForm();
}
