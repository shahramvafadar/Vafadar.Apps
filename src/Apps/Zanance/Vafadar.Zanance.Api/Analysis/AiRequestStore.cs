using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>Actual identity-scoped append-only credits and once-only dispatch; independent from every financial ledger.</summary>
internal sealed class AiRequestStore(IDbContextFactory<ServiceDbContext> factory, IAiProvider provider,
    IDataProtectionProvider protection, TimeProvider time)
{
    private const long MaximumUnits = 1_000_000_000_000;
    /// <summary>Reads local provider metadata/balance without transmitting a financial payload or granting credit.</summary>
    internal async Task<AiAvailability> AvailabilityAsync(ClaimsPrincipal actor, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var scope = await OpenAsync(actor, db, ct);
        var offer = provider.Offer(AiPurpose.PeriodExplanation); if (offer is not null) ValidateOffer(offer);
        var balance = await BalanceAsync(db, scope.Owner, ct); await scope.CommitAsync(ct);
        return new(offer is not null, offer?.Provider, offer?.Model, balance);
    }
    /// <summary>Issues metadata-only original consent terms, without receiving or forwarding personal financial content.</summary>
    internal async Task<AiOffer> OfferAsync(ClaimsPrincipal actor, AiPurpose purpose, CancellationToken ct)
    {
        if (!Enum.IsDefined(purpose)) throw new AiRefusal(422, "AiPurposeRefused");
        var original = provider.Offer(purpose) ?? throw new AiRefusal(503, "AiProviderNotConfigured"); ValidateOffer(original);
        await using var db = await factory.CreateDbContextAsync(ct); await using var scope = await OpenAsync(actor, db, ct);
        if (await db.Set<AiOfferRow>().CountAsync(r => r.UserId == scope.Owner && r.ExpiresAt > scope.Now, ct) >= 32)
            throw new AiRefusal(429, "AiOfferBound");
        var row = new AiOfferRow { Id = Guid.NewGuid(), UserId = scope.Owner, Provider = original.Provider, Model = original.Model,
            Purpose = purpose, MaximumUnits = original.MaximumUnits, ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(scope.Now.ToUnixTimeMilliseconds()).AddMinutes(5) };
        db.Add(row); await scope.CommitAsync(ct); return Offer(row);
    }
    /// <summary>Persists literal consent/reservation before one original dispatch; existing ids query status only.</summary>
    internal async Task<AiRequestResult> SubmitAsync(ClaimsPrincipal actor, AiSubmission submission, CancellationToken ct)
    {
        if (submission.RequestId == Guid.Empty || submission.Offer is null || submission.Payload is null) throw new AiRefusal(422, "AiConsentRefused");
        // Freeze the literal aggregate before the first await; mutable caller lists never alter accepted consent.
        var encoded = JsonSerializer.SerializeToUtf8Bytes(submission.Payload, AiJson.Default.AiPayload);
        if (encoded.Length > AiProtocol.MaximumPayloadBytes) throw new AiRefusal(413, "AiPayloadBound");
        var payload = JsonSerializer.Deserialize(encoded, AiJson.Default.AiPayload) ?? throw new AiRefusal(422, "AiConsentRefused");
        var digest = AiProtocol.Digest(payload); string owner; bool dispatch;
        await using (var db = await factory.CreateDbContextAsync(ct))
        await using (var scope = await OpenAsync(actor, db, ct))
        {
            owner = scope.Owner;
            var existing = await db.Set<AiRequestRow>().SingleOrDefaultAsync(r => r.Id == submission.RequestId, ct);
            if (existing is not null)
            {
                if (existing.UserId != owner) throw new AiRefusal(404, "AiRequestUnavailable");
                if (existing.OfferId != submission.Offer.Id || existing.PayloadDigest != digest ||
                    !SameOffer(await db.Set<AiOfferRow>().SingleAsync(r => r.Id == existing.OfferId, ct), submission.Offer))
                    throw new AiRefusal(409, "AiOriginalConsentChanged");
                dispatch = false;
            }
            else
            {
                var row = await db.Set<AiOfferRow>().SingleOrDefaultAsync(r => r.Id == submission.Offer.Id && r.UserId == owner, ct)
                    ?? throw new AiRefusal(404, "AiOfferUnavailable");
                if (!SameOffer(row, submission.Offer) || row.Purpose != payload.Purpose)
                    throw new AiRefusal(409, "AiOfferChanged");
                if (await db.Set<AiRequestRow>().AnyAsync(r => r.OfferId == row.Id, ct)) throw new AiRefusal(409, "AiOfferAlreadyUsed");
                var actual = provider.Offer(row.Purpose); if (actual is not null) ValidateOffer(actual);
                var balance = await BalanceAsync(db, owner, ct);
                var accepted = row.ExpiresAt > scope.Now && actual is not null && actual.Provider == row.Provider &&
                    actual.Model == row.Model && actual.MaximumUnits == row.MaximumUnits &&
                    checked(balance.PurchasedAvailable + balance.GiftedAvailable) >= row.MaximumUnits;
                if (accepted && await db.Set<AiRequestRow>().CountAsync(r => r.UserId == owner && r.State == AiRequestState.Pending, ct) >= 16)
                    throw new AiRefusal(429, "AiPendingBound");
                // A definitive unaccepted closure is durable too: late packets cannot charge after a later new consent.
                var gifted = accepted ? Math.Min(balance.GiftedAvailable, row.MaximumUnits) : 0;
                var purchased = accepted ? row.MaximumUnits - gifted : 0;
                db.Add(new AiRequestRow { Id = submission.RequestId, UserId = owner, OfferId = row.Id, PayloadDigest = digest,
                    ProtectedPayload = Protector(owner, submission.RequestId).Protect(JsonSerializer.Serialize(payload, AiJson.Default.AiPayload)),
                    PurchasedReserved = purchased, GiftedReserved = gifted, State = accepted ? AiRequestState.Pending : AiRequestState.NotAccepted, CreatedAt = scope.Now });
                if (accepted) db.Add(Event(owner, submission.RequestId, AiCreditEvent.Reserve, -purchased, -gifted, purchased, gifted, scope.Now));
                dispatch = accepted;
            }
            await scope.CommitAsync(ct);
        }
        if (!dispatch) return await StatusAsync(actor, submission.RequestId, ct);
        // No database transaction/owner lock is held across provider I/O. Crash/timeout never permits a second Begin.
        AiProviderOutcome? outcome = null;
        try { outcome = await provider.BeginAsync(ProviderKey(owner, submission.RequestId), submission.Offer, payload, ct); }
        catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or TimeoutException) { }
        if (outcome is not null) await ApplyAsync(owner, submission.RequestId, outcome, CancellationToken.None);
        return await ReadAsync(actor, submission.RequestId, ct);
    }
    /// <summary>Resolves only the original pending id; a missing provider lookup is never a refund or another dispatch.</summary>
    internal async Task<AiRequestResult> StatusAsync(ClaimsPrincipal actor, Guid request, CancellationToken ct)
    {
        string owner; AiOffer? offer = null;
        await using (var db = await factory.CreateDbContextAsync(ct))
        await using (var scope = await OpenAsync(actor, db, ct))
        {
            owner = scope.Owner; var row = await db.Set<AiRequestRow>().SingleOrDefaultAsync(r => r.Id == request && r.UserId == owner, ct)
                ?? throw new AiRefusal(404, "AiRequestUnavailable");
            if (row.State == AiRequestState.Pending)
            {
                if (row.StatusCheckedAt is { } last && scope.Now < last.AddSeconds(2)) throw new AiRefusal(429, "AiStatusTooSoon");
                row.StatusCheckedAt = scope.Now; offer = Offer(await db.Set<AiOfferRow>().SingleAsync(r => r.Id == row.OfferId, ct));
            }
            await scope.CommitAsync(ct);
        }
        if (offer is not null)
        {
            AiProviderOutcome? outcome = null;
            try { outcome = await provider.StatusAsync(ProviderKey(owner, request), offer, ct); }
            catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or TimeoutException) { }
            if (outcome is not null) await ApplyAsync(owner, request, outcome, CancellationToken.None);
        }
        return await ReadAsync(actor, request, ct);
    }
    /// <summary>Closes an absent original id durably before a late packet can reserve; accepted work is never guessed canceled/refunded.</summary>
    internal async Task<AiRequestResult> CloseAsync(ClaimsPrincipal actor, Guid id, AiCloseRequest original, CancellationToken ct)
    {
        if (id == Guid.Empty || original.Offer is null || original.PayloadDigest is not { Length: 64 } digest || !digest.All(char.IsAsciiHexDigit))
            throw new AiRefusal(422, "AiOriginalConsentChanged");
        await using (var db = await factory.CreateDbContextAsync(ct))
        await using (var scope = await OpenAsync(actor, db, ct))
        {
            var row = await db.Set<AiRequestRow>().SingleOrDefaultAsync(r => r.Id == id, ct);
            if (row is not null)
            {
                if (row.UserId != scope.Owner) throw new AiRefusal(404, "AiRequestUnavailable");
                if (row.OfferId != original.Offer.Id || row.PayloadDigest != digest || !SameOffer(await db.Set<AiOfferRow>().SingleAsync(r => r.Id == row.OfferId, ct), original.Offer)) throw new AiRefusal(409, "AiOriginalConsentChanged");
            }
            else
            {
                var offer = await db.Set<AiOfferRow>().SingleOrDefaultAsync(r => r.Id == original.Offer.Id && r.UserId == scope.Owner, ct)
                    ?? throw new AiRefusal(404, "AiOfferUnavailable");
                if (!SameOffer(offer, original.Offer) || await db.Set<AiRequestRow>().AnyAsync(r => r.OfferId == offer.Id, ct))
                    throw new AiRefusal(409, "AiOriginalConsentChanged");
                db.Add(new AiRequestRow { Id = id, UserId = scope.Owner, OfferId = offer.Id, PayloadDigest = digest,
                    ProtectedPayload = Protector(scope.Owner, id).Protect(""), State = AiRequestState.NotAccepted, CreatedAt = scope.Now });
            }
            await scope.CommitAsync(ct);
        }
        return await ReadAsync(actor, id, ct);
    }
    private async Task ApplyAsync(string owner, Guid id, AiProviderOutcome outcome, CancellationToken ct)
    {
        if (outcome.State == AiProviderState.Unknown) return;
        if (!Enum.IsDefined(outcome.State)) return;
        // Unusable or inconsistent provider evidence leaves the original reservation intact for actual reconciliation.
        if (outcome.ChargedUnits < 0 || outcome.State == AiProviderState.DefinitiveNoCharge && (outcome.ChargedUnits != 0 || outcome.Text is not null)) return;
        if (outcome.State == AiProviderState.Completed)
        { try { AiProtocol.ValidateText(outcome.Text ?? ""); } catch (ArgumentException) { return; } }
        await using var db = await factory.CreateDbContextAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await SyncRequestScope.LockOwnerAsync(db, owner, ct);
        var row = await db.Set<AiRequestRow>().SingleAsync(r => r.Id == id && r.UserId == owner, ct);
        if (row.State != AiRequestState.Pending) return;
        var maximum = checked(row.PurchasedReserved + row.GiftedReserved); if (outcome.ChargedUnits > maximum) return;
        var giftCost = Math.Min(row.GiftedReserved, outcome.ChargedUnits); var purchaseCost = outcome.ChargedUnits - giftCost;
        var giftRefund = row.GiftedReserved - giftCost; var purchaseRefund = row.PurchasedReserved - purchaseCost;
        var now = time.GetUtcNow();
        if (outcome.State == AiProviderState.Completed)
        {
            db.Add(Event(owner, id, AiCreditEvent.Settle, 0, 0, -purchaseCost, -giftCost, now));
            row.ProtectedText = Protector(owner, id).Protect(outcome.Text!); row.State = AiRequestState.Completed; row.ChargedUnits = outcome.ChargedUnits;
        }
        else row.State = AiRequestState.Refunded;
        if (giftRefund != 0 || purchaseRefund != 0)
            db.Add(Event(owner, id, AiCreditEvent.Refund, purchaseRefund, giftRefund, -purchaseRefund, -giftRefund, now));
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    private async Task<AiRequestResult> ReadAsync(ClaimsPrincipal actor, Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var scope = await OpenAsync(actor, db, ct);
        var row = await db.Set<AiRequestRow>().SingleOrDefaultAsync(r => r.Id == id && r.UserId == scope.Owner, ct)
            ?? throw new AiRefusal(404, "AiRequestUnavailable");
        var balance = await BalanceAsync(db, scope.Owner, ct);
        var text = row.ProtectedText is null ? null : Protector(scope.Owner, id).Unprotect(row.ProtectedText);
        var result = new AiRequestResult(id, row.PayloadDigest, row.State, row.ChargedUnits, text, balance); await scope.CommitAsync(ct); return result;
    }
    /// <summary>Trusted internal receipt/gift ingress only; no route or client DTO can issue credit or choose package units.</summary>
    internal async Task GrantAsync(string owner, string originalGrant, bool purchased, long units, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(owner) || owner.Length > 256 || originalGrant.Length is < 1 or > 256 || units is <= 0 or > MaximumUnits)
            throw new ArgumentException("Trusted original grant required.");
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(originalGrant)));
        await using var db = await factory.CreateDbContextAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await SyncRequestScope.LockOwnerAsync(db, owner, ct);
        var receiptPartition = "zanance.analysis.grant:" + key;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({receiptPartition}, 8))", ct);
        var existing = await db.Set<AiCreditRow>().SingleOrDefaultAsync(r => r.GrantKey == key, ct);
        var kind = purchased ? AiCreditEvent.PurchasedGrant : AiCreditEvent.GiftedGrant;
        if (existing is not null)
        {
            if (existing.UserId != owner || existing.Kind != kind || existing.PurchasedAvailable != (purchased ? units : 0) || existing.GiftedAvailable != (purchased ? 0 : units))
                throw new AiRefusal(409, "AiOriginalGrantChanged");
            return;
        }
        var balance = await BalanceAsync(db, owner, ct);
        if (checked((purchased ? balance.PurchasedAvailable + balance.PurchasedReserved : balance.GiftedAvailable + balance.GiftedReserved) + units) > MaximumUnits)
            throw new AiRefusal(409, "AiWalletBound");
        var row = Event(owner, null, kind, purchased ? units : 0, purchased ? 0 : units, 0, 0, time.GetUtcNow()); row.GrantKey = key;
        db.Add(row); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    private async Task<SyncRequestScope> OpenAsync(ClaimsPrincipal actor, ServiceDbContext db, CancellationToken ct) =>
        await SyncRequestScope.OpenAsync(actor, db, time, ct) ?? throw new AiRefusal(401, "AiIdentityRequired");
    // Stable per-identity operation namespace also survives ordinary service restoration; providers must prove idempotence.
    private static string ProviderKey(string owner, Guid id) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("zanance.analysis.v1\n" + owner + "\n" + id.ToString("N"))));
    private IDataProtector Protector(string owner, Guid id) => protection.CreateProtector("zanance.analysis.v1", owner, id.ToString("N"));
    // PostgreSQL stores microseconds; original in-flight pre-canonicalization offers may have one finer tick.
    // This comparison never extends the stored server deadline or changes destination/model/cost consent.
    private static bool SameOffer(AiOfferRow row, AiOffer original) => row.Id == original.Id && row.Provider == original.Provider &&
        row.Model == original.Model && row.Purpose == original.Purpose && row.MaximumUnits == original.MaximumUnits &&
        row.ExpiresAt.UtcTicks / 10 == original.ExpiresAt.UtcTicks / 10;
    private static AiOffer Offer(AiOfferRow row) => new(row.Id, row.Provider, row.Model, row.Purpose, row.MaximumUnits, row.ExpiresAt);
    private static void ValidateOffer(AiProviderOffer offer)
    {
        static bool Name(string name) => name.Length is > 0 and <= 64 && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_');
        if (!Name(offer.Provider) || !Name(offer.Model) || offer.MaximumUnits is <= 0 or > MaximumUnits) throw new AiRefusal(503, "AiProviderNotConfigured");
    }
    private static AiCreditRow Event(string owner, Guid? request, AiCreditEvent kind, long purchased, long gifted, long purchasedReserved, long giftedReserved, DateTimeOffset now)
        => new() { Id = Guid.NewGuid(), UserId = owner, RequestId = request, Kind = kind, PurchasedAvailable = purchased, GiftedAvailable = gifted,
            PurchasedReserved = purchasedReserved, GiftedReserved = giftedReserved, CreatedAt = now };
    private static async Task<AiBalance> BalanceAsync(ServiceDbContext db, string owner, CancellationToken ct)
    {
        var balance = await db.Set<AiCreditRow>().Where(r => r.UserId == owner).GroupBy(r => r.UserId)
            .Select(g => new AiBalance(g.Sum(r => r.PurchasedAvailable), g.Sum(r => r.GiftedAvailable), g.Sum(r => r.PurchasedReserved), g.Sum(r => r.GiftedReserved)))
            .SingleOrDefaultAsync(ct) ?? new(0, 0, 0, 0);
        if (balance.PurchasedAvailable < 0 || balance.GiftedAvailable < 0 || balance.PurchasedReserved < 0 || balance.GiftedReserved < 0)
            throw new AiRefusal(503, "AiWalletStateUnavailable");
        return balance;
    }
}
