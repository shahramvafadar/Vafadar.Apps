using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Data;

/// <summary>What the editors decided about a place a visitor proposed.</summary>
public enum ProposalOutcome
{
    /// <summary>The place has an entry of a collection now.</summary>
    Added = 0,

    /// <summary>The editors make no entry for it.</summary>
    Declined = 1,
}

/// <summary>
/// A place a visitor proposed to the editors (D-085, ADR 0023): somewhere people can have been at that no
/// collection has an entry for. It is the place in the visitor's words and the day – nothing about the
/// visitor is kept: no account, no name, no address.
/// </summary>
/// <param name="Id">The name of the proposal, made by the box.</param>
/// <param name="On">The day it was written.</param>
/// <param name="Language">The language of the page it was written on.</param>
/// <param name="Name">The name of the place, as the visitor wrote it.</param>
/// <param name="Kind">What kind of place it is: the key of one of the types of entity people can have been at.</param>
/// <param name="Where">Where it is, in the visitor's words, when they said so.</param>
/// <param name="Page">A public page about the place, when the visitor named one.</param>
/// <param name="Note">What else the visitor wrote for the editors.</param>
/// <param name="SettledBy">The editor who decided, once somebody has.</param>
/// <param name="SettledOn">The day that was.</param>
/// <param name="Outcome">What was decided.</param>
/// <param name="Entry">The entry the place has now, as <c>collection/entity</c>, when the editors named it.</param>
public sealed record PlaceProposal(
    string Id,
    DateOnly On,
    string Language,
    string Name,
    string Kind,
    string? Where = null,
    Uri? Page = null,
    string? Note = null,
    string? SettledBy = null,
    DateOnly? SettledOn = null,
    ProposalOutcome? Outcome = null,
    string? Entry = null);

/// <summary>What a visitor says about a place they propose.</summary>
/// <param name="Name">The name of the place.</param>
/// <param name="Kind">The key of the type of entity it would be.</param>
/// <param name="Where">Where it is, or nothing.</param>
/// <param name="Page">The address of a public page about it, or nothing.</param>
/// <param name="Note">What else the editors should know, or nothing.</param>
public sealed record ProposedPlace(string Name, string Kind, string? Where = null, string? Page = null, string? Note = null);

/// <summary>
/// The places visitors proposed, in the table <c>delivery."Proposals"</c> of the database (ADR 0015). The site
/// writes into it and reads nothing back; the tool of the editors reads it and notes what was decided. The two
/// programs share nothing but this table.
/// </summary>
public sealed partial class ProposalBox
{
    /// <summary>The longest name of a place, in characters: as long as the words of an affiliation.</summary>
    public const int LongestName = 200;

    /// <summary>The longest line of a proposal other than its name: where the place is, the address of a page.</summary>
    public const int LongestLine = 300;

    /// <summary>The longest note for the editors, in characters.</summary>
    public const int LongestNote = 500;

    /// <summary>The largest number of proposals that wait: a box that is full takes no more until an editor has decided.</summary>
    public const int MostWaiting = 500;

    private const string Columns = """ "Id", "On", "Language", "Name", "Kind", "Where", "Page", "Note", "SettledBy", "SettledOn", "Outcome", "Entry" """;
    private static readonly string Table = $"""{Delivery.Schema}."Proposals" """;
    private readonly string _connection;

    /// <summary>Initializes a new instance of the <see cref="ProposalBox"/> class for a database, by its connection.</summary>
    public ProposalBox(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        _connection = connection;
    }

    /// <summary>
    /// Returns what a form says about a place as a proposal that can be kept, or nothing: a name of a length
    /// a form allows, a kind that is one, lines without control characters, and – when a page is named – an
    /// address of the web that begins with <c>https</c> and names no user.
    /// </summary>
    /// <param name="said">What the form says.</param>
    /// <param name="kinds">The kinds a proposal may name.</param>
    public static ProposedPlace? Read(ProposedPlace said, IReadOnlyCollection<string> kinds)
    {
        ArgumentNullException.ThrowIfNull(said);
        ArgumentNullException.ThrowIfNull(kinds);
        var name = Line(said.Name);
        var where = Line(said.Where);
        var note = Clean(said.Note);
        var page = (said.Page ?? string.Empty).Trim();
        if (name.Length is 0 or > LongestName || !kinds.Contains(said.Kind) || where.Length > LongestLine || note.Length > LongestNote || page.Length > LongestLine)
        {
            return null;
        }

        if (page.Length > 0 && !(Uri.TryCreate(page, UriKind.Absolute, out var address) && address.Scheme == Uri.UriSchemeHttps && address.UserInfo.Length == 0 && address.Host.Contains('.', StringComparison.Ordinal)))
        {
            return null;
        }

        return new ProposedPlace(name, said.Kind, where.Length == 0 ? null : where, page.Length == 0 ? null : page, note.Length == 0 ? null : note);
    }

    /// <summary>
    /// Keeps a proposal that <see cref="Read"/> has made. Returns <see langword="false"/> when the box is full.
    /// </summary>
    public bool Take(ProposedPlace place, string language, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(place);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        if (place.Name.Length is 0 or > LongestName || !NameForm().IsMatch(place.Kind) || !NameForm().IsMatch(language))
        {
            throw new ArgumentException("That is no proposal.", nameof(place));
        }

        using var database = Delivery.Open(_connection);
        using var step = database.BeginTransaction();

        // Counted and added under one lock: the box does not take one more than it may, however many write at once.
        Delivery.Lock(database, "proposals");
        using (var waiting = Delivery.Command(database, $"""select count(*) from {Table} where "SettledOn" is null"""))
        {
            if ((long)waiting.ExecuteScalar()! >= MostWaiting)
            {
                return false;
            }
        }

        using var add = Delivery.Command(
            database,
            $"""insert into {Table} ("Id", "On", "Language", "Name", "Kind", "Where", "Page", "Note") values (@id, @on, @language, @name, @kind, @where, @page, @note)""",
            ("id", Guid.NewGuid().ToString("N")), ("on", today), ("language", language), ("name", place.Name), ("kind", place.Kind), ("where", place.Where), ("page", place.Page), ("note", place.Note));
        add.ExecuteNonQuery();
        step.Commit();
        return true;
    }

    /// <summary>Returns the proposals nobody has decided yet, the oldest first.</summary>
    public IReadOnlyList<PlaceProposal> Waiting() => Read($"""where "SettledOn" is null order by "On", "Id" """);

    /// <summary>Returns the proposals somebody has decided, the newest first.</summary>
    public IReadOnlyList<PlaceProposal> Decided() => Read($"""where "SettledOn" is not null order by "SettledOn" desc, "On" desc, "Id" """);

    /// <summary>
    /// Notes what an editor decided about a proposal and takes it out of those that wait. Returns
    /// <see langword="false"/> when no such proposal waits, or when the entry that is named is no
    /// <c>collection/entity</c>.
    /// </summary>
    /// <param name="id">The proposal.</param>
    /// <param name="by">The editor.</param>
    /// <param name="today">The day.</param>
    /// <param name="outcome">What was decided.</param>
    /// <param name="entry">For a place that was added: the entry it has, as <c>collection/entity</c>, when the editor names it.</param>
    public bool Settle(string id, string by, DateOnly today, ProposalOutcome outcome, string? entry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        var named = string.IsNullOrWhiteSpace(entry) ? null : entry.Trim();
        if (id is not { Length: 32 } || !id.All(char.IsAsciiHexDigit) || (named is not null && (outcome != ProposalOutcome.Added || !EntryForm().IsMatch(named))))
        {
            return false;
        }

        // One statement: of two editors who decide the same proposal at the same moment, one does and the other is told it no longer waits.
        using var database = Delivery.Open(_connection);
        using var settle = Delivery.Command(
            database,
            $"""update {Table} set "SettledBy" = @by, "SettledOn" = @on, "Outcome" = @outcome, "Entry" = @entry where "Id" = @id and "SettledOn" is null""",
            ("by", by), ("on", today), ("outcome", outcome == ProposalOutcome.Added ? "added" : "declined"), ("entry", named), ("id", id));
        return settle.ExecuteNonQuery() == 1;
    }

    /// <summary>
    /// Deletes the proposals that were decided before a day, and returns how many. A proposal that waits is
    /// never deleted this way: nobody has read it yet.
    /// </summary>
    public int Forget(DateOnly decidedBefore)
    {
        using var database = Delivery.Open(_connection);
        using var forget = Delivery.Command(database, $"""delete from {Table} where "SettledOn" < @before""", ("before", decidedBefore));
        return forget.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns a name with its letters brought together, so that the same place proposed twice stands together:
    /// without marks above letters, in one case, the forms of a letter of Arabic and Persian as one, and
    /// without what is no letter and no digit.
    /// </summary>
    public static string Folded(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var folded = new StringBuilder(name.Length);
        foreach (var letter in name.Normalize(NormalizationForm.FormKD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letter) is UnicodeCategory.NonSpacingMark or UnicodeCategory.Format)
            {
                continue;
            }

            var digit = CharUnicodeInfo.GetDecimalDigitValue(letter);
            if (digit >= 0)
            {
                folded.Append((char)('0' + digit));
            }
            else if (char.IsLetter(letter))
            {
                folded.Append(letter switch { 'ي' or 'ى' => 'ی', 'ك' => 'ک', 'ة' => 'ه', _ => char.ToLowerInvariant(letter) });
            }
        }

        return folded.ToString();
    }

    private List<PlaceProposal> Read(string which)
    {
        using var database = Delivery.Open(_connection);
        using var command = Delivery.Command(database, $"select {Columns} from {Table} {which}");
        using var rows = command.ExecuteReader();
        var proposals = new List<PlaceProposal>();
        while (rows.Read())
        {
            string? Text(int column) => rows.IsDBNull(column) ? null : rows.GetString(column);
            proposals.Add(new PlaceProposal(
                rows.GetString(0),
                rows.GetFieldValue<DateOnly>(1),
                rows.GetString(2),
                rows.GetString(3),
                rows.GetString(4),
                Text(5),
                Text(6) is { } page && Uri.TryCreate(page, UriKind.Absolute, out var address) ? address : null,
                Text(7),
                Text(8),
                rows.IsDBNull(9) ? null : rows.GetFieldValue<DateOnly>(9),
                Text(10) switch { "added" => ProposalOutcome.Added, "declined" => ProposalOutcome.Declined, _ => null },
                Text(11)));
        }

        return proposals;
    }

    // One line a visitor typed: without control characters and line ends, with blanks brought to one.
    private static string Line(string? text) => Blanks().Replace(new string([.. (text ?? string.Empty).Select(character => char.IsControl(character) ? ' ' : character)]), " ").Trim();

    // What a visitor typed: without control characters, with line ends kept as they are read.
    private static string Clean(string? text) =>
        new string([.. (text ?? string.Empty).ReplaceLineEndings("\n").Where(character => character == '\n' || !char.IsControl(character))]).Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Blanks();

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,120}\z")]
    private static partial Regex NameForm();

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,120}/[A-Za-z0-9._-]{1,120}\z")]
    private static partial Regex EntryForm();
}
