// AT-180: actual source/model migration checks against a candidate in independently owned fictitious files.
// This executable is never referenced by the app and never reads its storage or credentials.
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;
namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Runs actual original model/migrations against independently owned fictitious encrypted files.</summary>
internal static class SchemaCompatibility
{
    /// <summary>Verifies original schema, financial packets and refusal behavior beneath the caller-owned root.</summary>
    internal static async Task<string> RunAsync(string evidencePath, string ownedRoot)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        var evidenceRoot = Path.GetFullPath(evidencePath);
        var expectedRoot = Path.GetFullPath(ownedRoot).TrimEnd(Path.DirectorySeparatorChar);
        if (!evidenceRoot.StartsWith(expectedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Keep fictitious schema evidence beneath the independent caller-owned root.");
        Directory.CreateDirectory(evidenceRoot);
        var folder = Path.Combine(evidenceRoot, "fictitious-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var file = Path.Combine(folder, "profile.db");
        // D-44: use the actual data composition before loading its compiled model; Mono otherwise joins its own static initialization.
        _ = new ServiceCollection().AddZananceData(file);
        var connectionString = new SqliteConnectionStringBuilder { DataSource = file, Password = password, Pooling = false }.ToString();
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, "stage.txt"), "Creating actual compiled-model options");
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connectionString).UseModel(ZananceDbContextModel.Instance).Options;
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, "stage.txt"), "Opening native connection");
        string cipherVersion, providerVersion;
        await using (var native = new SqliteConnection(connectionString))
        {
            await native.OpenAsync();
            using var command = native.CreateCommand(); command.CommandText = "PRAGMA cipher_version"; cipherVersion = Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
            command.CommandText = "PRAGMA cipher_provider_version"; providerVersion = Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
            if (!cipherVersion.StartsWith("4.19.0", StringComparison.Ordinal) || !providerVersion.Contains("3.5.9", StringComparison.Ordinal)) throw new InvalidOperationException("Candidate native identity is not the reviewed cipher/provider.");
        }
        var accountId = Guid.CreateVersion7(); var entryId = Guid.CreateVersion7(); var settingsId = Guid.CreateVersion7(); var migrationCount = 0;
        await using (var db = new ZananceDbContext(options))
        {
            await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = "PRAGMA foreign_keys";
            if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("Original default foreign keys disabled.");
            var migrations = db.Database.GetMigrations().ToList(); migrationCount = migrations.Count;
            await db.GetService<IMigrator>().MigrateAsync(migrations[0]);
            await db.Database.ExecuteSqlAsync($"INSERT INTO Settings (Id, ReportCurrencyCode, Mode, BudgetCalendar, WeekStart, ReminderDaysBefore, ReminderTime, NotificationsShowDetails, AppLockEnabled, OnboardingCompleted, CreatedAt, UpdatedAt) VALUES ({settingsId}, 'EUR', 0, 0, 1, 3, '09:00:00', 0, 0, 1, 0, 0)");
            await db.Database.ExecuteSqlAsync($"INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ({accountId}, 'Fictitious بانک Ü', 0, 'EUR', 1000, '2026-01-01', 1, 1, 0, 0, 0, 0)");
            await db.Database.ExecuteSqlAsync($"INSERT INTO Entries (Id, Kind, Date, AccountId, Amount, Review, Source, CreatedAt, UpdatedAt) VALUES ({entryId}, 1, '2026-02-01', {accountId}, 250, 0, 0, 0, 0)");
            await db.Database.MigrateAsync();
        }
        await using (var db = new ZananceDbContext(options))
        {
            if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Original migrations incomplete.");
            var account = await db.Accounts.SingleAsync(); var entry = await db.Entries.SingleAsync();
            if (account.Id != accountId || account.Name != "Fictitious بانک Ü" || account.CurrencyCode != "EUR" || account.OpeningBalance != 1000 || entry.Id != entryId || entry.AccountId != accountId || entry.Amount != 250 || entry.Date != new DateOnly(2026, 2, 1)) throw new InvalidOperationException("Original schema upgrade changed its financial packets.");
            var settings = await db.Settings.SingleAsync();
            if (settings.Id != settingsId || settings.ReportCurrencyCode != "EUR" || !settings.OnboardingCompleted) throw new InvalidOperationException("Original settings packet changed.");
        }
        using var portable = new MemoryStream();
        await using (var exportSource = new SqliteConnection(connectionString))
        {
            await exportSource.OpenAsync();
            using var headers = exportSource.CreateCommand(); headers.CommandText = "PRAGMA user_version=73; PRAGMA application_id=123456; PRAGMA journal_mode=WAL;";
            await headers.ExecuteNonQueryAsync();
            await Vafadar.Data.SqlCipherPortableSnapshot.WriteAsync(exportSource, portable, 16 * 1024 * 1024);
            using var stillOpen = exportSource.CreateCommand(); stillOpen.CommandText = "SELECT COUNT(*) FROM Accounts";
            if (Convert.ToInt64(await stillOpen.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("Snapshot changed the caller's source.");
        }
        portable.Position = 0;
        await using (var memory = await Vafadar.Data.SqliteMemorySnapshot.OpenAsync(portable, 16 * 1024 * 1024))
        {
            using var read = memory.Connection.CreateCommand(); read.CommandText = "SELECT Name FROM Accounts";
            if ((string?)await read.ExecuteScalarAsync() != "Fictitious بانک Ü") throw new InvalidOperationException("Portable snapshot lost original Unicode metadata.");
            read.CommandText = "PRAGMA user_version";
            if (Convert.ToInt64(await read.ExecuteScalarAsync()) != 73) throw new InvalidOperationException("Portable header version lost.");
            read.CommandText = "PRAGMA application_id";
            if (Convert.ToInt64(await read.ExecuteScalarAsync()) != 123456) throw new InvalidOperationException("Portable application identifier lost.");
            read.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory";
            if (Convert.ToInt64(await read.ExecuteScalarAsync()) != migrationCount) throw new InvalidOperationException("Portable migration history lost.");
            var writeRefused = false;
            try { read.CommandText = "DELETE FROM Accounts"; await read.ExecuteNonQueryAsync(); } catch (SqliteException) { writeRefused = true; }
            if (!writeRefused) throw new InvalidOperationException("Portable read-only snapshot accepted writes.");
            var staged = Path.Combine(folder, "encrypted-restore.db");
            var restorePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await memory.ExportEncryptedAsync(staged, restorePassword);
            await using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = staged, Password = restorePassword, Pooling = false }.ToString());
            await target.OpenAsync();
            await Vafadar.Data.SqliteSnapshotVerifier.VerifyAsync(memory.Connection, target);
            using var restored = target.CreateCommand(); restored.CommandText = "SELECT Name FROM Accounts";
            if ((string?)await restored.ExecuteScalarAsync() != "Fictitious بانک Ü") throw new InvalidOperationException("Encrypted staged restore lost data.");
        }
        foreach (var rejectedPassword in new string?[] { null, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) })
        {
            var refused = false;
            try
            {
                var wrong = new SqliteConnectionStringBuilder { DataSource = file, Password = rejectedPassword ?? "", Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString();
                await using var connection = new SqliteConnection(wrong); await connection.OpenAsync(); using var command = connection.CreateCommand(); command.CommandText = "SELECT Name FROM Accounts"; await command.ExecuteScalarAsync();
            }
            catch (SqliteException) { refused = true; }
            if (!refused) throw new InvalidOperationException("Missing or wrong key read actual financial schema.");
        }
        if (File.ReadAllBytes(file).AsSpan().StartsWith("SQLite format 3"u8) || File.ReadAllBytes(file).AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes("Fictitious بانک Ü")) >= 0) throw new InvalidOperationException("Actual schema leaked its plaintext header or marker.");
        var tampered = Path.Combine(folder, "tampered.db"); File.Copy(file, tampered); var bytes = File.ReadAllBytes(tampered); bytes[200] ^= 1; await File.WriteAllBytesAsync(tampered, bytes);
        var tamperRefused = false;
        try
        {
            var changed = new SqliteConnectionStringBuilder { DataSource = tampered, Password = password, Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString();
            await using var connection = new SqliteConnection(changed); await connection.OpenAsync(); using var command = connection.CreateCommand(); command.CommandText = "SELECT Name FROM Accounts"; await command.ExecuteScalarAsync();
        }
        catch (SqliteException) { tamperRefused = true; }
        if (!tamperRefused) throw new InvalidOperationException("Modified candidate page was accepted.");
        var proof = new { CipherVersion = cipherVersion, ProviderVersion = providerVersion, WrongMissingKeyAndTamperRefused = true, PlaintextHeaderAndUnicodeMarkerAbsent = true, ActualDataAndSharedDataSourcesCompiled = true, ActualCompiledModelUsed = true, FirstSchemaToCurrentMigrations = migrationCount, CorrectKeyReopenUnicodeAndOriginalMoneyDate = true, ForeignKeysRemainEnabled = true, FictitiousFilesOnly = true, PortableMemorySnapshotHeaderSchemaAndEncryptedRestore = true, CompleteStagingSchemaAndTypedRowsVerified = true, NotAppKeyRecoveryCrashOrMobileAcceptance = true };
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, "ef-proof.json"), JsonSerializer.Serialize(proof, new JsonSerializerOptions { WriteIndented = true })); return JsonSerializer.Serialize(proof);
    }
}
