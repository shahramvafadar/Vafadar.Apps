using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;
using Vafadar.Data;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Exercises actual journal commit and native hot-journal recovery in distinct fictitious processes.</summary>
internal static class JournalCompatibility
{
    /// <summary>Runs one explicit create, kill-barrier or reopen phase inside an independently owned folder.</summary>
    internal static async Task<string> RunAsync(string folder, string phase)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        var context = Path.Combine(folder, "journal-context.txt");
        DatabaseGenerationCheckpoint initial;
        if (phase == "journal-create")
        {
            if (Directory.Exists(folder)) throw new InvalidOperationException("Use a fresh journal proof folder.");
            Directory.CreateDirectory(folder);
            initial = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await File.WriteAllTextAsync(context, string.Join("\n", initial.ProfileId.ToString("N"),
                initial.OriginalGeneration.ToString("N"), initial.CandidateGeneration.ToString("N")));
        }
        else
        {
            var ids = (await File.ReadAllTextAsync(context)).Split('\n');
            initial = new(Guid.ParseExact(ids[0], "N"), Guid.ParseExact(ids[1], "N"), Guid.ParseExact(ids[2], "N"));
        }
        var path = Path.Combine(folder, "fictitious-generation.sqlite");
        var store = new SqliteDatabaseGenerationJournal(path, initial);
        var verified = Next(Next(initial));
        if (phase == "journal-create")
        {
            await store.CreateNewAsync();
            await store.CompareAndCommitAsync(initial, Next(initial));
            await store.CompareAndCommitAsync(Next(initial), verified);
            return "{\"JournalCreated\":true}";
        }
        if (phase == "journal-uncommitted")
        {
            if (await store.ReadAsync() != verified) throw new InvalidOperationException("Unexpected pre-kill journal phase.");
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString());
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=DELETE; PRAGMA synchronous=EXTRA";
            await command.ExecuteNonQueryAsync();
            using var transaction = connection.BeginTransaction(deferred: false);
            command.Transaction = transaction;
            command.CommandText = "UPDATE generation_state SET phase=3 WHERE id=1";
            await command.ExecuteNonQueryAsync();
            // Force original native pager pages onto disk while the rollback journal is still active.
            if (FlushNative(connection.Handle!.DangerousGetHandle()) != 0)
                throw new IOException("Native uncommitted pager flush failed.");
            // Windows SQLite keeps the journal open for writing; observation must allow its existing write handle.
            using (var hot = new FileStream(path + "-journal", FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                var header = new byte[8];
                await hot.ReadExactlyAsync(header);
                if (hot.Length < 512 || !header.AsSpan().SequenceEqual(new byte[] { 0xd9, 0xd5, 0x05, 0xf9, 0x20, 0xa1, 0x63, 0xd7 }))
                    throw new IOException("Kill barrier requires an actual native hot journal.");
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "ready-uncommitted.json"), "{\"HotJournalFlushed\":true}");
            await Task.Delay(Timeout.Infinite); // Only the external runner terminates this process; no managed rollback runs.
        }
        if (phase == "journal-committed")
        {
            await store.CompareAndCommitAsync(verified, Next(verified));
            await File.WriteAllTextAsync(Path.Combine(folder, "ready-committed.json"), "{\"SelectionCommitted\":true}");
            await Task.Delay(Timeout.Infinite);
        }
        var expected = phase switch
        {
            "journal-reopen-rollback" => verified,
            "journal-reopen-selected" => Next(verified),
            _ => throw new ArgumentException("Unknown journal proof phase.")
        };
        var reopened = await store.ReadAsync();
        if (reopened != expected) throw new IOException("Native journal recovery selected the wrong generation.");
        return "{\"JournalReopened\":true,\"CandidateSelected\":" + (reopened.CandidateSelected ? "true" : "false") + "}";
    }

    private static DatabaseGenerationCheckpoint Next(DatabaseGenerationCheckpoint state) =>
        state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);

    // Independent fixture only: official SQLite API, absent from the managed wrapper, using the reviewed loaded candidate.
    [DllImport("sqlcipher", EntryPoint = "sqlite3_db_cacheflush", CallingConvention = CallingConvention.Cdecl)]
    private static extern int FlushNative(IntPtr database);
}
