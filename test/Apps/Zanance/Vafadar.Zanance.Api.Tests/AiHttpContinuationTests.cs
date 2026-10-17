using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>AT-298: original private clients, actual identity middleware, pinned TLS sockets and PostgreSQL; no live model.</summary>
[Trait("AT", "AT-298")][Trait("Qualification", "AiLocal")]
public sealed class AiHttpContinuationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Sessions : IPersonalSyncTokenStore
    {
        private readonly Dictionary<string, PersonalSyncSession> rows = [];
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) => Task.FromResult(rows.GetValueOrDefault(peer));
        public Task WriteAsync(string peer, PersonalSyncSession value, CancellationToken ct) { rows[peer] = value; return Task.CompletedTask; }
        public Task RemoveAsync(string peer, CancellationToken ct) { rows.Remove(peer); return Task.CompletedTask; }
    }
    private sealed class Private(string root, IDataProtector protection) : IAiPrivateStorage
    {
        public async Task<string?> ReadAsync(string scope, CancellationToken ct)
        { var path = Path.Combine(root, scope + ".protected"); return File.Exists(path) ? protection.Unprotect(await File.ReadAllTextAsync(path, ct)) : null; }
        public async Task WriteAsync(string scope, string value, CancellationToken ct)
        {
            var path = Path.Combine(root, scope + ".protected");
            await File.WriteAllTextAsync(path, protection.Protect(value), ct);
            if (protection.Unprotect(await File.ReadAllTextAsync(path, ct)) != value) throw new IOException("Original fixture persistence failed.");
        }
    }
    private sealed class Provider(string root) : IAiProvider
    {
        internal AiProviderOutcome Begin = new(AiProviderState.Completed, 3, "Fictitious local result, never a model or money command.");
        internal AiProviderOutcome Status = new(AiProviderState.Unknown);
        private readonly ConcurrentDictionary<string, int> calls = new();
        public AiProviderOffer? Offer(AiPurpose purpose) => new("fictitious-tls", "fixture-v1", 8);
        public Task<AiProviderOutcome> BeginAsync(string operation, AiOffer offer, AiPayload payload, CancellationToken ct)
        {
            var path = Path.Combine(root, "dispatch-" + operation + ".txt");
            var count = calls.AddOrUpdate(operation, File.Exists(path) ? int.Parse(File.ReadAllText(path), System.Globalization.CultureInfo.InvariantCulture) + 1 : 1, (_, prior) => prior + 1);
            File.WriteAllText(path, count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (count != 1) throw new InvalidOperationException("Original provider operation dispatched twice.");
            return Task.FromResult(Begin);
        }
        public Task<AiProviderOutcome> StatusAsync(string operation, AiOffer offer, CancellationToken ct) => Task.FromResult(Status);
    }
    /// <summary>Resumes only unfinished owned stages; completed original sockets/clients are not repeated.</summary>
    [Fact]
    public async Task Real_TLS_private_intent_lost_reply_concurrent_credit_and_revoked_identity_preserve_original_requests()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit owned analysis PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55478, settings.Port); Assert.Equal("zanance_ai_d298", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_AI_QA_ROOT")!);
        Assert.Equal("Zanance fictional analysis d298", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), Ct));
        var root = Path.Combine(parent, "tls-client"); Directory.CreateDirectory(root);
        var complete = Path.Combine(root, "complete.json"); Assert.False(File.Exists(complete), "Do not replay completed original TLS qualification.");
        var stagePath = Path.Combine(root, "stage.txt"); var stage = File.Exists(stagePath) ? int.Parse(await File.ReadAllTextAsync(stagePath, Ct), System.Globalization.CultureInfo.InvariantCulture) : 0;
        async Task Advance(int value) { await File.WriteAllTextAsync(stagePath, value.ToString(System.Globalization.CultureInfo.InvariantCulture), Ct); stage = value; }
        var clockPath = Path.Combine(root, "clock.json");
        var clock = new FakeTimeProvider(File.Exists(clockPath) ? JsonSerializer.Deserialize<DateTimeOffset>(await File.ReadAllTextAsync(clockPath, Ct)) : DateTimeOffset.UtcNow);
        async Task Tick(TimeSpan elapsed) { clock.Advance(elapsed); await File.WriteAllTextAsync(clockPath, JsonSerializer.Serialize(clock.GetUtcNow()), Ct); }
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = connection,
            ["Service:Root"] = Path.Combine(root, "service"), ["Analysis:LocalQualification"] = "true" });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        // Retention/mail/billing are separate completed qualifications; none may mutate another original fixture here.
        builder.Services.RemoveAll<IHostedService>(); builder.Services.AddSingleton<TimeProvider>(clock);
        var provider = new Provider(root); builder.Services.AddSingleton<IAiProvider>(provider);
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        var loseReply = false; var financialPosts = 0;
        app.Use(async (http, next) =>
        {
            if (http.Request.Path == "/analysis/requests" && http.Request.Method == "POST")
            {
                Interlocked.Increment(ref financialPosts);
                if (loseReply)
                {
                    loseReply = false; var original = http.Response.Body;
                    using var withheld = new MemoryStream(); http.Response.Body = withheld;
                    try { await next(http); http.Abort(); } finally { http.Response.Body = original; }
                    return;
                }
            }
            await next(http);
        });
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using (var anonymous = LocalSyncHttp.CreateClient(peer, clock))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("analysis/availability", Ct)).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsync("analysis/grants", null, Ct)).StatusCode);
            }
            const string email = "fictitious-ai-tls-d298@example.invalid";
            const string password = "Fictitious-Analysis-Tls-Password9!";
            string owner;
            using (var scope = app.Services.CreateScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>(); var user = await manager.FindByEmailAsync(email);
                if (user is null)
                { user = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true }; Assert.True((await manager.CreateAsync(user, password)).Succeeded); }
                owner = user.Id;
            }
            var sessions = new Sessions(); using var transport = new PersonalSyncTransport(peer, sessions, clock);
            Assert.Equal(owner, (await transport.SignInAsync(email, password, Ct)).Id);
            var store = app.Services.GetRequiredService<AiRequestStore>();
            var privateStorage = new Private(root, DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "client-keys"))).CreateProtector("fictional-analysis-client"));
            AiClient Client(string name) => new(transport, privateStorage, owner, Path.Combine(root, name + ".sqlite"), () => { });
            static AiPayload Payload() => new(1, AiPurpose.PeriodExplanation, "fa", new(new(2026, 10, 1), new(2026, 10, 17), 1, true, [new("EUR", 10000, 0, 3000, 700)], 0), null);
            async Task Original(string name, AiClient client)
            { var record = await client.ReadAsync(Ct); Assert.NotNull(record); await File.WriteAllTextAsync(Path.Combine(root, name + "-id.txt"), record.Submission.RequestId.ToString("D"), Ct); }
            if (stage < 1)
            {
                await store.GrantAsync(owner, "fictitious-original-tls-wallet", true, 20, Ct);
                var client = Client("before-http"); var offer = await client.OfferAsync(AiPurpose.PeriodExplanation, Ct); var before = financialPosts;
                await Assert.ThrowsAsync<IOException>(() => client.SubmitAsync(offer, Payload(), () => throw new IOException("Fictitious process boundary before HTTP."), Ct));
                Assert.Equal(before, financialPosts); await Original("before-http", client); await Advance(1);
            }
            if (stage < 2)
            {
                var client = Client("before-http"); var before = financialPosts;
                var result = await client.ContinueAsync(Ct); Assert.Equal(AiRequestState.Completed, result.State); Assert.Equal(3, result.ChargedUnits);
                Assert.Equal(Guid.Parse(await File.ReadAllTextAsync(Path.Combine(root, "before-http-id.txt"), Ct)), result.RequestId);
                Assert.Equal(before + 1, financialPosts); Assert.Equal(17, result.Balance.PurchasedAvailable); await Advance(2);
            }
            if (stage < 3)
            {
                var client = Client("lost-reply"); var prior = await client.ReadAsync(Ct);
                if (prior is null)
                {
                    var offer = await client.OfferAsync(AiPurpose.PeriodExplanation, Ct); loseReply = true;
                    await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.SubmitAsync(offer, Payload(), () => Task.CompletedTask, Ct));
                }
                Assert.Null((await client.ReadAsync(Ct))!.Result); await Original("lost-reply", client); await Advance(3);
            }
            if (stage < 4)
            {
                // A new client reads the original protected bytes; no extra financial POST/provider Begin is permitted.
                var client = Client("lost-reply"); var before = financialPosts; var result = await client.ContinueAsync(Ct);
                Assert.Equal(AiRequestState.Completed, result.State); Assert.Equal(14, result.Balance.PurchasedAvailable); Assert.Equal(before, financialPosts);
                Assert.Equal(Guid.Parse(await File.ReadAllTextAsync(Path.Combine(root, "lost-reply-id.txt"), Ct)), result.RequestId); await Advance(4);
            }
            if (stage < 5)
            {
                var client = Client("invalid-result");
                if (await client.ReadAsync(Ct) is null)
                {
                    provider.Begin = new(AiProviderState.Completed, 3, new string('x', 4001));
                    var result = await client.SubmitAsync(await client.OfferAsync(AiPurpose.PeriodExplanation, Ct), Payload(), () => Task.CompletedTask, Ct);
                    Assert.Equal(AiRequestState.Pending, result.State); Assert.Equal(8, result.Balance.PurchasedReserved);
                }
                var posts = financialPosts; Assert.Equal(AiRequestState.Pending, (await client.CloseAsync(Ct)).State); Assert.Equal(posts, financialPosts);
                provider.Status = new(AiProviderState.DefinitiveNoCharge); await Tick(TimeSpan.FromSeconds(3));
                var resolved = await client.ContinueAsync(Ct); Assert.Equal(AiRequestState.Refunded, resolved.State); Assert.Equal(14, resolved.Balance.PurchasedAvailable); await Advance(5);
            }
            if (stage < 6)
            {
                var a = Client("concurrent-a"); var b = Client("concurrent-b");
                provider.Begin = new(AiProviderState.Unknown); provider.Status = new(AiProviderState.Unknown);
                if (await a.ReadAsync(Ct) is null && await b.ReadAsync(Ct) is null)
                {
                    var offerA = await a.OfferAsync(AiPurpose.PeriodExplanation, Ct); var offerB = await b.OfferAsync(AiPurpose.PeriodExplanation, Ct);
                    var results = await Task.WhenAll(a.SubmitAsync(offerA, Payload(), () => Task.CompletedTask, Ct), b.SubmitAsync(offerB, Payload(), () => Task.CompletedTask, Ct));
                    Assert.Single(results, r => r.State == AiRequestState.Pending); Assert.Single(results, r => r.State == AiRequestState.NotAccepted);
                }
                // These clients share rotating-session serialization; AiConcurrentCreditTests qualifies separate worker owner locks.
                var balance = (await a.AvailabilityAsync(Ct)).Balance; Assert.Equal(6, balance.PurchasedAvailable); Assert.Equal(8, balance.PurchasedReserved);
                provider.Status = new(AiProviderState.DefinitiveNoCharge); await Tick(TimeSpan.FromSeconds(3));
                foreach (var client in new[] { a, b }) if ((await client.ReadAsync(Ct))!.Result!.State == AiRequestState.Pending) Assert.Equal(AiRequestState.Refunded, (await client.ContinueAsync(Ct)).State);
                Assert.Equal(14, (await a.AvailabilityAsync(Ct)).Balance.PurchasedAvailable); await Advance(6);
            }
            if (stage < 7)
            {
                var client = Client("expired-consent"); var prior = await client.ReadAsync(Ct);
                if (prior is null)
                {
                    var offer = await client.OfferAsync(AiPurpose.PeriodExplanation, Ct); await Tick(TimeSpan.FromMinutes(6)); var before = financialPosts;
                    var result = await client.SubmitAsync(offer, Payload(), () => Task.CompletedTask, Ct); Assert.Equal(AiRequestState.NotAccepted, result.State);
                    Assert.Equal(before + 1, financialPosts); Assert.Equal(14, result.Balance.PurchasedAvailable); Assert.Equal(0, result.Balance.PurchasedReserved);
                }
                await using var db = await app.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>().CreateDbContextAsync(Ct);
                await db.Sessions.Where(s => s.UserId == owner).ExecuteUpdateAsync(u => u.SetProperty(s => s.Revoked, true), Ct);
                var refusal = await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.AvailabilityAsync(Ct)); Assert.Equal(HttpStatusCode.Unauthorized, refusal.Status);
                Assert.NotNull(await Client("lost-reply").ReadAsync(Ct)); await Advance(7);
            }
            await File.WriteAllTextAsync(complete, "{\"Stages\":7,\"RealPinnedTls\":true,\"RealIdentityMiddleware\":true,\"RealPostgreSql\":true,\"OriginalPrivateContinuation\":true,\"ActualProviderOrPurchase\":false}", Ct);
        }
        finally { await app.StopAsync(CancellationToken.None); }
    }
}
