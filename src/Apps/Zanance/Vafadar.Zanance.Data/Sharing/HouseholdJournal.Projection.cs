using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Original device-only source-copy outcome; no source ids, private labels, source paths or authority frames are portable.</summary>
public sealed record HouseholdProjectionReceipt(Guid ReviewId, int SelectedAccounts, int Entries, int Attachments, int PrivateTransfers, int DetachedRefunds, bool Completed)
{
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdProjectionReceipt);
}

/// <summary>AOT-safe bounded local projection receipt; it is not an HTTP financial or membership message.</summary>
[JsonSerializable(typeof(HouseholdProjectionReceipt))]
internal partial class HouseholdProjectionJsonContext : JsonSerializerContext;

public sealed partial class HouseholdJournal
{
    /// <summary>Durably records the explicit selected-source request before its copy worker starts; a pending request cannot be mistaken for an empty household.</summary>
    public async Task<HouseholdProjectionReceipt> StageProjectionReviewAsync(HouseholdJournalAuthority authority, HouseholdProjectionReview review, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review); review.Check();
        if (!review.Rechecked) throw new InvalidOperationException("The explicit household source must be rechecked before creating its separate ledger.");
        await EnsureProjectionIntentAsync(authority, review, ct);
        return await ReadProjectionReceiptAsync(authority, ct) ?? throw Refusal();
    }

    /// <summary>Explicit owner re-review can supersede only an unapplied original request in an empty unpublished draft, retaining its original setup and keys.</summary>
    public async Task<HouseholdProjectionReceipt> ReplacePendingProjectionAsync(HouseholdJournalAuthority authority, Guid reviewedPending,
        HouseholdProjectionReview replacement, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(replacement); replacement.Check();
        if (!replacement.Rechecked || reviewedPending == Guid.Empty) throw Refusal();
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct); RequireUnpublishedOwner(binding);
        var previous = ProjectionReceipt(binding) ?? throw Refusal();
        if (previous.Completed || previous.ReviewId != reviewedPending || context.Path == replacement.SourcePath ||
            await context.Db.Set<SyncBindingRow>().AnyAsync(ct) || await context.Db.Set<SyncClientRound>().AnyAsync(ct)) throw Refusal();
        var actual = await PersonalSyncStore.ReadFinancialAsync(context.Db, ct);
        if (actual.Keys.Any(key => key.Table is not ("Settings" or "Categories"))) throw Refusal();
        var receipt = new HouseholdProjectionReceipt(replacement.Id, replacement.Graph.SelectedAccounts, replacement.Entries,
            replacement.Attachments, replacement.PrivateTransfers, replacement.DetachedRefunds, false);
        binding.ProjectionReviewId = receipt.ReviewId; binding.ProjectionSourceDigest = replacement.SourceDigest;
        binding.ProjectionReceiptJson = JsonSerializer.Serialize(receipt, HouseholdProjectionJsonContext.Default.HouseholdProjectionReceipt);
        replacement.Check(); await context.CommitAsync(ct); return receipt;
    }

    /// <summary>Installs an explicitly rechecked source into a distinct fresh protected owner draft, with original financial validation and one atomic device-only receipt.</summary>
    public async Task<HouseholdProjectionReceipt> ApplyProjectionAsync(HouseholdJournalAuthority authority, HouseholdProjectionReview review, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review); review.Check();
        if (!review.Rechecked) throw new InvalidOperationException("The explicit household source must be rechecked before creating its separate ledger.");
        await EnsureProjectionIntentAsync(authority, review, ct);
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        RequireUnpublishedOwner(binding);
        if (context.Path == review.SourcePath || await context.Db.Set<SyncBindingRow>().AnyAsync(ct) || await context.Db.Set<SyncClientRound>().AnyAsync(ct)) throw Refusal();
        var current = await PersonalSyncStore.ReadFinancialAsync(context.Db, ct);
        var originalReceipt = ProjectionReceipt(binding) ?? throw Refusal();
        if (binding.ProjectionSourceDigest != review.SourceDigest) throw Refusal();
        if (originalReceipt.Completed)
        {
            if (binding.ProjectionContentHash != HouseholdProjectionStore.Fingerprint(Records(current))) throw Refusal();
            review.Check(); context.Check(); return originalReceipt;
        }
        if (current.Keys.Any(key => key.Table is not ("Settings" or "Categories"))) throw Refusal();
        var expected = review.Graph.Records.ToDictionary(row => row.Record, row => row.Values);
        foreach (var row in current.Where(row => row.Key.Table == "Settings")) expected.Add(row.Key, row.Value);
        if (SyncFinancialValidation.Validate([], Records(expected)).Count != 0) throw Refusal();
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        using var owner = await ReadIdentityAsync(context.Db, RootSlot(new PublicSigningRecipientIdentity(binding.PinnedSigningKey, binding.PinnedWrappingKey).Fingerprint), ct);
        if (device.Public.Fingerprint != binding.DeviceFingerprint || owner.Public.SigningKey != binding.PinnedSigningKey || owner.Public.WrappingKey != binding.PinnedWrappingKey) throw Refusal();
        using var householdSeed = context.Db.ArmVerifiedHouseholdMaintenance(review.Graph.Records.Select(row => row.Record)
            .Concat(current.Keys.Where(key => key.Table == "Categories")).ToHashSet());
        await context.Db.Database.ExecuteSqlRawAsync("PRAGMA defer_foreign_keys=ON;", ct);
        await context.Db.Categories.ExecuteDeleteAsync(ct);
        var timestamp = (time ?? TimeProvider.System).GetUtcNow();
        foreach (var row in review.Graph.Records)
        {
            review.Check(); ct.ThrowIfCancellationRequested(); context.Check();
            await PersonalSyncStore.WriteProjectedFinancialAsync(context.Db, row.Record, row.Values!.Value, null, timestamp, review.ReadBlobAsync, ct);
        }
        var actual = await PersonalSyncStore.ReadFinancialAsync(context.Db, ct);
        if (actual.Count != expected.Count || expected.Any(row => !actual.TryGetValue(row.Key, out var value) ||
            row.Value is not { } expectedValue || value is not { } actualValue || !JsonElement.DeepEquals(expectedValue, actualValue)) ||
            SyncFinancialValidation.Validate([], Records(actual)).Count != 0) throw Refusal();
        var receipt = originalReceipt with { Completed = true };
        binding.ProjectionContentHash = HouseholdProjectionStore.Fingerprint(Records(actual));
        binding.ProjectionReceiptJson = JsonSerializer.Serialize(receipt, HouseholdProjectionJsonContext.Default.HouseholdProjectionReceipt);
        review.Check(); await context.CommitAsync(ct); return receipt;
    }

    /// <summary>Returns only a durably committed original source-copy outcome during cold setup continuation; missing receipt never authorizes filling existing finance.</summary>
    public async Task<HouseholdProjectionReceipt?> ReadProjectionReceiptAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct);
        return await ReadProjectionReceiptInContextAsync(context, authority, ct);
    }

    /// <summary>Uses the caller's original protected transaction while preserving this reader's complete validation.</summary>
    private async Task<HouseholdProjectionReceipt?> ReadProjectionReceiptInContextAsync(JournalContext context,HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
         var binding = await RequireBindingAsync(context, authority, ct);
        var result = ProjectionReceipt(binding);
        if (result is { Completed: true } && binding.MembershipVersion == 0 && binding.SpaceId == Guid.Empty)
        {
            RequireUnpublishedOwner(binding);
            var actual = await PersonalSyncStore.ReadFinancialAsync(context.Db, ct);
            if (binding.ProjectionContentHash != HouseholdProjectionStore.Fingerprint(Records(actual))) throw Refusal();
        }
        context.Check(); return result;
    }

    /// <summary>Intent precedes copying so a crash cannot silently turn a requested selected-history household into an empty one.</summary>
    private async Task EnsureProjectionIntentAsync(HouseholdJournalAuthority authority, HouseholdProjectionReview review, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct); RequireUnpublishedOwner(binding);
        if (context.Path == review.SourcePath || await context.Db.Set<SyncBindingRow>().AnyAsync(ct)) throw Refusal();
        var prior = ProjectionReceipt(binding);
        if (prior is not null)
        {
            if (binding.ProjectionSourceDigest != review.SourceDigest || prior.SelectedAccounts != review.Graph.SelectedAccounts || prior.Entries != review.Entries ||
                prior.Attachments != review.Attachments || prior.PrivateTransfers != review.PrivateTransfers || prior.DetachedRefunds != review.DetachedRefunds) throw Refusal();
            context.Check(); return;
        }
        var current = await PersonalSyncStore.ReadFinancialAsync(context.Db, ct);
        if (current.Keys.Any(key => key.Table is not ("Settings" or "Categories"))) throw Refusal();
        SetProjectionIntent(binding, review);
        review.Check(); await context.CommitAsync(ct);
    }

    // A new owner's keys and selected-copy obligation share their first commit. A process cut
    // after setup must never turn an acknowledged selected copy into publishable empty finance.
    private static void SetProjectionIntent(HouseholdLocalBinding binding, HouseholdProjectionReview review)
    {
        binding.ProjectionReviewId = review.Id; binding.ProjectionSourceDigest = review.SourceDigest;
        binding.ProjectionReceiptJson = JsonSerializer.Serialize(new HouseholdProjectionReceipt(review.Id, review.Graph.SelectedAccounts,
            review.Entries, review.Attachments, review.PrivateTransfers, review.DetachedRefunds, false), HouseholdProjectionJsonContext.Default.HouseholdProjectionReceipt);
    }

    private static SyncRecord[] Records(IReadOnlyDictionary<SyncRecordKey, JsonElement?> rows) =>
        rows.Select(row => new SyncRecord(row.Key, 0, row.Value)).ToArray();
    private static void RequireUnpublishedOwner(HouseholdLocalBinding binding)
    {
        if (!binding.OwnerCreation || binding.SpaceId != Guid.Empty || binding.DeviceId != Guid.Empty || binding.MembershipVersion != 0 ||
            binding.Cursor != 0 || binding.FinancialCursor != 0 || binding.SendingDisabled) throw Refusal();
    }
    private static HouseholdProjectionReceipt? ProjectionReceipt(HouseholdLocalBinding binding)
    {
        if (binding.ProjectionReviewId is null)
        {
            if (binding.ProjectionSourceDigest is not null || binding.ProjectionContentHash is not null || binding.ProjectionReceiptJson is not null) throw Refusal();
            return null;
        }
        if (!SyncProtocolDigest.IsDigest(binding.ProjectionSourceDigest) ||
            binding.ProjectionReceiptJson is not { Length: > 0 and <= 4096 }) throw Refusal();
        var result = JsonSerializer.Deserialize(binding.ProjectionReceiptJson, HouseholdProjectionJsonContext.Default.HouseholdProjectionReceipt) ?? throw Refusal();
        if (result.ReviewId == Guid.Empty || result.ReviewId != binding.ProjectionReviewId || result.SelectedAccounts < 0 || result.Entries < 0 ||
            result.Attachments < 0 || result.PrivateTransfers < 0 || result.DetachedRefunds < 0 || result.PrivateTransfers > result.Entries || result.DetachedRefunds > result.Entries ||
            (result.Completed ? !SyncProtocolDigest.IsDigest(binding.ProjectionContentHash) : binding.ProjectionContentHash is not null)) throw Refusal();
        return result;
    }
}
