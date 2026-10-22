using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data;

public sealed partial class GoalStore
{
    /// <summary>Captures every original detail input on a worker without initializing preferences or creating money.</summary>
    public async Task<GoalDetailSnapshot> ReadDetailSnapshotAsync(CancellationToken cancellationToken = default)
    {
        // Capture the maintenance/file lease before work is queued, exactly as the original list reader does.
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var finances = await ReadListSnapshotAsync(db, cancellationToken);
            var settings = await SettingsRowReader.ReadAsync(db, cancellationToken) ?? new ZananceSettings();
            var categories = await db.Categories.AsNoTracking().OrderBy(item => item.Kind).ThenBy(item => item.SortOrder).ToListAsync(cancellationToken);
            var schedules = await db.Schedules.AsNoTracking().OrderBy(item => item.State).ThenBy(item => item.Name).ToListAsync(cancellationToken);
            var states = await db.OccurrenceStates.AsNoTracking().ToListAsync(cancellationToken);
            finances.EnsureCurrent(); cancellationToken.ThrowIfCancellationRequested();
            return new GoalDetailSnapshot(finances, settings, categories, schedules, states);
        }, cancellationToken);
    }
}
