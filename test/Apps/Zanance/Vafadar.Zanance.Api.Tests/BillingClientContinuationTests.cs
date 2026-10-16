using System.Security.Claims;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Vafadar.Zanance.Data.Sync;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vafadar.Cryptography;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Actual signed private client and original service continuation against the retained isolated PostgreSQL fixture.</summary>
[Trait("AT", "AT-295")]
[Trait("Qualification", "BillingLocal")]
public sealed partial class BillingClientContinuationTests
{
    /// <summary>Lost acknowledgements, refund replay, native clock loss and never-arrived expired requests preserve original authority.</summary>
    [Fact]
    public async Task Private_client_preserves_original_receipts_and_refuses_stale_paid_authority()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        Assert.True(File.Exists(Path.Combine(parent, "complete.json")), "Original nine service stages must already be complete.");
        var root = Path.Combine(parent, "private-client"); Directory.CreateDirectory(root);
        var done = Path.Combine(root, "complete.json"); Assert.False(File.Exists(done), "Completed client qualification must not be replayed.");
        var phaseFile = Path.Combine(root, "phase.txt"); var phase = File.Exists(phaseFile) ? int.Parse(await File.ReadAllTextAsync(phaseFile, ct), System.Globalization.CultureInfo.InvariantCulture) : 0;
        var factory = new Factory(connection);
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "protection")));
        var serviceProtection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        var authority = new BillingAuthority(new ServiceStorage { Root = parent, LocalMailbox = true }, serviceProtection, factory);
        var clock = new Clock(); var native = new NativeAccess();
        var provider = new Provider(); var providers = new IBillingProvider[] { provider };
        var registry = new BillingRegistry(factory, authority, new BillingCatalog(new ConfigurationBuilder().Build()), serviceProtection, clock);
        var coordinator = new BillingPurchaseCoordinator(factory, authority, registry, providers, serviceProtection, clock);
        var commands = new BillingCommandStore(factory, authority, registry, coordinator, serviceProtection, clock);
        var transport = new Transport(registry, commands, authority, clock, factory);
        var storage = new Storage(root, protection.CreateProtector("fictional-client-state-v1"));
        var client = NewClient();
        if (phase < 1)
        {
            var fresh = await client.ObserveAsync(native, ct); Assert.Equal(ProductPlan.Free, fresh.Decision.Plan);
            var intent = await client.BeginNativeAsync(native, BillingProduct.PlusMonthly, ct);
            await File.WriteAllTextAsync(Path.Combine(root, "native-intent.txt"), intent.ToString("D"), ct); await Advance(1);
        }
        var originalIntent = Guid.ParseExact(await File.ReadAllTextAsync(Path.Combine(root, "native-intent.txt"), ct), "D");
        var initial = await client.ReadAsync(native, false, ct);
        provider.Binding = BillingAccountBinding.Google(initial.Snapshot!.AccountToken, false);
        if (phase < 2)
        {
            transport.LoseNextVerificationReply = true;
            await Assert.ThrowsAsync<IOException>(() => client.VerifyAsync(native, "fictional-private-client-reference", BillingProduct.PlusMonthly, false, ct));
            Assert.True((await client.ReadAsync(native, false, ct)).HasPendingCommand); await Advance(2);
        }
        if (phase < 3)
        {
            client = NewClient(); var before = provider.Queries;
            var reply = await client.ContinueAsync(native, ct); Assert.NotNull(reply); Assert.True(reply.MayFinish); Assert.Equal(before, provider.Queries);
            Assert.NotNull(reply.SignedReceipt);
            Assert.Equal(originalIntent, (await client.ReadAsync(native, false, ct)).NativePaymentId); await Advance(3);
        }
        if (phase < 4)
        {
            var view = await client.ReadAsync(native, false, ct);
            if (view.NativePaymentId is not null)
            {
                var receipt = await client.LastCompletionAsync(native, originalIntent, ct); Assert.NotNull(receipt); Assert.True(receipt.MayFinish);
                await client.FinishNativeAsync(native, originalIntent, ct);
            }
            var paid = await client.ObserveAsync(native, ct); Assert.Equal(ProductPlan.Plus, paid.Decision.Plan);
            Assert.False(paid.HasPendingCommand); Assert.False(paid.HasUnresolvedNativePayment);
            await storage.WriteProofAsync("old-paid-observation", transport.LastCurrent!.SignedSnapshot, ct);
            native.Boot = "different-native-boot";
            var rebooted = await NewClient().ReadAsync(native, false, ct);
            Assert.NotNull(rebooted.Snapshot); Assert.True(rebooted.Decision.NeedsVerification);
            Assert.Equal(BillingPurchaseState.Active, Assert.Single(rebooted.Snapshot.Grants).Facts.State);
            native.Boot = "fictional-native-boot"; await Advance(4);
        }
        if (phase < 5)
        {
            provider.State = BillingPurchaseState.Revoked;
            var notifications = new BillingNotifications(factory, serviceProtection, new ConfigurationBuilder().Build(), clock);
            await notifications.ScheduleAsync(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, "fictional-private-client-refund", "fictional-private-client-reference", ct);
            var worker = new BillingReconciliation(factory, providers, serviceProtection, clock);
            Assert.True(await worker.RunOneAsync(ct));
            var current = await client.ObserveAsync(native, ct);
            Assert.Equal(ProductPlan.Free, current.Decision.Plan);
            Assert.Equal(BillingPurchaseState.Revoked, Assert.Single(current.Snapshot!.Grants).Facts.State); await Advance(5);
        }
        if (phase < 6)
        {
            transport.ReplayCurrent = await storage.ReadProofAsync("old-paid-observation", ct);
            await Assert.ThrowsAsync<ArgumentException>(() => client.ObserveAsync(native, ct)); transport.ReplayCurrent = null;
            var retained = await client.ReadAsync(native, false, ct);
            Assert.Equal(BillingPurchaseState.Revoked, Assert.Single(retained.Snapshot!.Grants).Facts.State);
            Assert.Equal(ProductPlan.Free, retained.Decision.Plan); await Advance(6);
        }
        if (phase < 7)
        {
            transport.ChangeIdentityDuringTime = true;
            await Assert.ThrowsAsync<InvalidDataException>(() => client.ObserveAsync(native, ct));
            transport.User = null; transport.ChangeIdentityDuringTime = false;
            Assert.Equal(ProductPlan.Free, (await client.ReadAsync(native, false, ct)).Decision.Plan); await Advance(7);
        }
        if (phase < 8)
        {
            await client.ObserveAsync(native, ct);
            var view = await client.ReadAsync(native, false, ct);
            var intent = view.NativePaymentId ?? await client.BeginNativeAsync(native, BillingProduct.PlusMonthly, ct);
            var before = provider.Queries;
            transport.ExpireBeforeService = true;
            var expired = await client.VerifyAsync(native, "fictional-request-never-arrived-in-time", BillingProduct.PlusMonthly, false, ct);
            Assert.False(expired.MayFinish); Assert.Null(expired.State); Assert.NotNull(expired.SignedReceipt); Assert.Equal(before, provider.Queries);
            Assert.False((await client.ReadAsync(native, false, ct)).HasPendingCommand);
            Assert.Equal(intent, (await client.ReadAsync(native, false, ct)).NativePaymentId);
            await Assert.ThrowsAsync<InvalidDataException>(() => client.CancelNativeAsync(native, intent, ct));
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.BeginNativeAsync(native, BillingProduct.PlusMonthly, ct));
            await Advance(8);
        }
        await File.WriteAllTextAsync(done, "{\"Completed\":true,\"Stages\":8,\"ActualStoreSandbox\":false}", ct);
        BillingClient NewClient() => new(transport, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        async Task Advance(int next) { await File.WriteAllTextAsync(phaseFile, next.ToString(System.Globalization.CultureInfo.InvariantCulture), ct); phase = next; }
    }

    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    {
        public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
        public Task<ServiceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
    private sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2026, 10, 16, 19, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class NativeAccess : IBillingNativeAccess
    {
        internal string Boot { get; set; } = "fictional-native-boot";
        public void Validate() { }
        public BillingMonotonicInstant ReadMonotonic() => new(Boot, 1000);
    }
    /// <summary>Only a new receipt for the original product may resolve the retained native intent, including after process loss.</summary>
    [Fact]
    public async Task Older_owned_or_substituted_receipts_cannot_finish_a_new_native_payment()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "native-receipt-binding"); Directory.CreateDirectory(root);
        Assert.False(File.Exists(Path.Combine(root, "complete.json")), "Do not replay successful receipt qualification.");
        var factory = new Factory(connection); var clock = new Clock(); var native = new NativeAccess();
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        var authority = new BillingAuthority(new ServiceStorage { Root = parent, LocalMailbox = true }, protection, factory);
        var registry = new BillingRegistry(factory, authority, new BillingCatalog(new ConfigurationBuilder().Build()), protection, clock);
        var provider = new Provider { State = BillingPurchaseState.Pending };
        var coordinator = new BillingPurchaseCoordinator(factory, authority, registry, [provider], protection, clock);
        var commands = new BillingCommandStore(factory, authority, registry, coordinator, protection, clock);
        var transport = new Transport(registry, commands, authority, clock, factory);
        var storage = new Storage(root, protection.CreateProtector("fictional-native-receipt-binding-v1"));
        BillingClient Open() => new(transport, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        var client = Open(); var fresh = await client.ObserveAsync(native, ct);
        provider.Binding = BillingAccountBinding.Google(fresh.Snapshot!.AccountToken, false);
        var intent = await client.BeginNativeAsync(native, BillingProduct.PlusMonthly, ct, ["fictional-already-owned-receipt"]);
        Assert.False(await client.MatchesNativeAsync(native, "fictional-already-owned-receipt", BillingProduct.PlusMonthly, ct));
        Assert.False(await client.MatchesNativeAsync(native, "fictional-unrelated-product", BillingProduct.PlusYearly, ct));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.VerifyAsync(native, "fictional-already-owned-receipt", BillingProduct.PlusMonthly, true, ct));
        Assert.Equal(0, provider.Queries);
        var reply = await client.VerifyAsync(native, "fictional-original-native-receipt", BillingProduct.PlusMonthly, false, ct);
        Assert.False(reply.MayFinish); Assert.Equal(1, provider.Queries);
        client = Open(); Assert.Equal(intent, (await client.ReadAsync(native, false, ct)).NativePaymentId);
        Assert.True(await client.MatchesNativeAsync(native, "fictional-original-native-receipt", BillingProduct.PlusMonthly, ct));
        Assert.False(await client.MatchesNativeAsync(native, "fictional-substituted-native-receipt", BillingProduct.PlusMonthly, ct));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.VerifyAsync(native, "fictional-substituted-native-receipt", BillingProduct.PlusMonthly, true, ct));
        Assert.Equal(1, provider.Queries); Assert.False((await client.ReadAsync(native, false, ct)).HasPendingCommand);
        await File.WriteAllTextAsync(Path.Combine(root, "complete.json"), "{\"NewReceiptBound\":true,\"OldOrSubstitutedRefused\":true,\"ProviderQueries\":1}", ct);
    }

    /// <summary>A fresh but different provider base plan cannot authorize completion of the reviewed native product.</summary>
    [Fact]
    public async Task Different_verified_base_plan_preserves_original_payment_without_granting_completion()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "native-product-mismatch"); Directory.CreateDirectory(root);
        Assert.False(File.Exists(Path.Combine(root, "complete.json")), "Do not replay successful receipt qualification.");
        var factory = new Factory(connection); var clock = new Clock(); var native = new NativeAccess();
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        var authority = new BillingAuthority(new ServiceStorage { Root = parent, LocalMailbox = true }, protection, factory);
        var registry = new BillingRegistry(factory, authority, new BillingCatalog(new ConfigurationBuilder().Build()), protection, clock);
        var provider = new Provider { Product = BillingProduct.PlusYearly };
        var coordinator = new BillingPurchaseCoordinator(factory, authority, registry, [provider], protection, clock);
        var commands = new BillingCommandStore(factory, authority, registry, coordinator, protection, clock);
        var transport = new Transport(registry, commands, authority, clock, factory);
        var storage = new Storage(root, protection.CreateProtector("fictional-native-product-mismatch-v1"));
        BillingClient Open() => new(transport, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        var client = Open(); var fresh = await client.ObserveAsync(native, ct);
        provider.Binding = BillingAccountBinding.Google(fresh.Snapshot!.AccountToken, false);
        var intent = await client.BeginNativeAsync(native, BillingProduct.PlusMonthly, ct, []);
        var refused = await Assert.ThrowsAsync<BillingRefusal>(() => client.VerifyAsync(native, "fictional-different-native-base-plan", BillingProduct.PlusMonthly, false, ct));
        Assert.Equal(422, refused.Status); Assert.Equal(1, provider.Queries);
        var retained = await Open().ReadAsync(native, false, ct);
        Assert.Equal(intent, retained.NativePaymentId); Assert.True(retained.HasPendingCommand); Assert.Empty(retained.Snapshot!.Grants);
        await File.WriteAllTextAsync(Path.Combine(root, "complete.json"), "{\"DifferentVerifiedProductRefused\":true,\"OriginalIntentPreserved\":true}", ct);
    }

    /// <summary>Actual HTTP410 persists a cold refusal barrier instead of permitting an old paid cache during later outages.</summary>
    [Fact]
    public async Task Real_service_retirement_cannot_regain_paid_cache_after_cold_restart_or_outage()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "http-retired-cache"); Directory.CreateDirectory(root);
        Assert.False(File.Exists(Path.Combine(root, "complete.json")), "Do not replay successful HTTP retirement qualification.");
        var provider = new Provider();
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        await using var host = new BillingHttpHost(connection, parent, protection, provider);
        using var http = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var transport = new HttpTransport(http); var native = new NativeAccess();
        var storage = new Storage(root, protection.CreateProtector("fictional-http-retired-cache-v1"));
        BillingClient Open() => new(transport, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        var client = Open(); var fresh = await client.ObserveAsync(native, ct);
        provider.Binding = BillingAccountBinding.Google(fresh.Snapshot!.AccountToken, false);
        // Server time is current; this fictional provider's October term remains inside its actual verification window.
        await client.VerifyAsync(native, "fictional-http-retirement-purchase", BillingProduct.PlusMonthly, true, ct);
        var paid = await client.ObserveAsync(native, ct); Assert.Equal(ProductPlan.Plus, paid.Decision.Plan);
        await using (var db = new Factory(connection).CreateDbContext())
            await db.Set<BillingCustomerRow>().Where(row => row.Id == paid.Snapshot!.CustomerId)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Revoked, true), ct);
        var refused = await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.ObserveAsync(native, ct));
        Assert.Equal(HttpStatusCode.Gone, refused.Status);
        var reads = transport.Requests; client = Open();
        var closed = await client.ReadAsync(native, true, ct); Assert.Equal(ProductPlan.Free, closed.Decision.Plan);
        Assert.True(closed.Decision.NeedsVerification); Assert.Null(closed.Snapshot);
        await Assert.ThrowsAsync<InvalidDataException>(() => client.ObserveAsync(native, ct));
        Assert.Equal(reads, transport.Requests); Assert.Equal(1, provider.Queries);
        await File.WriteAllTextAsync(Path.Combine(root, "complete.json"), "{\"RealHttp410\":true,\"ColdPaidCacheRefused\":true,\"RepeatedHttp\":false,\"RealTlsHandshake\":false}", ct);
    }

    private sealed class BillingHttpHost(string connection, string root, IDataProtectionProvider protection, IBillingProvider provider,
        string hostName = "http-retired-cache") : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration"); builder.UseSetting("ConnectionStrings:Service", connection);
            builder.UseSetting("Service:Root", Path.Combine(root, hostName, "host")); builder.UseSetting("Billing:LocalQualification", "true");
            builder.ConfigureServices(services =>
            {
                // Retain the original isolated signing/protection authority and previous fixtures; no background cleanup or query.
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IBillingProvider>(); services.AddSingleton<IBillingProvider>(provider);
                // Keep the normal host protection ring/installation marker together. Only the already existing
                // billing fixture uses its original independently retained protection authority and signing file.
                services.RemoveAll<BillingAuthority>(); services.AddSingleton(sp => new BillingAuthority(new ServiceStorage { Root = root, LocalMailbox = false }, protection,
                    sp.GetRequiredService<IDbContextFactory<ServiceDbContext>>()));
                services.RemoveAll<BillingRegistry>(); services.AddSingleton(sp => new BillingRegistry(sp.GetRequiredService<IDbContextFactory<ServiceDbContext>>(),
                    sp.GetRequiredService<BillingAuthority>(), sp.GetRequiredService<BillingCatalog>(), protection, sp.GetRequiredService<TimeProvider>()));
                services.RemoveAll<BillingPurchaseCoordinator>(); services.AddSingleton(sp => new BillingPurchaseCoordinator(sp.GetRequiredService<IDbContextFactory<ServiceDbContext>>(),
                    sp.GetRequiredService<BillingAuthority>(), sp.GetRequiredService<BillingRegistry>(), sp.GetServices<IBillingProvider>(), protection, sp.GetRequiredService<TimeProvider>()));
                services.RemoveAll<BillingCommandStore>(); services.AddSingleton(sp => new BillingCommandStore(sp.GetRequiredService<IDbContextFactory<ServiceDbContext>>(),
                    sp.GetRequiredService<BillingAuthority>(), sp.GetRequiredService<BillingRegistry>(), sp.GetRequiredService<BillingPurchaseCoordinator>(), protection, sp.GetRequiredService<TimeProvider>()));
            });
        }
    }
    private sealed class HttpTransport(HttpClient http) : IBillingServiceTransport
    {
        public string PeerBinding => "fictional-actual-http-billing-d295";
        internal int Requests;
        internal string? User;
        public Task<string?> CurrentUserAsync(CancellationToken ct) => Task.FromResult(User);
        public async Task<BillingTimeResult> TimeAsync(Guid nonce, CancellationToken ct)
        {
            Requests++; using var reply = await http.GetAsync("/billing/time/" + nonce.ToString("D"), ct);
            reply.EnsureSuccessStatusCode(); return (await reply.Content.ReadFromJsonAsync(BillingJson.Default.BillingTimeResult, ct))!;
        }
        public async Task<BillingRegistrationResult> RegisterAsync(string? user, BillingRegistrationEnvelope input, CancellationToken ct)
        {
            Requests++; using var reply = await http.PostAsJsonAsync("/billing/devices", input, BillingJson.Default.BillingRegistrationEnvelope, ct);
            reply.EnsureSuccessStatusCode(); return (await reply.Content.ReadFromJsonAsync(BillingJson.Default.BillingRegistrationResult, ct))!;
        }
        public async Task<BillingCommandResult> CommandAsync(string? user, Guid customer, BillingSignedRequest input, CancellationToken ct)
        {
            Requests++; using var reply = await http.PostAsJsonAsync("/billing/devices/" + customer.ToString("D") + "/commands", input, BillingJson.Default.BillingSignedRequest, ct);
            if (!reply.IsSuccessStatusCode) throw new PersonalSyncHttpException(reply.StatusCode);
            return (await reply.Content.ReadFromJsonAsync(BillingJson.Default.BillingCommandResult, ct))!;
        }
    }

    /// <summary>Explicit private-state restoration keeps old pending bytes and resumes the same new key after a cut before publication.</summary>
    [Fact]
    public async Task Lost_private_state_restores_without_overwriting_original_pending_keys_or_replacing_the_saved_candidate()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "private-state-recovery"); Directory.CreateDirectory(root);
        Assert.False(File.Exists(Path.Combine(root, "complete.json")), "Do not replay completed private recovery qualification.");
        var factory = new Factory(connection); var clock = new Clock(); var native = new NativeAccess();
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        var authority = new BillingAuthority(new ServiceStorage { Root = parent, LocalMailbox = false }, protection, factory);
        var registry = new BillingRegistry(factory, authority, new BillingCatalog(new ConfigurationBuilder().Build()), protection, clock);
        var provider = new Provider();
        var coordinator = new BillingPurchaseCoordinator(factory, authority, registry, [provider], protection, clock);
        var commands = new BillingCommandStore(factory, authority, registry, coordinator, protection, clock);
        var transport = new Transport(registry, commands, authority, clock, factory);
        var storage = new RecoveryStorage(new Storage(root, protection.CreateProtector("fictional-private-recovery-v1")));
        BillingClient Open() => new(transport, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        var client = Open(); var fresh = await client.ObserveAsync(native, ct);
        provider.Binding = BillingAccountBinding.Google(fresh.Snapshot!.AccountToken, false);
        var originalCustomer = fresh.Snapshot.CustomerId;
        await client.BeginNativeAsync(native, BillingProduct.PlusMonthly, ct);
        transport.LoseNextVerificationReply = true;
        await Assert.ThrowsAsync<IOException>(() => client.VerifyAsync(native, "fictional-key-loss-pending-original", BillingProduct.PlusMonthly, false, ct));
        Assert.True((await client.ReadAsync(native, false, ct)).HasPendingCommand); Assert.Equal(1, provider.Queries);
        var originalSlot = storage.FirstStateSlot!;
        var originalBytes = await File.ReadAllBytesAsync(Path.Combine(root, originalSlot + ".protected"), ct);
        storage.Unavailable = originalSlot;
        await Assert.ThrowsAsync<BillingPrivateStateUnavailableException>(() => client.ObserveAsync(native, ct));
        Assert.Equal(1, provider.Queries);
        storage.FailAfterCandidateWrite = true;
        await Assert.ThrowsAsync<IOException>(() => client.ReenrollForStoreRestoreAsync(native, ct));
        var savedCandidate = storage.CandidateCustomer; Assert.NotNull(savedCandidate);
        await Assert.ThrowsAsync<BillingPrivateStateUnavailableException>(() => Open().ReadAsync(native, true, ct));
        client = Open(); await client.ReenrollForStoreRestoreAsync(native, ct);
        var replacement = await client.ObserveAsync(native, ct);
        Assert.Equal(savedCandidate, replacement.Snapshot!.CustomerId); Assert.NotEqual(originalCustomer, replacement.Snapshot.CustomerId);
        Assert.True(replacement.RequiresStoreRestore); Assert.Equal(ProductPlan.Free, replacement.Decision.Plan); Assert.Empty(replacement.Snapshot.Grants);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.BeginNativeAsync(native, BillingProduct.PlusYearly, ct));
        await client.CompleteStoreRestoreAsync(native, true, ct); Assert.True((await client.ReadAsync(native, false, ct)).RequiresStoreRestore);
        var restored = await client.VerifyAsync(native, "fictional-key-loss-pending-original", BillingProduct.PlusMonthly, true, ct);
        Assert.True(restored.MayFinish); Assert.Equal(2, provider.Queries);
        await client.ObserveAsync(native, ct); await client.CompleteStoreRestoreAsync(native, false, ct);
        var cold = await Open().ReadAsync(native, false, ct); Assert.Equal(ProductPlan.Plus, cold.Decision.Plan); Assert.False(cold.RequiresStoreRestore);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(Path.Combine(root, originalSlot + ".protected"), ct));
        await using (var db = factory.CreateDbContext())
        {
            var oldCommands = await db.Set<BillingCommandRow>().Where(row => row.CustomerId == originalCustomer).ToArrayAsync(ct);
            Assert.Single(oldCommands); Assert.NotEmpty(oldCommands[0].ProtectedResponse);
            Assert.Equal(1, await db.Set<BillingPurchaseRow>().CountAsync(row => row.CustomerId == originalCustomer && row.Verified, ct));
        }
        await File.WriteAllTextAsync(Path.Combine(root, "complete.json"), "{\"OriginalPrivateBytesPreserved\":true,\"SameCandidateAfterCut\":true,\"RestoreOnlyBeforeStoreProof\":true,\"ActualPostgres\":true,\"NativeEffectsFictional\":true}", ct);
    }

    private sealed class RecoveryStorage(Storage original) : IBillingPrivateStorage
    {
        internal string? FirstStateSlot;
        internal string? Unavailable;
        internal bool FailAfterCandidateWrite;
        internal Guid? CandidateCustomer;
        public Task<string?> ReadAsync(string scope, CancellationToken ct) => scope == Unavailable
            ? Task.FromException<string?>(new BillingPrivateStateUnavailableException()) : original.ReadAsync(scope, ct);
        public async Task WriteAsync(string scope, string value, CancellationToken ct)
        {
            if (scope == Unavailable) throw new IOException("Never overwrite original unavailable private state.");
            using var json = JsonDocument.Parse(value);
            var state = json.RootElement.TryGetProperty("PrivateIdentity", out _);
            if (state) FirstStateSlot ??= scope;
            await original.WriteAsync(scope, value, ct);
            if (state && json.RootElement.GetProperty("PrivateGeneration").GetGuid() != Guid.Empty)
            {
                var customer = json.RootElement.GetProperty("Customer").GetGuid();
                if (CandidateCustomer is not null) Assert.Equal(CandidateCustomer, customer);
                CandidateCustomer = customer;
                if (FailAfterCandidateWrite) { FailAfterCandidateWrite = false; throw new IOException("Fictional cut after candidate persistence, before active routing."); }
            }
        }
    }

    private sealed class Provider : IBillingProvider
    {
        public BillingIssuer Issuer => BillingIssuer.GooglePlay;
        internal string Binding { get; set; } = "";
        internal BillingProduct Product { get; set; } = BillingProduct.PlusMonthly;
        internal BillingPurchaseState State { get; set; } = BillingPurchaseState.Active;
        internal int Queries { get; private set; }
        public Task<BillingVerifiedPurchase> VerifyAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
        {
            Queries++; return Task.FromResult(new BillingVerifiedPurchase(Issuer, environment, reference, Binding,
                new(Product, State, new(2026, 10, 16, 0, 0, 0, TimeSpan.Zero), new(2026, 11, 16, 0, 0, 0, TimeSpan.Zero),
                    null, State != BillingPurchaseState.Revoked, false, true, "fictional-original-terms"), null));
        }
        public Task AcknowledgeAsync(BillingVerifiedPurchase purchase, CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class Transport(BillingRegistry registry, BillingCommandStore commands, BillingAuthority authority, Clock clock,
        Factory factory) : IBillingServiceTransport
    {
        public string PeerBinding => "fictional-billing-peer-d295";
        internal string? User { get; set; }
        internal bool LoseNextVerificationReply { get; set; }
        internal bool ChangeIdentityDuringTime { get; set; }
        internal bool ExpireBeforeService { get; set; }
        internal string? ReplayCurrent { get; set; }
        internal BillingCommandResult? LastCurrent { get; private set; }
        private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
        public Task<string?> CurrentUserAsync(CancellationToken ct) => Task.FromResult(User);
        public async Task<BillingTimeResult> TimeAsync(Guid nonce, CancellationToken ct)
        {
            using var signer = await authority.OpenAsync(ct); var now = clock.GetUtcNow();
            if (ChangeIdentityDuringTime) User = "fictional-other-confirmed-subject";
            return new(BillingAuthority.Public(signer), BillingAuthority.SignTime(new(nonce, now, now.AddMinutes(2)), signer));
        }
        public Task<BillingRegistrationResult> RegisterAsync(string? user, BillingRegistrationEnvelope input, CancellationToken ct)
            => registry.RegisterAsync(input, Anonymous, ct);
        public async Task<BillingCommandResult> CommandAsync(string? user, Guid customer, BillingSignedRequest input, CancellationToken ct)
        {
            await using var db = factory.CreateDbContext();
            var row = await db.Set<BillingCustomerRow>().SingleAsync(r => r.Id == customer, ct);
            var command = StandardJoseSignatures.VerifyJson(input.Signed, BillingAuthority.ReadPublic(row.PublicIdentity), BillingAuthority.CommandPurpose,
                32768, BillingJson.Default.BillingCommand);
            if (ExpireBeforeService && command.Operation == BillingOperation.VerifyPurchase)
            { clock.Now = clock.Now.AddMinutes(10); ExpireBeforeService = false; }
            var reply = await commands.ExecuteAsync(customer, input.Signed, Anonymous, ct);
            if (command.Operation == BillingOperation.Observe)
            {
                LastCurrent = reply;
                if (ReplayCurrent is not null) return reply with { SignedSnapshot = ReplayCurrent };
            }
            if (LoseNextVerificationReply && command.Operation == BillingOperation.VerifyPurchase)
            { LoseNextVerificationReply = false; throw new IOException("Fictional response lost after committed original work."); }
            return reply;
        }
    }
    private sealed class Storage(string root, IDataProtector protector) : IBillingPrivateStorage
    {
        public async Task<string?> ReadAsync(string scope, CancellationToken ct)
        {
            var path = Path.Combine(root, scope + ".protected");
            return File.Exists(path) ? protector.Unprotect(await File.ReadAllTextAsync(path, ct)) : null;
        }
        public Task WriteAsync(string scope, string value, CancellationToken ct) => File.WriteAllTextAsync(Path.Combine(root, scope + ".protected"), protector.Protect(value), ct);
        internal Task WriteProofAsync(string name, string value, CancellationToken ct) => WriteAsync(name, value, ct);
        internal async Task<string> ReadProofAsync(string name, CancellationToken ct) => await ReadAsync(name, ct) ?? throw new InvalidDataException("Original proof required.");
    }
}
