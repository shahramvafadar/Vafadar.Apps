using System.Text;
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
    /// <summary>Forty explicitly seeded historical terminal service rows cannot crowd out one actual signed active purchase or keep it paid after verified revocation.</summary>
    [Fact]
    public async Task Historical_terminal_purchases_do_not_overflow_current_authority_or_survive_its_complete_replacement()
    {
        var ct = TestContext.Current.CancellationToken; var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "terminal-history-bounds"); Directory.CreateDirectory(root);
        var done = Path.Combine(root, "complete.json"); Assert.False(File.Exists(done), "Completed history-bound qualification must not be replayed.");
        var factory = new Factory(connection); var clock = new Clock(); var native = new NativeAccess();
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        var authority = new BillingAuthority(new ServiceStorage { Root = parent, LocalMailbox = true }, protection, factory);
        var registry = new BillingRegistry(factory, authority, new BillingCatalog(new ConfigurationBuilder().Build()), protection, clock);
        var provider = new Provider(); var coordinator = new BillingPurchaseCoordinator(factory, authority, registry, [provider], protection, clock);
        var commands = new BillingCommandStore(factory, authority, registry, coordinator, protection, clock);
        var transport = new Transport(registry, commands, authority, clock, factory);
        var storage = new Storage(root, protection.CreateProtector("fictional-terminal-history-client-v1"));
        var client = new BillingClient(transport, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        var fresh = await client.ObserveAsync(native, ct); var customer = fresh.Snapshot!.CustomerId;
        provider.Binding = BillingAccountBinding.Google(fresh.Snapshot.AccountToken, false);
        const string activeReference = "fictional-d295-history-active-reference";
        await client.VerifyAsync(native, activeReference, BillingProduct.PlusMonthly, true, ct);
        var activeId = BillingAccountBinding.PurchaseId(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, activeReference);
        await using (var db = factory.CreateDbContext())
        {
            var original = await db.Set<BillingPurchaseRow>().SingleAsync(row => row.Id == activeId, ct);
            var originalFacts = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(original.FactsJson), BillingJson.Default.BillingPurchaseFacts, 8192);
            for (var index = 0; index < 40; index++)
            {
                var reference = "fictional-d295-history-terminal-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var id = BillingAccountBinding.PurchaseId(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, reference);
                if (await db.Set<BillingPurchaseRow>().AnyAsync(row => row.Id == id, ct)) continue;
                var state = index % 2 == 0 ? BillingPurchaseState.Revoked : BillingPurchaseState.Expired;
                var facts = originalFacts with { State = state, StartsAt = clock.Now.AddDays(-50), PaidUntil = clock.Now.AddDays(-20), AutoRenews = false };
                db.Add(new BillingPurchaseRow { Id = id, Issuer = BillingIssuer.GooglePlay, Environment = BillingEnvironment.Sandbox,
                    CustomerId = customer, Product = facts.Product, State = state, Verified = true, ProviderAccountBinding = provider.Binding,
                    VerifiedAt = clock.Now.AddMinutes(-index - 1), RecheckAt = clock.Now.AddDays(100), AccessUntil = facts.PaidUntil,
                    FactsJson = JsonSerializer.Serialize(facts, BillingJson.Default.BillingPurchaseFacts),
                    ProtectedReference = protection.CreateProtector("zanance-billing-provider-reference-v1").Protect(reference) });
                db.Add(new BillingDevicePurchaseRow { CustomerId = customer, PurchaseId = id });
            }
            await db.SaveChangesAsync(ct);
        }
        var paid = await client.ObserveAsync(native, ct); Assert.Equal(ProductPlan.Plus, paid.Decision.Plan);
        Assert.Equal(32, paid.Snapshot!.Grants.Count); Assert.Contains(paid.Snapshot.Grants, grant => grant.PurchaseId == activeId && grant.Facts.State == BillingPurchaseState.Active);
        provider.State = BillingPurchaseState.Revoked; await client.RefreshAsync(native, ct);
        var revoked = await client.ObserveAsync(native, ct); Assert.Equal(ProductPlan.Free, revoked.Decision.Plan);
        Assert.Equal(32, revoked.Snapshot!.Grants.Count); Assert.DoesNotContain(revoked.Snapshot.Grants, grant => grant.Facts.State == BillingPurchaseState.Active);
        await using (var db = factory.CreateDbContext())
            Assert.Equal(41, await db.Set<BillingPurchaseRow>().CountAsync(row => row.CustomerId == customer, ct));
        await File.WriteAllTextAsync(done, "{\"Completed\":true,\"HistoryRetained\":41,\"SnapshotBound\":32,\"ActualStoreSandbox\":false}", ct);
    }
}
