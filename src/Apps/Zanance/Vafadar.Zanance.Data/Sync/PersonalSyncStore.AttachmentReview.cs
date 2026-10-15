using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>An exact verified attachment version for an in-app preview; callers must dispose its transient content.</summary>
public sealed record PersonalSyncAttachmentContent(string FileName, string ContentType, byte[] Data) : IDisposable
{
    /// <summary>Clears only the transient preview buffer, never original or staged protected storage.</summary>
    public void Dispose() => CryptographicOperations.ZeroMemory(Data);
}

public sealed partial class PersonalSyncStore
{
    /// <summary>Reads only an exact still-pending reviewed version; it neither selects a conflict nor writes a cache file.</summary>
    public async Task<PersonalSyncAttachmentContent> ReadAttachmentReviewAsync(PersonalSyncConflictReview review, bool localProposal, CancellationToken ct = default)
    {
        if (review.Conflict.Proposal.Record.Table != "Attachments" || review.Conflict.LinkedGroup is not null)
            throw new ArgumentException("An exact attachment review is required.", nameof(review));
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await RequireAttachmentReviewAsync(db, review, ct);
            var body = (localProposal ? review.Conflict.Proposal.Values : review.Conflict.Current?.Values)
                ?? throw new InvalidOperationException("The reviewed attachment version is absent or deleted.");
            var manifest = body.GetProperty("Data"); var length = manifest.GetProperty("length").GetInt64();
            if (length is < 1 or > EntryAttachment.MaxBytes) throw new InvalidDataException("The reviewed attachment exceeds its original bound.");
            var bytes = await VerifiedBlobAsync(db, manifest.GetProperty("sha256").GetString()!, length, ct);
            try
            {
                access.EnsureSnapshotCurrent(); await transaction.CommitAsync(ct);
                return new PersonalSyncAttachmentContent(body.GetProperty("FileName").GetString()!, body.GetProperty("ContentType").GetString()!, bytes);
            }
            catch { CryptographicOperations.ZeroMemory(bytes); throw; }
        }, ct);
    }

    private static async Task RequireAttachmentReviewAsync(ZananceDbContext db, PersonalSyncConflictReview review, CancellationToken ct)
    {
        var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
        var round = await db.Set<SyncClientRound>().AsNoTracking().SingleOrDefaultAsync(ct);
        if (binding is null || !binding.Enabled || binding.Applying || binding.GenerationPath != Generation(db) || Generation(db) != review.Connection.GenerationPath ||
            binding.ProfileId != review.Connection.RemoteProfile || binding.IdentityId != review.Connection.IdentityId ||
            binding.PeerKey != review.Connection.PeerKey || binding.DeviceId != review.Connection.DeviceId || round is null ||
            round.OperationId != review.Operation || round.Phase != review.Phase)
            throw new InvalidOperationException("The reviewed attachment connection changed.");
        var key = review.Conflict.Proposal.Record;
        var row = await db.Set<SyncConflictRow>().AsNoTracking().SingleOrDefaultAsync(row => row.TableName == key.Table && row.RecordKey == key.Key, ct);
        var boundary = await db.Set<SyncOutboxRow>().Where(row => row.TableName == key.Table && row.RecordKey == key.Key).MaxAsync(row => (long?)row.Sequence, ct) ?? 0;
        if (row is null || row.UseProposal is not null || row.LocalSequence != review.LocalSequence || boundary != review.LocalSequence ||
            ReviewDigest(row.Payload) != review.Digest || ReviewDigest(JsonSerializer.Serialize(review.Conflict, PersonalSyncJsonContext.Default.SyncConflict)) != review.Digest)
            throw new InvalidOperationException("The reviewed attachment version changed.");
        if (review.LocalContext is { } context)
            foreach (var expected in context)
                if (!Same(expected.Values, (await ReadReviewRecordAsync(db, expected.Record, ct)).Values))
                    throw new InvalidOperationException("A displayed attachment relationship changed.");
        var actual = await ReadReviewAttachmentRecordAsync(db, key, ct);
        if (!Same(review.Conflict.Proposal.Values, actual.Values))
            throw new InvalidOperationException("The local attachment changed after review.");
    }

    private static async Task<SyncRecord> ReadReviewAttachmentRecordAsync(ZananceDbContext db, SyncRecordKey key, CancellationToken ct)
    {
        var table = SyncSchema.Manifest.Tables.Single(table => table.Name == "Attachments");
        if (!SyncGraphValidation.ValidIdentity(table, key)) throw new InvalidDataException("The attachment identity is invalid.");
        using var identity = JsonDocument.Parse(key.Key); var id = identity.RootElement[0].GetString();
        JsonElement metadata;
        await using (var query = Command(db, SyncTriggerSql.ReadExistingAliased(table with { Columns = table.Columns.Where(column => column.Kind != "blob").ToArray() }) +
            " WHERE source.Id=$id AND EXISTS(SELECT 1 FROM Entries AS parent WHERE parent.Id=source.EntryId);"))
        {
            Parameter(query, "$id", id); await using var reader = await query.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return new(key, 0, null);
            using var document = JsonDocument.Parse(reader.GetString(1)); metadata = document.RootElement.Clone();
        }
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); long length = 0; var chunk = new byte[81920];
        try
        {
            await using var query = Command(db, "SELECT Data FROM Attachments WHERE Id=$id;"); Parameter(query, "$id", id);
            await using var reader = await query.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("The attachment snapshot changed.");
            await using var data = reader.GetStream(0); int count;
            while ((count = await data.ReadAsync(chunk, ct)) != 0)
            {
                length += count; if (length > EntryAttachment.MaxBytes) throw new InvalidDataException("The local attachment exceeds its original bound.");
                digest.AppendData(chunk, 0, count);
            }
            using var output = new MemoryStream();
            using (var writer = new Utf8JsonWriter(output))
            {
                writer.WriteStartObject(); foreach (var property in metadata.EnumerateObject()) property.WriteTo(writer);
                writer.WritePropertyName("Data"); writer.WriteStartObject(); writer.WriteString("sha256", Convert.ToHexStringLower(digest.GetHashAndReset()));
                writer.WriteNumber("length", length); writer.WriteEndObject(); writer.WriteEndObject();
            }
            using var body = JsonDocument.Parse(output.ToArray()); return new(key, 0, body.RootElement.Clone());
        }
        finally { CryptographicOperations.ZeroMemory(chunk); }
    }
}
