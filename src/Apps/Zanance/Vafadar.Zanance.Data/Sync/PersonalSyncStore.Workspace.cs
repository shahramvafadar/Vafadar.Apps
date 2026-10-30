using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>One original file/transaction supplies connection, counts, complete review context and first-run eligibility.</summary>
public sealed record PersonalSyncWorkspace(PersonalSyncReview Connection, PersonalSyncStatus Status,
    PersonalSyncReviewData Review, bool CanCompleteOnboarding);

public sealed partial class PersonalSyncStore
{
    /// <summary>Reads the original workspace on a worker without mixing independent context generations or financial horizons.</summary>
    public async Task<PersonalSyncWorkspace> ReadWorkspaceAsync(bool firstRun, CancellationToken ct = default)
    {
        // Capture the actual factory context before dispatch; no later factory lookup can silently select another profile.
        await using var db = await factory.CreateDbContextAsync(ct);
        var access = Access(db);
        return await Task.Run(async () =>
        {
            ct.ThrowIfCancellationRequested(); await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var connection = await ReadConnectionInContextAsync(db, ct);
            var status = await ReadStatusInContextAsync(db, ct);
            var conflicts = await ReadConflictsInContextAsync(db, ct);
            var prepared = await PrepareReviewsInContextAsync(db, conflicts, ct);
            var eligible = firstRun && await OnboardingEligibleAsync(db, connection, ct);
            access.EnsureSnapshotCurrent(); ct.ThrowIfCancellationRequested();
            return new PersonalSyncWorkspace(connection, status, prepared, eligible);
        }, ct);
    }
}
