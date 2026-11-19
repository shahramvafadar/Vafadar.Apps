using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The mail that waits to be sent (step 19.4, ADR 0016): a message is kept once however often it is handed
/// over, exists exactly when what it tells of does, is sent once – also when several send at the same moment –
/// tried again later and later after a failure, given up after a number of tries, and leaves by SMTP as it was
/// written. Test data.
/// </summary>
public sealed class MailOutboxTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 19, 9, 0, 0, DateTimeKind.Utc);
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-mail-" + Guid.NewGuid().ToString("N"));
    private readonly string _database;

    public MailOutboxTests() => _database = TestDatabase.For(Path.Combine(_folder, "mail.db"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static OutgoingMail Mail(string key, string to = "mina@example.org") =>
        new(key, "confirm-address", to, "en", "Confirm your address", "Open this link: https://dastan.test/confirm/abc", "<p>Open <a href=\"https://dastan.test/confirm/abc\">this link</a>.</p>");

    // Remembers what it was asked to send, fails as often as it is told to, and takes its time when told to.
    private sealed class Remembering(int failures = 0, int delay = 0) : IMailSender
    {
        private int _failures = failures;

        public System.Collections.Concurrent.ConcurrentBag<(string Key, string MessageId)> Sent { get; } = [];

        public void Send(OutgoingMail mail, string messageId)
        {
            Thread.Sleep(delay);
            if (Interlocked.Decrement(ref _failures) >= 0)
            {
                throw new InvalidOperationException($"The server refused {mail.To}.");
            }

            Sent.Add((mail.Key, messageId));
        }
    }

    private T Ask<T>(string sql)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)command.ExecuteScalar()!;
    }

    [Fact]
    public void A_message_is_kept_once_however_often_it_is_handed_over_and_sent_once()
    {
        var outbox = new MailOutbox(_database);
        Assert.True(outbox.Put(Mail("confirm:mina:1"), Now));
        Assert.False(outbox.Put(Mail("confirm:mina:1") with { Subject = "Another subject" }, Now));
        var waiting = Assert.Single(outbox.Waiting());
        Assert.Equal(("confirm:mina:1", "confirm-address", "mina@example.org", 0), (waiting.Key, waiting.Kind, waiting.To, waiting.Attempts));

        var sender = new Remembering();
        Assert.Equal(1, outbox.Deliver(sender, Now, "dastan.test"));
        Assert.Equal(0, outbox.Deliver(sender, Now.AddHours(1), "dastan.test"));
        Assert.Equal(("confirm:mina:1", "<confirm%3Amina%3A1@dastan.test>"), Assert.Single(sender.Sent));
        Assert.Empty(outbox.Waiting());

        // Once sent, the body is gone: it held a link that opens an account. That it was sent, and to whom, stays.
        Assert.Equal(string.Empty, Ask<string>("""select "Text" from delivery."Mail" """));
        Assert.True(Ask<bool>("""select "Html" is null and "SentAt" is not null from delivery."Mail" """));

        // And a key that was sent is not sent again by being handed over again.
        Assert.False(outbox.Put(Mail("confirm:mina:1"), Now.AddDays(1)));
        Assert.Equal(0, outbox.Deliver(sender, Now.AddDays(1), "dastan.test"));
    }

    [Fact]
    public void A_message_exists_exactly_when_what_it_tells_of_does()
    {
        var outbox = new MailOutbox(_database);
        using (var connection = new Npgsql.NpgsqlConnection(_database))
        {
            connection.Open();
            using (var undone = connection.BeginTransaction())
            {
                Assert.True(MailOutbox.Put(connection, undone, Mail("undone"), Now));
                undone.Rollback();
            }

            using var kept = connection.BeginTransaction();
            Assert.True(MailOutbox.Put(connection, kept, Mail("kept"), Now));

            // Not sent before the transaction is kept: nobody else sees the row.
            Assert.Equal(0, outbox.Deliver(new Remembering(), Now, "dastan.test"));
            kept.Commit();
        }

        Assert.Equal("kept", Assert.Single(outbox.Waiting()).Key);
    }

    [Fact]
    public void A_message_that_cannot_be_sent_waits_longer_each_time_and_is_given_up_in_the_end()
    {
        var outbox = new MailOutbox(_database);
        outbox.Put(Mail("slow"), Now);
        var failing = new Remembering(failures: int.MaxValue);
        var at = Now;

        // Not before its time, and each wait longer than the one before.
        var waits = new List<TimeSpan>();
        for (var attempt = 1; attempt < MailOutbox.MostAttempts; attempt++)
        {
            Assert.Equal(0, outbox.Deliver(failing, at, "dastan.test"));
            var state = Assert.Single(outbox.Waiting());
            Assert.Equal((attempt, "InvalidOperationException"), (state.Attempts, state.LastError));
            waits.Add(state.NextTry - at);
            Assert.Equal(0, outbox.Deliver(failing, state.NextTry.AddSeconds(-1), "dastan.test"));
            Assert.Equal(attempt, Assert.Single(outbox.Waiting()).Attempts);
            at = state.NextTry;
        }

        Assert.Equal(waits.Order(), waits);
        Assert.True(waits[0] < waits[^1]);

        Assert.Equal(0, outbox.Deliver(failing, at, "dastan.test"));
        Assert.Empty(outbox.Waiting());
        var gone = Assert.Single(outbox.GivenUp());
        Assert.Equal((MailOutbox.MostAttempts, at), (gone.Attempts, gone.GaveUpAt));

        // What the error said is not kept – it can repeat an address – only what kind of error it was.
        Assert.DoesNotContain("mina@example.org", gone.LastError, StringComparison.Ordinal);

        // The operator lets it be tried again, and now the server takes it.
        Assert.False(outbox.TryAgain("no-such-key", at));
        Assert.True(outbox.TryAgain("slow", at));
        var working = new Remembering();
        Assert.Equal(1, outbox.Deliver(working, at, "dastan.test"));
        Assert.Single(working.Sent);

        // Kept as long as it may be asked whether it was sent, and no longer.
        Assert.Equal(0, outbox.Forget(at));
        Assert.Equal(1, outbox.Forget(at.AddSeconds(1)));
    }

    [Fact]
    public async Task Several_that_send_at_the_same_moment_send_every_message_once()
    {
        var outbox = new MailOutbox(_database);
        for (var number = 0; number < 24; number++)
        {
            outbox.Put(Mail($"message-{number}"), Now);
        }

        var sender = new Remembering(delay: 20);
        using var start = new ManualResetEventSlim();
        var sending = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            start.Wait();
            return new MailOutbox(_database).Deliver(sender, Now, "dastan.test", most: 24);
        })).ToArray();
        start.Set();

        Assert.Equal(24, (await Task.WhenAll(sending)).Sum());
        Assert.Equal(24, sender.Sent.Select(sent => sent.Key).Distinct().Count());
        Assert.Equal(24, sender.Sent.Count);
    }

    [Theory]
    [InlineData("mina@example.org", true)]
    [InlineData("  mina@example.org ", true)]
    [InlineData("مینا@example.org", true)]
    [InlineData("mina@example", false)]
    [InlineData("mina.example.org", false)]
    [InlineData("mina@@example.org", false)]
    [InlineData("mina@example.org, sara@example.org", false)]
    [InlineData("mina@example.org\r\nBcc: everybody@example.org", false)]
    [InlineData("Mina <mina@example.org>", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void One_address_is_an_address_and_nothing_that_could_add_a_line_to_a_message_is(string? address, bool is_one)
    {
        Assert.Equal(is_one, MailOutbox.IsAddress(address));
    }

    [Fact]
    public void What_is_no_message_is_refused()
    {
        var outbox = new MailOutbox(_database);
        Assert.Throws<ArgumentException>(() => outbox.Put(Mail("x") with { To = "nobody" }, Now));
        Assert.Throws<ArgumentException>(() => outbox.Put(Mail("x") with { Subject = "A subject\r\nBcc: everybody@example.org" }, Now));
        Assert.Throws<ArgumentException>(() => outbox.Put(Mail("x") with { Text = " " }, Now));
        Assert.Throws<ArgumentException>(() => outbox.Put(Mail(new string('k', 101)), Now));
        Assert.Empty(outbox.Waiting());
    }

    [Fact]
    public async Task A_message_leaves_by_smtp_as_it_was_written_in_any_script()
    {
        MailCatcher.Needed();
        var to = MailCatcher.NewAddress("mina");
        var outbox = new MailOutbox(_database);
        outbox.Put(new OutgoingMail($"confirm:{to}", "confirm-address", to, "fa", "نشانی خود را تأیید کنید", "این پیوند را باز کنید:\nhttps://dastan.test/fa/confirm/abc", "<div dir=\"rtl\" lang=\"fa\"><p>این <a href=\"https://dastan.test/fa/confirm/abc\">پیوند</a> را باز کنید.</p></div>"), Now);
        var options = new MailOptions { Host = MailCatcher.Host, Port = MailCatcher.Port, Security = "None", From = "no-reply@dastan.test", FromName = "داستان" };

        Assert.Equal(1, outbox.Deliver(new SmtpMailSender(options), Now, options.Domain));

        var caught = Assert.Single(await MailCatcher.For(to));
        Assert.Equal("نشانی خود را تأیید کنید", (string?)caught["Subject"]);
        Assert.Equal(to, (string?)caught["To"]![0]!["Address"]);
        Assert.Equal(("no-reply@dastan.test", "داستان"), ((string?)caught["From"]!["Address"], (string?)caught["From"]!["Name"]));
        Assert.Contains("https://dastan.test/fa/confirm/abc", (string?)caught["Text"], StringComparison.Ordinal);
        Assert.Contains("این پیوند را باز کنید", (string?)caught["Text"], StringComparison.Ordinal);
        Assert.Contains("dir=\"rtl\"", (string?)caught["HTML"], StringComparison.Ordinal);
        Assert.EndsWith("@dastan.test", (string?)caught["MessageID"], StringComparison.Ordinal);
    }

    [Fact]
    public void A_server_that_is_not_there_does_not_lose_the_message()
    {
        var outbox = new MailOutbox(_database);
        outbox.Put(Mail("unreachable"), Now);
        var nowhere = new MailOptions { Host = "127.0.0.1", Port = 1, Security = "None", From = "no-reply@dastan.test" };

        Assert.Equal(0, outbox.Deliver(new SmtpMailSender(nowhere), Now, nowhere.Domain));

        var waiting = Assert.Single(outbox.Waiting());
        Assert.Equal(1, waiting.Attempts);
        Assert.NotNull(waiting.LastError);
        Assert.Throws<ArgumentException>(() => new SmtpMailSender(new MailOptions { Host = "smtp.example.org" }));
        Assert.False(new MailOptions { From = "no-reply@dastan.test" }.CanSend);
    }
}
