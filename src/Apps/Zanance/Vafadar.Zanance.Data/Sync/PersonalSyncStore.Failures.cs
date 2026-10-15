using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Content-free retained automatic-sync failures; never a network body or credential.</summary>
public enum PersonalSyncFailure
{
    /// <summary>The service or its retained local session could not complete a request.</summary>
    ServiceUnavailable,
    /// <summary>The owner must explicitly sign in again before automatic work can resume.</summary>
    SignInRequired,
    /// <summary>The saved public endpoint does not match this profile's reviewed binding.</summary>
    ConnectionReviewRequired,
}

public sealed partial class PersonalSyncStore
{
    /// <summary>Retains feedback only for the exact still-enabled binding which attempted the operation.</summary>
    public async Task ReportFailureAsync(PersonalSyncReview review, PersonalSyncFailure failure, CancellationToken ct = default)
    {
        var code = failure switch
        {
            PersonalSyncFailure.ServiceUnavailable => "service-unavailable",
            PersonalSyncFailure.SignInRequired => "sign-in-required",
            PersonalSyncFailure.ConnectionReviewRequired => "connection-review-required",
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        };
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        await Task.Run(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var binding = await db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
            if (binding is null || !binding.Enabled || binding.GenerationPath != Generation(db) || Generation(db) != review.GenerationPath ||
                binding.ProfileId != review.RemoteProfile || binding.IdentityId != review.IdentityId || binding.PeerKey != review.PeerKey || binding.DeviceId != review.DeviceId)
                throw new InvalidOperationException("The failed personal connection was retired.");
            binding.LastFailure = code; access.EnsureSnapshotCurrent(); await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }, ct);
    }
}
