using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

public sealed partial class PersonalSyncStore
{
    private static async Task ReconcileAttachmentCaptureAsync(ZananceDbContext db, long after, CancellationToken ct)
    {
        var through = await db.Set<SyncOutboxRow>().MaxAsync(row => (long?)row.Sequence, ct) ?? 0;
        if (through <= after) return;
        var table = SyncSchema.Manifest.Tables.Single(row => row.Name == "Attachments");
        await using (var related = Command(db, SyncTriggerSql.CaptureRelatedAttachments(table)))
        {
            Parameter(related, "$after", after); Parameter(related, "$through", through);
            await related.ExecuteNonQueryAsync(ct);
        }
        // An attachment can also be edited while its entry is held only for Undo. Its physical bytes must
        // stay local, not become a live orphan on the service. The fixed prefix prevents self-insert rescanning.
        await using var inactive = Command(db, "INSERT INTO SyncOutbox(TableName,RecordKey,Body) " +
            "SELECT 'Attachments',candidate.RecordKey,NULL FROM SyncOutbox AS candidate " +
            "WHERE candidate.TableName='Attachments' AND candidate.Sequence>$after AND candidate.Sequence<=$through AND candidate.Body IS NOT NULL " +
            "AND candidate.Sequence=(SELECT MAX(last.Sequence) FROM SyncOutbox AS last WHERE last.TableName='Attachments' AND last.RecordKey=candidate.RecordKey AND last.Sequence<=$through) " +
            "AND NOT EXISTS(SELECT 1 FROM Entries AS parent WHERE parent.Id=json_extract(candidate.Body,'$.EntryId'));" );
        Parameter(inactive, "$after", after); Parameter(inactive, "$through", through); await inactive.ExecuteNonQueryAsync(ct);
    }
}
