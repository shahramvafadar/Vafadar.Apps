using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Tests;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site sends the mail that waits in its database (step 19.4, ADR 0016): a message handed over while the
/// site runs arrives at the server the site was told, without anybody asking for it; and a site that was told
/// a server and not the rest does not start.
/// </summary>
public sealed class SiteMailTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-site-mail-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private sealed class Mailing(string? database, string? from, int port) : DemoSite
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Dastan:Mail:Host", MailCatcher.Host);
            builder.UseSetting("Dastan:Mail:Port", port.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("Dastan:Mail:Security", "None");
            if (from is not null)
            {
                builder.UseSetting("Dastan:Mail:From", from);
            }

            if (database is not null)
            {
                builder.UseSetting("Dastan:Database", database);
            }
        }
    }

    [Fact]
    public async Task What_waits_in_the_database_is_sent_while_the_site_runs()
    {
        MailCatcher.Needed();
        var database = TestDatabase.For(Path.Combine(_folder, "site.db"));
        var to = MailCatcher.NewAddress("sara");
        using var site = new Mailing(database, "no-reply@dastan.test", MailCatcher.Port);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await site.Visitor().Get("/en/")).Status);

        new MailOutbox(database).Put(new OutgoingMail($"note:{to}", "note", to, "en", "A note from the site", "The site wrote this while it ran."), DateTime.UtcNow);

        var caught = Assert.Single(await MailCatcher.For(to));
        Assert.Equal("A note from the site", (string?)caught["Subject"]);
        Assert.Empty(new MailOutbox(database).Waiting());

        // The pump of the site is the one the site was given, and a round with nothing due sends nothing.
        Assert.Equal(0, site.Services.GetRequiredService<MailPump>().Round());
    }

    [Fact]
    public async Task A_site_told_a_server_for_mail_and_not_the_rest_does_not_start()
    {
        var database = TestDatabase.For(Path.Combine(_folder, "site.db"));

        using (var nobody = new Mailing(database, from: null, port: 1025))
        {
            Assert.Contains("Dastan:Mail:From", (await Assert.ThrowsAnyAsync<Exception>(() => nobody.Visitor().Get("/en/"))).ToString(), StringComparison.Ordinal);
        }

        using (var nowhere = new Mailing(database: null, "no-reply@dastan.test", port: 1025))
        {
            Assert.Contains("Dastan:Database", (await Assert.ThrowsAnyAsync<Exception>(() => nowhere.Visitor().Get("/en/"))).ToString(), StringComparison.Ordinal);
        }

        using var early = new Mailing(TestDatabase.Empty(), "no-reply@dastan.test", port: 1025);
        Assert.Contains("database migrate", (await Assert.ThrowsAnyAsync<Exception>(() => early.Visitor().Get("/en/"))).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_server_for_mail_that_does_not_answer_does_not_stop_the_site()
    {
        var database = TestDatabase.For(Path.Combine(_folder, "site.db"));
        new MailOutbox(database).Put(new OutgoingMail("note:1", "note", "sara@example.org", "en", "A note", "Words."), DateTime.UtcNow.AddMinutes(-1));
        using var site = new Mailing(database, "no-reply@dastan.test", port: 1);

        Assert.Equal(System.Net.HttpStatusCode.OK, (await site.Visitor().Get("/en/")).Status);

        // The first round ran when the site started: the message was tried, was not lost, and waits.
        for (var attempt = 0; attempt < 100 && new MailOutbox(database).Waiting()[0].Attempts == 0; attempt++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, Assert.Single(new MailOutbox(database).Waiting()).Attempts);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await site.Visitor().Get("/en/")).Status);
    }
}
