using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Original native historical content survives resident-buffer retirement, while unsafe archive redirects and portable authority leakage refuse.</summary>
internal static class ArchiveQualification
{
    /// <summary>Continues only the original retained native fixture; it never recreates keys, source files or financial history.</summary>
    internal static async Task RunAsync(ProtectedClient client, HouseholdJournal journal, HouseholdSyncCoordinator coordinator,
        HouseholdServiceTransport transport, HouseholdJournalState state, string root, bool cold, Action<string> proof, CancellationToken ct, bool resume = false)
    {
        var original = JsonSerializer.Deserialize<FinancialProof>(await File.ReadAllTextAsync(Path.Combine(root, "public-financial-proof.json"), ct)) ?? throw new InvalidDataException();
        void Require(bool value) { if (!value) throw new InvalidDataException("The current original historical attachment invariant failed."); }
        var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        if (!cold)
        {
            Require(state.Cursor == 7 && await journal.ReadAttemptAsync(state.Authority, ct) is null);
            if (resume)
            { Require(await client.Store.GetAttachmentAsync(original.Attachment, ct) is null); proof("same-original-queued-attachment-deletion-continues-after-additive-native-schema-upgrade"); }
            else
            {
                Require((await client.Store.GetAttachmentAsync(original.Attachment, ct))?.Data.Length > 2 * 1024 * 1024);
                await client.Store.DeleteAttachmentAsync(original.Attachment, ct);
            }
            Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
        }
        using var replica = await journal.ReadReplicaAsync(state.Authority, ct);
        Require(replica.Cursor == 8 && replica.ResidentAttachmentBytes == 1800);
        Require(await client.Store.GetAttachmentAsync(original.Attachment, ct) is null);
        using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            var manifest = replica.AttachmentManifest(original.AttachmentHash);
            Require(manifest.Parts.Count == 2);
            for (var index = 0; index < manifest.Parts.Count; index++)
            {
                var bytes = await replica.ReadAttachmentPieceAsync(original.AttachmentHash, index, ct);
                try { hash.AppendData(bytes); } finally { CryptographicOperations.ZeroMemory(bytes); }
                Require(replica.ResidentAttachmentBytes == 1800);
            }
            Require(Convert.ToHexStringLower(hash.GetHashAndReset()) == original.AttachmentHash);
        }
        proof(cold ? "cold-original-historical-attachment-reads-exact-authenticated-ciphertext-without-returning-to-resident-memory"
            : "unreferenced-historical-attachment-leaves-resident-memory-and-remains-byte-exact-through-bounded-authenticated-archive-reads");
        if (cold) return;

        var cursor = replica.Cursor; var head = replica.PublicationHead; int savedEvent;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var source = await db.Set<HouseholdLocalBlobReference>().SingleAsync(row => row.Hash == original.AttachmentHash && row.PieceIndex == 0, ct);
            savedEvent = source.EventIndex; Require(savedEvent > 0);
            source.EventIndex = 0; await db.SaveChangesAsync(ct);
        }
        try
        {
            var readRefused = false;
            try { var unexpected = await replica.ReadAttachmentPieceAsync(original.AttachmentHash, 0, ct); CryptographicOperations.ZeroMemory(unexpected); }
            catch (CryptographicException) { readRefused = true; }
            Require(readRefused);
            var reindexRefused = false;
            try { using var unexpected = await journal.ReadReplicaAsync(state.Authority, ct); }
            catch (CryptographicException) { reindexRefused = true; }
            Require(reindexRefused && replica.Cursor == cursor && replica.PublicationHead == head && replica.ResidentAttachmentBytes == 1800);
        }
        finally
        {
            // Restore only this fixture's explicitly captured derived pointer after the negative
            // case. The implementation itself never silently replaces an inconsistent pointer.
            await using var db = await factory.CreateDbContextAsync(ct);
            var source = await db.Set<HouseholdLocalBlobReference>().SingleAsync(row => row.Hash == original.AttachmentHash && row.PieceIndex == 0, ct);
            Require(source.EventIndex == 0); source.EventIndex = savedEvent; await db.SaveChangesAsync(ct);
        }
        proof("altered-original-archive-source-refused-before-content-return-and-derived-cache-is-not-silently-repaired");

        using var output = new MemoryStream();
        await client.Services.GetServices<IBackupSource>().Single(source => source.Name == "database.sqlite").WriteAsync(output, ct);
        output.Position = 0;
        await using (var portable = await SqliteMemorySnapshot.OpenAsync(output, 64 * 1024 * 1024, ct))
        {
            var names = new List<string>();
            await using (var command = portable.Connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name LIKE 'HouseholdLocal%' ORDER BY name";
                await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) names.Add(reader.GetString(0));
            }
            Require(names.ToHashSet(StringComparer.Ordinal).SetEquals(HouseholdLocalModel.PortableTables));
            foreach (var name in names)
            {
                Require(name.All(char.IsAsciiLetterOrDigit));
                await using var command = portable.Connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM \"" + name + "\"";
                Require(Convert.ToInt64(await command.ExecuteScalarAsync(ct)) == 0);
            }
        }
        using (var keys = await journal.ReadKeysAsync(state.Authority, ct))
        {
            var frame = keys.Device.ExportForProtectedStorage();
            try { Require(output.GetBuffer().AsSpan(0, (int)output.Length).IndexOf(frame) < 0); }
            finally { CryptographicOperations.ZeroMemory(frame); }
        }
        await using (var db = await factory.CreateDbContextAsync(ct))
            Require(await db.Set<HouseholdLocalBlobReference>().AnyAsync(row => row.Hash == original.AttachmentHash, ct));
        proof("original-portable-memory-export-scrubs-all-current-household-authority-and-archive-tables-without-changing-original-ciphertext-history");
    }
}
