using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Signed operation dispatch and original durable reply; device consent is never inferred from HTTP success.</summary>
internal sealed class BillingCommandStore(IDbContextFactory<ServiceDbContext> factory, BillingAuthority authority,
    BillingRegistry registry, BillingPurchaseCoordinator purchases, IDataProtectionProvider protection, TimeProvider time)
{
    /// <summary>Uses exact signed input and current optional session; provider work opens its own short transactions.</summary>
    internal async Task<BillingCommandResult> ExecuteAsync(Guid customerId, string signed, ClaimsPrincipal actor, CancellationToken ct)
    {
        using var signer = await authority.OpenAsync(ct);
        await using (var db = await factory.CreateDbContextAsync(ct))
        await using (var scope = await BillingRequestScope.OpenAsync(db, actor, time, ct))
        {
            await BillingRequestScope.LockCustomerAsync(db, customerId, ct);
            var customer = await db.Set<BillingCustomerRow>().SingleOrDefaultAsync(row => row.Id == customerId, ct)
                ?? throw new BillingRefusal(404, "BillingDeviceUnavailable");
            var command = BillingRegistry.Verify(customer, signed, scope.UserId, time.GetUtcNow(), requireFresh: false);
            var journal = await ReadOriginalAsync(db, customerId, command.OperationId, signed, ct);
            if (journal?.ProtectedResponse is { Length: > 0 })
            { var original = ReadResponse(journal, protection); await scope.CommitAsync(ct); return original; }
            if (journal is null && command.Deadline <= time.GetUtcNow() &&
                command.Operation is BillingOperation.VerifyPurchase or BillingOperation.RestorePurchase or BillingOperation.Refresh)
            {
                // A request lost before arrival has no accepted provider work. Under this same customer lock,
                // retain an immutable no-op tombstone: a delayed copy can never start after this observation.
                // The receipt does not mean payment cancellation or native finish; restore/reverification stays separate.
                var now = time.GetUtcNow();
                await db.Set<BillingCommandRow>().Where(row => row.CustomerId == customerId && row.ExpiresAt < now && row.ProtectedResponse != "").ExecuteDeleteAsync(ct);
                if (await db.Set<BillingCommandRow>().CountAsync(row => row.CustomerId == customerId, ct) >= 256)
                    throw new BillingRefusal(413, "BillingPendingWorkBound");
                var snapshot = await registry.SnapshotAsync(db, customer, scope.UserId, command.OperationId, ct, BillingSnapshotKind.Operation);
                var result = BillingAuthority.CommandResult(snapshot, signer, signed, false, null, command.NativePaymentId);
                journal = NewJournal(customerId, command.OperationId, signed, now, protection);
                SaveResponse(journal, result, protection); db.Add(journal); await scope.CommitAsync(ct); return result;
            }
            if (journal is null) RequireFresh(command, customer, time.GetUtcNow());
            if (command.Operation is BillingOperation.VerifyPurchase or BillingOperation.RestorePurchase or BillingOperation.Refresh)
            {
                // No scope, native reader, transaction or identity lock is retained while querying a store.
                await scope.CommitAsync(ct);
            }
            else
            {
                var changed = false;
                if (command.ProviderReference is not null || command.ProductHint is not null)
                    throw new BillingRefusal(422, "BillingCommandFieldsRefused");
                if (command.Operation == BillingOperation.LinkIdentity)
                {
                    if (scope.UserId is null || customer.UserId is not null && customer.UserId != scope.UserId)
                        throw new BillingRefusal(403, "CurrentBillingIdentityRequired");
                    // Linking a customer is not a transfer of someone else's store purchase. Each purchase
                    // is associated only through its separately verified restoration/ownership path.
                    if (customer.UserId is null)
                    { customer.UserId = scope.UserId; customer.Revision = checked(customer.Revision + 1); changed = true; }
                }
                else if (command.Operation == BillingOperation.RetireDevice)
                {
                    if (customer.Revoked) throw new BillingRefusal(410, "BillingDeviceRetired");
                    customer.Revoked = true; customer.Revision = checked(customer.Revision + 1); changed = true;
                }
                else if (command.Operation != BillingOperation.Observe) throw new BillingRefusal(422, "BillingOperationRefused");
                var kind = command.Operation == BillingOperation.Observe ? BillingSnapshotKind.Current : BillingSnapshotKind.Operation;
                var snapshot = await registry.SnapshotAsync(db, customer, scope.UserId, command.OperationId, ct, kind);
                var result = BillingAuthority.CommandResult(snapshot, signer, signed, false, null, command.NativePaymentId);
                // Fresh read-only observations never fill the durable purchase-command journal.
                if (changed)
                {
                    journal ??= NewJournal(customerId, command.OperationId, signed, time.GetUtcNow(), protection);
                    if (db.Entry(journal).State == EntityState.Detached) db.Add(journal);
                    SaveResponse(journal, result, protection);
                }
                await scope.CommitAsync(ct); return result;
            }
        }
        return await purchases.ExecuteAsync(customerId, signed, actor, ct);
    }

    /// <summary>Returns only the original idempotent journal; a changed signature/payload never reuses its operation id.</summary>
    internal static async Task<BillingCommandRow?> ReadOriginalAsync(ServiceDbContext db, Guid customer, Guid operation,
        string signed, CancellationToken ct)
    {
        var original = await db.Set<BillingCommandRow>().SingleOrDefaultAsync(row => row.CustomerId == customer && row.Id == operation, ct);
        if (original is not null && original.RequestHash != BillingAccountBinding.CommandHash(signed))
            throw new BillingRefusal(409, "OriginalBillingCommandDiffers");
        return original;
    }

    /// <summary>Only a genuinely new operation consumes current consent/time/revision.</summary>
    internal static void RequireFresh(BillingCommand command, BillingCustomerRow customer, DateTimeOffset now)
    {
        if ((command.Operation == BillingOperation.Observe ? command.ExpectedRevision > customer.Revision : command.ExpectedRevision != customer.Revision) ||
            command.Deadline <= now || command.Deadline > now.AddMinutes(10))
            throw new BillingRefusal(409, "BillingReviewChanged");
    }

    /// <summary>Retains the exact signed request privately before any provider HTTP or externally uncertain effect.</summary>
    internal static BillingCommandRow NewJournal(Guid customer, Guid operation, string signed, DateTimeOffset now, IDataProtectionProvider protection) => new()
    {
        CustomerId = customer, Id = operation, RequestHash = BillingAccountBinding.CommandHash(signed),
        ProtectedRequest = protection.CreateProtector("zanance-billing-command-v1").Protect(signed), ExpiresAt = now.AddDays(14),
    };

    /// <summary>Protects an exact original response, separate from the fresh-current entitlement cache exchange.</summary>
    internal static void SaveResponse(BillingCommandRow row, BillingCommandResult result, IDataProtectionProvider protection) => row.ProtectedResponse =
        Convert.ToBase64String(protection.CreateProtector("zanance-billing-response-v1")
            .Protect(JsonSerializer.SerializeToUtf8Bytes(result, BillingJson.Default.BillingCommandResult)));

    /// <summary>Original replies may finish a provider operation but cannot replace a newer current snapshot.</summary>
    internal static BillingCommandResult ReadResponse(BillingCommandRow row, IDataProtectionProvider protection) => Vafadar.Cryptography.StrictJsonPayload.Deserialize(
        protection.CreateProtector("zanance-billing-response-v1").Unprotect(Convert.FromBase64String(row.ProtectedResponse)),
        BillingJson.Default.BillingCommandResult, 262144);
}
