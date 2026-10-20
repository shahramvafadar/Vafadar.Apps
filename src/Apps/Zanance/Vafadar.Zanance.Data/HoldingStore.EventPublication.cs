using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Data;

public sealed partial class HoldingStore
{
    /// <summary>Revalidates the original complete event-form packet and actual file before native publication.</summary>
    public async Task ValidateEventReadReviewAsync(HoldingEventReadReview review, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review); review.EnsureCurrent();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var current = await HoldingEventReadReview.ReadAsync(db, review.Inputs.Type.Id, review.EventId, ct);
        review.ValidateWriter(db, current); ct.ThrowIfCancellationRequested();
    }
}
