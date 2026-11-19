using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Vafadar.Dastan.Data;

/// <summary>
/// A notice somebody gave about something on the site they hold to be against the law or against the terms
/// (Article 16 of Regulation (EU) 2022/2065). It holds what the article says a notice can hold, and nothing the
/// site found out by itself – no address of a machine, no account.
/// </summary>
/// <param name="Id">The reference of the notice, made by the box; who gave the notice is told it.</param>
/// <param name="ReceivedAt">When it came, in universal time.</param>
/// <param name="Language">The language of the page it was given on.</param>
/// <param name="Address">Where on the site the content is: an address of the site.</param>
/// <param name="Kind">What is held against it, by one of the kinds of <see cref="NoticeBox.Kinds"/>.</param>
/// <param name="Explanation">Why it is held to be against the law or the terms.</param>
/// <param name="Name">The name of who gave the notice, when they gave one.</param>
/// <param name="Mail">Their mail address, when they gave one: the answer goes there.</param>
/// <param name="GoodFaith">Whether they declared that the notice is accurate and complete to their honest belief.</param>
/// <param name="SettledAt">When who runs the site decided, once they have.</param>
/// <param name="Decision">What was decided.</param>
public sealed record ContentNotice(
    string Id,
    DateTime ReceivedAt,
    string Language,
    string Address,
    string Kind,
    string Explanation,
    string? Name,
    string? Mail,
    bool GoodFaith,
    DateTime? SettledAt = null,
    string? Decision = null);

/// <summary>
/// The folder the notices about content lie in: one small file each. The site writes into it and reads nothing
/// back; who runs the site reads it with the command, decides, answers by mail where an address was given, and
/// settles the notice, which moves it into the folder <c>settled</c>.
/// </summary>
public sealed class NoticeBox
{
    /// <summary>The kinds of what can be held against content, as the form offers them.</summary>
    public static IReadOnlyList<string> Kinds { get; } = ["privacy", "image", "copyright", "defamation", "hate", "minors", "violence", "other-law", "terms"];

    /// <summary>The longest explanation, in characters.</summary>
    public const int LongestExplanation = 4000;

    /// <summary>The longest address, name or mail address, in characters.</summary>
    public const int LongestLine = 300;

    /// <summary>The largest number of notices that wait: a box that is full takes no more, and the page says to write by mail.</summary>
    public const int MostWaiting = 1000;

    private static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _folder;

    /// <summary>Initializes a new instance of the <see cref="NoticeBox"/> class for a folder; it is made when the first notice comes.</summary>
    public NoticeBox(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        _folder = Path.GetFullPath(folder);
    }

    private string Settled => Path.Combine(_folder, "settled");

    /// <summary>
    /// Returns what is wrong with a notice as it was written, by the names of the fields – nothing when it can
    /// be kept. A name and a mail address are not required: the mechanism lets them be given, and a notice
    /// without them is looked at all the same.
    /// </summary>
    public static IReadOnlyList<string> Problems(string? address, string? kind, string? explanation, string? name, string? mail, bool goodFaith)
    {
        var problems = new List<string>();
        if (address is not { Length: > 0 and <= LongestLine } || address.Any(char.IsControl))
        {
            problems.Add("address");
        }

        if (kind is null || !Kinds.Contains(kind))
        {
            problems.Add("kind");
        }

        if (explanation is null || explanation.Trim().Length is < 20 or > LongestExplanation)
        {
            problems.Add("explanation");
        }

        if (name is { Length: > LongestLine } || name?.Any(char.IsControl) == true)
        {
            problems.Add("name");
        }

        if (mail is { Length: > 0 } && (mail.Length > LongestLine || mail.Any(char.IsWhiteSpace) || mail.IndexOf('@', StringComparison.Ordinal) is <= 0 || mail.EndsWith('@')))
        {
            problems.Add("mail");
        }

        if (!goodFaith)
        {
            problems.Add("good-faith");
        }

        return problems;
    }

    /// <summary>Keeps a notice and returns its reference, or nothing when the box is full. A notice that has a problem is refused with an exception: the form looks first.</summary>
    public string? Take(string language, string address, string kind, string explanation, string? name, string? mail, bool goodFaith, DateTime now)
    {
        if (Problems(address, kind, explanation, name, mail, goodFaith) is { Count: > 0 } problems)
        {
            throw new ArgumentException($"The notice cannot be kept: {string.Join(", ", problems)}.", nameof(address));
        }

        Directory.CreateDirectory(_folder);
        if (Directory.EnumerateFiles(_folder, "*.json").Take(MostWaiting).Count() >= MostWaiting)
        {
            return null;
        }

        // The reference is told to who gave the notice and names it in every answer: short enough to be read out.
        var id = $"{now:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
        var notice = new ContentNotice(id, now, language, address.Trim(), kind, explanation.Trim(), Line(name), Line(mail), goodFaith);
        var path = Path.Combine(_folder, id + ".json");
        var arriving = path + ".part";
        File.WriteAllText(arriving, JsonSerializer.Serialize(notice, Format));
        File.Move(arriving, path);
        return id;
    }

    /// <summary>Returns the notices that wait, the oldest first.</summary>
    public IReadOnlyList<ContentNotice> Waiting() => Read(_folder);

    /// <summary>Returns the notices that were settled, the oldest first.</summary>
    public IReadOnlyList<ContentNotice> AllSettled() => Read(Settled);

    /// <summary>Notes what was decided about a notice and moves it to the settled ones. Returns whether there was such a notice waiting.</summary>
    public bool Settle(string id, string decision, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);
        if (!IsReference(id) || Path.Combine(_folder, id + ".json") is not { } path || !File.Exists(path))
        {
            return false;
        }

        var notice = JsonSerializer.Deserialize<ContentNotice>(File.ReadAllText(path), Format)!;
        Directory.CreateDirectory(Settled);
        File.WriteAllText(Path.Combine(Settled, id + ".json"), JsonSerializer.Serialize(notice with { SettledAt = now, Decision = decision.Trim() }, Format));
        File.Delete(path);
        return true;
    }

    /// <summary>
    /// Deletes the settled notices that were settled before a day, and returns how many: a notice names who
    /// gave it and is kept as long as it may be needed to answer for the decision, and no longer.
    /// </summary>
    public int Forget(DateTime settledBefore)
    {
        if (!Directory.Exists(Settled))
        {
            return 0;
        }

        var forgotten = 0;
        foreach (var file in Directory.EnumerateFiles(Settled, "*.json").ToList())
        {
            if (JsonSerializer.Deserialize<ContentNotice>(File.ReadAllText(file), Format) is { SettledAt: { } at } && at < settledBefore)
            {
                File.Delete(file);
                forgotten++;
            }
        }

        return forgotten;
    }

    private static bool IsReference(string? id) =>
        id is { Length: 17 } && id[8] == '-' && id.All(letter => char.IsAsciiDigit(letter) || letter == '-' || (letter >= 'A' && letter <= 'F'))
        && DateOnly.TryParseExact(id[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static string? Line(string? written) => string.IsNullOrWhiteSpace(written) ? null : written.Trim();

    private static IReadOnlyList<ContentNotice> Read(string folder) =>
        !Directory.Exists(folder)
            ? []
            : [.. Directory.EnumerateFiles(folder, "*.json").Order(StringComparer.Ordinal).Select(file => JsonSerializer.Deserialize<ContentNotice>(File.ReadAllText(file), Format)!)];
}
