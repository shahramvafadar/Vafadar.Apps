using System.Data.Common;
using Npgsql;

namespace Vafadar.Dastan.Data;

/// <summary>A message that is to be sent to one address.</summary>
/// <param name="Key">
/// What names the message for good: the same key is the same message, however often it is handed over. It is
/// written once and sent once – a second hand-over of the same key does nothing.
/// </param>
/// <param name="Kind">What kind of message it is, such as <c>confirm-address</c>; the operator reads it in a list, where no body is shown.</param>
/// <param name="To">The address it goes to.</param>
/// <param name="Language">The language it is written in, by its tag.</param>
/// <param name="Subject">Its subject.</param>
/// <param name="Text">Its body as plain text.</param>
/// <param name="Html">Its body as simple HTML, when it has one.</param>
public sealed record OutgoingMail(string Key, string Kind, string To, string Language, string Subject, string Text, string? Html = null);

/// <summary>A message of the outbox as the operator sees it: everything but its body.</summary>
/// <param name="Key">The key of the message.</param>
/// <param name="Kind">What kind of message it is.</param>
/// <param name="To">The address it goes to.</param>
/// <param name="CreatedAt">When it was handed over.</param>
/// <param name="Attempts">How often sending it was tried.</param>
/// <param name="NextTry">When it is tried next, while it waits.</param>
/// <param name="SentAt">When it was sent, once it was.</param>
/// <param name="GaveUpAt">When sending it was given up, when it was.</param>
/// <param name="LastError">What the last try failed with, in a word.</param>
public sealed record MailState(string Key, string Kind, string To, DateTime CreatedAt, int Attempts, DateTime NextTry, DateTime? SentAt, DateTime? GaveUpAt, string? LastError);

/// <summary>Who sends a message: SMTP in the product, something that remembers in a test.</summary>
public interface IMailSender
{
    /// <summary>Sends a message, or throws. The identifier names the message to the servers on its way, so that one sent twice is known as the same.</summary>
    void Send(OutgoingMail mail, string messageId);
}

/// <summary>
/// The mail of the site that waits to be sent, in the table <c>delivery."Mail"</c> (ADR 0016). A message is
/// written there in the same transaction as what it tells of – so it exists exactly when that does – and a
/// service of the site sends what waits: again after a failure, each time later, and no more after
/// <see cref="MostAttempts"/> tries, when it is kept for the operator. Once sent, the body of a message is
/// emptied: it may hold a link that opens an account.
/// </summary>
public sealed class MailOutbox
{
    /// <summary>How often a message is tried before it is given up.</summary>
    public const int MostAttempts = 8;

    /// <summary>The longest address the outbox takes, as the standard of mail allows.</summary>
    public const int LongestAddress = 320;

    // How long a message waits after its first, second, … failed try.
    private static readonly TimeSpan[] Waits =
    [
        TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2),
        TimeSpan.FromHours(6), TimeSpan.FromHours(12), TimeSpan.FromHours(24),
    ];

    private static readonly string Table = $"""{Delivery.Schema}."Mail" """;
    private readonly string _connection;

    /// <summary>Initializes a new instance of the <see cref="MailOutbox"/> class for a database, by its connection.</summary>
    public MailOutbox(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        _connection = connection;
    }

    /// <summary>
    /// Hands a message over inside a transaction that is under way: it is sent if and only if that transaction
    /// is kept. Returns whether the message is new – a key that is there already is the same message.
    /// </summary>
    /// <param name="connection">The open connection of the transaction.</param>
    /// <param name="transaction">The transaction, or nothing for a connection that keeps each command by itself.</param>
    /// <param name="mail">The message.</param>
    /// <param name="now">The moment, in universal time.</param>
    public static bool Put(DbConnection connection, DbTransaction? transaction, OutgoingMail mail, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mail);
        if (Refused(mail) is { } refused)
        {
            throw new ArgumentException(refused, nameof(mail));
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            insert into {Table} ("Key", "Kind", "To", "Language", "Subject", "Text", "Html", "CreatedAt", "NextTry", "Attempts")
            values (@key, @kind, @to, @language, @subject, @text, @html, @now, @now, 0)
            on conflict ("Key") do nothing
            """;
        foreach (var (name, value) in new (string, object?)[]
        {
            ("key", mail.Key), ("kind", mail.Kind), ("to", mail.To.Trim()), ("language", mail.Language), ("subject", mail.Subject),
            ("text", mail.Text), ("html", mail.Html), ("now", Utc(now)),
        })
        {
            var parameter = command.CreateParameter();
            (parameter.ParameterName, parameter.Value) = (name, value ?? DBNull.Value);
            command.Parameters.Add(parameter);
        }

        return command.ExecuteNonQuery() == 1;
    }

    /// <summary>Hands a message over by itself, for what is told without anything else being written.</summary>
    public bool Put(OutgoingMail mail, DateTime now)
    {
        using var database = Delivery.Open(_connection);
        return Put(database, null, mail, now);
    }

    /// <summary>Returns why a message cannot be taken, or nothing when it can.</summary>
    public static string? Refused(OutgoingMail mail)
    {
        ArgumentNullException.ThrowIfNull(mail);
        return string.IsNullOrWhiteSpace(mail.Key) || mail.Key.Length > 100 ? "a message has a key of at most 100 characters."
            : string.IsNullOrWhiteSpace(mail.Kind) || mail.Kind.Length > 40 ? "a message says what kind it is, in at most 40 characters."
            : !IsAddress(mail.To) ? "a message goes to one mail address."
            : string.IsNullOrWhiteSpace(mail.Subject) || mail.Subject.Length > 300 || mail.Subject.Any(char.IsControl) ? "a message has a subject of one line."
            : string.IsNullOrWhiteSpace(mail.Text) ? "a message has a text."
            : null;
    }

    /// <summary>
    /// Returns whether a text can be one mail address: something before one <c>@</c> and a name with a dot
    /// after it, without a space or a control character – so that nothing written into a field can add a line
    /// to the head of a message.
    /// </summary>
    public static bool IsAddress(string? address)
    {
        if (address is null || address.Trim() is not { Length: > 0 and <= LongestAddress } written || written.Any(character => char.IsWhiteSpace(character) || char.IsControl(character) || character is '<' or '>' or ',' or ';' or '"'))
        {
            return false;
        }

        var at = written.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at == written.LastIndexOf('@') && written[(at + 1)..] is { Length: > 2 } domain && domain.Contains('.', StringComparison.Ordinal) && !domain.StartsWith('.') && !domain.EndsWith('.');
    }

    /// <summary>
    /// Sends what is due, at most so many messages, and returns how many were sent. Each message is taken with
    /// a lock of its row, so that two services that send at the same moment never take the same one. A message
    /// that cannot be sent waits longer each time and is given up after <see cref="MostAttempts"/> tries.
    /// </summary>
    public int Deliver(IMailSender sender, DateTime now, string host, int most = 20)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var sent = 0;
        for (var taken = 0; taken < most; taken++)
        {
            using var database = Delivery.Open(_connection);
            using var step = database.BeginTransaction();
            OutgoingMail? mail;
            int attempts;
            using (var due = Delivery.Command(
                database,
                $"""
                select "Key", "Kind", "To", "Language", "Subject", "Text", "Html", "Attempts" from {Table}
                where "SentAt" is null and "GaveUpAt" is null and "NextTry" <= @now
                order by "NextTry", "Key" limit 1 for update skip locked
                """,
                ("now", Utc(now))))
            using (var row = due.ExecuteReader())
            {
                if (!row.Read())
                {
                    return sent;
                }

                mail = new OutgoingMail(row.GetString(0), row.GetString(1), row.GetString(2), row.GetString(3), row.GetString(4), row.GetString(5), row.IsDBNull(6) ? null : row.GetString(6));
                attempts = row.GetInt32(7) + 1;
            }

            string? failed = null;
            try
            {
                // The same message carries the same identifier on every try: a server that got it twice knows it.
                sender.Send(mail, $"<{Uri.EscapeDataString(mail.Key)}@{host}>");
            }
#pragma warning disable CA1031 // Whatever a sender fails with, the message waits and is tried again; nothing of it stops the others.
            catch (Exception refused)
#pragma warning restore CA1031
            {
                // What failed in a word, never the text of a message: an error of a server can repeat an address.
                failed = refused.GetType().Name;
            }

            if (failed is null)
            {
                using var done = Delivery.Command(database, $"""update {Table} set "SentAt" = @now, "Attempts" = @attempts, "LastError" = null, "Text" = '', "Html" = null where "Key" = @key""", ("now", Utc(now)), ("attempts", attempts), ("key", mail.Key));
                done.ExecuteNonQuery();
                sent++;
            }
            else
            {
                var gaveUp = attempts >= MostAttempts;
                using var later = Delivery.Command(
                    database,
                    $"""update {Table} set "Attempts" = @attempts, "LastError" = @error, "NextTry" = @next, "GaveUpAt" = @gaveUp where "Key" = @key""",
                    ("attempts", attempts), ("error", failed), ("next", Utc(now + Waits[Math.Min(attempts, Waits.Length) - 1])), ("gaveUp", gaveUp ? Utc(now) : null), ("key", mail.Key));
                later.ExecuteNonQuery();
            }

            step.Commit();
        }

        return sent;
    }

    /// <summary>Returns the messages that wait to be sent, the one that is tried next first.</summary>
    public IReadOnlyList<MailState> Waiting() => Read("""where "SentAt" is null and "GaveUpAt" is null order by "NextTry", "Key" """);

    /// <summary>Returns the messages sending was given up for, the oldest first.</summary>
    public IReadOnlyList<MailState> GivenUp() => Read("""where "GaveUpAt" is not null order by "CreatedAt", "Key" """);

    /// <summary>Lets a message that was given up be tried again from now, as often as a new one. Returns whether there was such a message.</summary>
    public bool TryAgain(string key, DateTime now)
    {
        using var database = Delivery.Open(_connection);
        using var again = Delivery.Command(database, $"""update {Table} set "GaveUpAt" = null, "Attempts" = 0, "NextTry" = @now where "Key" = @key and "GaveUpAt" is not null""", ("now", Utc(now)), ("key", key));
        return again.ExecuteNonQuery() == 1;
    }

    /// <summary>
    /// Deletes what was sent or given up before a moment, and returns how many: a row names an address and is
    /// kept as long as it may be needed to answer whether a message was sent, and no longer.
    /// </summary>
    public int Forget(DateTime before)
    {
        using var database = Delivery.Open(_connection);
        using var forget = Delivery.Command(database, $"""delete from {Table} where "SentAt" < @before or "GaveUpAt" < @before""", ("before", Utc(before)));
        return forget.ExecuteNonQuery();
    }

    private List<MailState> Read(string which)
    {
        using var database = Delivery.Open(_connection);
        using var command = Delivery.Command(database, $"""select "Key", "Kind", "To", "CreatedAt", "Attempts", "NextTry", "SentAt", "GaveUpAt", "LastError" from {Table} {which}""");
        using var rows = command.ExecuteReader();
        var mail = new List<MailState>();
        while (rows.Read())
        {
            mail.Add(new MailState(
                rows.GetString(0), rows.GetString(1), rows.GetString(2), rows.GetDateTime(3), rows.GetInt32(4), rows.GetDateTime(5),
                rows.IsDBNull(6) ? null : rows.GetDateTime(6), rows.IsDBNull(7) ? null : rows.GetDateTime(7), rows.IsDBNull(8) ? null : rows.GetString(8)));
        }

        return mail;
    }

    private static DateTime Utc(DateTime moment) => moment.Kind == DateTimeKind.Utc ? moment : DateTime.SpecifyKind(moment, DateTimeKind.Utc);
}
