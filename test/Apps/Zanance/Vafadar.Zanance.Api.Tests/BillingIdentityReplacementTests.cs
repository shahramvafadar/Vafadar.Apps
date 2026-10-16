using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Api.Tests;

public sealed partial class BillingClientContinuationTests
{
    /// <summary>Actual HTTP sessions, signed clients and PostgreSQL enforce explicit Plus association before a retained-binding Pro upgrade, cross-store identity access and other-user refusal.</summary>
    [Fact]
    public async Task Actual_identity_upgrade_preserves_original_store_binding_and_refuses_another_identity()
    {
        var ct = TestContext.Current.CancellationToken; var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "http-identity-replacement"); Directory.CreateDirectory(root);
        var done = Path.Combine(root, "complete.json"); Assert.False(File.Exists(done), "Never repeat completed identity replacement qualification.");
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        var provider = new IdentityReplacementProvider(); var native = new NativeAccess();
        await using var host = new BillingHttpHost(connection, parent, protection, provider, "http-identity-replacement");
        using var anonymousHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var ownerHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var otherHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var ownerId = await Login(ownerHttp, "billing-upgrade-owner-d295@example.invalid");
        var otherId = await Login(otherHttp, "billing-upgrade-other-d295@example.invalid");
        var localWire = new HttpTransport(anonymousHttp); var ownerWire = new HttpTransport(ownerHttp) { User = ownerId };
        var otherWire = new HttpTransport(otherHttp) { User = otherId };
        var storage = new Storage(root, protection.CreateProtector("fictional-identity-replacement-client-v1"));
        var local = new BillingClient(localWire, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        var identity = new BillingClient(ownerWire, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        var current = await local.ObserveAsync(native, ct);
        provider.Binding = BillingAccountBinding.Google(current.Snapshot!.AccountToken, false);
        var originalId = BillingAccountBinding.PurchaseId(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, IdentityReplacementProvider.OldReference);
        await local.VerifyAsync(native, IdentityReplacementProvider.OldReference, BillingProduct.PlusMonthly, true, ct);
        await identity.ObserveAsync(native, ct);
        var before = provider.Queries;
        await Assert.ThrowsAsync<InvalidOperationException>(() => identity.BeginNativeAsync(native, BillingProduct.ProMonthly, ct,
            [IdentityReplacementProvider.OldReference], originalId));
        Assert.Equal(before, provider.Queries);
        // This is the same explicit user-confirmed Restore operation used by the original screen, with a real current HTTP session.
        await identity.VerifyAsync(native, IdentityReplacementProvider.OldReference, BillingProduct.PlusMonthly, true, ct);
        var associated = await identity.ObserveAsync(native, ct);
        Assert.Contains(associated.Snapshot!.Grants, grant => grant.PurchaseId == originalId && grant.MatchingIdentity);
        var intent = await identity.BeginNativeAsync(native, BillingProduct.ProMonthly, ct, [IdentityReplacementProvider.OldReference], originalId);
        var upgraded = await identity.VerifyAsync(native, IdentityReplacementProvider.NewReference, BillingProduct.ProMonthly, false, ct);
        Assert.True(upgraded.MayFinish); await identity.FinishNativeAsync(native, intent, ct);
        var actual = await identity.ObserveAsync(native, ct); Assert.Equal(ProductPlan.Pro, actual.Decision.Plan);
        Assert.Contains(actual.Snapshot!.Grants, grant => grant.PurchaseId == originalId && grant.Facts.State == BillingPurchaseState.Revoked);
        // Distribution changes do not change the confirmed identity's Pro authority or fabricate a second purchase.
        var otherStore = new BillingClient(ownerWire, storage, BillingIssuer.AppStore, BillingEnvironment.Sandbox);
        var crossStore = await otherStore.ObserveAsync(native, ct); Assert.Equal(ProductPlan.Pro, crossStore.Decision.Plan);
        Assert.Contains(crossStore.Snapshot!.Grants, grant => grant.Issuer == BillingIssuer.GooglePlay && grant.MatchingIdentity && grant.Facts.Product == BillingProduct.ProMonthly);
        var stranger = new BillingClient(otherWire, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
        await stranger.ObserveAsync(native, ct);
        var refusal = await Assert.ThrowsAsync<PersonalSyncHttpException>(() => stranger.VerifyAsync(native, IdentityReplacementProvider.NewReference,
            BillingProduct.ProMonthly, true, ct));
        Assert.Equal(HttpStatusCode.Forbidden, refusal.Status);
        Assert.Equal(ProductPlan.Free, (await stranger.ReadAsync(native, false, ct)).Decision.Plan);
        Assert.Equal(ProductPlan.Pro, (await identity.ObserveAsync(native, ct)).Decision.Plan);
        await File.WriteAllTextAsync(done, "{\"Completed\":true,\"ActualHttpSessions\":true,\"ActualStoreSandbox\":false,\"RealTlsHandshake\":false}", ct);

        async Task<string> Login(HttpClient http, string email)
        {
            string id;
            using (var scope = host.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>(); var user = await users.FindByEmailAsync(email);
                if (user is null)
                {
                    user = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
                    Assert.True((await users.CreateAsync(user, "Fictional-Upgrade-Fixture-Password9!")).Succeeded);
                }
                id = user.Id;
            }
            using var reply = await http.PostAsJsonAsync("/identity/login", new IdentityInput(email, "Fictional-Upgrade-Fixture-Password9!"), ct);
            Assert.Equal(HttpStatusCode.OK, reply.StatusCode); var result = await reply.Content.ReadFromJsonAsync<JsonElement>(ct);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.GetProperty("accessToken").GetString()); return id;
        }
    }

    private sealed class IdentityReplacementProvider : IBillingProvider
    {
        internal const string OldReference = "fictional-d295-identity-original-plus";
        internal const string NewReference = "fictional-d295-identity-upgraded-pro";
        internal string Binding = "";
        internal int Queries;
        public BillingIssuer Issuer => BillingIssuer.GooglePlay;
        public Task<BillingVerifiedPurchase> VerifyAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
        {
            Queries++; Assert.True(reference is OldReference or NewReference);
            var upgrade = reference == NewReference; var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new BillingVerifiedPurchase(Issuer, environment, reference, Binding,
                new(upgrade ? BillingProduct.ProMonthly : BillingProduct.PlusMonthly, BillingPurchaseState.Active, now.AddDays(-1), now.AddDays(30),
                    null, true, false, true, "fictional-sold-identity-terms"), upgrade ? OldReference : null,
                StoreProductId: upgrade ? "fictional.pro" : "fictional.plus"));
        }
        public Task AcknowledgeAsync(BillingVerifiedPurchase purchase, CancellationToken ct) => Task.CompletedTask;
    }
}
