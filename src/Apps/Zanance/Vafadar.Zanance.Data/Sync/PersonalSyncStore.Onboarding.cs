using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Data.Sync;

public sealed partial class PersonalSyncStore
{
    /// <summary>Reads first-run readiness from this exact generation after a completely applied personal round.</summary>
    public async Task<bool> CanCompleteOnboardingAsync(PersonalSyncReview review, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        await db.Database.OpenConnectionAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var eligible = await OnboardingEligibleAsync(db, review, ct);
        access.EnsureSnapshotCurrent(); await transaction.CommitAsync(ct); return eligible;
    }

    /// <summary>Completes only device-local first-run state after explicit consent, without creating accounts or replacing preferences.</summary>
    public async Task<bool> CompleteOnboardingAsync(PersonalSyncReview review, CancellationToken ct = default,
        Action? validateOriginal = null)
    {
        validateOriginal?.Invoke();
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        await db.Database.OpenConnectionAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await OnboardingEligibleAsync(db, review, ct)) return false;
        var settings = await db.Settings.OrderBy(row => row.CreatedAt).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("The applied profile settings are unavailable.");
        settings.OnboardingCompleted = true;
        access.EnsureSnapshotCurrent(); validateOriginal?.Invoke(); await db.SaveChangesAsync(ct);
        validateOriginal?.Invoke(); await transaction.CommitAsync(ct); return true;
    }

    private static async Task<bool> OnboardingEligibleAsync(ZananceDbContext db, PersonalSyncReview review, CancellationToken ct)
    {
        var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
        if (binding is null || !binding.Enabled || binding.Applying || binding.LastSuccessAt is null || binding.GenerationPath != Generation(db) ||
            review.GenerationPath != Generation(db) || binding.ProfileId != review.RemoteProfile || binding.IdentityId != review.IdentityId ||
            binding.DeviceId != review.DeviceId || binding.PeerKey != review.PeerKey) return false;
        return !await db.Set<SyncClientRound>().AnyAsync(ct) && !await db.Set<SyncConflictRow>().AnyAsync(ct) &&
            await db.Accounts.AnyAsync(ct) && await db.Settings.AnyAsync(ct);
    }
}
