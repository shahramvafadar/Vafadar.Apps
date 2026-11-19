using System.Text.RegularExpressions;
using Npgsql;

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
/// The reports of visitors, in the table <c>delivery."Reports"</c> of the database (ADR 0015). The site writes
/// into it and reads nothing back; the tool of the editors reads it and notes a report as looked at. The two
/// programs share nothing but this table.
/// </summary>
public sealed partial class ReportBox
{
    /// <summary>The longest text of a report, in characters.</summary>
    public const int LongestText = 1000;

    /// <summary>The largest number of reports that wait: a box that is full takes no more until an editor has looked.</summary>
    public const int MostWaiting = 500;

    private const string Columns = """ "Id", "On", "Collection", "Revision", "Language", "Statement", "Text", "SettledBy", "SettledOn" """;
    private static readonly string Table = $"""{Delivery.Schema}."Reports" """;
    private readonly string _connection;

    /// <summary>Initializes a new instance of the <see cref="ReportBox"/> class for a database, by its connection.</summary>
    public ReportBox(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        _connection = connection;
    }

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

        using var database = Delivery.Open(_connection);
        using var step = database.BeginTransaction();

        // Counted and added under one lock: the box does not take one more than it may, however many write at once.
        Delivery.Lock(database, "reports");
        using (var waiting = Delivery.Command(database, $"""select count(*) from {Table} where "SettledOn" is null"""))
        {
            if ((long)waiting.ExecuteScalar()! >= MostWaiting)
            {
                return false;
            }
        }

        using var add = Delivery.Command(
            database,
            $"""insert into {Table} ("Id", "On", "Collection", "Revision", "Language", "Statement", "Text") values (@id, @on, @collection, @revision, @language, @statement, @text)""",
            ("id", Guid.NewGuid().ToString("N")), ("on", today), ("collection", collection), ("revision", revision), ("language", language), ("statement", statement), ("text", words));
        add.ExecuteNonQuery();
        step.Commit();
        return true;
    }

    /// <summary>Returns the reports nobody has looked at yet, the oldest first.</summary>
    public IReadOnlyList<VisitorReport> Waiting() => Read($"""where "SettledOn" is null order by "On", "Id" """);

    /// <summary>Returns the reports somebody has looked at, the newest first.</summary>
    public IReadOnlyList<VisitorReport> Looked() => Read($"""where "SettledOn" is not null order by "SettledOn" desc, "On" desc, "Id" """);

    /// <summary>Notes that an editor has looked at a report and takes it out of those that wait. Returns <see langword="false"/> when no such report waits.</summary>
    public bool Settle(string id, string by, DateOnly today)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        if (id is not { Length: 32 } || !id.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        // One statement: of two editors who note the same report at the same moment, one does and the other is told it no longer waits.
        using var database = Delivery.Open(_connection);
        using var settle = Delivery.Command(database, $"""update {Table} set "SettledBy" = @by, "SettledOn" = @on where "Id" = @id and "SettledOn" is null""", ("by", by), ("on", today), ("id", id));
        return settle.ExecuteNonQuery() == 1;
    }

    /// <summary>
    /// Deletes the reports that were looked at before a day, and returns how many. A report that waits is never
    /// deleted this way: nobody has read it yet.
    /// </summary>
    public int Forget(DateOnly lookedAtBefore)
    {
        using var database = Delivery.Open(_connection);
        using var forget = Delivery.Command(database, $"""delete from {Table} where "SettledOn" < @before""", ("before", lookedAtBefore));
        return forget.ExecuteNonQuery();
    }

    private List<VisitorReport> Read(string which)
    {
        using var database = Delivery.Open(_connection);
        using var command = Delivery.Command(database, $"select {Columns} from {Table} {which}");
        using var rows = command.ExecuteReader();
        var reports = new List<VisitorReport>();
        while (rows.Read())
        {
            reports.Add(new VisitorReport(
                rows.GetString(0),
                rows.GetFieldValue<DateOnly>(1),
                rows.GetString(2),
                rows.IsDBNull(3) ? null : rows.GetInt32(3),
                rows.GetString(4),
                rows.GetString(5),
                rows.GetString(6),
                rows.IsDBNull(7) ? null : rows.GetString(7),
                rows.IsDBNull(8) ? null : rows.GetFieldValue<DateOnly>(8)));
        }

        return reports;
    }

    // What a visitor typed: without control characters, with line ends kept as they are read.
    private static string Clean(string? text) =>
        new string([.. (text ?? string.Empty).ReplaceLineEndings("\n").Where(character => character == '\n' || !char.IsControl(character))]).Trim();

    [GeneratedRegex(@"^evidence/(entity|relation|media)/[A-Za-z0-9._-]{1,120}\z|^evidence/event/[A-Za-z0-9._-]{1,120}(/entity/[A-Za-z0-9._-]{1,120})?\z")]
    private static partial Regex StatementForm();

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,120}\z")]
    private static partial Regex NameForm();
}
