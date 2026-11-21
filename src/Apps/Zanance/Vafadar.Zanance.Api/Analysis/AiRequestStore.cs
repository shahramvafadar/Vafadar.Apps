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
    IDataProtectionProvider protection, TimeProvider time, AiPilotProvider? pilot = null)
{
    internal const int MaximumOperatorChecks = 3;
    private static readonly TimeSpan OperatorStatusInterval = TimeSpan.FromMinutes(1);
    private const long MaximumUnits = 1_000_000_000_000;
    private const int PilotDispatchBatchSize = 40;
    private sealed record PilotDispatchCursor(DateTimeOffset CreatedAt, Guid Id);
    private PilotDispatchCursor? pilotDispatchCursor;
    /// <summary>Reads local provider metadata/balance without transmitting a financial payload or granting credit.</summary>
    internal async Task<AiAvailability> AvailabilityAsync(ClaimsPrincipal actor, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var scope = await OpenAsync(actor, db, ct);
        if (await DeletionPendingAsync(db, scope.Owner, ct))
            return new(false, null, null, await BalanceAsync(db, scope.Owner, ct), false);
        var offer = pilot is null ? provider.Offer(AiPurpose.PeriodExplanation)
            : (await pilot.OfferAsync(db, AiPurpose.PeriodExplanation, ct))?.Offer;
        if (offer is not null) ValidateOffer(offer);
        var comparison = pilot is null ? provider.Offer(AiPurpose.PeriodComparison)
            : (await pilot.OfferAsync(db, AiPurpose.PeriodComparison, ct))?.Offer;
        if (comparison is not null) ValidateOffer(comparison);
        var balance = await BalanceAsync(db, scope.Owner, ct); await scope.CommitAsync(ct);
        return new(offer is not null, offer?.Provider, offer?.Model, balance, comparison is not null);
    }
    /// <summary>Issues metadata-only original consent terms, without receiving or forwarding personal financial content.</summary>
    internal async Task<AiOffer> OfferAsync(ClaimsPrincipal actor, AiPurpose purpose, CancellationToken ct)
    {
        if (!Enum.IsDefined(purpose)) throw new AiRefusal(422, "AiPurposeRefused");
        await using var db = await factory.CreateDbContextAsync(ct); await using var scope = await OpenAsync(actor, db, ct);
        await RequireNewWorkAsync(db, scope.Owner, ct);
        if (pilot is not null) await AiPilotProvider.LockAsync(db, ct);
        var selection = pilot is null ? null : await pilot.OfferAsync(db, purpose, ct);
        var original = (pilot is null ? provider.Offer(purpose) : selection?.Offer) ?? throw new AiRefusal(503, "AiProviderNotConfigured");
        ValidateOffer(original);
        if (await db.Set<AiOfferRow>().CountAsync(r => r.UserId == scope.Owner && r.ExpiresAt > scope.Now, ct) >= 32)
            throw new AiRefusal(429, "AiOfferBound");
        var row = new AiOfferRow { Id = Guid.NewGuid(), UserId = scope.Owner, Provider = original.Provider, Model = original.Model,
            Purpose = purpose, MaximumUnits = original.MaximumUnits, PilotRouteId = selection?.RouteId, ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(scope.Now.ToUnixTimeMilliseconds()).AddMinutes(5) };
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
                await RequireNewWorkAsync(db, owner, ct);
                var row = await db.Set<AiOfferRow>().SingleOrDefaultAsync(r => r.Id == submission.Offer.Id && r.UserId == owner, ct)
                    ?? throw new AiRefusal(404, "AiOfferUnavailable");
                if (!SameOffer(row, submission.Offer) || row.Purpose != payload.Purpose)
                    throw new AiRefusal(409, "AiOfferChanged");
                if (await db.Set<AiRequestRow>().AnyAsync(r => r.OfferId == row.Id, ct)) throw new AiRefusal(409, "AiOfferAlreadyUsed");
                if (pilot is not null) await AiPilotProvider.LockAsync(db, ct);
                var selected = pilot is null ? null : await pilot.OfferAsync(db, row.Purpose, ct);
                var actual = pilot is null ? provider.Offer(row.Purpose) : selected?.Offer;
                if (actual is not null) ValidateOffer(actual);
                var balance = await BalanceAsync(db, owner, ct);
                var accepted = row.ExpiresAt > scope.Now && actual is not null && actual.Provider == row.Provider &&
                    actual.Model == row.Model && actual.MaximumUnits == row.MaximumUnits && row.PilotRouteId == selected?.RouteId &&
                    (pilot is null ? checked(balance.PurchasedAvailable + balance.GiftedAvailable) >= row.MaximumUnits
                        : AiPilotProvider.IsSynthetic(row.Purpose, digest) && balance.GiftedAvailable >= row.MaximumUnits
                            && !await pilot.AtCapacityAsync(db, row.Provider, ct));
                long? pilotCostCeiling = null;
                if (accepted && selected is { } route)
                {
                    var admittedRoute = await db.Set<AiPilotRouteRow>().SingleAsync(r => r.Id == route.RouteId, ct);
                    pilotCostCeiling = await pilot!.AvailableCostCeilingAsync(db, owner, admittedRoute, submission.Offer, ct);
                    accepted = pilotCostCeiling is not null;
                }
                if (accepted && await db.Set<AiRequestRow>().CountAsync(r => r.UserId == owner && r.State == AiRequestState.Pending, ct) >= 16)
                    throw new AiRefusal(429, "AiPendingBound");
                // A definitive unaccepted closure is durable too: late packets cannot charge after a later new consent.
                var gifted = accepted ? Math.Min(balance.GiftedAvailable, row.MaximumUnits) : 0;
                var purchased = accepted ? row.MaximumUnits - gifted : 0;
                db.Add(new AiRequestRow { Id = submission.RequestId, UserId = owner, OfferId = row.Id, PayloadDigest = digest,
                    ProtectedPayload = Protector(owner, submission.RequestId).Protect(JsonSerializer.Serialize(payload, AiJson.Default.AiPayload)),
                    PurchasedReserved = purchased, GiftedReserved = gifted, PilotCostCeilingMicros = pilotCostCeiling,
                    PilotDispatchStage = pilotCostCeiling.HasValue ? AiPilotDispatchStage.Queued : null,
                    State = accepted ? AiRequestState.Pending : AiRequestState.NotAccepted, CreatedAt = scope.Now });
                if (accepted) db.Add(Event(owner, submission.RequestId, AiCreditEvent.Reserve, -purchased, -gifted, purchased, gifted, scope.Now));
                dispatch = accepted;
            }
            await scope.CommitAsync(ct);
        }
        if (!dispatch) return await StatusAsync(actor, submission.RequestId, ct);
        if (pilot is not null)
        {
            await DispatchPilotAsync(submission.RequestId, ct);
            return await ReadAsync(actor, submission.RequestId, ct);
        }
        // No database transaction/owner lock is held across provider I/O. Crash/timeout never permits a second Begin.
        AiProviderOutcome? outcome = null;
        try
        {
            var operation = ProviderKey(owner, submission.RequestId);
            outcome = await provider.BeginAsync(operation, submission.Offer, payload, ct);
        }
        catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or TimeoutException) { }
        if (outcome is not null) await ApplyAsync(owner, submission.RequestId, outcome, CancellationToken.None);
        return await ReadAsync(actor, submission.RequestId, ct);
    }
    /// <summary>Continues only synthetic requests committed before any process claimed their create.</summary>
    internal async Task DrainPilotDispatchesAsync(CancellationToken ct)
    {
        if (pilot is null) return;
        PilotDispatchCursor[] pending;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var queue = db.Set<AiRequestRow>().Where(row => row.PilotDispatchStage == AiPilotDispatchStage.Queued &&
                    row.State == AiRequestState.Pending && row.VendorOperationId == null &&
                    !db.AccountDeletionRequests.Any(deletion => deletion.UserId == row.UserId));
            if (pilotDispatchCursor is { } cursor)
                queue = queue.Where(row => row.CreatedAt > cursor.CreatedAt ||
                    (row.CreatedAt == cursor.CreatedAt && row.Id.CompareTo(cursor.Id) > 0));
            pending = await queue.OrderBy(row => row.CreatedAt).ThenBy(row => row.Id)
                .Select(row => new PilotDispatchCursor(row.CreatedAt, row.Id)).Take(PilotDispatchBatchSize).ToArrayAsync(ct);
        }
        foreach (var candidate in pending)
        {
            // Temporarily unavailable original routes remain queued but must not starve later work.
            pilotDispatchCursor = candidate;
            await DispatchPilotAsync(candidate.Id, ct);
        }
        if (pending.Length < PilotDispatchBatchSize) pilotDispatchCursor = null;
    }

    private async Task DispatchPilotAsync(Guid request, CancellationToken ct)
    {
        if (pilot is null) return;
        string owner, digest;
        AiOffer original;
        AiPilotRouteRow route;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            // Read only the original dispatch metadata. The pilot maps the allowlisted digest to its own fixed synthetic input.
            var pending = db.Set<AiRequestRow>().Where(row => row.Id == request && row.State == AiRequestState.Pending &&
                row.PilotDispatchStage == AiPilotDispatchStage.Queued && row.VendorOperationId == null);
            var originalOwner = await pending.Select(row => row.UserId).SingleOrDefaultAsync(ct);
            if (originalOwner is null) return;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await SyncRequestScope.LockOwnerAsync(db, originalOwner, ct);
            if (await DeletionPendingAsync(db, originalOwner, ct)) return;
            pending = pending.Where(row => row.UserId == originalOwner);
            var metadata = await pending.Select(row => new { row.PayloadDigest, row.OfferId }).SingleOrDefaultAsync(ct);
            if (metadata is null) return;
            var offer = await db.Set<AiOfferRow>().AsNoTracking().SingleAsync(row => row.Id == metadata.OfferId, ct);
            if (offer.PilotRouteId is not { } routeId) return;
            route = await db.Set<AiPilotRouteRow>().AsNoTracking().SingleAsync(row => row.Id == routeId, ct);
            original = Offer(offer);
            digest = metadata.PayloadDigest;
            owner = originalOwner;
            if (!pilot.CanBegin(route, original, digest)) return;
            // The claim and pending account deletion share the owner lock. A committed claim is the one original boundary.
            var now = time.GetUtcNow();
            var claimed = await pending.ExecuteUpdateAsync(set => set
                .SetProperty(row => row.PilotDispatchStage, AiPilotDispatchStage.Claimed)
                .SetProperty(row => row.PilotDispatchClaimedAt, now), ct);
            if (claimed != 1) return;
            await transaction.CommitAsync(ct);
        }

        AiProviderOutcome? outcome = null;
        try
        {
            outcome = await pilot.BeginAsync(route, original, ProviderKey(owner, request), digest,
                handle => SaveVendorHandleAsync(owner, request, handle), ct);
        }
        catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or TimeoutException) { }
        if (outcome is not null) await ApplyAsync(owner, request, outcome, CancellationToken.None);
    }

    private static Task<bool> DeletionPendingAsync(ServiceDbContext db, string owner, CancellationToken ct) =>
        db.AccountDeletionRequests.AnyAsync(row => row.UserId == owner, ct);

    private static async Task RequireNewWorkAsync(ServiceDbContext db, string owner, CancellationToken ct)
    {
        if (await DeletionPendingAsync(db, owner, ct)) throw new AiRefusal(409, "AiAccountDeletionPending");
    }

    /// <summary>Binds an authenticated retrieval to one exact original, already claimed synthetic operation.</summary>
    internal async Task<bool> RecoverPilotNoticeAsync(string providerName, string responseId, CancellationToken ct)
    {
        if (pilot is null) return true;
        await using var db = await factory.CreateDbContextAsync(ct);
        var requests = db.Set<AiRequestRow>();
        var offers = db.Set<AiOfferRow>().Where(item => item.Provider == providerName && item.PilotRouteId != null);
        var known = await (from request in requests.AsNoTracking()
                           join offer in offers.AsNoTracking() on request.OfferId equals offer.Id
                           where request.VendorOperationId == responseId
                           select new { Request = request, Offer = offer }).Take(2).ToArrayAsync(ct);
        if (known.Length > 1) return false;
        if (known.Length == 1)
        {
            var originalRequest = known[0].Request;
            if (originalRequest.State != AiRequestState.Pending || originalRequest.PilotVendorStatus.HasValue) return true;
            var frozenOffer = known[0].Offer;
            var originalRoute = await db.Set<AiPilotRouteRow>().AsNoTracking().SingleAsync(item => item.Id == frozenOffer.PilotRouteId, ct);
            var observed = await pilot.StatusAsync(originalRoute, Offer(frozenOffer),
                ProviderKey(originalRequest.UserId, originalRequest.Id), originalRequest.PayloadDigest, responseId, ct);
            if (!observed.VendorStatus.HasValue) return false;
            await ApplyAsync(originalRequest.UserId, originalRequest.Id, observed, CancellationToken.None);
            return true;
        }
        static IQueryable<AiRequestRow> Unbound(IQueryable<AiRequestRow> rows) => rows.AsNoTracking().Where(request =>
            request.State == AiRequestState.Pending && request.PilotDispatchStage == AiPilotDispatchStage.Claimed &&
            request.VendorOperationId == null);

        if (!await Unbound(requests).AnyAsync(request => offers.Any(offer => offer.Id == request.OfferId), ct)) return true;
        using var recovered = await pilot.RetrieveNoticeAsync(providerName, responseId, ct);
        if (recovered is null) return false;
        var value = recovered.RootElement;
        var operation = AiPilotProvider.RecoveredOperation(providerName, value, responseId);
        if (operation is not { Length: 64 } || !operation.All(char.IsAsciiHexDigitLower)) return false;

        // Use the same UTF-8 format as dispatch and return only exact, eligible matches.
        var correlated = requests.FromSql($"""
            SELECT * FROM "AiRequests"
            WHERE encode(sha256(convert_to({ProviderKeyPrefix} || "UserId" || {"\n"} || replace("Id"::text, '-', ''), 'UTF8')), 'hex') = {operation}
            """);
        var matches = await (from request in Unbound(correlated)
                             join offer in offers.AsNoTracking() on request.OfferId equals offer.Id
                             select new { request.Id, request.UserId, request.PayloadDigest, Offer = offer }).Take(2).ToArrayAsync(ct);
        if (matches.Length == 0) return true;
        if (matches.Length != 1) return false;
        var original = matches[0];
        var route = await db.Set<AiPilotRouteRow>().AsNoTracking().SingleAsync(item => item.Id == original.Offer.PilotRouteId, ct);
        var outcome = pilot.RecoveredNoticeOutcome(route, Offer(original.Offer), original.PayloadDigest, value);
        if (outcome is null) return true;
        // Binding and applying the same authenticated observation must survive or roll back together.
        await ApplyAsync(original.UserId, original.Id, outcome, CancellationToken.None, recoveredHandle: responseId);
        return true;
    }

    /// <summary>Resolves only the original pending id; a missing provider lookup is never a refund or another dispatch.</summary>
    internal async Task<AiRequestResult> StatusAsync(ClaimsPrincipal actor, Guid request, CancellationToken ct)
    {
        string owner; AiOffer? offer = null; long? pilotRouteId = null; string? vendorId = null; string? originalDigest = null;
        AiPilotRouteRow? originalRoute = null;
        await using (var db = await factory.CreateDbContextAsync(ct))
        await using (var scope = await OpenAsync(actor, db, ct))
        {
            owner = scope.Owner; var row = await db.Set<AiRequestRow>().SingleOrDefaultAsync(r => r.Id == request && r.UserId == owner, ct)
                ?? throw new AiRefusal(404, "AiRequestUnavailable");
            if (row.State == AiRequestState.Pending)
            {
                if (row.StatusCheckedAt is { } last && scope.Now < last.AddSeconds(2)) throw new AiRefusal(429, "AiStatusTooSoon");
                row.StatusCheckedAt = scope.Now;
                var original = await db.Set<AiOfferRow>().SingleAsync(r => r.Id == row.OfferId, ct);
                offer = Offer(original);
                pilotRouteId = original.PilotRouteId;
                vendorId = row.VendorOperationId;
                originalDigest = row.PayloadDigest;
                if (pilot is not null && pilotRouteId is { } routeId)
                    originalRoute = await db.Set<AiPilotRouteRow>().SingleAsync(r => r.Id == routeId, ct);
            }
            await scope.CommitAsync(ct);
        }
        if (offer is not null)
        {
            AiProviderOutcome? outcome = null;
            try
            {
                if (pilotRouteId is null)
                    outcome = await provider.StatusAsync(ProviderKey(owner, request), offer, ct);
                else if (originalRoute is not null && originalDigest is not null)
                    outcome = await pilot!.StatusAsync(originalRoute, offer, ProviderKey(owner, request), originalDigest, vendorId, ct);
            }
            catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or TimeoutException) { }
            if (outcome is not null) await ApplyAsync(owner, request, outcome, CancellationToken.None);
        }
        return await ReadAsync(actor, request, ct);
    }
    /// <summary>Reserves at most one bounded status lookup for an existing synthetic pilot request.</summary>
    internal async Task<AiRequestState?> CheckPilotStatusAsync(string actor, Guid request, CancellationToken ct)
    {
        if (pilot is null || string.IsNullOrWhiteSpace(actor) || actor.Length > 256 || request == Guid.Empty) return null;

        string owner, digest, originalId;
        AiOffer offer;
        AiPilotRouteRow route;
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var requestOwner = await db.Set<AiRequestRow>().Where(row => row.Id == request).Select(row => row.UserId).SingleOrDefaultAsync(ct);
            if (requestOwner is null) return null;
            owner = requestOwner;
            // Share the original owner lock with normal status and settlement, without creating a user session.
            await SyncRequestScope.LockOwnerAsync(db, owner, ct);
            var row = await db.Set<AiRequestRow>().SingleOrDefaultAsync(item => item.Id == request && item.UserId == owner, ct);
            if (row is not { State: AiRequestState.Pending, VendorOperationId: { Length: > 0 } handle }) return null;
            var original = await db.Set<AiOfferRow>().SingleAsync(item => item.Id == row.OfferId, ct);
            offer = Offer(original);
            if (original.PilotRouteId is not { } routeId) return null;
            route = await db.Set<AiPilotRouteRow>().SingleAsync(item => item.Id == routeId, ct);
            if (!pilot.CanCheckStatus(route, offer, row.PayloadDigest, handle)) return null;
            var now = time.GetUtcNow();
            if (row.StatusCheckedAt is { } last && now - last < OperatorStatusInterval) return null;
            var lastAttempt = await db.Set<AiPilotStatusCheckRow>().Where(item => item.RequestId == request)
                .MaxAsync(item => (int?)item.Attempt, ct) ?? 0;
            if (lastAttempt >= MaximumOperatorChecks) return null;
            originalId = handle;
            digest = row.PayloadDigest;
            row.StatusCheckedAt = now;
            db.Add(new AiPilotStatusCheckRow { RequestId = request, Attempt = lastAttempt + 1, Actor = actor, CheckedAt = now });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        AiProviderOutcome? outcome = null;
        try { outcome = await pilot.StatusAsync(route, offer, ProviderKey(owner, request), digest, originalId, ct); }
        catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or TimeoutException) { }
        if (outcome is not null) await ApplyAsync(owner, request, outcome, CancellationToken.None);
        await using var observation = await factory.CreateDbContextAsync(CancellationToken.None);
        return await observation.Set<AiRequestRow>().Where(item => item.Id == request && item.UserId == owner)
            .Select(item => (AiRequestState?)item.State).SingleOrDefaultAsync(CancellationToken.None);
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
    private async Task SaveVendorHandleAsync(string owner, Guid id, string handle)
    {
        await using var db = await factory.CreateDbContextAsync(CancellationToken.None);
        // Update only the admitted pending owner request and never overwrite its original handle.
        var updated = await db.Set<AiRequestRow>().Where(r => r.UserId == owner && r.Id == id &&
            r.State == AiRequestState.Pending && r.VendorOperationId == null)
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.VendorOperationId, handle), CancellationToken.None);
        if (updated != 1) throw new IOException("Original provider operation could not be recorded.");
    }

    private async Task ApplyAsync(string owner, Guid id, AiProviderOutcome outcome, CancellationToken ct, string? recoveredHandle = null)
    {
        if (outcome.State == AiProviderState.Unknown && outcome.Metering is null &&
            outcome.VendorStatus is null && outcome.CreateRefusal is null) return;
        if (!Enum.IsDefined(outcome.State)) return;
        // Unusable or inconsistent provider evidence leaves the original reservation intact for actual reconciliation.
        if (outcome.ChargedUnits < 0 || outcome.State == AiProviderState.DefinitiveNoCharge && (outcome.ChargedUnits != 0 || outcome.Text is not null)) return;
        if (outcome.State == AiProviderState.Completed)
        { try { AiProtocol.ValidateText(outcome.Text ?? ""); } catch (ArgumentException) { return; } }
        await using var db = await factory.CreateDbContextAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await SyncRequestScope.LockOwnerAsync(db, owner, ct);
        var row = await db.Set<AiRequestRow>().SingleAsync(r => r.Id == id && r.UserId == owner, ct);
        if (row.State != AiRequestState.Pending) return;
        if (recoveredHandle is not null)
        {
            if (row.PilotDispatchStage != AiPilotDispatchStage.Claimed ||
                row.VendorOperationId is { } originalHandle && originalHandle != recoveredHandle) return;
            row.VendorOperationId = recoveredHandle;
        }
        if (outcome.CreateRefusal is { } createRefusal)
        {
            if (row.PilotDispatchStage != AiPilotDispatchStage.Claimed || row.VendorOperationId is not null ||
                !Enum.IsDefined(createRefusal) || outcome.State != AiProviderState.Unknown ||
                row.PilotCreateRefusal is { } previousRefusal && previousRefusal != createRefusal) return;
            row.PilotCreateRefusal = createRefusal;
        }
        if (outcome.VendorStatus is { } vendorStatus)
        {
            if (row.VendorOperationId is null || !Enum.IsDefined(vendorStatus) ||
                row.PilotVendorStatus is { } previousStatus && previousStatus != vendorStatus) return;
            row.PilotVendorStatus = vendorStatus;
        }
        if (outcome.Metering is { } metering)
        {
            // Keep one terminal vendor observation even when the original customer result remains unusable.
            if (row.PilotMeteredCostMicros is { } previous && (previous != metering.EstimateMicros ||
                row.VendorInputTokens != metering.Input || row.VendorOutputTokens != metering.Output ||
                row.VendorCachedInputTokens != metering.CachedInput || row.VendorThinkingTokens != metering.Thinking ||
                row.PilotMeteredTariff != AiPilotUsage.Tariff)) return;
            row.VendorInputTokens = metering.Input;
            row.VendorOutputTokens = metering.Output;
            row.VendorCachedInputTokens = metering.CachedInput;
            row.VendorThinkingTokens = metering.Thinking;
            row.PilotMeteredCostMicros = metering.EstimateMicros;
            row.PilotMeteredTariff = AiPilotUsage.Tariff;
        }
        if (outcome.State == AiProviderState.Unknown)
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return;
        }
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
        RecordRefund(db, row, purchaseRefund, giftRefund, now);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    private async Task<AiRequestResult> ReadAsync(ClaimsPrincipal actor, Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var scope = await OpenAsync(actor, db, ct);
        var row = await db.Set<AiRequestRow>().SingleOrDefaultAsync(r => r.Id == id && r.UserId == scope.Owner, ct)
            ?? throw new AiRefusal(404, "AiRequestUnavailable");
        if (row.State == AiRequestState.Completed && row.ProtectedPayload.Length == 0 && row.ProtectedText == "")
            throw new AiRefusal(410, "AiResultRetired");
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
    private const string ProviderKeyPrefix = "zanance.analysis.v1\n";

    /// <summary>Derives the same opaque request reference for dispatch, recovery and retention.</summary>
    internal static string ProviderKey(string owner, Guid id) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(ProviderKeyPrefix + owner + "\n" + id.ToString("N"))));
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
    internal static void RecordRefund(ServiceDbContext db, AiRequestRow row, long purchased, long gifted, DateTimeOffset now)
    {
        if (purchased != 0 || gifted != 0)
            db.Add(Event(row.UserId, row.Id, AiCreditEvent.Refund, purchased, gifted, -purchased, -gifted, now));
    }
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
