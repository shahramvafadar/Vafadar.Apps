using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data.Sync;

public sealed partial class PersonalSyncStore
{
    /// <summary>Recognizes only the exact clean already-applied household financial horizon, so an ownership response retry does not create another empty financial round.</summary>
    internal async Task<bool> OwnershipHorizonAppliedAsync(HouseholdJournalState expected, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
        if (binding is null || !binding.Enabled || binding.Applying || binding.LastSuccessAt is null || binding.TransportKind != ProfileSynchronizationKind.Household ||
            binding.GenerationPath != Generation(db) || binding.GenerationPath != expected.Authority.GenerationPath || binding.PeerKey != expected.Authority.PeerKey ||
            binding.IdentityId != expected.Authority.IdentityId || binding.ProfileId != expected.SpaceId || binding.DeviceId != expected.DeviceId ||
            binding.SchemaHash != Core.Sharing.HouseholdFinancialSchema.Hash || binding.Cursor != expected.FinancialCursor) return false;
        var clean = !await db.Set<SyncClientRound>().AnyAsync(ct) && !await db.Set<SyncIncomingRow>().AnyAsync(ct) && !await db.Set<SyncOutboxRow>().AnyAsync(ct) &&
            !await db.Set<SyncConflictRow>().AnyAsync(ct) && !await db.Set<SyncBlobPart>().AnyAsync(ct) && !await db.Set<SyncBlobTransfer>().AnyAsync(ct);
        access.EnsureSnapshotCurrent(); return clean;
    }
}
