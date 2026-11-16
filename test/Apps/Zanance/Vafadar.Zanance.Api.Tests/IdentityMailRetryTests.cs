using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Late sender setup and expired confirmation recovery without replacing a user's password or accepted current request.</summary>
[Trait("Qualification", "IdentityMailLocal")]
public sealed class IdentityMailRetryTests
{
    /// <summary>Only the correct original unconfirmed credentials may reissue after expiry; concurrent repeats share one new envelope.</summary>
    [Fact]
    public async Task Late_key_setup_can_continue_after_expiry_without_replacing_an_active_or_confirmed_request()
    {
        var input = Environment.GetEnvironmentVariable("ZANANCE_MAIL_TEST_DB");
        Assert.SkipWhen(input is null, "Requires the independent fictional identity-mail database.");
        var connection = new NpgsqlConnectionStringBuilder(input!);
        Assert.Equal("zanance_identity_mail_d1227", connection.Database); Assert.Equal("127.0.0.1", connection.Host);
        connection.Database = "zanance_identity_mail_retry_d1227";
        var ct = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), "zanance-brevo-retry-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var key = Path.Combine(root, "late-fictional-key"); var calls = 0;
        try
        {
            await using (var db = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection.ConnectionString).Options))
            { await db.Database.MigrateAsync(ct); Assert.Equal(0, await db.Users.CountAsync(ct)); }
            using var sender = new BrevoIdentityMailSender("sender@example.invalid", key, new Peer((_, _) =>
            {
                calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                { Content = new StringContent("{\"messageIds\":[\"fictional-receipt\"]}") });
            }));
            var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
            await using var host = new Host(connection.ConnectionString, Path.Combine(root, "private"), clock, sender);
            using var http = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            async Task Register(string password, string language)
            {
                using var response = await http.PostAsJsonAsync("/identity/register", new IdentityInput("retry-recipient@example.invalid", password, language), ct);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                Assert.Equal("", await response.Content.ReadAsStringAsync(ct));
            }
            async Task<IdentityChallenge[]> Rows()
            {
                await using var scope = host.Services.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<ServiceDbContext>().Challenges.AsNoTracking().OrderBy(x => x.ExpiresAt).ToArrayAsync(ct);
            }
            const string password = "Fictional-Original-Password9!";
            await Register(password, "de"); var first = Assert.Single(await Rows());
            using var worker = new PrivateMaintenance(host.Services.GetRequiredService<IServiceScopeFactory>(),
                host.Services.GetRequiredService<ServiceStorage>(), clock, host.Services.GetRequiredService<IHostApplicationLifetime>());
            await worker.RunOnceAsync(ct); Assert.Equal(0, calls); Assert.False(Assert.Single(await Rows()).Delivered);
            clock.Advance(TimeSpan.FromMinutes(16));
            var oldMail = IdentityMailEnvelope.Read(first, host.Services.GetRequiredService<IDataProtectionProvider>(), sender.Authority);
            using (var expired = await http.PostAsJsonAsync("/identity/complete", new ChallengeInput(oldMail.Id, oldMail.Token, null), ct))
                Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
            await Register("Fictional-Wrong-Password8!", "fr"); Assert.Single(await Rows());
            // Both explicit callers arrive after the old envelope expires. The original user lock permits one replacement only.
            await Task.WhenAll(Register(password, "it"), Register(password, "it"));
            var rows = await Rows(); Assert.Equal(2, rows.Length); Assert.Equal(first.Id, rows[0].Id); Assert.NotEqual(first.Id, rows[1].Id);
            await Register(password, "fr"); Assert.Equal(2, (await Rows()).Length);
            await File.WriteAllTextAsync(key, "fictional-not-a-provider-key", ct);
            await worker.RunOnceAsync(ct); Assert.Equal(1, calls);
            rows = await Rows(); Assert.False(rows[0].Delivered); Assert.True(rows[1].Delivered);
            await using var finalScope = host.Services.CreateAsyncScope();
            var protection = finalScope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
            var mail = IdentityMailEnvelope.Read(rows[1], protection, sender.Authority);
            Assert.Equal("it", mail.Language); Assert.Equal(2, mail.Format);
            using (var response = await http.PostAsJsonAsync("/identity/complete", new ChallengeInput(mail.Id, mail.Token, null), ct))
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await Register(password, "es"); Assert.Equal(2, (await Rows()).Length); Assert.Equal(1, calls);
            var dbFinal = finalScope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            Assert.Equal(1, await dbFinal.Users.CountAsync(ct)); Assert.Empty(await dbFinal.Sessions.ToArrayAsync(ct)); Assert.Empty(await dbFinal.Objects.ToArrayAsync(ct));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private sealed class Host(string connection, string root, TimeProvider clock, IIdentityMailSender sender) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration"); builder.UseSetting("ConnectionStrings:Service", connection); builder.UseSetting("Service:Root", root);
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(PrivateMaintenance)).ToArray()) services.Remove(descriptor);
                services.RemoveAll<TimeProvider>(); services.AddSingleton(clock); services.AddSingleton(sender);
                if (!OperatingSystem.IsWindows()) services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            });
        }
    }
    private sealed class Peer(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);
    }
}
