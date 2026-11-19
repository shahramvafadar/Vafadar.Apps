using System.Globalization;
using System.Security.Cryptography;
using Npgsql;

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
/// The notices about content, in the table <c>delivery."Notices"</c> of the database (ADR 0015). The site writes
/// into it and reads nothing back; who runs the site reads it with the command, decides, answers by mail where
/// an address was given, and settles the notice.
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

    private const string Columns = """ "Id", "ReceivedAt", "Language", "Address", "Kind", "Explanation", "Name", "Mail", "GoodFaith", "SettledAt", "Decision" """;
    private static readonly string Table = $"""{Delivery.Schema}."Notices" """;
    private readonly string _connection;

    /// <summary>Initializes a new instance of the <see cref="NoticeBox"/> class for a database, by its connection.</summary>
    public NoticeBox(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        _connection = connection;
    }

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

        using var database = Delivery.Open(_connection);
        using var step = database.BeginTransaction();

        // Counted and added under one lock: the box does not take one more than it may, however many write at once.
        Delivery.Lock(database, "notices");
        using (var waiting = Delivery.Command(database, $"""select count(*) from {Table} where "SettledAt" is null"""))
        {
            if ((long)waiting.ExecuteScalar()! >= MostWaiting)
            {
                return null;
            }
        }

        // The reference is told to who gave the notice and names it in every answer: short enough to be read out.
        var id = $"{now:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
        using var add = Delivery.Command(
            database,
            $"""insert into {Table} ("Id", "ReceivedAt", "Language", "Address", "Kind", "Explanation", "Name", "Mail", "GoodFaith") values (@id, @at, @language, @address, @kind, @explanation, @name, @mail, @faith)""",
            ("id", id), ("at", Utc(now)), ("language", language), ("address", address.Trim()), ("kind", kind), ("explanation", explanation.Trim()), ("name", Line(name)), ("mail", Line(mail)), ("faith", goodFaith));
        add.ExecuteNonQuery();
        step.Commit();
        return id;
    }

    /// <summary>Returns the notices that wait, the oldest first.</summary>
    public IReadOnlyList<ContentNotice> Waiting() => Read("""where "SettledAt" is null order by "ReceivedAt", "Id" """);

    /// <summary>Returns the notices that were settled, the oldest first.</summary>
    public IReadOnlyList<ContentNotice> AllSettled() => Read("""where "SettledAt" is not null order by "ReceivedAt", "Id" """);

    /// <summary>Notes what was decided about a notice and takes it out of those that wait. Returns whether there was such a notice waiting.</summary>
    public bool Settle(string id, string decision, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);
        if (!IsReference(id))
        {
            return false;
        }

        using var database = Delivery.Open(_connection);
        using var settle = Delivery.Command(database, $"""update {Table} set "SettledAt" = @at, "Decision" = @decision where "Id" = @id and "SettledAt" is null""", ("at", Utc(now)), ("decision", decision.Trim()), ("id", id));
        return settle.ExecuteNonQuery() == 1;
    }

    /// <summary>
    /// Deletes the settled notices that were settled before a day, and returns how many: a notice names who
    /// gave it and is kept as long as it may be needed to answer for the decision, and no longer.
    /// </summary>
    public int Forget(DateTime settledBefore)
    {
        using var database = Delivery.Open(_connection);
        using var forget = Delivery.Command(database, $"""delete from {Table} where "SettledAt" < @before""", ("before", Utc(settledBefore)));
        return forget.ExecuteNonQuery();
    }

    // The database keeps a moment in universal time and takes no other.
    private static DateTime Utc(DateTime moment) => moment.Kind == DateTimeKind.Utc ? moment : DateTime.SpecifyKind(moment, DateTimeKind.Utc);

    private static bool IsReference(string? id) =>
        id is { Length: 17 } && id[8] == '-' && id.All(letter => char.IsAsciiDigit(letter) || letter == '-' || (letter >= 'A' && letter <= 'F'))
        && DateOnly.TryParseExact(id[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static string? Line(string? written) => string.IsNullOrWhiteSpace(written) ? null : written.Trim();

    private List<ContentNotice> Read(string which)
    {
        using var database = Delivery.Open(_connection);
        using var command = Delivery.Command(database, $"select {Columns} from {Table} {which}");
        using var rows = command.ExecuteReader();
        var notices = new List<ContentNotice>();
        while (rows.Read())
        {
            notices.Add(new ContentNotice(
                rows.GetString(0),
                rows.GetDateTime(1),
                rows.GetString(2),
                rows.GetString(3),
                rows.GetString(4),
                rows.GetString(5),
                rows.IsDBNull(6) ? null : rows.GetString(6),
                rows.IsDBNull(7) ? null : rows.GetString(7),
                rows.GetBoolean(8),
                rows.IsDBNull(9) ? null : rows.GetDateTime(9),
                rows.IsDBNull(10) ? null : rows.GetString(10)));
        }

        return notices;
    }
}
