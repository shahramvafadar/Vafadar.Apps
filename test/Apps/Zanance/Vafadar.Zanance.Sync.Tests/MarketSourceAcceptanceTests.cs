using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Data;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Rates;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Rates;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Sync.Tests;

/// <summary>Original actual TLS/identity/PostgreSQL and two migrated SQLite clients; no live market provider or owner data.</summary>
[Trait("AT", "AT-299")][Trait("Qualification", "MarketLocal")]
public sealed class MarketSourceAcceptanceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string Email = "fictitious-market-d299@example.invalid";
    private const string Password = "Fictitious-Market-Tls-Password9!";
    private sealed class Private(string root) : IPersonalSyncTokenStore, IMarketPrivateCache
    {
        private readonly IDataProtector protector = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "keys"))).CreateProtector("fictitious-market-client-v1");
        private string PathFor(string kind, string key)
        {
            var leaf = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(kind + "\n" + key)));
            Directory.CreateDirectory(root); return Path.Combine(root, leaf + ".private");
        }
        private async Task<string?> Read(string kind, string key, CancellationToken ct)
        { var path = PathFor(kind, key); return File.Exists(path) ? protector.Unprotect(await File.ReadAllTextAsync(path, ct)) : null; }
        private Task Write(string kind, string key, string value, CancellationToken ct) => File.WriteAllTextAsync(PathFor(kind, key), protector.Protect(value), ct);
        public async Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct)
        { var value = await Read("session", peer, ct); return value is null ? null : JsonSerializer.Deserialize(value, PersonalSyncSessionJsonContext.Default.PersonalSyncSession); }
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct) => Write("session", peer, JsonSerializer.Serialize(session, PersonalSyncSessionJsonContext.Default.PersonalSyncSession), ct);
        public Task RemoveAsync(string peer, CancellationToken ct) { File.Delete(PathFor("session", peer)); return Task.CompletedTask; }
        Task<string?> IMarketPrivateCache.ReadAsync(string scope, CancellationToken ct) => Read("cache", scope, ct);
        Task IMarketPrivateCache.WriteAsync(string scope, string value, CancellationToken ct) => Write("cache", scope, value, ct);
    }
    private sealed class Provider : IMarketProvider
    {
        internal int Calls;
        public MarketCatalog? Catalog => new(1, "fictitious-http-market", "fixture-v1", "Fictitious original HTTP source",
            "https://source.example.invalid/reference", "https://source.example.invalid/licence", "Fictitious local qualification only",
            "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), true,
            [new("EURUSD", "Fictitious EUR/USD", "EUR", "USD", MarketUnit.Currency)]);
        public Task<MarketObservation> FetchAsync(MarketRequest request, CancellationToken ct)
        { Calls++; return Task.FromResult(new MarketObservation("fictitious-http-observation", request.OnOrBefore, DateTimeOffset.UtcNow.AddMinutes(-10), 1.23456789m)); }
    }
    private sealed class Client : IAsyncDisposable
    {
        internal readonly string File;
        internal readonly ServiceProvider Services;
        internal readonly Private Storage;
        internal readonly PersonalSyncTransport Transport;
        internal PersonalSyncStore Sync => Services.GetRequiredService<PersonalSyncStore>();
        internal MarketReferenceStore Sources => Services.GetRequiredService<MarketReferenceStore>();
        internal ZananceStore Finance => Services.GetRequiredService<ZananceStore>();
        internal Client(string root, LocalSyncPeer peer)
        {
            Directory.CreateDirectory(root); File = Path.Combine(root, "financial.db"); Storage = new(Path.Combine(root, "private"));
            Services = new ServiceCollection().AddSingleton(TimeProvider.System).AddZananceData(File).BuildServiceProvider();
            Services.MigrateLocalDatabase<ZananceDbContext>(); Transport = new(peer, Storage);
        }
        internal Task<PersonalSyncOutcome> Synchronize() => Services.GetRequiredService<PersonalSyncCoordinator>().SynchronizeAsync(Transport, Ct);
        public async ValueTask DisposeAsync() { Transport.Dispose(); await Services.DisposeAsync(); LocalDatabasePools.ClearFile(File); }
    }
    [Fact]
    public async Task Real_original_source_companion_preserves_lost_reply_two_device_history_bounds_and_revocation_without_financial_mutation()
    {
        var admin = Environment.GetEnvironmentVariable("ZANANCE_MARKET_QA_CONNECTION");
        if (admin is null) { Assert.Skip("Explicit owned market PostgreSQL fixture required."); return; }
        var runtime = Environment.GetEnvironmentVariable("ZANANCE_MARKET_QA_RUNTIME_CONNECTION") ?? throw new InvalidOperationException("Owned non-owner runtime required.");
        var root = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_MARKET_QA_ROOT")!);
        var settings = new NpgsqlConnectionStringBuilder(admin);
        Assert.Equal("zanance_market_d299", settings.Database); Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" });
        Assert.Equal("Zanance fictitious market qualification v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        var complete = Path.Combine(root, "complete.json"); Assert.False(File.Exists(complete), "Do not repeat a completed original market qualification.");
        var stagePath = Path.Combine(root, "stage.txt"); var stage = File.Exists(stagePath) ? int.Parse(await File.ReadAllTextAsync(stagePath, Ct), System.Globalization.CultureInfo.InvariantCulture) : 0;
        async Task Advance(int value) { await File.WriteAllTextAsync(stagePath, value.ToString(System.Globalization.CultureInfo.InvariantCulture), Ct); stage = value; }
        await using (var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(admin).Options))
        {
            await schema.Database.MigrateAsync(Ct); Assert.False(schema.Database.HasPendingModelChanges());
            await schema.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime;", Ct);
        }
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = runtime,
            ["Service:Root"] = Path.Combine(root, "service"), ["Market:LocalQualification"] = "true" });
        builder.AddZananceService(); builder.Services.RemoveAll<IHostedService>(); builder.Logging.ClearProviders();
        var provider = new Provider(); builder.Services.AddSingleton<IMarketProvider>(provider);
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 15499));
        await using var app = builder.Build(); var loseReply = false; var sourcePuts = 0;
        app.Use(async (http, next) =>
        {
            if (http.Request.Path.Value?.EndsWith("/source-documents", StringComparison.Ordinal) == true && http.Request.Method == "PUT")
            {
                Interlocked.Increment(ref sourcePuts);
                if (loseReply)
                {
                    loseReply = false; var original = http.Response.Body; using var withheld = new MemoryStream(); http.Response.Body = withheld;
                    try { await next(http); http.Abort(); } finally { http.Response.Body = original; } return;
                }
            }
            await next(http);
        });
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var peer = new LocalSyncPeer("https://localhost:15499/", await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using (var scope = app.Services.CreateScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
                if (await manager.FindByEmailAsync(Email) is null)
                    Assert.True((await manager.CreateAsync(new() { UserName = Email, Email = Email, EmailConfirmed = true }, Password)).Succeeded);
            }
            await using var a = new Client(Path.Combine(root, "client-a"), peer);
            await using var b = new Client(Path.Combine(root, "client-b"), peer);
            if (await a.Transport.BillingIdentityAsync(Ct) is null) await a.Transport.SignInAsync(Email, Password, Ct);
            if (await b.Transport.BillingIdentityAsync(Ct) is null) await b.Transport.SignInAsync(Email, Password, Ct);
            var owner = (await a.Transport.IdentityAsync(Ct)).Id; Assert.Equal(owner, (await b.Transport.IdentityAsync(Ct)).Id);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious source device A", Ct);
            var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious source device B", Ct);
            var operationPath = Path.Combine(root, "profile-operation.txt");
            if (!File.Exists(operationPath)) await File.WriteAllTextAsync(operationPath, Guid.CreateVersion7().ToString("D"), Ct);
            var operation = Guid.Parse(await File.ReadAllTextAsync(operationPath, Ct));
            var remote = await a.Transport.CreateProfileAsync(new(deviceA.Id, operation, "Fictitious original source profile", SyncSchema.Hash), Ct);
            MarketReferenceDocument original;
            await using (var local = await a.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            {
                var receipt = await local.MarketReferenceReceipts.AsNoTracking().SingleOrDefaultAsync(Ct);
                if (receipt is null)
                {
                    var client = new MarketQuoteClient(a.Storage, peer, a.File, TimeProvider.System, () => { });
                    var availability = await client.AvailabilityAsync(a.Transport, owner, Ct); Assert.NotNull(availability.Catalog);
                    var request = new MarketRequest(availability.Catalog.Provider, availability.Catalog.Revision, "EURUSD", DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date));
                    var quote = await client.FetchAsync(a.Transport, owner, availability.Catalog, request, Ct);
                    Assert.Equal(1, provider.Calls); Assert.Single(await client.ReadAsync(Ct));
                    receipt = await a.Sources.ApplyAsync(await a.Sources.ReviewRateAsync(quote, Ct), false, () => { }, Ct);
                }
                original = MarketReferenceDocument.From(receipt);
            }
            if ((await a.Sync.ReviewAsync(Ct)).RemoteProfile is null)
                await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, owner, deviceA.Id, remote, Ct);
            if (stage < 1)
            {
                using var anonymous = LocalSyncHttp.CreateClient(peer);
                Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"sync/profiles/{remote.Id:D}/source-documents?deviceId={deviceA.Id:D}&after=0", Ct)).StatusCode);
                var attempted = Path.Combine(root, "original-lost-source-reply.attempted");
                if (!File.Exists(attempted))
                {
                    await File.WriteAllTextAsync(attempted, "Original source reply loss, do not dispatch another loss scenario", Ct); loseReply = true;
                    var failure = await Record.ExceptionAsync(async () => await a.Synchronize()); Assert.NotNull(failure);
                }
                await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var stored = Assert.Single(await db.Set<PersonalMarketSourceRow>().AsNoTracking().ToListAsync(Ct));
                Assert.Equal(original.Id, stored.DocumentId); Assert.DoesNotContain("Fictitious original HTTP source", stored.ProtectedDocument);
                Assert.Single(await a.Finance.GetRatesAsync(Ct)); await Advance(1);
            }
            if (stage < 2)
            {
                Assert.True((await a.Synchronize()).Applied);
                await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                Assert.Single(await db.Set<PersonalMarketSourceRow>().AsNoTracking().ToListAsync(Ct)); Assert.Single(await a.Finance.GetRatesAsync(Ct));
                remote = (await a.Transport.ProfilesAsync(deviceA.Id, Ct)).Single(row => row.Id == remote.Id);
                if ((await b.Sync.ReviewAsync(Ct)).RemoteProfile is null)
                    await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, owner, deviceB.Id, remote, Ct, receiveFirstRun: true);
                Assert.True((await b.Synchronize()).Applied);
                var restored = Assert.Single(await b.Sources.CurrentAsync(MarketReferenceTarget.ExchangeRate, original.TargetId, Ct));
                Assert.Equal(original, MarketReferenceDocument.From(restored));
                var rate = Assert.Single(await b.Finance.GetRatesAsync(Ct)); Assert.Equal(original.TargetId, rate.Id); Assert.Equal(1.23456789m, rate.Rate);
                Assert.Empty(await b.Finance.GetEntriesAsync(cancellationToken: Ct)); await Advance(2);
            }
            if (stage < 3)
            {
                var beforePuts = sourcePuts; Assert.True((await b.Synchronize()).Applied); Assert.Equal(beforePuts, sourcePuts);
                using var raw = await Authorized(peer, a.Storage);
                var path = $"sync/profiles/{remote.Id:D}/source-documents?deviceId={deviceA.Id:D}";
                var valid = JsonSerializer.Serialize(new MarketSourceUpload(1, deviceA.Id, [original]), MarketSourceJson.Default.MarketSourceUpload);
                var unknown = valid.Insert(valid.LastIndexOf('}'), ",\"AccountBalance\":100");
                Assert.Equal(HttpStatusCode.UnprocessableEntity, (await raw.PutAsync(path, new StringContent(unknown, Encoding.UTF8, "application/json"), Ct)).StatusCode);
                var duplicate = valid.Insert(1, "\"Version\":1,");
                Assert.Equal(HttpStatusCode.UnprocessableEntity, (await raw.PutAsync(path, new StringContent(duplicate, Encoding.UTF8, "application/json"), Ct)).StatusCode);
                var altered = original with { AcceptedAt = original.AcceptedAt.AddSeconds(1) };
                Assert.Equal(HttpStatusCode.Conflict, (await raw.PutAsJsonAsync(path, new MarketSourceUpload(1, deviceA.Id, [altered]), MarketSourceJson.Default.MarketSourceUpload, Ct)).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await raw.GetAsync($"sync/profiles/{Guid.NewGuid():D}/source-documents?deviceId={deviceA.Id:D}&after=0", Ct)).StatusCode);
                await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                Assert.Single(await db.Set<PersonalMarketSourceRow>().AsNoTracking().ToListAsync(Ct));
                await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("UPDATE \"PersonalMarketSources\" SET \"Bytes\"=1", Ct));
                await Advance(3);
            }
            if (stage < 4)
            {
                var changed = Assert.Single(await b.Finance.GetRatesAsync(Ct)); changed.Rate = 2.3456789m; await b.Finance.SaveRateAsync(changed, Ct);
                Assert.Empty(await b.Sources.CurrentAsync(MarketReferenceTarget.ExchangeRate, original.TargetId, Ct));
                Assert.True((await b.Synchronize()).Applied); Assert.True((await a.Synchronize()).Applied);
                Assert.Equal(2.3456789m, Assert.Single(await a.Finance.GetRatesAsync(Ct)).Rate);
                Assert.Empty(await a.Sources.CurrentAsync(MarketReferenceTarget.ExchangeRate, original.TargetId, Ct));
                await using var local = await a.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
                Assert.Equal(original, MarketReferenceDocument.From(Assert.Single(await local.MarketReferenceReceipts.AsNoTracking().ToListAsync(Ct))));
                await Advance(4);
            }
            if (stage < 5)
            {
                await a.Transport.RevokeDeviceAsync(deviceB.Id, Ct); using var revoked = await Authorized(peer, b.Storage);
                Assert.Equal(HttpStatusCode.Unauthorized, (await revoked.GetAsync($"sync/profiles/{remote.Id:D}/source-documents?deviceId={deviceB.Id:D}&after=0", Ct)).StatusCode);
                Assert.Single(await b.Finance.GetRatesAsync(Ct)); await Advance(5);
            }
            await File.WriteAllTextAsync(complete, "{\"Stages\":5,\"RealPinnedTls\":true,\"ActualIdentityMiddleware\":true,\"ActualPostgreSql\":true,\"TwoActualSqliteClients\":true,\"LiveProviderOrLicenceAcceptance\":false}", Ct);
        }
        finally { await app.StopAsync(CancellationToken.None); }
    }
    private static async Task<HttpClient> Authorized(LocalSyncPeer peer, Private storage)
    {
        var session = await storage.ReadAsync(peer.BindingKey("session"), Ct) ?? throw new InvalidOperationException("Original fixture session unavailable.");
        var client = LocalSyncHttp.CreateClient(peer); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken); return client;
    }
}
