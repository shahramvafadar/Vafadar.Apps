using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>AT-298: actual owned PostgreSQL, original immutable intent and append-only credits; no real provider or purchase.</summary>
[Trait("AT", "AT-298")][Trait("Qualification", "AiLocal")]
public sealed class AiCreditPersistenceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    { public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options); }
    private sealed class Provider(string callsFile) : IAiProvider
    {
        internal AiProviderOutcome BeginOutcome = new(AiProviderState.Unknown);
        internal AiProviderOutcome StatusOutcome = new(AiProviderState.Unknown);
        internal bool LoseBeginResponse;
        internal long Units = 9;
        public AiProviderOffer? Offer(AiPurpose purpose) => new("fictitious-local", "fixture-v1", Units);
        public async Task<AiProviderOutcome> BeginAsync(string operation, AiOffer offer, AiPayload payload, CancellationToken ct)
        {
            var calls = File.Exists(callsFile) ? JsonSerializer.Deserialize<Dictionary<string, int>>(await File.ReadAllTextAsync(callsFile, ct))! : [];
            calls[operation] = calls.GetValueOrDefault(operation) + 1;
            await File.WriteAllTextAsync(callsFile, JsonSerializer.Serialize(calls), ct);
            if (calls[operation] != 1) throw new InvalidOperationException("Original provider operation was dispatched twice.");
            if (LoseBeginResponse) throw new TimeoutException("Fictitious lost response.");
            return BeginOutcome;
        }
        public Task<AiProviderOutcome> StatusAsync(string operation, AiOffer offer, CancellationToken ct) => Task.FromResult(StatusOutcome);
    }
    /// <summary>Resumes only unfinished original stages; completed credit qualification cannot be replayed or reset.</summary>
    [Fact]
    public async Task Original_credit_reservation_timeout_settlement_closure_and_cold_history_are_durable()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_QA_CONNECTION");
        if (string.IsNullOrEmpty(connection)) { Assert.Skip("Explicit isolated analysis PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55478, settings.Port); Assert.Equal("zanance_ai_d298", settings.Database);
        var root = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_AI_QA_ROOT") ?? throw new InvalidOperationException("Owned analysis root required."));
        Assert.Equal("Zanance fictional analysis d298", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        var complete = Path.Combine(root, "credit-store-complete.json"); Assert.False(File.Exists(complete), "Do not replay completed original credit stages.");
        var factory = new Factory(connection);
        await using (var schema = factory.CreateDbContext()) { await schema.Database.MigrateAsync(Ct); Assert.False(schema.Database.HasPendingModelChanges()); }
        var clockFile = Path.Combine(root, "credit-clock.json");
        var clock = new FakeTimeProvider(File.Exists(clockFile) ? JsonSerializer.Deserialize<DateTimeOffset>(await File.ReadAllTextAsync(clockFile, Ct)) : TimeProvider.System.GetUtcNow());
        async Task Tick() { clock.Advance(TimeSpan.FromSeconds(3)); await File.WriteAllTextAsync(clockFile, JsonSerializer.Serialize(clock.GetUtcNow()), Ct); }
        var stageFile = Path.Combine(root, "credit-stage.txt"); var stage = File.Exists(stageFile) ? int.Parse(await File.ReadAllTextAsync(stageFile, Ct), System.Globalization.CultureInfo.InvariantCulture) : 0;
        async Task Advance(int next) { await File.WriteAllTextAsync(stageFile, next.ToString(System.Globalization.CultureInfo.InvariantCulture), Ct); stage = next; }
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "credit-protection")));
        var provider = new Provider(Path.Combine(root, "credit-provider-calls.json")); var store = new AiRequestStore(factory, provider, protection, clock);
        const string owner = "fictitious-ai-owner-d298"; const string other = "fictitious-ai-other-d298";
        var idsPath = Path.Combine(root, "credit-session-ids.json");
        var ids = File.Exists(idsPath) ? JsonSerializer.Deserialize<Guid[]>(await File.ReadAllTextAsync(idsPath, Ct))! : new[] { Guid.NewGuid(), Guid.NewGuid() };
        if (!File.Exists(idsPath)) await File.WriteAllTextAsync(idsPath, JsonSerializer.Serialize(ids), Ct);
        await using (var db = factory.CreateDbContext())
        {
            foreach (var item in new[] { (owner, ids[0]), (other, ids[1]) })
                if (!await db.Users.AnyAsync(u => u.Id == item.Item1, Ct))
                {
                    db.Users.Add(new ServiceUser { Id = item.Item1, UserName = item.Item1, SecurityStamp = Guid.NewGuid().ToString("N") });
                    db.Sessions.Add(new DeviceSession { Id = item.Item2, UserId = item.Item1, Generation = 1, ExpiresAt = clock.GetUtcNow().AddDays(14) });
                }
            await db.SaveChangesAsync(Ct);
        }
        static ClaimsPrincipal Actor(string user, Guid id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user),
            new Claim(ServiceHost.SessionClaim, id.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1") }, "fictitious-original-session"));
        var actor = Actor(owner, ids[0]); var stranger = Actor(other, ids[1]);
        static AiPayload Payload() => new(1, AiPurpose.PeriodExplanation, "en", new(new(2026, 10, 1), new(2026, 10, 17), 1, true, [new("EUR", 10000, 0, 3000, 700)], 0), null);
        async Task<AiSubmission> Intent(string name)
        {
            var path = Path.Combine(root, name + "-original.json");
            if (File.Exists(path)) return JsonSerializer.Deserialize(await File.ReadAllTextAsync(path, Ct), AiJson.Default.AiSubmission)!;
            var value = new AiSubmission(Guid.NewGuid(), await store.OfferAsync(actor, AiPurpose.PeriodExplanation, Ct), Payload());
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, AiJson.Default.AiSubmission), Ct); return value;
        }
        if (stage < 1)
        {
            var closed = new AiRequestStore(factory, new UnconfiguredAiProvider(), protection, clock);
            var unavailable = await closed.AvailabilityAsync(actor, Ct); Assert.False(unavailable.Configured); Assert.Equal(new AiBalance(0, 0, 0, 0), unavailable.Balance);
            var refusal = await Assert.ThrowsAsync<AiRefusal>(() => closed.OfferAsync(actor, AiPurpose.PeriodExplanation, Ct)); Assert.Equal(503, refusal.Status);
            await store.GrantAsync(owner, "fictitious-original-purchased-receipt", true, 40, Ct);
            await store.GrantAsync(owner, "fictitious-original-gift", false, 6, Ct);
            await store.GrantAsync(owner, "fictitious-original-purchased-receipt", true, 40, Ct);
            Assert.Equal(new AiBalance(40, 6, 0, 0), (await store.AvailabilityAsync(actor, Ct)).Balance);
            await Assert.ThrowsAsync<AiRefusal>(() => store.GrantAsync(owner, "fictitious-original-purchased-receipt", true, 41, Ct));
            await Advance(1);
        }
        var lost = await Intent("lost-response");
        if (stage < 2)
        {
            provider.LoseBeginResponse = true; await Tick();
            var pending = await store.SubmitAsync(actor, lost, Ct); Assert.Equal(AiRequestState.Pending, pending.State);
            Assert.Equal(new AiBalance(37, 0, 3, 6), pending.Balance);
            await using var db = factory.CreateDbContext();
            Assert.Equal(1, await db.Set<AiCreditRow>().CountAsync(r => r.RequestId == lost.RequestId && r.Kind == AiCreditEvent.Reserve, Ct));
            var row = await db.Set<AiRequestRow>().SingleAsync(r => r.Id == lost.RequestId, Ct);
            Assert.DoesNotContain("CurrencyCode", row.ProtectedPayload); Assert.Equal(AiProtocol.Digest(lost.Payload), row.PayloadDigest);
            await Advance(2);
        }
        if (stage < 3)
        {
            await Tick(); Assert.Equal(AiRequestState.Pending, (await store.SubmitAsync(actor, lost, Ct)).State);
            provider.StatusOutcome = new(AiProviderState.Completed, 7, "Fictitious local explanation; no model or financial action.");
            await Tick(); var completed = await store.StatusAsync(actor, lost.RequestId, Ct);
            Assert.Equal(AiRequestState.Completed, completed.State); Assert.Equal(7, completed.ChargedUnits);
            Assert.Equal(new AiBalance(39, 0, 0, 0), completed.Balance);
            Assert.Equal(completed, await store.StatusAsync(actor, lost.RequestId, Ct));
            Assert.Equal(completed, await store.SubmitAsync(actor, lost, Ct));
            // A fresh store/protector reads the original terminal credit/text, not retained view-model/provider memory.
            var cold = new AiRequestStore(new Factory(connection), new UnconfiguredAiProvider(), DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "credit-protection"))), clock);
            Assert.Equal(completed, await cold.StatusAsync(actor, lost.RequestId, Ct));
            await Advance(3);
        }
        if (stage < 4)
        {
            var changed = lost with { Payload = lost.Payload with { Language = "de" } };
            Assert.Equal(409, (await Assert.ThrowsAsync<AiRefusal>(() => store.SubmitAsync(actor, changed, Ct))).Status);
            Assert.Equal(404, (await Assert.ThrowsAsync<AiRefusal>(() => store.StatusAsync(stranger, lost.RequestId, Ct))).Status);
            Assert.Equal(409, (await Assert.ThrowsAsync<AiRefusal>(() => store.GrantAsync(other, "fictitious-original-purchased-receipt", true, 40, Ct))).Status);
            Assert.Equal(new AiBalance(0, 0, 0, 0), (await store.AvailabilityAsync(stranger, Ct)).Balance);
            await using var db = factory.CreateDbContext();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"AiCredits\" SET \"PurchasedAvailable\" = 999 WHERE \"UserId\" = {owner}", Ct));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"AiCredits\" WHERE \"UserId\" = {owner}", Ct));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"AiRequests\" SET \"PayloadDigest\" = 'changed' WHERE \"Id\" = {lost.RequestId}", Ct));
            Assert.Equal(new AiBalance(39, 0, 0, 0), (await store.AvailabilityAsync(actor, Ct)).Balance); await Advance(4);
        }
        if (stage < 5)
        {
            var unsent = await Intent("never-sent"); var closed = await store.CloseAsync(actor, unsent.RequestId, new(unsent.Offer, AiProtocol.Digest(unsent.Payload)), Ct);
            Assert.Equal(AiRequestState.NotAccepted, closed.State); Assert.Equal(closed, await store.SubmitAsync(actor, unsent, Ct));
            await using var db = factory.CreateDbContext(); Assert.False(await db.Set<AiCreditRow>().AnyAsync(r => r.RequestId == unsent.RequestId, Ct));
            await Advance(5);
        }
        if (stage < 6)
        {
            provider.LoseBeginResponse = false; provider.BeginOutcome = new(AiProviderState.DefinitiveNoCharge);
            provider.StatusOutcome = new(AiProviderState.DefinitiveNoCharge); await Tick();
            var failure = await Intent("definitive-no-charge"); var refunded = await store.SubmitAsync(actor, failure, Ct);
            Assert.Equal(AiRequestState.Refunded, refunded.State); Assert.Equal(new AiBalance(39, 0, 0, 0), refunded.Balance);
            Assert.Equal(refunded, await store.SubmitAsync(actor, failure, Ct)); await Advance(6);
        }
        if (stage < 7)
        {
            provider.Units = 50; var insufficient = await Intent("insufficient"); var declined = await store.SubmitAsync(actor, insufficient, Ct);
            Assert.Equal(AiRequestState.NotAccepted, declined.State);
            await store.GrantAsync(owner, "fictitious-later-credit", true, 11, Ct);
            Assert.Equal(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, insufficient, Ct)).State);
            Assert.Equal(new AiBalance(50, 0, 0, 0), (await store.AvailabilityAsync(actor, Ct)).Balance); await Advance(7);
        }
        if (stage < 8)
        {
            await using var db = factory.CreateDbContext();
            await db.Sessions.Where(r => r.Id == ids[1]).ExecuteUpdateAsync(update => update.SetProperty(r => r.Revoked, true), Ct);
            Assert.Equal(401, (await Assert.ThrowsAsync<AiRefusal>(() => store.AvailabilityAsync(stranger, Ct))).Status);
            Assert.Equal(1, await db.Set<AiCreditRow>().CountAsync(r => r.RequestId == lost.RequestId && r.Kind == AiCreditEvent.Settle, Ct));
            Assert.Equal(1, await db.Set<AiCreditRow>().CountAsync(r => r.RequestId == lost.RequestId && r.Kind == AiCreditEvent.Refund, Ct));
            await Advance(8);
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Stages = stage, PurchasedAvailable = 50, GiftedAvailable = 0,
            RealPostgreSql = true, ProviderCalls = "fictitious unique original operations only", ModelOrPurchase = false }), Ct);
    }
}
