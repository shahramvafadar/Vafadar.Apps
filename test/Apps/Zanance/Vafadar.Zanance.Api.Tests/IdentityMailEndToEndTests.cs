using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Original register/mail/confirm/recover/reset lifecycle with fictional identities and an in-memory Brevo peer.</summary>
[Trait("Qualification", "IdentityMailLocal")]
public sealed class IdentityMailEndToEndTests
{
    [Fact]
    public async Task Recovery_preserves_the_original_queued_challenge_until_it_is_delivered()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_MAIL_TEST_DB");
        Assert.SkipWhen(connection is null, "Requires the independent fictional identity-mail database.");
        var authority = new NpgsqlConnectionStringBuilder(connection!);
        Assert.Equal("zanance_identity_mail_d1227", authority.Database);
        Assert.Equal("127.0.0.1", authority.Host);
        var ct = TestContext.Current.CancellationToken;
        var root = Directory.CreateTempSubdirectory("zanance-recovery-queue-");
        var key = Path.Combine(root.FullName, "fictional-key");
        await File.WriteAllTextAsync(key, "fictional-not-a-provider-key", ct);
        try
        {
            await using (var setup = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection!).Options))
                await setup.Database.MigrateAsync(ct);

            var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
            using var sender = new BrevoIdentityMailSender("sender@example.invalid", key,
                new Peer((_, _) => throw new InvalidOperationException("No mail may be sent in this route test.")));
            await using var host = new Host(connection!, Path.Combine(root.FullName, "private"), clock, sender);
            using var http = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
            var email = $"recovery-{Guid.NewGuid():N}@example.invalid";
            var owner = new ServiceUser { Email = email, UserName = email, EmailConfirmed = true };
            Assert.True((await manager.CreateAsync(owner)).Succeeded);
            try
            {
                async Task RequestAsync(string language)
                {
                    using var response = await http.PostAsJsonAsync("/identity/recover", new IdentityInput(email, "", language), ct);
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                }

                var resets = db.Challenges.AsNoTracking().Where(row => row.UserId == owner.Id && row.Purpose == "reset");
                await RequestAsync("de-DE");
                var original = await resets.SingleAsync(ct);
                await RequestAsync("fa-IR");
                var retained = await resets.SingleAsync(ct);
                Assert.Equal((original.Id, original.ExpiresAt), (retained.Id, retained.ExpiresAt));
                Assert.True(original.ProtectedMail == retained.ProtectedMail, "The retained encrypted envelope changed.");

                await resets.Where(row => row.Id == original.Id).ExecuteUpdateAsync(set => set.SetProperty(row => row.Delivered, true), ct);
                await RequestAsync("fa-IR");
                Assert.Equal(2, await resets.CountAsync(ct));
                var queued = await resets.SingleAsync(row => !row.Delivered, ct);
                Assert.NotEqual(original.Id, queued.Id);

                var originalMail = IdentityMailEnvelope.Read(original,
                    scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>(), sender.Authority);
                using (var response = await http.PostAsJsonAsync("/identity/complete",
                    new ChallengeInput(originalMail.Id, originalMail.Token, "Fictional-Recovered-Password8!"), ct))
                    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                Assert.True(await resets.Where(row => row.Id == queued.Id).Select(row => row.Consumed).SingleAsync(ct));

                await RequestAsync("fr-FR");
                var current = await resets.SingleAsync(row => !row.Consumed, ct);
                Assert.NotEqual(queued.Id, current.Id);
                Assert.Equal(3, await resets.CountAsync(ct));
            }
            finally
            {
                await db.Challenges.Where(row => row.UserId == owner.Id).ExecuteDeleteAsync(CancellationToken.None);
                Assert.Equal(1, await db.Users.Where(row => row.Id == owner.Id).ExecuteDeleteAsync(CancellationToken.None));
            }
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    /// <summary>The captured language/code survives an unknown provider outcome; delivered codes complete both original Identity purposes.</summary>
    [Fact]
    public async Task Original_confirmation_and_recovery_complete_through_the_versioned_provider_outbox()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_MAIL_TEST_DB");
        Assert.SkipWhen(connection is null, "Requires the independent fictional identity-mail database.");
        var authority = new NpgsqlConnectionStringBuilder(connection!);
        Assert.Equal("zanance_identity_mail_d1227", authority.Database); Assert.Equal("127.0.0.1", authority.Host);
        var ct = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), "zanance-brevo-complete-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); var key = Path.Combine(root, "fictional-key");
        await File.WriteAllTextAsync(key, "fictional-not-a-provider-key", ct);
        var priorCulture = CultureInfo.CurrentUICulture;
        try
        {
            await using (var db = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection!).Options))
            {
                await db.Database.MigrateAsync(ct); Assert.False(db.Database.HasPendingModelChanges());
                Assert.Equal(0, await db.Users.CountAsync(ct));
            }
            var clock = new FakeTimeProvider(DateTimeOffset.UtcNow); var packets = new List<string>();
            using var sender = new BrevoIdentityMailSender("sender@example.invalid", key, new Peer(async (request, token) =>
            {
                packets.Add(await request.Content!.ReadAsStringAsync(token));
                if (packets.Count == 1) throw new HttpRequestException("Fictional unknown response after submission.");
                return new HttpResponseMessage(packets.Count == 2 ? HttpStatusCode.BadRequest : HttpStatusCode.Created)
                { Content = new StringContent(packets.Count == 2 ? "{\"code\":\"duplicate_parameter\"}" : "{\"messageIds\":[\"fictional-accepted\"]}") };
            }));
            await using var host = new Host(connection!, Path.Combine(root, "private"), clock, sender);
            using var http = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using (var response = await http.PostAsJsonAsync("/identity/register",
                new IdentityInput("recipient@example.invalid", "Fictional-Only-Password9!", "fa-IR"), ct))
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var first = await ReadAsync(host, "confirm", ct);
            Assert.Equal(2, first.Format); Assert.Equal("fa", first.Language);
            using var worker = new PrivateMaintenance(host.Services.GetRequiredService<IServiceScopeFactory>(),
                host.Services.GetRequiredService<ServiceStorage>(), clock, host.Services.GetRequiredService<IHostApplicationLifetime>());
            await worker.RunOnceAsync(ct); Assert.Single(packets);
            clock.Advance(TimeSpan.FromSeconds(61)); CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            await worker.RunOnceAsync(ct); Assert.Equal(2, packets.Count); Assert.Equal(packets[0], packets[1]);
            using (var mail = JsonDocument.Parse(packets[1]))
            {
                Assert.Equal("تأیید حساب Zanance", mail.RootElement.GetProperty("subject").GetString());
                Assert.Contains(first.Token, mail.RootElement.GetProperty("textContent").GetString());
                Assert.Contains(first.Id.ToString("D"), mail.RootElement.GetProperty("textContent").GetString());
            }
            using (var response = await http.PostAsJsonAsync("/identity/complete", new ChallengeInput(first.Id, first.Token, null), ct))
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            using (var response = await http.PostAsJsonAsync("/identity/recover", new IdentityInput("recipient@example.invalid", "", "de-DE"), ct))
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var second = await ReadAsync(host, "reset", ct); Assert.Equal("de", second.Language);
            await worker.RunOnceAsync(ct); Assert.Equal(3, packets.Count);
            using (var mail = JsonDocument.Parse(packets[2]))
            {
                Assert.Equal("Stellen Sie Ihr Zanance-Konto wieder her", mail.RootElement.GetProperty("subject").GetString());
                Assert.Contains(second.Token, mail.RootElement.GetProperty("textContent").GetString());
            }
            using (var response = await http.PostAsJsonAsync("/identity/complete",
                new ChallengeInput(second.Id, second.Token, "Different-Fictional-Password8!"), ct))
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var scope = host.Services.CreateAsyncScope(); var dbFinal = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            Assert.Equal(1, await dbFinal.Users.CountAsync(ct)); Assert.Equal(2, await dbFinal.Challenges.CountAsync(x => x.Delivered && x.Consumed, ct));
            Assert.Empty(await dbFinal.Sessions.ToArrayAsync(ct)); Assert.Empty(await dbFinal.Objects.ToArrayAsync(ct));
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>(); var user = await dbFinal.Users.SingleAsync(ct);
            Assert.True(user.EmailConfirmed); Assert.True(await manager.CheckPasswordAsync(user, "Different-Fictional-Password8!"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = priorCulture;
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<IdentityMail> ReadAsync(Host host, string purpose, CancellationToken ct)
    {
        await using var scope = host.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var row = await db.Challenges.AsNoTracking().SingleAsync(x => x.Purpose == purpose, ct);
        return IdentityMailEnvelope.Read(row, scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>(), "brevo-v1:sender@example.invalid");
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
