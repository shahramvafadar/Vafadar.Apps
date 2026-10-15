using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Data;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Sync.Tests;

/// <summary>Original service, pinned TLS transport and two original SQLite clients; never owner app data.</summary>
[Trait("AT", "AT-292")]
public sealed partial class PersonalSyncAcceptanceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string Email = "personal-sync@example.invalid";
    private const string Password = "Fictitious-Sync-Password9!";

    private sealed class TlsFailureLog(string path) : ILoggerProvider
    {
        private sealed class Logger(string path) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
            {
                if (error is null) return;
                // TLS exceptions only: never request bodies, credentials, arguments or financial objects.
                var chain = new List<string>();
                for (var current = error; current is not null && chain.Count < 5; current = current.InnerException)
                    chain.Add(current.GetType().Name + " " + current.HResult.ToString("X8") + " " + current.Message);
                lock (this) File.AppendAllText(path, id.Name + ": " + string.Join(" -> ", chain) + Environment.NewLine);
            }
        }
        public ILogger CreateLogger(string categoryName) => new Logger(path);
        public void Dispose() { }
    }

    private sealed class SessionMemory : IPersonalSyncTokenStore
    {
        private readonly Dictionary<string, PersonalSyncSession> sessions = [];
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) => Task.FromResult(sessions.GetValueOrDefault(peer));
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct)
        { ct.ThrowIfCancellationRequested(); sessions[peer] = session; return Task.CompletedTask; }
        public Task RemoveAsync(string peer, CancellationToken ct) { sessions.Remove(peer); return Task.CompletedTask; }
    }

    private sealed class Client : IDisposable
    {
        internal readonly ServiceProvider Services;
        internal readonly PersonalSyncTransport Transport;
        internal ZananceStore Finances => Services.GetRequiredService<ZananceStore>();
        internal PersonalSyncStore Sync => Services.GetRequiredService<PersonalSyncStore>();
        internal PersonalSyncCoordinator Coordinator => Services.GetRequiredService<PersonalSyncCoordinator>();
        internal Client(string path, LocalSyncPeer peer)
        {
            Services = new ServiceCollection().AddSingleton(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
            Services.MigrateLocalDatabase<ZananceDbContext>();
            Transport = new(peer, new SessionMemory());
        }
        public void Dispose() { Transport.Dispose(); Services.Dispose(); }
    }

    /// <summary>Original round recovery, financial merge/review, attachments, tombstones and revocation over real TLS.</summary>
    [Fact]
    public async Task Two_actual_clients_preserve_financial_choices_and_resume_an_unacknowledged_commit()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB")
            ?? throw new InvalidOperationException("An explicit isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT")
            ?? throw new InvalidOperationException("An explicit isolated sync fixture directory is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" });
        Assert.True(Path.IsPathFullyQualified(root));
        const string marker = "Zanance fictional personal sync acceptance v1";
        if (Directory.Exists(root))
        {
            Assert.Equal(marker, await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
            Assert.False(File.Exists(Path.Combine(root, "completed-stages.txt")));
            Assert.False(File.Exists(Path.Combine(root, "client-a.sqlite")));
        }
        else { Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), marker, Ct); }
        void Checkpoint(string value) => File.AppendAllText(Path.Combine(root, "completed-stages.txt"), value + Environment.NewLine);
        await using (var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options))
        {
            await schema.Database.MigrateAsync(Ct);
            Assert.False(schema.Database.HasPendingModelChanges());
            await schema.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime;", Ct);
        }
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new TlsFailureLog(Path.Combine(root, "tls-failures.txt")))
            .AddFilter<TlsFailureLog>("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Debug);
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        var loseNextCommit = 0;
        app.Use(async (http, next) =>
        {
            if (!http.Request.Path.Value!.EndsWith("/commit", StringComparison.Ordinal) || Interlocked.CompareExchange(ref loseNextCommit, 0, 1) != 1)
            { await next(http); return; }
            // Suppress only the fictional response after the original transaction commits. No server algorithm is replaced.
            var actual = http.Response.Body; await using var suppressed = new MemoryStream(); http.Response.Body = suppressed;
            try { await next(http); }
            finally { http.Response.Body = actual; }
            http.Abort();
        });
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using var enrollment = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            await EnrollAsync(enrollment, app.Services);
            Checkpoint("original-service-and-pinned-tls-ready");
            using var a = new Client(Path.Combine(root, "client-a.sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-b.sqlite"), peer);
            var owner = await a.Transport.SignInAsync(Email, Password, Ct);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious device A", Ct);
            var operation = Guid.NewGuid(); var create = new SyncCreateProfile(deviceA.Id, operation, "Fictitious shared personal copy", SyncSchema.Hash);
            var remote = await a.Transport.CreateProfileAsync(create, Ct);
            Assert.Equal(remote.Id, (await a.Transport.CreateProfileAsync(create, Ct)).Id);
            await a.Finances.GetSettingsAsync(Ct);
            var cash = new Account { Name = "Fictitious cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 15), OpeningBalance = 100_000 };
            await a.Finances.SaveAccountAsync(cash, Ct);
            var entry = new LedgerEntry { AccountId = cash.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 15), Amount = 1234, Title = "Fictitious purchase" };
            Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, owner.Id, deviceA.Id, remote, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Checkpoint("first-original-profile-published");
            var secondOwner = await b.Transport.SignInAsync(Email, Password, Ct); Assert.Equal(owner.Id, secondOwner.Id);
            var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious device B", Ct);
            await b.Finances.GetSettingsAsync(Ct);
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, secondOwner.Id, deviceB.Id, remote, Ct);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Equal(1234, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Amount);
            Assert.Equal(cash.Id, Assert.Single(await b.Finances.GetAccountsAsync(cancellationToken: Ct)).Id);
            Checkpoint("second-original-client-received-profile");

            cash.Name = "Fictitious name from A"; await a.Finances.SaveAccountAsync(cash, Ct);
            var cashB = Assert.Single(await b.Finances.GetAccountsAsync(cancellationToken: Ct));
            cashB.OpeningBalance = 101_000; await b.Finances.SaveAccountAsync(cashB, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            var merged = Assert.Single(await a.Finances.GetAccountsAsync(cancellationToken: Ct));
            Assert.Equal("Fictitious name from A", merged.Name); Assert.Equal(101_000, merged.OpeningBalance);
            Checkpoint("disjoint-whole-row-changes-merged");

            entry = Assert.Single(await a.Finances.GetEntriesAsync(cancellationToken: Ct)); entry.Amount = 1300;
            Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            var entryB = Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct)); entryB.Amount = 1400;
            Assert.True((await b.Finances.SaveEntryAsync(entryB, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            var contested = await b.Coordinator.SynchronizeAsync(b.Transport, Ct); Assert.False(contested.Applied); Assert.True(contested.NeedsReview);
            var conflict = Assert.Single(await b.Sync.ConflictsAsync(Ct)); Assert.Contains("Amount", conflict.Conflict.Fields);
            Assert.Equal(1400, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Amount);
            await b.Sync.ResolveAsync(conflict, keepLocalProposal: false, Ct);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Equal(1300, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Amount);
            Checkpoint("competing-money-waits-for-explicit-existing-choice");

            var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "fictitious-receipt.pdf", ContentType = "application/pdf",
                Data = Encoding.UTF8.GetBytes("Fictitious bounded receipt bytes. No owner document.") };
            await a.Finances.AddAttachmentAsync(attachment, Ct);
            Interlocked.Exchange(ref loseNextCommit, 1);
            var dropped = await Record.ExceptionAsync(() => a.Coordinator.SynchronizeAsync(a.Transport, Ct));
            Assert.NotNull(dropped); Assert.True(dropped is HttpRequestException or IOException or OperationCanceledException);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var received = await b.Finances.GetAttachmentAsync(attachment.Id, Ct);
            Assert.NotNull(received); Assert.Equal(attachment.Data, received.Data);
            Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct));
            Checkpoint("lost-real-commit-response-recovered-with-identical-attachment");

            await a.Finances.DeleteAttachmentAsync(attachment.Id, Ct); await a.Finances.DeleteEntryAsync(entry.Id, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Empty(await b.Finances.GetEntriesAsync(cancellationToken: Ct)); Assert.Null(await b.Finances.GetAttachmentAsync(attachment.Id, Ct));
            Checkpoint("explicit-deletion-propagated-as-tombstones");

            await a.Transport.RevokeDeviceAsync(deviceB.Id, Ct);
            var refused = await Assert.ThrowsAsync<PersonalSyncHttpException>(() => b.Transport.IdentityAsync(Ct));
            Assert.Equal(HttpStatusCode.Unauthorized, refused.Status);
            Assert.Single(await b.Finances.GetAccountsAsync(cancellationToken: Ct));
            Checkpoint("revocation-refuses-old-session-with-local-finances-preserved");
        }
        finally { await app.StopAsync(Ct); }
        // Preserve both successful and failed owned fixtures for inspection; the operator stops only its exact container.
    }

    private static async Task EnrollAsync(HttpClient client, IServiceProvider services)
    {
        using var registration = await client.PostAsJsonAsync("identity/register", new IdentityInput(Email, Password), Ct);
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        await using var scope = services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var user = await db.Users.SingleAsync(row => row.Email == Email, Ct);
        var challenge = await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", Ct);
        var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException("The fictitious challenge is unavailable.");
        using var confirmation = await client.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), Ct);
        Assert.Equal(HttpStatusCode.NoContent, confirmation.StatusCode);
    }

    /// <summary>Continues the retained successful two-client fixture; previous successful scenarios are not reseeded or rerun.</summary>
    [Fact]
    public async Task Duplicate_full_settlements_keep_one_actual_payment_after_explicit_related_row_review()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("revocation-refuses-old-session-with-local-finances-preserved", await File.ReadAllLinesAsync(Path.Combine(root, "completed-stages.txt"), Ct));
        var proof = Path.Combine(root, "completed-settlement-stages.txt"); Assert.False(File.Exists(proof));
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var previousPin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, previousPin);
            Assert.Equal(previousPin, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using var a = new Client(Path.Combine(root, "client-a.sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-b.sqlite"), peer);
            var ownerA = await a.Transport.SignInAsync(Email, Password, Ct); var ownerB = await b.Transport.SignInAsync(Email, Password, Ct);
            Assert.Equal(ownerA.Id, ownerB.Id);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious returned device A", Ct);
            var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious returned device B", Ct);
            var remote = Assert.Single(await a.Transport.ProfilesAsync(deviceA.Id, Ct));
            // The explicit reconnection is required: these are new sessions on a new local socket, not silently restored credentials.
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, ownerA.Id, deviceA.Id, remote, Ct);
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, ownerB.Id, deviceB.Id, remote, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Empty(await a.Finances.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(await b.Finances.GetEntriesAsync(cancellationToken: Ct));
            Checkpoint("same-key-ring-and-financial-files-explicitly-reconnected");
            var account = Assert.Single(await a.Finances.GetAccountsAsync(cancellationToken: Ct));
            var date = new DateOnly(2026, 10, 15);
            var plan = new Schedule { Name = "Fictitious once-only payment", AccountId = account.Id, Kind = EntryKind.Expense,
                AmountMode = AmountMode.Fixed, Amount = 500, Rule = new() { Start = date, Frequency = Frequency.Once } };
            var plansA = a.Services.GetRequiredService<PlanStore>(); var plansB = b.Services.GetRequiredService<PlanStore>();
            await plansA.SaveScheduleAsync(plan, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var planB = await plansB.GetScheduleAsync(plan.Id, Ct); Assert.NotNull(planB);
            var occurrenceA = Assert.Single(Occurrences.Between(plan, [], date, date, date));
            var occurrenceB = Assert.Single(Occurrences.Between(planB, [], date, date, date));
            var paymentA = Occurrences.CreateEntry(occurrenceA, 500, date, ReviewState.Confirmed);
            var paymentB = Occurrences.CreateEntry(occurrenceB, 500, date, ReviewState.Confirmed);
            Assert.NotEqual(paymentA.Id, paymentB.Id);
            Assert.True((await plansA.SettleAsync(occurrenceA, paymentA, Ct)).Succeeded);
            Assert.True((await plansB.SettleAsync(occurrenceB, paymentB, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            var duplicate = await b.Coordinator.SynchronizeAsync(b.Transport, Ct); Assert.True(duplicate.NeedsReview); Assert.False(duplicate.Applied);
            var reviews = await b.Sync.ConflictsAsync(Ct);
            var paymentReview = Assert.Single(reviews, row => row.Conflict.Reason == "OccurrenceAlreadySettled");
            Assert.NotNull(paymentReview.Conflict.ExistingSettlement); Assert.NotNull(paymentReview.Conflict.RelatedRecord?.Values);
            Assert.Equal("Entries", paymentReview.Conflict.RelatedRecord!.Record.Table);
            Assert.Single(await a.Finances.GetEntriesAsync(cancellationToken: Ct));
            Assert.Equal(paymentB.Id, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Id);
            Checkpoint("duplicate-full-payment-blocked-with-existing-payment-for-review");
            foreach (var review in reviews) await b.Sync.ResolveAsync(review, keepLocalProposal: false, Ct);
            var resolved = await b.Coordinator.SynchronizeAsync(b.Transport, Ct); Assert.True(resolved.Applied); Assert.False(resolved.NeedsReview);
            Assert.Equal(paymentA.Id, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Id);
            var state = Assert.Single(await plansB.GetStatesAsync(plan.Id, Ct));
            Assert.Equal(paymentA.Id, state.EntryId); Assert.Equal(OccurrenceStatus.Settled, state.Status);
            Assert.Empty(await b.Sync.ConflictsAsync(Ct)); Assert.Equal(0, (await b.Sync.StatusAsync(Ct)).PendingChanges);
            Checkpoint("explicit-existing-row-choices-preserve-one-payment-and-matching-occurrence-state");
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>Continues only the next conflict phase on the retained files; stale approval cannot select unseen money.</summary>
    [Fact]
    public async Task Changed_related_payment_requires_new_review_without_losing_other_explicit_choices()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("explicit-existing-row-choices-preserve-one-payment-and-matching-occurrence-state",
            await File.ReadAllLinesAsync(Path.Combine(root, "completed-settlement-stages.txt"), Ct));
        var proof = Path.Combine(root, "completed-related-review-stages.txt"); Assert.False(File.Exists(proof));
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, pin);
            using var a = new Client(Path.Combine(root, "client-a.sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-b.sqlite"), peer);
            var ownerA = await a.Transport.SignInAsync(Email, Password, Ct); var ownerB = await b.Transport.SignInAsync(Email, Password, Ct);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious stale-review A", Ct);
            var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious stale-review B", Ct);
            var remote = Assert.Single(await a.Transport.ProfilesAsync(deviceA.Id, Ct));
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, ownerA.Id, deviceA.Id, remote, Ct);
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, ownerB.Id, deviceB.Id, remote, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var date = new DateOnly(2026, 10, 15); var account = Assert.Single(await a.Finances.GetAccountsAsync(cancellationToken: Ct));
            var plan = new Schedule { Name = "Fictitious version-bound review", AccountId = account.Id, Kind = EntryKind.Expense,
                AmountMode = AmountMode.Fixed, Amount = 500, Rule = new() { Start = date, Frequency = Frequency.Once } };
            var plansA = a.Services.GetRequiredService<PlanStore>(); var plansB = b.Services.GetRequiredService<PlanStore>();
            await plansA.SaveScheduleAsync(plan, Ct); Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var planB = await plansB.GetScheduleAsync(plan.Id, Ct); Assert.NotNull(planB);
            var occurrenceA = Assert.Single(Occurrences.Between(plan, [], date, date, date));
            var occurrenceB = Assert.Single(Occurrences.Between(planB, [], date, date, date));
            var paymentA = Occurrences.CreateEntry(occurrenceA, 500, date, ReviewState.Confirmed);
            var paymentB = Occurrences.CreateEntry(occurrenceB, 500, date, ReviewState.Confirmed);
            Assert.True((await plansA.SettleAsync(occurrenceA, paymentA, Ct)).Succeeded);
            Assert.True((await plansB.SettleAsync(occurrenceB, paymentB, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var reviewed = await b.Sync.ConflictsAsync(Ct); Assert.Equal(2, reviewed.Count);
            var round = await ReadRoundReceiptAsync(b);
            var handle = await b.Transport.OpenAsync(remote.Id, new(deviceB.Id, round.Operation, SyncSchema.Hash), Ct);
            var missingCounterpart = reviewed.Select(row => new SyncResolution(row.Conflict.Proposal.Record, row.Conflict.Current?.Version ?? 0, false)).ToArray();
            var refused = await b.Transport.CommitAsync(remote.Id, deviceB.Id, handle.Id,
                new(handle.Parts, checked((int)handle.Rows), round.Hash, missingCounterpart), Ct);
            Assert.False(refused.Committed); Assert.All(refused.Conflicts, row => Assert.Equal("ReviewStale", row.Reason));
            Checkpoint("omitting-the-required-existing-counterpart-review-is-refused");
            foreach (var review in reviewed) await b.Sync.ResolveAsync(review, keepLocalProposal: false, Ct);
            paymentA.Amount = 600; Assert.True((await a.Finances.SaveEntryAsync(paymentA, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            var stale = await b.Coordinator.SynchronizeAsync(b.Transport, Ct); Assert.True(stale.NeedsReview); Assert.False(stale.Applied);
            var fresh = Assert.Single(await b.Sync.ConflictsAsync(Ct)); Assert.Equal("ReviewStale", fresh.Conflict.Reason);
            Assert.Equal(600, fresh.Conflict.RelatedRecord!.Values!.Value.GetProperty("Amount").GetInt64());
            Assert.Equal(500, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.ScheduleId == plan.Id).Amount);
            Checkpoint("changed-existing-money-invalidates-only-its-reviewed-choice");
            await b.Sync.ResolveAsync(fresh, keepLocalProposal: false, Ct);
            var applied = await b.Coordinator.SynchronizeAsync(b.Transport, Ct); Assert.True(applied.Applied); Assert.False(applied.NeedsReview);
            var chosen = Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.ScheduleId == plan.Id);
            Assert.Equal(paymentA.Id, chosen.Id); Assert.Equal(600, chosen.Amount);
            Assert.Equal(paymentA.Id, Assert.Single(await plansB.GetStatesAsync(plan.Id, Ct)).EntryId);
            Assert.Equal(0, (await b.Sync.StatusAsync(Ct)).PendingChanges); Assert.Empty(await b.Sync.ConflictsAsync(Ct));
            Checkpoint("fresh-approval-completes-with-the-other-unchanged-choice-preserved");
        }
        finally { await app.StopAsync(Ct); }
    }

    private static async Task<(Guid Operation, string Hash)> ReadRoundReceiptAsync(Client client)
    {
        await using var db = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT OperationId,ContentHash FROM SyncClientRound WHERE Id=1";
        await using var row = await command.ExecuteReaderAsync(Ct); Assert.True(await row.ReadAsync(Ct));
        return (Guid.Parse(row.GetString(0)), row.GetString(1));
    }

    /// <summary>Exercises the original delete/Undo ownership rule, which intentionally retains sender receipt bytes.</summary>
    [Fact]
    public async Task Original_delete_and_undo_propagate_logical_receipts_without_destroying_sender_recovery_bytes()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("fresh-approval-completes-with-the-other-unchanged-choice-preserved",
            await File.ReadAllLinesAsync(Path.Combine(root, "completed-related-review-stages.txt"), Ct));
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "undo-attachment-" + scenario + ".txt");
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, pin);
            using var a = new Client(Path.Combine(root, "client-a.sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-b.sqlite"), peer);
            var ownerA = await a.Transport.SignInAsync(Email, Password, Ct); var ownerB = await b.Transport.SignInAsync(Email, Password, Ct);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious receipt-undo A", Ct);
            var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious receipt-undo B", Ct);
            var remote = Assert.Single(await a.Transport.ProfilesAsync(deviceA.Id, Ct));
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, ownerA.Id, deviceA.Id, remote, Ct);
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, ownerB.Id, deviceB.Id, remote, Ct);
            var reconnected = await a.Coordinator.SynchronizeAsync(a.Transport, Ct);
            if (reconnected.NeedsReview)
            {
                // The retained pre-fix failure can have left a live service orphan. Retire only that
                // exact reviewed orphan through the normal protocol; never reset the files or hide money conflicts.
                var legacy = await a.Sync.ConflictsAsync(Ct); Assert.NotEmpty(legacy);
                Assert.All(legacy, row =>
                {
                    Assert.Equal("Attachments", row.Conflict.Proposal.Record.Table);
                    Assert.Equal("RelatedRecordMissing", row.Conflict.Reason);
                    Assert.Null(row.Conflict.Proposal.Values); Assert.NotNull(row.Conflict.Current?.Values);
                });
                foreach (var row in legacy) await a.Sync.ResolveAsync(row, keepLocalProposal: true, Ct);
                Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
                Checkpoint("previous-failed-case-service-orphan-explicitly-retired-with-files-preserved");
            }
            else Assert.True(reconnected.Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var account = Assert.Single(await a.Finances.GetAccountsAsync(cancellationToken: Ct));
            var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 999,
                Date = new(2026, 10, 15), Title = "Fictitious receipt Undo " + scenario };
            Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            var receipt = new EntryAttachment { EntryId = entry.Id, FileName = "fictitious-undo-" + scenario + ".pdf", ContentType = "application/pdf",
                Data = Encoding.UTF8.GetBytes("Fictitious exact receipt bytes retained for original Undo.") };
            await a.Finances.AddAttachmentAsync(receipt, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Equal(receipt.Data, (await b.Finances.GetAttachmentAsync(receipt.Id, Ct))!.Data);
            Checkpoint("original-receipt-published-to-both-clients");
            var deleted = await a.Finances.DeleteEntryAsync(entry.Id, Ct); Assert.Single(deleted);
            Assert.NotNull(await a.Finances.GetAttachmentAsync(receipt.Id, Ct));
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.DoesNotContain(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id);
            Assert.Null(await b.Finances.GetAttachmentAsync(receipt.Id, Ct));
            Assert.Equal(receipt.Data, (await a.Finances.GetAttachmentAsync(receipt.Id, Ct))!.Data);
            Checkpoint("logical-deletion-removes-remote-receipt-but-retains-sender-undo-bytes");
            await a.Finances.RestoreEntriesAsync(deleted, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Equal(999, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id).Amount);
            Assert.Equal(receipt.Id, Assert.Single(await b.Finances.GetAttachmentsAsync(entry.Id, Ct)).Id);
            Assert.Equal(receipt.Data, (await b.Finances.GetAttachmentAsync(receipt.Id, Ct))!.Data);
            Assert.Equal(0, (await a.Sync.StatusAsync(Ct)).PendingChanges); Assert.Equal(0, (await b.Sync.StatusAsync(Ct)).PendingChanges);
            Checkpoint("original-undo-republishes-one-entry-and-the-identical-retained-receipt");
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>A real committed feed retains its acknowledgement while explicit local review is refreshed without financial writes.</summary>
    [Fact]
    public async Task Committed_review_refresh_recalculates_later_edits_without_discarding_the_acknowledged_round()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("fresh-approval-completes-with-the-other-unchanged-choice-preserved",
            await File.ReadAllLinesAsync(Path.Combine(root, "completed-related-review-stages.txt"), Ct));
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "committed-review-" + scenario + ".txt");
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        Func<Task>? afterCommit = null;
        app.Use(async (http, next) =>
        {
            await next(http);
            if (http.Request.Path.Value!.EndsWith("/commit", StringComparison.Ordinal) && afterCommit is { } action)
            { afterCommit = null; await action(); }
        });
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, pin);
            using var a = new Client(Path.Combine(root, "client-a.sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-b.sqlite"), peer);
            var ownerA = await a.Transport.SignInAsync(Email, Password, Ct); var ownerB = await b.Transport.SignInAsync(Email, Password, Ct);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious committed-review A", Ct);
            var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious committed-review B", Ct);
            var remote = Assert.Single(await a.Transport.ProfilesAsync(deviceA.Id, Ct));
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, ownerA.Id, deviceA.Id, remote, Ct);
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, ownerB.Id, deviceB.Id, remote, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var account = Assert.Single(await a.Finances.GetAccountsAsync(cancellationToken: Ct));
            var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1000,
                Date = new(2026, 10, 15), Title = "Fictitious committed review " + scenario };
            Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            entry.Amount = 1300; Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            var local = Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id);
            afterCommit = async () => { local.Amount = 1400; Assert.True((await b.Finances.SaveEntryAsync(local, Ct)).Succeeded); };
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var old = Assert.Single(await b.Sync.ConflictsAsync(Ct)); Assert.Equal(2, old.Phase);
            local.Amount = 1500; Assert.True((await b.Finances.SaveEntryAsync(local, Ct)).Succeeded);
            await Assert.ThrowsAsync<InvalidOperationException>(() => b.Sync.ResolveAsync(old, false, Ct));
            var before = await b.Sync.StatusAsync(Ct);
            Assert.True(await b.Coordinator.RestartReviewAsync(b.Transport, Ct));
            var fresh = Assert.Single(await b.Sync.ConflictsAsync(Ct));
            Assert.Equal(old.Operation, fresh.Operation); Assert.Equal(2, fresh.Phase);
            Assert.Equal(1500, fresh.Conflict.Proposal.Values!.Value.GetProperty("Amount").GetInt64());
            Assert.True(fresh.LocalSequence > old.LocalSequence);
            Assert.Equal(1500, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id).Amount);
            Assert.Equal(before.PendingChanges, (await b.Sync.StatusAsync(Ct)).PendingChanges);
            Checkpoint("committed-review-refreshed-with-same-operation-and-current-money-without-financial-write");
            await b.Sync.ResolveAsync(fresh, false, Ct);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Equal(1300, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id).Amount);
            Assert.Equal(0, (await b.Sync.StatusAsync(Ct)).PendingChanges);
            Checkpoint("fresh-explicit-choice-applies-original-acknowledgement-and-clears-queue");
            entry.Amount = 1600; Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            afterCommit = async () => { local.Amount = 1700; Assert.True((await b.Finances.SaveEntryAsync(local, Ct)).Succeeded); };
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            local.Amount = 1600; Assert.True((await b.Finances.SaveEntryAsync(local, Ct)).Succeeded);
            before = await b.Sync.StatusAsync(Ct);
            Assert.True(await b.Coordinator.RestartReviewAsync(b.Transport, Ct));
            Assert.Empty(await b.Sync.ConflictsAsync(Ct));
            Assert.Equal(before.PendingChanges, (await b.Sync.StatusAsync(Ct)).PendingChanges);
            Assert.Equal(1600, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id).Amount);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Equal(0, (await b.Sync.StatusAsync(Ct)).PendingChanges);
            Checkpoint("edits-that-remove-conflict-still-retain-queue-until-explicit-sync");
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>An actual relationship rename invalidates prior consent even while the competing financial row is unchanged.</summary>
    [Fact]
    public async Task Authenticated_review_context_requires_fresh_consent_after_a_remote_account_rename()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("fresh-approval-completes-with-the-other-unchanged-choice-preserved",
            await File.ReadAllLinesAsync(Path.Combine(root, "completed-related-review-stages.txt"), Ct));
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "remote-context-" + scenario + ".txt");
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, pin);
            using var a = new Client(Path.Combine(root, "client-a.sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-b.sqlite"), peer);
            var ownerA = await a.Transport.SignInAsync(Email, Password, Ct); var ownerB = await b.Transport.SignInAsync(Email, Password, Ct);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious remote-context A", Ct);
            var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious remote-context B", Ct);
            var remote = Assert.Single(await a.Transport.ProfilesAsync(deviceA.Id, Ct));
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, ownerA.Id, deviceA.Id, remote, Ct);
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, ownerB.Id, deviceB.Id, remote, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var account = Assert.Single(await a.Finances.GetAccountsAsync(cancellationToken: Ct));
            var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1000,
                Date = new(2026, 10, 15), Title = "Fictitious reference consent " + scenario };
            Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var local = Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id);
            local.Amount = 2200; Assert.True((await b.Finances.SaveEntryAsync(local, Ct)).Succeeded);
            entry.Amount = 2100; Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var old = Assert.Single(await b.Sync.ConflictsAsync(Ct));
            var original = Assert.Single(old.Conflict.Context!, row => row.Record.Table == "Accounts");
            Assert.Equal(account.Name, original.Values!.Value.GetProperty("Name").GetString());
            await b.Sync.ResolveAsync(old, false, Ct);
            account.Name = "Fictitious remote renamed account " + scenario; await a.Finances.SaveAccountAsync(account, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var fresh = Assert.Single(await b.Sync.ConflictsAsync(Ct)); Assert.Equal("ReviewStale", fresh.Conflict.Reason);
            Assert.Equal(old.Conflict.Current!.Version, fresh.Conflict.Current!.Version);
            var updated = Assert.Single(fresh.Conflict.Context!, row => row.Record == original.Record);
            Assert.True(updated.Version > original.Version); Assert.Equal(account.Name, updated.Values!.Value.GetProperty("Name").GetString());
            Assert.Equal(2200, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id).Amount);
            Checkpoint("changed-authenticated-relationship-invalidates-choice-with-unchanged-entry-version");
            await b.Sync.ResolveAsync(fresh, false, Ct);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            Assert.Equal(2100, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct), row => row.Id == entry.Id).Amount);
            Assert.Equal(account.Name, Assert.Single(await b.Finances.GetAccountsAsync(cancellationToken: Ct)).Name);
            Assert.Equal(0, (await b.Sync.StatusAsync(Ct)).PendingChanges);
            Checkpoint("fresh-relationship-consent-applies-money-and-account-name-without-guessing");
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>A real revoked initial device cannot strand an unpublished space or reset an already published one.</summary>
    [Fact]
    public async Task Unpublished_initialization_moves_explicitly_after_device_loss_and_never_resets_published_data()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("fresh-approval-completes-with-the-other-unchanged-choice-preserved",
            await File.ReadAllLinesAsync(Path.Combine(root, "completed-related-review-stages.txt"), Ct));
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "initialization-recovery-" + scenario + ".txt");
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, pin);
            using var a = new Client(Path.Combine(root, "client-a.sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-b.sqlite"), peer);
            var ownerA = await a.Transport.SignInAsync(Email, Password, Ct); var ownerB = await b.Transport.SignInAsync(Email, Password, Ct);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious initialization-recovery A", Ct);
            var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious initialization-recovery B", Ct);
            var original = (await a.Transport.ProfilesAsync(deviceA.Id, Ct)).Single(row => !row.Initializing);
            var source = (await a.Transport.FeedAsync(original.Id, deviceA.Id, 0, original.Cursor, Ct)).Records.First(row => row.Record.Table == "Accounts" && row.Values is not null);
            var incomplete = await a.Transport.CreateProfileAsync(new(deviceA.Id, Guid.NewGuid(), "Fictitious interrupted initialization " + scenario, SyncSchema.Hash), Ct);
            Assert.True(incomplete.Initializing); Assert.Equal(deviceA.Id, incomplete.BootstrapDeviceId);
            var pending = await a.Transport.OpenAsync(incomplete.Id, new(deviceA.Id, Guid.NewGuid(), SyncSchema.Hash), Ct);
            var part = new SyncRoundPart(0, [new(source.Record, 0, source.Values)]);
            await a.Transport.PutPartAsync(incomplete.Id, deviceA.Id, pending.Id, part, Ct);
            await b.Transport.RevokeDeviceAsync(deviceA.Id, Ct);
            var claimed = await b.Transport.ClaimInitializationAsync(incomplete.Id, new(deviceB.Id, deviceA.Id), Ct);
            Assert.Equal(deviceB.Id, claimed.BootstrapDeviceId); Assert.True(claimed.Initializing); Assert.Equal(0, claimed.Cursor);
            await using (var verification = app.Services.CreateAsyncScope())
            {
                var db = verification.ServiceProvider.GetRequiredService<ServiceDbContext>();
                Assert.True((await db.Set<Vafadar.Zanance.Api.Sync.SyncRoundRow>().AsNoTracking().SingleAsync(row => row.Id == pending.Id, Ct)).Abandoned);
                Assert.Equal(1, await db.Set<Vafadar.Zanance.Api.Sync.SyncPartRow>().CountAsync(row => row.RoundId == pending.Id, Ct));
            }
            Assert.Equal(claimed, await b.Transport.ClaimInitializationAsync(incomplete.Id, new(deviceB.Id, deviceA.Id), Ct));
            var wrongDevice = await Assert.ThrowsAsync<PersonalSyncHttpException>(() => b.Transport.ClaimInitializationAsync(incomplete.Id, new(deviceA.Id, deviceB.Id), Ct));
            Assert.Equal(HttpStatusCode.NotFound, wrongDevice.Status);
            var oldSession = await Assert.ThrowsAsync<PersonalSyncHttpException>(() => a.Transport.OpenAsync(incomplete.Id, new(deviceA.Id, Guid.NewGuid(), SyncSchema.Hash), Ct));
            Assert.Equal(HttpStatusCode.Unauthorized, oldSession.Status);
            Checkpoint("explicit-new-device-claim-abandons-old-staging-after-actual-revocation-without-erasing-parts");
            var fresh = await b.Transport.OpenAsync(incomplete.Id, new(deviceB.Id, Guid.NewGuid(), SyncSchema.Hash), Ct);
            await b.Transport.PutPartAsync(incomplete.Id, deviceB.Id, fresh.Id, part, Ct);
            var committed = await b.Transport.CommitAsync(incomplete.Id, deviceB.Id, fresh.Id,
                new(1, 1, SyncProtocolDigest.Round([SyncProtocolDigest.Part(part)]), []), Ct);
            Assert.True(committed.Committed);
            var prohibited = await Assert.ThrowsAsync<PersonalSyncHttpException>(() => b.Transport.ClaimInitializationAsync(incomplete.Id, new(deviceB.Id, deviceB.Id), Ct));
            Assert.Equal(HttpStatusCode.Conflict, prohibited.Status);
            var readback = await b.Transport.FeedAsync(incomplete.Id, deviceB.Id, 0, committed.Cursor, Ct);
            Assert.True(JsonElement.DeepEquals(source.Values!.Value, Assert.Single(readback.Records).Values!.Value));
            Assert.False((await b.Transport.ProfilesAsync(deviceB.Id, Ct)).Single(row => row.Id == incomplete.Id).Initializing);
            Checkpoint("new-source-publishes-exact-financial-record-and-published-space-refuses-initialization-reset");
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>A fresh original installation receives the existing financial graph and finishes first run without a manufactured account.</summary>
    [Fact]
    public async Task Fresh_install_receives_existing_accounts_and_completes_first_run_without_an_extra_account()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("fresh-approval-completes-with-the-other-unchanged-choice-preserved",
            await File.ReadAllLinesAsync(Path.Combine(root, "completed-related-review-stages.txt"), Ct));
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "first-run-receive-" + scenario + ".txt");
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, pin);
            var clientPath = Path.Combine(root, "client-first-run-" + scenario + ".sqlite"); Assert.False(File.Exists(clientPath));
            using var client = new Client(clientPath, peer);
            var initialSettings = await client.Finances.GetSettingsAsync(Ct); Assert.False(initialSettings.OnboardingCompleted);
            Assert.Empty(await client.Finances.GetAccountsAsync(cancellationToken: Ct));
            var owner = await client.Transport.SignInAsync(Email, Password, Ct);
            var device = await client.Transport.RegisterDeviceAsync("Fictitious fresh first-run installation", Ct);
            var remote = (await client.Transport.ProfilesAsync(device.Id, Ct)).Single(row => row.Name == "Fictitious shared personal copy");
            await client.Sync.ConnectAsync(await client.Sync.ReviewAsync(Ct), peer, owner.Id, device.Id, remote, Ct);
            var received = await client.Coordinator.SynchronizeAsync(client.Transport, Ct);
            Assert.True(received.Applied); Assert.False(received.NeedsReview);
            var accounts = await client.Finances.GetAccountsAsync(cancellationToken: Ct); Assert.NotEmpty(accounts);
            Checkpoint("fresh-original-client-receives-existing-account-graph-from-authenticated-service");
            var review = await client.Sync.ReviewAsync(Ct); Assert.True(await client.Sync.CanCompleteOnboardingAsync(review, Ct));
            var before = JsonSerializer.Serialize(accounts);
            Assert.True(await client.Sync.CompleteOnboardingAsync(review, Ct));
            Assert.True((await client.Finances.GetSettingsAsync(Ct)).OnboardingCompleted);
            Assert.Equal(before, JsonSerializer.Serialize(await client.Finances.GetAccountsAsync(cancellationToken: Ct)));
            Checkpoint("first-run-completion-preserves-all-received-accounts-without-a-new-financial-account");
        }
        finally { await app.StopAsync(Ct); }
    }
    /// <summary>A fresh original installation receives the existing financial graph and finishes first run without a manufactured account.</summary>
    [Fact]
    public async Task First_run_reception_keeps_nondefault_service_preferences_instead_of_proposing_wizard_defaults()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("fresh-approval-completes-with-the-other-unchanged-choice-preserved",
            await File.ReadAllLinesAsync(Path.Combine(root, "completed-related-review-stages.txt"), Ct));
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "first-run-preferences-" + scenario + ".txt");
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, pin);
            using (var source = new Client(Path.Combine(root, "client-a.sqlite"), peer))
            {
                var sourceOwner = await source.Transport.SignInAsync(Email, Password, Ct);
                var sourceDevice = await source.Transport.RegisterDeviceAsync("Fictitious saved-preferences source", Ct);
                var sourceRemote = (await source.Transport.ProfilesAsync(sourceDevice.Id, Ct)).Single(row => row.Name == "Fictitious shared personal copy");
                await source.Sync.ConnectAsync(await source.Sync.ReviewAsync(Ct), peer, sourceOwner.Id, sourceDevice.Id, sourceRemote, Ct);
                Assert.True((await source.Coordinator.SynchronizeAsync(source.Transport, Ct)).Applied);
                var saved = await source.Finances.GetSettingsAsync(Ct); saved.ReportCurrencyCode = "JPY"; saved.DefaultCurrencyCode = "KWD";
                saved.Mode = Vafadar.Zanance.Core.Settings.ExperienceMode.Advanced; await source.Finances.SaveSettingsAsync(saved, Ct);
                Assert.True((await source.Coordinator.SynchronizeAsync(source.Transport, Ct)).Applied);
            }
            var clientPath = Path.Combine(root, "client-first-run-preferences-" + scenario + ".sqlite"); Assert.False(File.Exists(clientPath));
            using var client = new Client(clientPath, peer);
            var initialSettings = await client.Finances.GetSettingsAsync(Ct); Assert.False(initialSettings.OnboardingCompleted);
            Assert.Empty(await client.Finances.GetAccountsAsync(cancellationToken: Ct));
            var owner = await client.Transport.SignInAsync(Email, Password, Ct);
            var device = await client.Transport.RegisterDeviceAsync("Fictitious fresh first-run installation", Ct);
            var remote = (await client.Transport.ProfilesAsync(device.Id, Ct)).Single(row => row.Name == "Fictitious shared personal copy");
            await client.Sync.ConnectAsync(await client.Sync.ReviewAsync(Ct), peer, owner.Id, device.Id, remote, Ct, receiveFirstRun: true);
            var received = await client.Coordinator.SynchronizeAsync(client.Transport, Ct);
            Assert.True(received.Applied); Assert.False(received.NeedsReview);
            var accounts = await client.Finances.GetAccountsAsync(cancellationToken: Ct); Assert.NotEmpty(accounts);
            Checkpoint("fresh-original-client-receives-existing-account-graph-from-authenticated-service");
            var review = await client.Sync.ReviewAsync(Ct); Assert.True(await client.Sync.CanCompleteOnboardingAsync(review, Ct));
            var before = JsonSerializer.Serialize(accounts);
            Assert.True(await client.Sync.CompleteOnboardingAsync(review, Ct));
            var adopted = await client.Finances.GetSettingsAsync(Ct); Assert.True(adopted.OnboardingCompleted);
            Assert.Equal("JPY", adopted.ReportCurrencyCode); Assert.Equal("KWD", adopted.DefaultCurrencyCode);
            Assert.Equal(Vafadar.Zanance.Core.Settings.ExperienceMode.Advanced, adopted.Mode);
            Assert.Equal(before, JsonSerializer.Serialize(await client.Finances.GetAccountsAsync(cancellationToken: Ct)));
            Checkpoint("first-run-completion-preserves-all-received-accounts-without-a-new-financial-account");
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>Whole original holding writes must not become a mixed financial group through separate conflict decisions.</summary>
    [Fact]
    public async Task Linked_holding_group_requires_one_consistent_choice_for_event_payment_and_fee()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        Assert.Contains("fresh-approval-completes-with-the-other-unchanged-choice-preserved",
            await File.ReadAllLinesAsync(Path.Combine(root, "completed-related-review-stages.txt"), Ct));
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "linked-group-" + scenario + ".txt");
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = runtime, ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        Func<Task>? afterCommit = null;
        app.Use(async (http, next) =>
        {
            await next(http);
            if (http.Request.Path.Value!.EndsWith("/commit", StringComparison.Ordinal) && afterCommit is { } action)
            { afterCommit = null; await action(); }
        });
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, pin);
            using var a = new Client(Path.Combine(root, "client-group-a-" + scenario + ".sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-group-b-" + scenario + ".sqlite"), peer);
            var ownerA = await a.Transport.SignInAsync(Email, Password, Ct); var ownerB = await b.Transport.SignInAsync(Email, Password, Ct);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious group A", Ct); var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious group B", Ct);
            var remote = await a.Transport.CreateProfileAsync(new(deviceA.Id, Guid.NewGuid(), "Fictitious isolated group " + scenario, SyncSchema.Hash), Ct);
            await a.Finances.GetSettingsAsync(Ct); await b.Finances.GetSettingsAsync(Ct);
            var account = new Account { Name = "Fictitious group cash", CurrencyCode = "EUR", OpeningBalance = 100000, OpeningDate = new(2026, 10, 1) };
            await a.Finances.SaveAccountAsync(account, Ct);
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, ownerA.Id, deviceA.Id, remote, Ct);
            var ha = a.Services.GetRequiredService<HoldingStore>(); var hb = b.Services.GetRequiredService<HoldingStore>();
            var type = new AssetType { Name = "Fictitious grouped gold", Dimension = AssetDimension.Mass, PriceCurrencyCode = "EUR" };
            Assert.True(await ha.SaveTypeAsync(type, Ct)); var location = new AssetLocation { Name = "Fictitious group safe" }; await ha.SaveLocationAsync(location, Ct);
            var purchase = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Purchase,
                Date = new(2026, 10, 15), Quantity = 1000, BasisAmount = 1000 };
            var payment = new LedgerEntry { AccountId = account.Id, Date = purchase.Date, Kind = EntryKind.AssetPurchase, Amount = 1000 };
            var fee = new LedgerEntry { AccountId = account.Id, Date = purchase.Date, Kind = EntryKind.Expense, Amount = 10, CategoryId = await ha.FeesCategoryAsync(Ct) };
            Assert.True((await ha.SaveEventAsync(purchase, [payment, fee], Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            remote = (await b.Transport.ProfilesAsync(deviceB.Id, Ct)).Single(row => row.Id == remote.Id);
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, ownerB.Id, deviceB.Id, remote, Ct, receiveFirstRun: true);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var local = Assert.Single(await hb.GetEventsAsync(type.Id, Ct));
            var localEntries = (await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Where(row => row.GroupId == local.GroupId).ToArray();
            purchase.Quantity = 2000; purchase.BasisAmount = 2000; payment.Amount = 2000;
            Assert.True((await ha.SaveEventAsync(purchase, [payment, fee], Ct)).Succeeded);
            local.BasisAmount = 3000; localEntries.Single(row => row.Kind == EntryKind.AssetPurchase).Amount = 3000;
            Assert.True((await hb.SaveEventAsync(local, localEntries, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var reviews = await b.Sync.ConflictsAsync(Ct); Assert.NotEmpty(reviews);
            Assert.Equal(3, reviews.Count); Assert.All(reviews, review => Assert.Equal(local.GroupId, review.Conflict.LinkedGroup));
            var round = await ReadRoundReceiptAsync(b);
            var handle = await b.Transport.OpenAsync(remote.Id, new(deviceB.Id, round.Operation, SyncSchema.Hash), Ct);
            var choices = reviews.Select(review => new SyncResolution(review.Conflict.Proposal.Record, review.Conflict.Current?.Version ?? 0,
                review.Conflict.Proposal.Record.Table == "AssetEvents", review.Conflict.RelatedRecord?.Record, review.Conflict.RelatedRecord?.Version,
                review.Conflict.Context?.Select(row => new SyncRecordVersion(row.Record, row.Version)).ToArray())).ToArray();
            var mixed = await b.Transport.CommitAsync(remote.Id, deviceB.Id, handle.Id,
                new(handle.Parts, checked((int)handle.Rows), round.Hash, choices), Ct);
            Assert.False(mixed.Committed); Assert.Equal(3, mixed.Conflicts.Count);
            Assert.All(mixed.Conflicts, conflict => Assert.Equal("LinkedGroupReview", conflict.Reason));
            Checkpoint("mixed-row-choices-cannot-publish-an-inconsistent-holding-group");
            await Assert.ThrowsAsync<InvalidOperationException>(() => b.Sync.ResolveAsync(reviews[0] with { GroupMembers = null }, true, Ct));
            await b.Sync.ResolveAsync(reviews[0], true, Ct);
            Assert.Empty(await b.Sync.ConflictsAsync(Ct));
            var applied = await b.Coordinator.SynchronizeAsync(b.Transport, Ct); Assert.True(applied.Applied); Assert.False(applied.NeedsReview);
            var chosen = Assert.Single(await hb.GetEventsAsync(type.Id, Ct));
            Assert.Equal(1000, chosen.Quantity); Assert.Equal(3000, chosen.BasisAmount);
            var chosenEntries = (await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Where(row => row.GroupId == chosen.GroupId).ToArray();
            Assert.Equal(2, chosenEntries.Length); Assert.Equal(3000, Assert.Single(chosenEntries, row => row.Kind == EntryKind.AssetPurchase).Amount);
            Assert.Equal(10, Assert.Single(chosenEntries, row => row.Kind == EntryKind.Expense).Amount);
            Checkpoint("one-complete-local-group-choice-preserves-quantity-payment-fee-without-duplicates");
            // Exercise the other side of acknowledgement: original B writes occur after its upload commits.
            purchase = Assert.Single(await ha.GetEventsAsync(type.Id, Ct));
            var aEntries = (await a.Finances.GetEntriesAsync(cancellationToken: Ct)).Where(row => row.GroupId == purchase.GroupId).ToArray();
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            purchase = Assert.Single(await ha.GetEventsAsync(type.Id, Ct));
            aEntries = (await a.Finances.GetEntriesAsync(cancellationToken: Ct)).Where(row => row.GroupId == purchase.GroupId).ToArray();
            purchase.Quantity = 4000; purchase.BasisAmount = 4000; aEntries.Single(row => row.Kind == EntryKind.AssetPurchase).Amount = 4000;
            Assert.True((await ha.SaveEventAsync(purchase, aEntries, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            chosen.BasisAmount = 5000; chosenEntries.Single(row => row.Kind == EntryKind.AssetPurchase).Amount = 5000;
            afterCommit = async () => Assert.True((await hb.SaveEventAsync(chosen, chosenEntries, Ct)).Succeeded);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var receivedGroup = await b.Sync.ConflictsAsync(Ct); Assert.Equal(3, receivedGroup.Count);
            Assert.All(receivedGroup, row => Assert.Equal(2, row.Phase));
            await b.Sync.ResolveAsync(receivedGroup[0], false, Ct);
            // A later fee edit invalidates all three approved members, without consuming the acknowledgement.
            chosenEntries.Single(row => row.Kind == EntryKind.Expense).Amount = 20;
            Assert.True((await hb.SaveEventAsync(chosen, chosenEntries, Ct)).Succeeded);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var refreshed = await b.Sync.ConflictsAsync(Ct); Assert.Equal(3, refreshed.Count);
            Assert.All(refreshed, row => Assert.Equal(receivedGroup[0].Operation, row.Operation));
            Assert.Equal(5000, Assert.Single(await hb.GetEventsAsync(type.Id, Ct)).BasisAmount);
            await b.Sync.ResolveAsync(refreshed[0], false, Ct);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var final = Assert.Single(await hb.GetEventsAsync(type.Id, Ct));
            Assert.Equal(4000, final.Quantity); Assert.Equal(4000, final.BasisAmount);
            var finalEntries = (await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Where(row => row.GroupId == final.GroupId).ToArray();
            Assert.Equal(2, finalEntries.Length); Assert.Equal(4000, Assert.Single(finalEntries, row => row.Kind == EntryKind.AssetPurchase).Amount);
            Assert.Equal(10, Assert.Single(finalEntries, row => row.Kind == EntryKind.Expense).Amount);
            Checkpoint("post-acknowledgement-fee-edit-invalidates-whole-group-and-fresh-remote-choice-applies-exactly");
            purchase.Quantity = 6000; purchase.BasisAmount = 6000; aEntries.Single(row => row.Kind == EntryKind.AssetPurchase).Amount = 6000;
            Assert.True((await ha.SaveEventAsync(purchase, aEntries, Ct)).Succeeded);
            finalEntries.Single(row => row.Kind == EntryKind.Expense).Amount = 30;
            Assert.True((await hb.SaveEventAsync(final, finalEntries, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var disjoint = await b.Sync.ConflictsAsync(Ct); Assert.Equal(3, disjoint.Count);
            await b.Sync.ResolveAsync(disjoint[0], false, Ct);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            final = Assert.Single(await hb.GetEventsAsync(type.Id, Ct)); Assert.Equal(6000, final.BasisAmount);
            Checkpoint("disjoint-quantity-payment-and-fee-edits-require-one-group-choice-instead-of-row-merging");
            var extraFee = new LedgerEntry { AccountId = account.Id, Date = purchase.Date, Kind = EntryKind.Expense, Amount = 25,
                CategoryId = await ha.FeesCategoryAsync(Ct) };
            Assert.True((await ha.SaveEventAsync(purchase, aEntries.Append(extraFee).ToArray(), Ct)).Succeeded);
            finalEntries = (await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Where(row => row.GroupId == final.GroupId).ToArray();
            final.BasisAmount = 7000; finalEntries.Single(row => row.Kind == EntryKind.AssetPurchase).Amount = 7000;
            Assert.True((await hb.SaveEventAsync(final, finalEntries, Ct)).Succeeded);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var extra = await b.Sync.ConflictsAsync(Ct); Assert.Equal(3, extra.Count);
            Assert.Contains(extra[0].Conflict.Context!, row => row.Record.Table == "Entries" && row.Values?.GetProperty("Id").GetGuid() == extraFee.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => b.Sync.ResolveAsync(extra[0], true, Ct));
            await b.Sync.ResolveAsync(extra[0], false, Ct);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            final = Assert.Single(await hb.GetEventsAsync(type.Id, Ct)); Assert.Equal(6000, final.BasisAmount);
            finalEntries = (await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Where(row => row.GroupId == final.GroupId).ToArray();
            Assert.Equal(3, finalEntries.Length); Assert.Equal(25, Assert.Single(finalEntries, row => row.Id == extraFee.Id).Amount);
            Checkpoint("new-remote-group-member-is-explicit-context-and-refuses-unreviewed-local-hybrid");
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>Preview receives exact authenticated content before any conflict decision or financial publication.</summary>
    [Fact]
    public async Task Attachment_review_downloads_exact_versions_before_commit_and_refuses_corrupt_or_stale_content()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("The isolated sync database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained isolated sync fixture is required.");
        var configured = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", configured.Database ?? ""); Assert.Contains(configured.Host, new[] { "127.0.0.1", "localhost" });
        Assert.True(Path.IsPathFullyQualified(root));
        Assert.Equal("Zanance fictional personal sync acceptance v1", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "attachment-review-" + scenario + ".txt");
        void Checkpoint(string value) => File.AppendAllText(proof, value + Environment.NewLine);
        var pin = await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Service"] = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString,
            ["Service:Root"] = Path.Combine(root, "service") });
        builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single(); var peer = new LocalSyncPeer(address, pin);
            using var a = new Client(Path.Combine(root, "client-attachment-a-" + scenario + ".sqlite"), peer);
            using var b = new Client(Path.Combine(root, "client-attachment-b-" + scenario + ".sqlite"), peer);
            var ownerA = await a.Transport.SignInAsync(Email, Password, Ct); var ownerB = await b.Transport.SignInAsync(Email, Password, Ct);
            var deviceA = await a.Transport.RegisterDeviceAsync("Fictitious attachment A", Ct); var deviceB = await b.Transport.RegisterDeviceAsync("Fictitious attachment B", Ct);
            var profile = await a.Transport.CreateProfileAsync(new(deviceA.Id, Guid.NewGuid(), "Fictitious attachment review " + scenario, SyncSchema.Hash), Ct);
            await a.Finances.GetSettingsAsync(Ct); await b.Finances.GetSettingsAsync(Ct);
            var account = new Account { Name = "Fictitious attachment cash", CurrencyCode = "KWD", OpeningDate = new(2026, 10, 1) };
            await a.Finances.SaveAccountAsync(account, Ct);
            var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1250, Date = new(2026, 10, 15) };
            Assert.True((await a.Finances.SaveEntryAsync(entry, Ct)).Succeeded);
            var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "fictitious.pdf", ContentType = "application/pdf",
                Data = Encoding.UTF8.GetBytes("Fictitious original binary content; no PDF rendering is claimed.") };
            await a.Finances.AddAttachmentAsync(attachment, Ct);
            await a.Sync.ConnectAsync(await a.Sync.ReviewAsync(Ct), peer, ownerA.Id, deviceA.Id, profile, Ct);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            profile = (await b.Transport.ProfilesAsync(deviceB.Id, Ct)).Single(row => row.Id == profile.Id);
            await b.Sync.ConnectAsync(await b.Sync.ReviewAsync(Ct), peer, ownerB.Id, deviceB.Id, profile, Ct, receiveFirstRun: true);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).Applied);
            var remoteBytes = Encoding.UTF8.GetBytes("Fictitious changed remote binary content, exact bytes A.");
            var localBytes = Encoding.UTF8.GetBytes("Fictitious changed local binary content, exact bytes B.");
            async Task MutateOriginalModel(Client client, byte[] bytes, string? name = null)
            {
                // Valid restored model values exercise original EF persistence and capture triggers, not an invented file algorithm.
                await using var db = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
                var row = await db.Attachments.SingleAsync(row => row.Id == attachment.Id, Ct); row.Data = bytes;
                if (name is not null) row.FileName = name;
                await db.SaveChangesAsync(Ct);
            }
            await MutateOriginalModel(a, remoteBytes); await MutateOriginalModel(b, localBytes);
            Assert.True((await a.Coordinator.SynchronizeAsync(a.Transport, Ct)).Applied);
            Assert.True((await b.Coordinator.SynchronizeAsync(b.Transport, Ct)).NeedsReview);
            var review = Assert.Single((await b.Sync.PrepareReviewsAsync(await b.Sync.ConflictsAsync(Ct), Ct)).Reviews);
            Assert.Equal("Attachments", review.Conflict.Proposal.Record.Table); Assert.Equal(1, review.Phase);
            using (var local = await b.Sync.ReadAttachmentReviewAsync(review, true, Ct)) Assert.Equal(localBytes, local.Data);
            var remote = await b.Sync.ReadAttachmentReviewAsync(review, false, Ct); Assert.Equal(remoteBytes, remote.Data);
            remote.Dispose(); Assert.All(remote.Data, value => Assert.Equal(0, value));
            Assert.Equal(localBytes, (await b.Finances.GetAttachmentAsync(attachment.Id, Ct))!.Data);
            Assert.Equal(1250, Assert.Single(await b.Finances.GetEntriesAsync(cancellationToken: Ct)).Amount);
            Assert.Single(await b.Sync.ConflictsAsync(Ct));
            Checkpoint("authenticated-phase-one-preview-has-both-exact-versions-without-choice-or-financial-write");
            var tooLarge = System.Text.Json.Nodes.JsonNode.Parse(review.Conflict.Proposal.Values!.Value.GetRawText())!.AsObject();
            tooLarge["Data"]!["length"] = EntryAttachment.MaxBytes + 1;
            Assert.Contains(SyncGraphValidation.ValidateRow(SyncSchema.Manifest.Tables.Single(table => table.Name == "Attachments"), review.Conflict.Proposal.Record, JsonSerializer.SerializeToElement(tooLarge)), issue => issue.Reason == "InvalidAttachmentSize");
            tooLarge["Data"]!["length"] = 0;
            Assert.Contains(SyncGraphValidation.ValidateRow(SyncSchema.Manifest.Tables.Single(table => table.Name == "Attachments"), review.Conflict.Proposal.Record, JsonSerializer.SerializeToElement(tooLarge)), issue => issue.Reason == "InvalidAttachmentSize");
            var hash = review.Conflict.Current!.Values!.Value.GetProperty("Data").GetProperty("sha256").GetString()!;
            await using (var db = await b.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SyncBlobParts SET Data={new byte[] { 1, 2, 3 }} WHERE Hash={hash} AND Part=0", Ct);
            await Assert.ThrowsAsync<InvalidDataException>(() => b.Sync.ReadAttachmentReviewAsync(review, false, Ct));
            Checkpoint("tampered-private-staging-is-refused-with-original-live-attachment-preserved");
            await MutateOriginalModel(b, localBytes, "fictitious-later-name.pdf");
            await Assert.ThrowsAsync<InvalidOperationException>(() => b.Sync.ReadAttachmentReviewAsync(review, true, Ct));
            Assert.Equal(localBytes, (await b.Finances.GetAttachmentAsync(attachment.Id, Ct))!.Data);
            Checkpoint("later-original-attachment-edit-invalidates-preview-and-retains-pending-review");
        }
        finally { await app.StopAsync(Ct); }
    }
}
