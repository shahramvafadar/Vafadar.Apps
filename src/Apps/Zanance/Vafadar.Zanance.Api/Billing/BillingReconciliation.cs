using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Bounded durable acknowledgement and authoritative reconciliation of already verified purchases only.</summary>
internal sealed class BillingReconciliation(IDbContextFactory<ServiceDbContext> factory, IEnumerable<IBillingProvider> providers,
    IDataProtectionProvider protection, TimeProvider time)
{
    /// <summary>Reconciles at most one due original purchase; no client command or completed prefix is replayed.</summary>
    internal async Task<bool> RunOneAsync(CancellationToken ct)
    {
        BillingPurchaseLease lease; string protectedReference;
        await using (var db = await factory.CreateDbContextAsync(ct))
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var now = time.GetUtcNow();
            var id = await db.Set<BillingPurchaseRow>().Where(p => p.Verified && p.RecheckAt <= now &&
                    (p.LeaseUntil == null || p.LeaseUntil <= now))
                .OrderBy(p => p.RecheckAt).ThenBy(p => p.Id).Select(p => p.Id).FirstOrDefaultAsync(ct);
            if (id is null) return false;
            await BillingRequestScope.LockPurchaseAsync(db, id, ct);
            var row = await db.Set<BillingPurchaseRow>().SingleAsync(p => p.Id == id, ct);
            if (!row.Verified || row.RecheckAt > now || row.LeaseUntil > now) return true;
            protectedReference = row.ProtectedReference;
            lease = new(id, Guid.NewGuid(), 0, row.Issuer, row.Environment, row.Product, "", now, row.RequestedRecheckRevision);
            row.LeaseOwner = lease.LeaseOwner; row.LeaseUntil = now.AddMinutes(2);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        try
        {
            lease = lease with { Reference = protection.CreateProtector("zanance-billing-provider-reference-v1").Unprotect(protectedReference) };
            var provider = providers.SingleOrDefault(p => p.Issuer == lease.Issuer)
                ?? throw new BillingRefusal(503, "BillingProviderUnavailable");
            var verified = await provider.VerifyExistingAsync(lease.Reference, lease.Hint, lease.Environment, ct);
            verified.Facts.Validate();
            if (verified.Issuer != lease.Issuer || verified.Environment != lease.Environment || verified.Reference != lease.Reference ||
                BillingAuthority.PurchaseId(verified.Issuer, verified.Environment, verified.Canonical) != lease.PurchaseId)
                throw new BillingRefusal(422, "OriginalStoreChainDiffers");
            var acknowledge = false;
            await using (var db = await factory.CreateDbContextAsync(ct))
            await using (var tx = await db.Database.BeginTransactionAsync(ct))
            {
                await BillingRequestScope.LockPurchaseAsync(db, lease.PurchaseId, ct);
                var row = await db.Set<BillingPurchaseRow>().SingleAsync(p => p.Id == lease.PurchaseId, ct);
                var now = time.GetUtcNow();
                if (row.LeaseOwner != lease.LeaseOwner || row.LeaseUntil <= now) return true;
                if (!row.Verified || row.ProviderAccountBinding != verified.AccountBinding ||
                    BillingProducts.RequiresIdentity(verified.Facts.Product, verified.Issuer) && row.UserId is null ||
                    verified.Facts.StartsAt > now.AddMinutes(1))
                    throw new BillingRefusal(422, "OriginalPurchaseOwnershipDiffers");
                var facts = BillingAcceptedTerms.Preserve(row, verified.Facts);
                row.Product = facts.Product; row.State = facts.State;
                row.AccessUntil = facts.State == BillingPurchaseState.Grace ? facts.GraceUntil : facts.PaidUntil;
                row.FactsJson = JsonSerializer.Serialize(facts, BillingJson.Default.BillingPurchaseFacts);
                row.VerifiedAt = now; row.Revision = checked(row.Revision + 1);
                acknowledge = verified.Issuer == BillingIssuer.GooglePlay && !facts.Acknowledged &&
                    facts.State is BillingPurchaseState.Active or BillingPurchaseState.Grace;
                row.AcknowledgePending = acknowledge;
                row.RecheckAt = row.RequestedRecheckRevision != lease.ObservedRecheckRevision ? now
                    : acknowledge ? now.AddMinutes(1) : NextCheck(now, facts); row.RecheckFailures = 0;
                // Keep the exact lease through acknowledgement; another observer cannot finish newer work under this receipt.
                if (!acknowledge) { row.LeaseOwner = null; row.LeaseUntil = null; }
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            }
            if (acknowledge)
            {
                // Persist the actual verified grant before calling the provider, including a first acknowledgement retry.
                await provider.AcknowledgeAsync(verified, ct);
                await using var db = await factory.CreateDbContextAsync(ct);
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                await BillingRequestScope.LockPurchaseAsync(db, lease.PurchaseId, ct);
                var row = await db.Set<BillingPurchaseRow>().SingleAsync(p => p.Id == lease.PurchaseId, ct);
                if (row.LeaseOwner == lease.LeaseOwner && row.LeaseUntil > time.GetUtcNow() && row.AcknowledgePending)
                {
                    var facts = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(row.FactsJson), BillingJson.Default.BillingPurchaseFacts, 8192);
                    row.FactsJson = JsonSerializer.Serialize(facts with { Acknowledged = true }, BillingJson.Default.BillingPurchaseFacts);
                    row.AcknowledgePending = false; row.LeaseOwner = null; row.LeaseUntil = null;
                    row.RecheckAt = row.RequestedRecheckRevision != lease.ObservedRecheckRevision
                        ? time.GetUtcNow() : NextCheck(time.GetUtcNow(), facts);
                    await db.SaveChangesAsync(ct);
                }
                await tx.CommitAsync(ct);
            }
            return true;
        }
        catch
        {
            await FailedAsync(lease); throw;
        }
    }

    /// <summary>Finite observation schedule; terminal purchases remain recheckable for store recovery/refund reversal.</summary>
    internal static DateTimeOffset NextCheck(DateTimeOffset now, BillingPurchaseFacts facts)
    {
        var scheduled = now.Add(facts.State is BillingPurchaseState.Pending or BillingPurchaseState.Hold or BillingPurchaseState.Grace
            ? TimeSpan.FromMinutes(5) : facts.State is BillingPurchaseState.Expired or BillingPurchaseState.Revoked
                ? TimeSpan.FromDays(1) : TimeSpan.FromHours(6));
        var end = facts.State == BillingPurchaseState.Grace ? facts.GraceUntil : facts.PaidUntil;
        return facts.State is BillingPurchaseState.Active or BillingPurchaseState.Grace &&
            end is { } due && due > now && due < scheduled ? due : scheduled;
    }

    /// <summary>A failed current query never fabricates revocation; only its owned lease/backoff changes.</summary>
    private async Task FailedAsync(BillingPurchaseLease lease)
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await using var db = await factory.CreateDbContextAsync(cleanup.Token);
            await using var tx = await db.Database.BeginTransactionAsync(cleanup.Token);
            await BillingRequestScope.LockPurchaseAsync(db, lease.PurchaseId, cleanup.Token);
            var row = await db.Set<BillingPurchaseRow>().SingleOrDefaultAsync(p => p.Id == lease.PurchaseId, cleanup.Token);
            if (row?.LeaseOwner == lease.LeaseOwner)
            {
                row.RecheckFailures = Math.Min(10, row.RecheckFailures + 1);
                row.RecheckAt = time.GetUtcNow().AddMinutes(Math.Min(360, Math.Pow(2, row.RecheckFailures)));
                row.LeaseOwner = null; row.LeaseUntil = null; await db.SaveChangesAsync(cleanup.Token);
            }
            await tx.CommitAsync(cleanup.Token);
        }
        catch (Exception error) when (error is Npgsql.NpgsqlException or OperationCanceledException or InvalidOperationException)
        { /* Preserve the finite original lease if cleanup storage is unavailable. */ }
    }
}

/// <summary>Sequential bounded service work, enabled only for the explicit local qualification composition.</summary>
internal sealed class BillingReconciliationWorker(IServiceProvider services) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!services.GetRequiredService<ServiceStorage>().LocalBillingQualification) return;
        var reconciliation = services.GetRequiredService<BillingReconciliation>();
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            for (var count = 0; count < 16; count++)
            {
                try { if (!await reconciliation.RunOneAsync(stoppingToken)) break; }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception error) when (error is BillingRefusal or System.Security.Cryptography.CryptographicException or
                    Npgsql.NpgsqlException or DbUpdateException or JsonException or HttpRequestException or OperationCanceledException or
                    ArgumentException or InvalidOperationException or FormatException)
                { break; /* Backoff is durable; never log provider tokens, private envelopes or credentials. */ }
            }
        }
    }
}
