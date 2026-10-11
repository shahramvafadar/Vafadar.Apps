using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Rechecks complete preferences and the first recorded account date in the same reviewed file, without creating Settings.</summary>
    public async Task ValidateReviewReminderInputsAsync(PlanWorkSnapshot snapshot, string originalSettingsPacket,
        DateOnly? firstData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot); ArgumentException.ThrowIfNullOrWhiteSpace(originalSettingsPacket);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (snapshot.Access is null || snapshot.DatabasePath != Path.GetFullPath(db.Database.GetDbConnection().DataSource))
            throw new InvalidOperationException("The review reminder belongs to another original file.");
        snapshot.Access.DemandFeature(CommercialFeature.History); snapshot.Access.EnsureSnapshotCurrent();
        var current = await db.Settings.AsNoTracking().OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var first = await db.Accounts.Select(account => (DateOnly?)account.OpeningDate).MinAsync(cancellationToken);
        if (originalSettingsPacket != JsonSerializer.Serialize(current) || first != firstData)
            throw new InvalidOperationException("Original review reminder preferences or period inputs changed.");
        snapshot.Access.EnsureSnapshotCurrent();
    }
}
