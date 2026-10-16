using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vafadar.Cryptography;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Original billing continuation and worker races against one explicitly owned real PostgreSQL fixture.</summary>
[Trait("AT", "AT-295")]
[Trait("Qualification", "BillingLocal")]
public sealed class BillingPersistenceTests
{
    /// <summary>Retains original keys/commands and completed stages, without resetting successful evidence after a later failure.</summary>
    [Fact]
    public async Task Original_purchase_recovery_acknowledgement_refund_and_store_restore_are_durable()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (string.IsNullOrEmpty(connection)) { Assert.Skip("Explicit isolated billing PostgreSQL fixture is required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("zanance_billing_d295", settings.Database); Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port);
        var root = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT") ?? throw new InvalidOperationException("Original fixture root required."));
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        var done = Path.Combine(root, "complete.json"); Assert.False(File.Exists(done), "Completed original billing qualification must not be replayed.");
        var stagePath = Path.Combine(root, "stage.txt"); var stage = File.Exists(stagePath) ? int.Parse(await File.ReadAllTextAsync(stagePath, ct), System.Globalization.CultureInfo.InvariantCulture) : 0;
        var factory = new Factory(connection);
        await using (var schema = factory.CreateDbContext()) { await schema.Database.MigrateAsync(ct); Assert.False(schema.Database.HasPendingModelChanges()); }
        var clock = new Clock(); var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "protection")));
        var secret = protection.CreateProtector("fictional-billing-review-original-v1");
        using var device = LoadIdentity("original"); using var second = LoadIdentity("restored");
        var customerId = LoadId("original"); var secondId = LoadId("restored");
        var authority = new BillingAuthority(new ServiceStorage { Root = root, LocalMailbox = true, LocalBillingQualification = true }, protection, factory);
        var registry = new BillingRegistry(factory, authority, new BillingCatalog(new ConfigurationBuilder().Build()), protection, clock);
        var provider = new Provider(); var providers = new IBillingProvider[] { provider };
        var purchases = new BillingPurchaseCoordinator(factory, authority, registry, providers, protection, clock);
        var commands = new BillingCommandStore(factory, authority, registry, purchases, protection, clock);
        var worker = new BillingReconciliation(factory, providers, protection, clock);
        var notifications = new BillingNotifications(factory, protection, new ConfigurationBuilder().Build(), clock);
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var publicJson = JsonSerializer.Serialize(device.Public, BillingProviderJson.Default.PublicSigningRecipientIdentity);
        var registration = new BillingRegistration(customerId, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, publicJson, null);
        var envelope = new BillingRegistrationEnvelope(publicJson, Signed(registration, device, BillingAuthority.RegistrationPurpose, BillingJson.Default.BillingRegistration));
        var original = await registry.RegisterAsync(envelope, anonymous, ct);
        var trusted = BillingAuthority.ReadPublic(original.Authority);
        provider.Binding = BillingAccountBinding.Google(original.AccountToken, identity: false);
        var purchaseReference = "fictional-store-reference-d295";
        var purchaseId = BillingAuthority.PurchaseId(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, purchaseReference);
        var first = OriginalCommand("pending", BillingOperation.VerifyPurchase, device, customerId, purchaseReference);
        var paid = OriginalCommand("paid", BillingOperation.VerifyPurchase, device, customerId, purchaseReference);

        if (stage < 1)
        {
            var initial = Read(original.SignedSnapshot);
            Assert.Equal(BillingSnapshotKind.Registration, initial.Kind); Assert.Empty(initial.Grants);
            Assert.Equal(original.SignedSnapshot, (await registry.RegisterAsync(envelope, anonymous, ct)).SignedSnapshot);
            provider.FailQuery = true;
            await Assert.ThrowsAsync<BillingRefusal>(() => commands.ExecuteAsync(customerId, first, anonymous, ct));
            await using var db = factory.CreateDbContext();
            var row = await db.Set<BillingPurchaseRow>().SingleAsync(ct);
            Assert.False(row.Verified); Assert.Null(row.LeaseOwner);
            Assert.Equal(0, (await db.Set<BillingCommandRow>().SingleAsync(ct)).CompletedWork);
            await Advance(1);
        }
        if (stage < 2)
        {
            provider.FailQuery = false; provider.State = BillingPurchaseState.Pending;
            var pending = await commands.ExecuteAsync(customerId, first, anonymous, ct);
            Assert.False(pending.MayFinish); Assert.Equal(BillingPurchaseState.Pending, pending.State);
            await using var db = factory.CreateDbContext(); Assert.Equal(1, await db.Set<BillingPurchaseRow>().CountAsync(ct));
            Assert.True((await db.Set<BillingPurchaseRow>().SingleAsync(ct)).Verified);
            await Advance(2);
        }
        if (stage < 3)
        {
            provider.State = BillingPurchaseState.Active; provider.FailAcknowledgement = true;
            var result = await commands.ExecuteAsync(customerId, paid, anonymous, ct);
            Assert.True(result.MayFinish); Assert.Equal(BillingPurchaseState.Active, result.State);
            await File.WriteAllTextAsync(Path.Combine(root, "paid-reply.protected"), secret.Protect(result.SignedSnapshot), ct);
            await using var db = factory.CreateDbContext(); var row = await db.Set<BillingPurchaseRow>().SingleAsync(ct);
            var paidOperation = ReadCommand(paid).OperationId;
            Assert.True(row.AcknowledgePending); Assert.Equal(1, (await db.Set<BillingCommandRow>().SingleAsync(p => p.Id == paidOperation, ct)).CompletedWork);
            await Advance(3);
        }
        if (stage < 4)
        {
            var before = provider.QueryCount;
            var reply = await commands.ExecuteAsync(customerId, paid, anonymous, ct);
            Assert.Equal(secret.Unprotect(await File.ReadAllTextAsync(Path.Combine(root, "paid-reply.protected"), ct)), reply.SignedSnapshot);
            Assert.Equal(before, provider.QueryCount);
            var changed = ReadCommand(paid) with { Deadline = clock.GetUtcNow().AddMinutes(4) };
            var altered = Signed(changed, device, BillingAuthority.CommandPurpose, BillingJson.Default.BillingCommand);
            var refused = await Assert.ThrowsAsync<BillingRefusal>(() => commands.ExecuteAsync(customerId, altered, anonymous, ct));
            Assert.Equal("OriginalBillingCommandDiffers", refused.Message); Assert.Equal(before, provider.QueryCount);
            await Advance(4);
        }
        clock.Now = Clock.Start.AddMinutes(2);
        if (stage < 5)
        {
            provider.State = BillingPurchaseState.Active; provider.FailAcknowledgement = false; provider.Acknowledged = false;
            Assert.True(await worker.RunOneAsync(ct));
            await using var db = factory.CreateDbContext(); var row = await db.Set<BillingPurchaseRow>().SingleAsync(ct);
            Assert.False(row.AcknowledgePending); Assert.Null(row.LeaseOwner);
            Assert.True(JsonSerializer.Deserialize(row.FactsJson, BillingJson.Default.BillingPurchaseFacts)!.Acknowledged);
            await Advance(5);
        }
        clock.Now = Clock.Start.AddHours(7);
        if (stage < 6)
        {
            provider.State = BillingPurchaseState.Active; provider.Acknowledged = true;
            provider.DuringQuery = () => notifications.ScheduleAsync(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
                "fictional-event-during-http", purchaseReference, ct);
            Assert.True(await worker.RunOneAsync(ct)); provider.DuringQuery = null;
            await using (var db = factory.CreateDbContext())
            {
                var row = await db.Set<BillingPurchaseRow>().SingleAsync(ct);
                Assert.Equal(1, row.RequestedRecheckRevision); Assert.Equal(clock.Now, row.RecheckAt);
            }
            Assert.True(await worker.RunOneAsync(ct));
            await notifications.ScheduleAsync(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, "fictional-event-during-http", purchaseReference, ct);
            await using (var db = factory.CreateDbContext())
            { Assert.Equal(1, await db.Set<BillingNotificationRow>().CountAsync(ct)); Assert.Equal(1, (await db.Set<BillingPurchaseRow>().SingleAsync(ct)).RequestedRecheckRevision); }
            await Advance(6);
        }
        if (stage < 7)
        {
            provider.State = BillingPurchaseState.Revoked;
            await notifications.ScheduleAsync(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, "fictional-refund", purchaseReference, ct);
            Assert.True(await worker.RunOneAsync(ct));
            var observe = OriginalCommand("observe-refund", BillingOperation.Observe, device, customerId, null);
            var current = Read((await commands.ExecuteAsync(customerId, observe, anonymous, ct)).SignedSnapshot);
            Assert.Equal(BillingSnapshotKind.Current, current.Kind); Assert.Equal(BillingPurchaseState.Revoked, Assert.Single(current.Grants).Facts.State);
            var context = new BillingSnapshotContext(customerId, current.RequestId, device.Public.Fingerprint,
                BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, null, 0, Clock.Start);
            BillingSnapshotPolicy.RequireCurrent(current, context);
            var historical = Read((await commands.ExecuteAsync(customerId, paid, anonymous, ct)).SignedSnapshot);
            Assert.Equal(BillingPurchaseState.Active, Assert.Single(historical.Grants).Facts.State);
            Assert.Throws<ArgumentException>(() => BillingSnapshotPolicy.RequireCurrent(historical, context));
            Assert.Throws<ArgumentException>(() => BillingSnapshotPolicy.RequireCurrent(current with { RequestId = Guid.NewGuid() }, context));
            await Advance(7);
        }
        if (stage < 8)
        {
            var secondPublic = JsonSerializer.Serialize(second.Public, BillingProviderJson.Default.PublicSigningRecipientIdentity);
            var body = new BillingRegistration(secondId, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, secondPublic, null);
            await registry.RegisterAsync(new(secondPublic, Signed(body, second, BillingAuthority.RegistrationPurpose, BillingJson.Default.BillingRegistration)), anonymous, ct);
            var implicitTransfer = OriginalCommand("second-implicit", BillingOperation.VerifyPurchase, second, secondId, purchaseReference);
            var before = provider.QueryCount;
            Assert.Equal("ExplicitStoreRestoreRequired", (await Assert.ThrowsAsync<BillingRefusal>(() => commands.ExecuteAsync(secondId, implicitTransfer, anonymous, ct))).Message);
            Assert.Equal(before, provider.QueryCount);
            provider.State = BillingPurchaseState.Active; provider.Acknowledged = true;
            var restore = OriginalCommand("second-restore", BillingOperation.RestorePurchase, second, secondId, purchaseReference);
            Assert.True((await commands.ExecuteAsync(secondId, restore, anonymous, ct)).MayFinish);
            await using var db = factory.CreateDbContext(); Assert.Equal(1, await db.Set<BillingPurchaseRow>().CountAsync(ct));
            Assert.Equal(2, await db.Set<BillingDevicePurchaseRow>().CountAsync(ct)); Assert.Equal(0, await db.Users.CountAsync(ct));
            var purchase = await db.Set<BillingPurchaseRow>().SingleAsync(ct);
            Assert.Equal(customerId, purchase.CustomerId); Assert.Null(purchase.UserId); Assert.Equal(purchaseId, purchase.Id);
            await Advance(8);
        }
        if (stage < 9)
        {
            var file = Path.Combine(root, "billing-authority.protected"); var held = file + ".qualification-held";
            Assert.False(File.Exists(held)); File.Move(file, held, overwrite: false);
            try { Assert.Equal("OriginalBillingAuthorityUnavailable", (await Assert.ThrowsAsync<BillingRefusal>(() => authority.OpenAsync(ct))).Message); Assert.False(File.Exists(file)); }
            finally { File.Move(held, file, overwrite: false); }
            using var originalSigner = await authority.OpenAsync(ct); Assert.Equal(trusted.Fingerprint, originalSigner.Public.Fingerprint);
            await Advance(9);
        }
        await File.WriteAllTextAsync(done, "{\"Completed\":true,\"Stages\":9,\"RealPostgreSql\":true,\"ActualStoreSandbox\":false}", ct);

        async Task Advance(int next) { await File.WriteAllTextAsync(stagePath, next.ToString(System.Globalization.CultureInfo.InvariantCulture), ct); stage = next; }
        SigningRecipientIdentity LoadIdentity(string label)
        {
            var path = Path.Combine(root, label + ".key.protected");
            if (!File.Exists(path))
            {
                using var created = SigningRecipientIdentity.Create(); var clear = created.ExportForProtectedStorage();
                try { File.WriteAllBytes(path, secret.Protect(clear)); } finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(clear); }
            }
            var bytes = secret.Unprotect(File.ReadAllBytes(path));
            try { return SigningRecipientIdentity.ImportFromProtectedStorage(bytes); } finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
        }
        Guid LoadId(string label)
        {
            var path = Path.Combine(root, label + ".id");
            if (!File.Exists(path)) File.WriteAllText(path, Guid.NewGuid().ToString("D"));
            return Guid.ParseExact(File.ReadAllText(path), "D");
        }
        string OriginalCommand(string label, BillingOperation operation, SigningRecipientIdentity key, Guid customer, string? reference)
        {
            var path = Path.Combine(root, label + ".command.protected");
            if (!File.Exists(path))
            {
                var command = new BillingCommand(customer, Guid.NewGuid(), operation, 0, null,
                    reference is null ? null : BillingProduct.PlusMonthly, reference, clock.GetUtcNow().AddMinutes(5));
                File.WriteAllText(path, secret.Protect(Signed(command, key, BillingAuthority.CommandPurpose, BillingJson.Default.BillingCommand)));
            }
            return secret.Unprotect(File.ReadAllText(path));
        }
        BillingCommand ReadCommand(string signed) => StandardJoseSignatures.VerifyJson(signed, device.Public, BillingAuthority.CommandPurpose, 32768, BillingJson.Default.BillingCommand);
        BillingSnapshot Read(string signed) => StandardJoseSignatures.VerifyJson(signed, trusted, BillingAuthority.SnapshotPurpose, 65536, BillingJson.Default.BillingSnapshot);
    }

    private static string Signed<T>(T body, SigningRecipientIdentity device, string purpose, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type)
        => StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(body, type), device, purpose);
    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    {
        public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
        public Task<ServiceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
    private sealed class Clock : TimeProvider
    {
        internal static readonly DateTimeOffset Start = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        internal DateTimeOffset Now { get; set; } = Start;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Provider : IBillingProvider
    {
        public BillingIssuer Issuer => BillingIssuer.GooglePlay;
        internal string Binding { get; set; } = "";
        internal BillingPurchaseState State { get; set; } = BillingPurchaseState.Active;
        internal bool FailQuery { get; set; }
        internal bool FailAcknowledgement { get; set; }
        internal bool Acknowledged { get; set; }
        internal int QueryCount { get; private set; }
        internal Func<Task>? DuringQuery { get; set; }
        public async Task<BillingVerifiedPurchase> VerifyAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
        {
            QueryCount++; if (FailQuery) throw new BillingRefusal(503, "FictionalProviderUnavailable");
            if (DuringQuery is not null) await DuringQuery();
            return new(Issuer, environment, reference, Binding, new(BillingProduct.PlusMonthly, State,
                Clock.Start.AddDays(-1), Clock.Start.AddDays(20), null, State != BillingPurchaseState.Revoked, false,
                Acknowledged, "fictional-original-v1"), null, StoreProductId: "fictional.plus");
        }
        public Task AcknowledgeAsync(BillingVerifiedPurchase purchase, CancellationToken ct)
        { if (FailAcknowledgement) throw new BillingRefusal(503, "FictionalAcknowledgementUnavailable"); Acknowledged = true; return Task.CompletedTask; }
    }
}
