using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sharing;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Original PostgreSQL outbox and actual sender under concurrent workers, without authentication or financial effects.</summary>
[Trait("Qualification", "IdentityMailLocal")]
public sealed class IdentityMailOutboxTests
{
    /// <summary>Unknown sender return keeps the original lease; another worker cannot duplicate it, and restart reuses its exact batch.</summary>
    [Fact]
    public async Task Concurrent_workers_and_unknown_return_preserve_original_challenge_until_exact_sender_acknowledgement()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_MAIL_TEST_DB");
        Assert.SkipWhen(connection is null, "Requires the separately owned fictional identity-mail database.");
        var authority = new NpgsqlConnectionStringBuilder(connection!);
        Assert.Equal("zanance_identity_mail_d1226", authority.Database);
        Assert.Equal("127.0.0.1", authority.Host);
        var ct = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), "zanance-mail-outbox-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "objects")); Directory.CreateDirectory(Path.Combine(root, "mailbox"));
        var key = Path.Combine(root, "fictional-key"); await File.WriteAllTextAsync(key, "fictional-not-a-provider-key", ct);
        try
        {
            var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
            var protection = new EphemeralDataProtectionProvider();
            var first = Guid.NewGuid(); var second = Guid.NewGuid(); var expired = Guid.NewGuid(); var consumed = Guid.NewGuid();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var packets = new ConcurrentDictionary<Guid, List<string>>();
            using var sender = new BrevoIdentityMailSender("sender@example.invalid", key, new Peer(async (request, token) =>
            {
                var packet = await request.Content!.ReadAsStringAsync(token);
                using var json = JsonDocument.Parse(packet);
                var id = Guid.Parse(json.RootElement.GetProperty("headers").GetProperty("idempotencyKey").GetString()!);
                var records = packets.GetOrAdd(id, _ => []); int count;
                lock (records) { records.Add(packet); count = records.Count; }
                if (id == first && count == 1)
                {
                    started.SetResult(); await release.Task.WaitAsync(token);
                    throw new HttpRequestException("Fictional unknown provider return.");
                }
                return new HttpResponseMessage(id == first ? HttpStatusCode.BadRequest : HttpStatusCode.Created)
                { Content = new StringContent(id == first ? "{\"code\":\"duplicate_parameter\"}" : "{\"messageIds\":[\"fictional-acceptance\"]}") };
            }));
            var services = new ServiceCollection();
            services.AddSingleton<TimeProvider>(clock); services.AddSingleton<IDataProtectionProvider>(protection);
            services.AddSingleton(new ServiceStorage { Root = root, LocalMailbox = false });
            services.AddSingleton<IIdentityMailSender>(sender);
            services.AddSingleton<IHouseholdDeletionLedger, HouseholdDeletionLedger>(); services.AddSingleton<HouseholdRetirement>();
            services.AddDbContextFactory<ServiceDbContext>(options => options.UseNpgsql(connection!));
            await using var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                await db.Database.MigrateAsync(ct); Assert.False(db.Database.HasPendingModelChanges());
                Assert.Equal(0, await db.Users.CountAsync(ct));
                var user = new ServiceUser { Id = Guid.NewGuid().ToString("N"), Email = "recipient@example.invalid" };
                db.Users.Add(user);
                foreach (var id in new[] { first, second, expired, consumed })
                    db.Challenges.Add(new IdentityChallenge
                    {
                        Id = id, UserId = user.Id, Purpose = id == second ? "reset" : "confirm",
                        ExpiresAt = id == expired ? clock.GetUtcNow().AddMinutes(-1) : clock.GetUtcNow().AddMinutes(id == first ? 10 : 14),
                        Consumed = id == consumed,
                        ProtectedMail = protection.CreateProtector("identity-mail-v1").Protect(JsonSerializer.Serialize(new PrivateMail(user.Email, "fictional-original-code", sender.Authority)))
                    });
                await db.SaveChangesAsync(ct);
            }
            PrivateMaintenance Worker() => new(provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<ServiceStorage>(), clock, new Lifetime());
            using var a = Worker(); using var b = Worker();
            var pending = a.RunOnceAsync(ct);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
            await b.RunOnceAsync(ct);
            Assert.Single(packets[first]); Assert.Single(packets[second]);
            release.SetResult(); await pending;
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                Assert.False((await db.Challenges.SingleAsync(x => x.Id == first, ct)).Delivered);
                Assert.True((await db.Challenges.SingleAsync(x => x.Id == second, ct)).Delivered);
                Assert.Equal(1, await db.Users.CountAsync(ct)); Assert.Empty(await db.Objects.ToArrayAsync(ct));
            }
            clock.Advance(TimeSpan.FromSeconds(61));
            using var restarted = Worker(); await restarted.RunOnceAsync(ct);
            Assert.Equal(2, packets[first].Count); Assert.Equal(packets[first][0], packets[first][1]);
            Assert.False(packets.ContainsKey(expired)); Assert.False(packets.ContainsKey(consumed));
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                Assert.Equal(2, await db.Challenges.CountAsync(x => x.Delivered, ct));
                Assert.Equal(1, await db.Challenges.CountAsync(x => x.Consumed, ct));
                Assert.Empty(await db.Sessions.ToArrayAsync(ct));
            }
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, "mailbox")));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private sealed class Peer(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);
    }
    private sealed class Lifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
}
