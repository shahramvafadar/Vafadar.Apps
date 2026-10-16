using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vafadar.Cryptography;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.Api.Tests;

public sealed partial class BillingClientContinuationTests
{
    /// <summary>Actual PostgreSQL, signed requests and protected cold clients preserve a deferred native target and finish its lost reply without repeating provider work.</summary>
    [Fact]
    public async Task Deferred_replacement_continues_original_completion_and_grants_future_plan_only_after_provider_refresh()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "deferred-native-replacement"); Directory.CreateDirectory(root);
        var done = Path.Combine(root, "complete.json"); Assert.False(File.Exists(done), "Completed replacement qualification must not be replayed.");
        var phaseFile = Path.Combine(root, "phase.txt");
        var phase = File.Exists(phaseFile) ? int.Parse(await File.ReadAllTextAsync(phaseFile, ct), System.Globalization.CultureInfo.InvariantCulture) : 0;
        var factory = new Factory(connection); var clock = new Clock(); var native = new NativeAccess();
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        var authority = new BillingAuthority(new ServiceStorage { Root = parent, LocalMailbox = true }, protection, factory);
        var registry = new BillingRegistry(factory, authority, new BillingCatalog(new ConfigurationBuilder().Build()), protection, clock);
        var provider = new ReplacementProvider();
        var coordinator = new BillingPurchaseCoordinator(factory, authority, registry, [provider], protection, clock);
        var commands = new BillingCommandStore(factory, authority, registry, coordinator, protection, clock);
        var transport = new Transport(registry, commands, authority, clock, factory);
        var storage = new Storage(root, protection.CreateProtector("fictional-deferred-replacement-v1"));
        BillingClient Open() => new(transport, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        var client = Open(); var fresh = await client.ObserveAsync(native, ct);
        provider.Binding = BillingAccountBinding.Google(fresh.Snapshot!.AccountToken, false);
        var oldId = BillingAccountBinding.PurchaseId(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, ReplacementProvider.OldReference);
        if (phase < 1)
        {
            var original = await client.VerifyAsync(native, ReplacementProvider.OldReference, BillingProduct.PlusMonthly, true, ct);
            Assert.True(original.MayFinish); await client.ObserveAsync(native, ct); await Advance(1);
        }
        if (phase < 2)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.BeginNativeAsync(native, BillingProduct.PlusYearly, ct, [ReplacementProvider.OldReference]));
            var intent = await client.BeginNativeAsync(native, BillingProduct.PlusYearly, ct, [ReplacementProvider.OldReference], oldId);
            await File.WriteAllTextAsync(Path.Combine(root, "intent.txt"), intent.ToString("D"), ct); await Advance(2);
        }
        var originalIntent = Guid.ParseExact(await File.ReadAllTextAsync(Path.Combine(root, "intent.txt"), ct), "D");
        if (phase < 3)
        {
            transport.LoseNextVerificationReply = true;
            await Assert.ThrowsAsync<IOException>(() => client.VerifyAsync(native, ReplacementProvider.NewReference, BillingProduct.PlusYearly, false, ct));
            Assert.True((await client.ReadAsync(native, false, ct)).HasPendingCommand); await Advance(3);
        }
        if (phase < 4)
        {
            client = Open(); var before = provider.Queries;
            var reply = await client.ContinueAsync(native, ct); Assert.NotNull(reply); Assert.True(reply.MayFinish); Assert.Equal(before, provider.Queries);
            using (var signer = await authority.OpenAsync(ct))
            {
                var receipt = StandardJoseSignatures.VerifyJson(reply.SignedReceipt!, signer.Public, BillingAuthority.ReceiptPurpose,
                    8192, BillingJson.Default.BillingOperationReceipt);
                Assert.Equal(BillingProduct.PlusYearly, receipt.CompletedProduct); Assert.Equal(originalIntent, receipt.NativePaymentId);
            }
            var completion = await client.LastCompletionAsync(native, originalIntent, ct); Assert.NotNull(completion); Assert.True(completion.MayFinish);
            await client.FinishNativeAsync(native, originalIntent, ct);
            var current = await client.ObserveAsync(native, ct);
            Assert.False(current.HasUnresolvedNativePayment); Assert.Equal(ProductPlan.Plus, current.Decision.Plan);
            var active = Assert.Single(current.Snapshot!.Grants, grant => grant.Facts.State == BillingPurchaseState.Active);
            Assert.Equal(BillingProduct.PlusMonthly, active.Facts.Product); Assert.Equal(BillingProduct.PlusYearly, active.Facts.ScheduledChange!.Product);
            Assert.Contains(current.Snapshot.Grants, grant => grant.PurchaseId == oldId && grant.Facts.State == BillingPurchaseState.Revoked);
            await Advance(4);
        }
        if (phase < 5)
        {
            provider.Effective = true; clock.Now = new(2026, 11, 16, 0, 1, 0, TimeSpan.Zero);
            await client.RefreshAsync(native, ct);
            var current = await Open().ObserveAsync(native, ct);
            var active = Assert.Single(current.Snapshot!.Grants, grant => grant.Facts.State == BillingPurchaseState.Active);
            Assert.Equal(BillingProduct.PlusYearly, active.Facts.Product); Assert.Null(active.Facts.ScheduledChange);
            Assert.Equal("original-next-yearly-terms", active.Facts.TermsVersion);
            await Advance(5);
        }
        await File.WriteAllTextAsync(done, "{\"Completed\":true,\"Stages\":5,\"ActualStoreSandbox\":false}", ct);
        async Task Advance(int value) { await File.WriteAllTextAsync(phaseFile, value.ToString(System.Globalization.CultureInfo.InvariantCulture), ct); phase = value; }
    }

    /// <summary>Explicit provider fixture facts, never an actual payment or a native store acceptance claim.</summary>
    private sealed class ReplacementProvider : IBillingProvider
    {
        internal const string OldReference = "fictional-d295-original-replacement-plus";
        internal const string NewReference = "fictional-d295-new-deferred-plus";
        internal string Binding = "";
        internal bool Effective;
        internal int Queries;
        public BillingIssuer Issuer => BillingIssuer.GooglePlay;
        public Task<BillingVerifiedPurchase> VerifyAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
        {
            Queries++; Assert.True(reference is OldReference or NewReference);
            var next = reference == NewReference; var start = new DateTimeOffset(2026, 10, 16, 0, 0, 0, TimeSpan.Zero); var end = start.AddMonths(1);
            var facts = new BillingPurchaseFacts(next && Effective ? BillingProduct.PlusYearly : BillingProduct.PlusMonthly, BillingPurchaseState.Active,
                start, next && Effective ? end.AddYears(1) : end, null, !next || Effective, false, true,
                next && Effective ? "later-catalog-must-not-replace-sold-terms" : "original-monthly-terms", true,
                next && !Effective ? new(BillingProduct.PlusYearly, end, "original-next-yearly-terms") : null);
            return Task.FromResult(new BillingVerifiedPurchase(Issuer, environment, reference, Binding, facts,
                next ? OldReference : null, StoreProductId: next && Effective ? "fictional.yearly" : "fictional.monthly"));
        }
        public Task AcknowledgeAsync(BillingVerifiedPurchase purchase, CancellationToken ct) => Task.CompletedTask;
    }
}
