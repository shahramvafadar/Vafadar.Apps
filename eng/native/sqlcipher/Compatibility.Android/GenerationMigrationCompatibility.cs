using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Runs the original complete native session using only this package's fictitious profiles and keys.</summary>
internal static class GenerationMigrationCompatibility
{
    /// <summary>Creates a real original model or resumes/interrupts its captured native migration.</summary>
    internal static async Task<string> RunAsync(string folder, string phase)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        var contextPath = Path.Combine(folder, "migration-context.txt");
        DatabaseGenerationCheckpoint initial;
        Guid installation;
        var original = Path.Combine(folder, "profile.db");
        if (phase == "migration-create")
        {
            if (Directory.Exists(folder)) throw new InvalidOperationException("Use a fresh migration proof folder.");
            Directory.CreateDirectory(folder);
            initial = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()); installation = Guid.NewGuid();
            await File.WriteAllTextAsync(contextPath, string.Join("\n", initial.ProfileId.ToString("N"), initial.OriginalGeneration.ToString("N"),
                initial.CandidateGeneration.ToString("N"), installation.ToString("N")));
            await CreateOriginalAsync(original);
            await File.WriteAllTextAsync(Path.Combine(folder, "original-digest.txt"), Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))));
        }
        else
        {
            var ids = (await File.ReadAllTextAsync(contextPath)).Split('\n');
            initial = new(Guid.ParseExact(ids[0], "N"), Guid.ParseExact(ids[1], "N"), Guid.ParseExact(ids[2], "N"));
            installation = Guid.ParseExact(ids[3], "N");
        }
        var journal = new SqliteDatabaseGenerationJournal(Path.Combine(folder, "migration.sqlite"), initial);
        var keys = new AndroidGenerationKeyStore(folder);
        var session = new SqlCipherGenerationMigrationSession(initial, installation, original, journal, keys,
            new AndroidGenerationFileDurability(folder), () => { if (!Directory.Exists(folder)) throw new IOException("Retired fictitious lease."); },
            16 * 1024 * 1024, "4.19.0");
        if (phase == "migration-create")
        {
            await journal.CreateNewAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, "candidate-path.txt"), Path.GetFileName(session.CandidatePath));
            return "{\"OriginalModelCreated\":true,\"Migrations\":32}";
        }
        if (phase == "migration-key-prepare")
        {
            await session.PrepareKeyAsync(initial, CancellationToken.None);
            var prepared = initial.Advance(initial.ProfileId, initial.CandidateGeneration, DatabaseGenerationPhase.KeyPrepared);
            await session.PersistCheckpointAsync(initial, prepared, CancellationToken.None);
            var envelope = Path.Combine(folder, initial.ProfileId.ToString("N") + "-" + initial.CandidateGeneration.ToString("N") + ".key");
            await File.WriteAllTextAsync(Path.Combine(folder, "envelope-digest.txt"), Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(envelope))));
            return "{\"KeyDurablyPrepared\":true}";
        }
        if (phase == "migration-check-interruption")
        {
            var state = await journal.ReadAsync();
            if (state.Phase != DatabaseGenerationPhase.KeyPrepared || !File.Exists(session.CandidatePath) || !File.Exists(original))
                throw new IOException("Actual export interruption boundary was not retained.");
            await CheckDigestsAsync(folder, original, initial);
            return "{\"InterruptedExportBeforeSelection\":true,\"OriginalAndKeyPreserved\":true}";
        }
        if (phase == "migration-missing-key")
        {
            var selected = await journal.ReadAsync();
            if (selected.Phase != DatabaseGenerationPhase.Selected) throw new IOException("Missing-key proof requires real Selected.");
            var envelope = Path.Combine(folder, initial.ProfileId.ToString("N") + "-" + initial.CandidateGeneration.ToString("N") + ".key");
            File.Delete(envelope); // This package owns this exact generated fixture envelope only.
            var refused = false;
            try { await DatabaseGenerationMigration.ResumeAsync(initial, session); }
            catch (Exception error) when (error is CryptographicException or global::Android.Systems.ErrnoException or IOException) { refused = true; }
            if (!refused || File.Exists(envelope) || !File.Exists(original) || await journal.ReadAsync() != selected)
                throw new IOException("Missing selected key was replaced or original was cleaned/fallback selected.");
            return "{\"MissingSelectedKeyRefused\":true,\"NoReplacementOrPlaintextFallback\":true}";
        }
        if (phase is not ("migration-resume" or "migration-factory" or "migration-export-kill" or "migration-selected-kill"))
            throw new ArgumentException("Unknown complete migration phase.");
        var port = phase == "migration-selected-kill" ? (IDatabaseGenerationMigrationSession)new SelectionBarrier(session, folder) : session;
        var selectedGeneration = await DatabaseGenerationMigration.ResumeAsync(initial, port);
        if (phase is not ("migration-resume" or "migration-factory")) throw new IOException("Runner did not interrupt its live migration at the required boundary.");
        var stateAfter = await journal.ReadAsync();
        if (selectedGeneration != initial.CandidateGeneration || stateAfter.Phase != DatabaseGenerationPhase.Completed ||
            new[] { "", "-wal", "-shm", "-journal" }.Any(suffix => File.Exists(original + suffix)) ||
            !File.Exists(session.RollbackPath) || await File.ReadAllTextAsync(Path.Combine(folder, "unrelated.txt")) != "Keep this fictitious unrelated file")
            throw new IOException("Complete migration did not preserve ownership/selection/rollback.");
        var binding = new DatabaseKeyBinding(initial.ProfileId, installation, initial.CandidateGeneration);
        var key = await keys.ReadAsync(binding, CancellationToken.None);
        try
        {
            if (phase == "migration-factory") await CheckProtectedFactoryAsync(session.CandidatePath, key);
            _ = new ServiceCollection().AddZananceData(session.CandidatePath);
            var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(new SqliteConnectionStringBuilder
            { DataSource = session.CandidatePath, Password = Convert.ToBase64String(key), Pooling = false }.ToString()).UseModel(ZananceDbContextModel.Instance).Options;
            await using var db = new ZananceDbContext(options);
            var account = await db.Accounts.SingleAsync(); var entry = await db.Entries.SingleAsync();
            if (account.Name != (phase == "migration-factory" ? "Fictitious protected factory" : "Fictitious بانک Ü") || account.OpeningBalance != 1000 || account.CurrencyCode != "EUR" ||
                entry.Amount != 250 || entry.Date != new DateOnly(2026, 2, 1) || (await db.Database.GetPendingMigrationsAsync()).Any())
                throw new IOException("Selected actual model changed financial packets or migrations.");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
        foreach (var path in new[] { session.CandidatePath, session.RollbackPath })
        {
            var content = await File.ReadAllBytesAsync(path);
            if (content.AsSpan().StartsWith("SQLite format 3\0"u8) || content.AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes("Fictitious بانک Ü")) >= 0)
                throw new IOException("Selected or rollback file leaked the fictitious marker.");
        }
        if (phase == "migration-factory") return "{\"ProtectedFactoryReadWriteAndRetirement\":true,\"LegacyBackupRefusedBeforeData\":true}";
        return "{\"CompleteMigration\":true,\"OriginalModelAndMoneyPreserved\":true,\"EncryptedRollbackRetained\":true,\"ExactPlaintextCleanup\":true}";
    }

    private static async Task CheckProtectedFactoryAsync(string path, byte[] key)
    {
        using var services = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        SqliteConnection Create() => new(new SqliteConnectionStringBuilder
        { DataSource = path, Password = Convert.ToBase64String(key), Mode = SqliteOpenMode.ReadWrite, Pooling = false, ForeignKeys = true }.ToString());
        location.MoveToProtected(path, Create, "4.19.0");
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using var first = await factory.CreateDbContextAsync();
        var account = await first.Accounts.SingleAsync(); var entry = await first.Entries.SingleAsync();
        if (account.OpeningBalance != 1000 || entry.Amount != 250) throw new IOException("Actual factory changed financial packets.");
        account.Name = "Fictitious protected factory";
        await first.SaveChangesAsync();
        await using (var second = await factory.CreateDbContextAsync())
            if ((await second.Accounts.SingleAsync()).Name != "Fictitious protected factory") throw new IOException("Actual protected factory write lost.");
        location.MoveToProtected(path, Create, "4.19.0");
        var refused = false;
        try { await first.Accounts.SingleAsync(); } catch (InvalidOperationException) { refused = true; }
        if (!refused) throw new IOException("Already-open retired protected context still queried.");
        refused = false;
        try { location.MoveTo(path); } catch (InvalidOperationException) { refused = true; }
        if (!refused) throw new IOException("Protected location fell back to unkeyed access.");
        using var output = new MemoryStream();
        refused = false;
        try { await services.GetRequiredService<Vafadar.Backup.IBackupSource>().WriteAsync(output, CancellationToken.None); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused || output.Length != 0) throw new IOException("Legacy keyed backup consumed or wrote data.");
    }

    private static async Task CreateOriginalAsync(string original)
    {
        // Actual original registration and compiled model (D-44), not a duplicate financial schema or mapper.
        _ = new ServiceCollection().AddZananceData(original);
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(new SqliteConnectionStringBuilder
        { DataSource = original, Pooling = false }.ToString()).UseModel(ZananceDbContextModel.Instance).Options;
        await using var db = new ZananceDbContext(options);
        var migrations = db.Database.GetMigrations().ToList();
        if (migrations.Count != 32) throw new IOException("Review the actual migration fixture baseline.");
        await db.GetService<IMigrator>().MigrateAsync(migrations[0]);
        var account = Guid.NewGuid(); var entry = Guid.NewGuid(); var settings = Guid.NewGuid();
        await db.Database.ExecuteSqlAsync($"INSERT INTO Settings (Id, ReportCurrencyCode, Mode, BudgetCalendar, WeekStart, ReminderDaysBefore, ReminderTime, NotificationsShowDetails, AppLockEnabled, OnboardingCompleted, CreatedAt, UpdatedAt) VALUES ({settings}, 'EUR', 0, 0, 1, 3, '09:00:00', 0, 0, 1, 0, 0)");
        await db.Database.ExecuteSqlAsync($"INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ({account}, 'Fictitious بانک Ü', 0, 'EUR', 1000, '2026-01-01', 1, 1, 0, 0, 0, 0)");
        await db.Database.ExecuteSqlAsync($"INSERT INTO Entries (Id, Kind, Date, AccountId, Amount, Review, Source, CreatedAt, UpdatedAt) VALUES ({entry}, 1, '2026-02-01', {account}, 250, 0, 0, 0, 0)");
        await db.Database.MigrateAsync();
        await db.Database.OpenConnectionAsync();
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "CREATE TABLE fictitious_packets(amount INTEGER, label TEXT, bytes BLOB); INSERT INTO fictitious_packets VALUES(-9223372036854775808,'Unicode بانک Ü',zeroblob(2097152)); PRAGMA user_version=73; PRAGMA application_id=123456";
        await command.ExecuteNonQueryAsync();
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(original)!, "unrelated.txt"), "Keep this fictitious unrelated file");
    }

    private static async Task CheckDigestsAsync(string folder, string original, DatabaseGenerationCheckpoint initial)
    {
        if (Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(original))) != await File.ReadAllTextAsync(Path.Combine(folder, "original-digest.txt")))
            throw new IOException("Interrupted migration changed original bytes.");
        var envelope = Path.Combine(folder, initial.ProfileId.ToString("N") + "-" + initial.CandidateGeneration.ToString("N") + ".key");
        if (Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(envelope))) != await File.ReadAllTextAsync(Path.Combine(folder, "envelope-digest.txt")))
            throw new IOException("Interrupted export replaced its prepared key envelope.");
    }

    private sealed class SelectionBarrier(IDatabaseGenerationMigrationSession inner, string folder) : IDatabaseGenerationMigrationSession
    {
        public void ValidateContext() => inner.ValidateContext();
        public Task<DatabaseGenerationCheckpoint> ReadCheckpointAsync(CancellationToken ct) => inner.ReadCheckpointAsync(ct);
        public Task PrepareKeyAsync(DatabaseGenerationCheckpoint state, CancellationToken ct) => inner.PrepareKeyAsync(state, ct);
        public Task CreateAndVerifyCandidateAsync(DatabaseGenerationCheckpoint state, CancellationToken ct) => inner.CreateAndVerifyCandidateAsync(state, ct);
        public Task ReopenCandidateAsync(DatabaseGenerationCheckpoint state, CancellationToken ct) => inner.ReopenCandidateAsync(state, ct);
        public Task CleanupOriginalAsync(DatabaseGenerationCheckpoint state, CancellationToken ct) => inner.CleanupOriginalAsync(state, ct);
        public async Task PersistCheckpointAsync(DatabaseGenerationCheckpoint expected, DatabaseGenerationCheckpoint next, CancellationToken ct)
        {
            await inner.PersistCheckpointAsync(expected, next, ct);
            if (next.Phase == DatabaseGenerationPhase.Selected)
            {
                await File.WriteAllTextAsync(Path.Combine(folder, "ready-selected.json"), "{\"ActualSelectedCheckpoint\":true}");
                await Task.Delay(Timeout.Infinite, ct);
            }
        }
    }
}
