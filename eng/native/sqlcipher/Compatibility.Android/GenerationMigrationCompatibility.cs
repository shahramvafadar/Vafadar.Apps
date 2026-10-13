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
        if (phase is "restore-session" or "restore-session-reopen")
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
            return phase == "restore-session"
                ? await Vafadar.SqlCipher.Compatibility.RestoreSessionCompatibility.RunAsync(folder,
                    path => new AndroidGenerationKeyStore(path), path => new AndroidGenerationFileDurability(path))
                : await Vafadar.SqlCipher.Compatibility.RestoreSessionCompatibility.ReopenAsync(folder,
                    path => new AndroidGenerationKeyStore(path), path => new AndroidGenerationFileDurability(path));
        }
        if (phase is "fresh-startup-create" or "fresh-startup-reopen" or "fresh-startup-notification")
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
            return await Vafadar.SqlCipher.Compatibility.FreshStartupCompatibility.RunAsync(folder, phase == "fresh-startup-create",
                path => new AndroidGenerationKeyStore(path), path => new AndroidGenerationFileDurability(path), phase == "fresh-startup-notification");
        }
        if (phase is "startup-create" or "startup-reopen")
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
            return await Vafadar.SqlCipher.Compatibility.ProtectedStartupCompatibility.RunAsync(folder, phase == "startup-create",
                path => new AndroidGenerationKeyStore(path), path => new AndroidGenerationFileDurability(path));
        }
        if (phase is "safety-create" or "safety-reopen")
            return await SafetyCopyCompatibility.RunAsync(folder, phase == "safety-create");
        if (phase is "metadata-journal" or "metadata-history" or "metadata-reopen")
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
            return await MetadataPublicationCompatibility.RunAsync(folder, phase);
        }
        if (phase is "settings-create" or "settings-reopen")
            return await SettingsPersistenceCompatibility.RunAsync(folder, phase == "settings-create");
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        if (phase is "profile-create-flow" or "profile-create-cold")
            return await ProfileCreationCompatibility.RunAsync(folder, phase == "profile-create-flow");
        if (phase is "new-profile-create" or "new-profile-resume" or "new-profile-reopen")
            return await NewProfileCompatibility.RunAsync(folder, phase);
        if (phase is "all-profiles-create" or "all-profiles-reopen")
            return await ProfileBootstrapCompatibility.RunAsync(folder, phase == "all-profiles-create");
        if (phase is "bootstrap-intent" or "bootstrap-create" or "bootstrap-reopen") return await InitialGenerationCompatibility.RunAsync(folder, phase);
        if (phase is "identity-create" or "identity-reopen") return await IdentityCatalogueCompatibility.RunAsync(folder, phase == "identity-create");
        if (phase == "profile-maintenance") return await ProfileMaintenanceCompatibility.RunAsync(folder);
        if (phase == "restore-staging") return await CheckRestoreStagingAsync(folder);
        if (phase == "restore-select") return await CheckRestoreStagingAsync(folder, select: true);
        if (phase == "restore-selected-resume") return await ResumeRestoreSelectionAsync(folder);
        if (phase == "package-restore") return await CheckRestoreStagingAsync(folder, select: true, package: true);
        if (phase == "package-resume") return await ResumePackageAsync(folder);
        if (phase == "package-applied-reopen") return await ResumePackageAsync(folder, applied: true);
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
        if (phase == "migration-factory") return "{\"ProtectedFactoryReadWriteAndRetirement\":true,\"ProtectedPortableBackupVerified\":true,\"RetiredExportStopped\":true,\"LegacyRestoreRefusedBeforeData\":true}";
        return "{\"CompleteMigration\":true,\"OriginalModelAndMoneyPreserved\":true,\"EncryptedRollbackRetained\":true,\"ExactPlaintextCleanup\":true}";
    }

    private static async Task<string> CheckRestoreStagingAsync(string folder, bool select = false, bool package = false)
    {
        if (Directory.Exists(folder)) throw new IOException("Use a fresh restore fixture directory.");
        Directory.CreateDirectory(folder);
        var sourcePath = Path.Combine(folder, "fictitious-old-backup.db");
        _ = new ServiceCollection().AddZananceData(sourcePath);
        var sourceOptions = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(new SqliteConnectionStringBuilder
        { DataSource = sourcePath, Pooling = false }.ToString()).UseModel(ZananceDbContextModel.Instance).Options;
        var accountId = Guid.NewGuid(); var entryId = Guid.NewGuid(); var settingsId = Guid.NewGuid();
        await using (var old = new ZananceDbContext(sourceOptions))
        {
            var migrations = old.Database.GetMigrations().ToList();
            if (migrations.Count != 32) throw new IOException("Review actual restored migration baseline.");
            await old.GetService<IMigrator>().MigrateAsync(migrations[0]);
            await old.Database.ExecuteSqlAsync($"INSERT INTO Settings (Id, ReportCurrencyCode, Mode, BudgetCalendar, WeekStart, ReminderDaysBefore, ReminderTime, NotificationsShowDetails, AppLockEnabled, OnboardingCompleted, CreatedAt, UpdatedAt) VALUES ({settingsId}, 'EUR', 0, 0, 1, 3, '09:00:00', 0, 0, 1, 0, 0)");
            await old.Database.ExecuteSqlAsync($"INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ({accountId}, 'Fictitious old restore بانک Ü', 0, 'EUR', 9100, '2025-01-01', 1, 1, 0, 0, 0, 0)");
            await old.Database.ExecuteSqlAsync($"INSERT INTO Entries (Id, Kind, Date, AccountId, Amount, Review, Source, CreatedAt, UpdatedAt) VALUES ({entryId}, 1, '2025-01-19', {accountId}, 730, 0, 0, 0, 0)");
        }
        var digest = SHA256.HashData(await File.ReadAllBytesAsync(sourcePath));
        using var portable = new MemoryStream();
        await using (var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sourcePath, Pooling = false, Mode = SqliteOpenMode.ReadWrite }.ToString()))
        {
            await source.OpenAsync();
            await SqlCipherPortableSnapshot.WriteAsync(source, portable, 16 * 1024 * 1024);
        }
        portable.Position = 0;
        DatabaseProfileIdentity? packageIdentity = null;
        if (package)
        {
            var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => { });
            await identities.CreateNewAsync([""]);
            packageIdentity = (await identities.ReadAsync()).Single(row => row.LegacyProfileId == "");
        }
        var binding = new DatabaseKeyBinding(packageIdentity?.ProfileId ?? Guid.NewGuid(), packageIdentity?.InstallationId ?? Guid.NewGuid(), Guid.NewGuid());
        var keys = new AndroidGenerationKeyStore(folder);
        var candidate = Path.Combine(folder, "fictitious-restored.cipher.db");
        var upgraded = false;
        await SqlCipherRestoreStaging.PrepareAsync(portable, candidate, binding, keys, new AndroidGenerationFileDurability(folder),
            async (connection, ct) =>
            {
                var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false)
                    .UseModel(ZananceDbContextModel.Instance).Options;
                await using var context = new ZananceDbContext(options);
                await context.Database.MigrateAsync(ct);
                if ((await context.Database.GetPendingMigrationsAsync(ct)).Any()) throw new IOException("Restored actual migrations incomplete.");
                upgraded = true;
            }, () => { if (!Directory.Exists(folder)) throw new IOException("Retired fixture lease."); }, 16 * 1024 * 1024, "4.19.0");
        var key = await keys.ReadAsync(binding, CancellationToken.None);
        try
        {
            var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(new SqliteConnectionStringBuilder
            { DataSource = candidate, Pooling = false, Mode = SqliteOpenMode.ReadOnly, Password = Convert.ToBase64String(key) }.ToString())
                .UseModel(ZananceDbContextModel.Instance).Options;
            await using var current = new ZananceDbContext(options);
            var account = await current.Accounts.SingleAsync(); var entry = await current.Entries.SingleAsync(); var settings = await current.Settings.SingleAsync();
            if (!upgraded || account.Id != accountId || account.Name != "Fictitious old restore بانک Ü" || account.OpeningBalance != 9100 ||
                entry.Id != entryId || entry.Amount != 730 || entry.Date != new DateOnly(2025, 1, 19) || settings.Id != settingsId || !settings.OnboardingCompleted)
                throw new IOException("Restored actual financial/profile packet changed.");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
        var afterDigest = SHA256.HashData(await File.ReadAllBytesAsync(sourcePath));
        if (!CryptographicOperations.FixedTimeEquals(digest, afterDigest))
            throw new IOException("Restore staging changed original backup source.");
        var encrypted = await File.ReadAllBytesAsync(candidate);
        if (encrypted.AsSpan().StartsWith("SQLite format 3"u8) || encrypted.AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes("Fictitious old restore بانک Ü")) >= 0)
            throw new IOException("Restored staging leaked its plain marker.");
        if (select)
        {
            var oldBinding = new DatabaseKeyBinding(binding.ProfileId, binding.InstallationId, packageIdentity?.CandidateGeneration ?? Guid.NewGuid());
            var currentPath = Path.Combine(folder, "fictitious-current.cipher.db");
            portable.Position = 0;
            await SqlCipherRestoreStaging.PrepareAsync(portable, currentPath, oldBinding, keys, new AndroidGenerationFileDurability(folder),
                async (connection, ct) =>
                {
                    var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options;
                    await using var db = new ZananceDbContext(options); await db.Database.MigrateAsync(ct);
                }, () => { if (!Directory.Exists(folder)) throw new IOException("Retired fixture lease."); }, 16 * 1024 * 1024, "4.19.0");
            var oldKey = await keys.ReadAsync(oldBinding, CancellationToken.None);
            try
            {
                using var services = new ServiceCollection().AddZananceData(currentPath).BuildServiceProvider();
                var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                location.MoveToProtected(currentPath, () => new SqliteConnection(new SqliteConnectionStringBuilder
                { DataSource = currentPath, Password = Convert.ToBase64String(oldKey), Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString()), "4.19.0");
                await using var current = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
                var account = await current.Accounts.SingleAsync(); account.Name = "Fictitious before restore";
                var entry = await current.Entries.SingleAsync(); entry.Amount = 250; await current.SaveChangesAsync();
                await current.Database.OpenConnectionAsync();
                var captured = new DatabaseGenerationCheckpoint(binding.ProfileId, oldBinding.GenerationId, binding.GenerationId);
                var journal = new SqliteDatabaseGenerationJournal(Path.Combine(folder, "restore-selection.sqlite"), captured);
                await journal.CreateNewAsync();
                await File.WriteAllTextAsync(Path.Combine(folder, "restore-bindings.txt"), string.Join("\n", binding.ProfileId.ToString("N"), binding.InstallationId.ToString("N"), oldBinding.GenerationId.ToString("N"), binding.GenerationId.ToString("N")));
                if (package)
                {
                    await RestorePackageAsync(folder, sourcePath, captured, journal, binding, keys,
                        (SqliteConnection)current.Database.GetDbConnection());
                    throw new IOException("Package interruption barrier unexpectedly returned.");
                }
                var selected = await SqlCipherRestoreSelection.SelectAsync(captured, journal, candidate, binding,
                    (SqliteConnection)current.Database.GetDbConnection(), keys, new AndroidGenerationFileDurability(folder),
                    () => { if (!Directory.Exists(folder)) throw new IOException("Retired fixture lease."); }, 16 * 1024 * 1024, "4.19.0");
                if (selected.Phase != DatabaseGenerationPhase.Selected) throw new IOException("Actual restore selection was not committed.");
                await File.WriteAllTextAsync(Path.Combine(folder, "restore-selected.json"), "{\"ActualRestoreSelected\":true}");
                await Task.Delay(Timeout.Infinite); // External runner kills after actual selection, before application publication.
            }
            finally { CryptographicOperations.ZeroMemory(oldKey); }
        }
        return "{\"EncryptedRestoreStaging\":true,\"FirstSchemaTo32Migrations\":true,\"OriginalFinancialPacketsPreserved\":true,\"OriginalSourceUnchanged\":true,\"NoLiveSelection\":true}";
    }

    private static async Task<string> ResumeRestoreSelectionAsync(string folder)
    {
        var ids = (await File.ReadAllTextAsync(Path.Combine(folder, "restore-bindings.txt"))).Split('\n');
        var binding = new DatabaseKeyBinding(Guid.ParseExact(ids[0], "N"), Guid.ParseExact(ids[1], "N"), Guid.ParseExact(ids[3], "N"));
        var captured = new DatabaseGenerationCheckpoint(binding.ProfileId, Guid.ParseExact(ids[2], "N"), binding.GenerationId);
        var journal = new SqliteDatabaseGenerationJournal(Path.Combine(folder, "restore-selection.sqlite"), captured);
        var candidate = Path.Combine(folder, "fictitious-restored.cipher.db"); var keys = new AndroidGenerationKeyStore(folder);
        // D-44: new resume process must register actual data before Mono compiled-model initialization.
        _ = new ServiceCollection().AddZananceData(candidate);
        using var unopenedOld = new SqliteConnection();
        var selected = await SqlCipherRestoreSelection.SelectAsync(captured, journal, candidate, binding, unopenedOld,
            keys, new AndroidGenerationFileDurability(folder), () => { if (!Directory.Exists(folder)) throw new IOException("Retired fixture lease."); }, 16 * 1024 * 1024, "4.19.0");
        if (selected.Phase != DatabaseGenerationPhase.Selected) throw new IOException("Actual selected restore restart regressed.");
        var key = await keys.ReadAsync(binding, CancellationToken.None);
        try
        {
            foreach (var item in new[] { (candidate, "Fictitious old restore بانک Ü", 730L), (candidate + ".before-restore.cipher.db", "Fictitious before restore", 250L) })
            {
                var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(new SqliteConnectionStringBuilder
                { DataSource = item.Item1, Password = Convert.ToBase64String(key), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()).UseModel(ZananceDbContextModel.Instance).Options;
                await using var db = new ZananceDbContext(options);
                if ((await db.Accounts.SingleAsync()).Name != item.Item2 || (await db.Entries.SingleAsync()).Amount != item.Item3)
                    throw new IOException("Selected restore or encrypted rollback packet changed.");
            }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
        if (!File.Exists(Path.Combine(folder, "fictitious-current.cipher.db"))) throw new IOException("Restore selection deleted original current data.");
        return "{\"RestoreSelectedAfterProcessKill\":true,\"RestoredAndPreviousFinancialPacketsCorrect\":true,\"EncryptedRollbackAndOriginalRetained\":true,\"NoOldConnectionNeededOnSelectedRestart\":true}";
    }

    private static readonly string[] PackagePreferenceKeys = ["localization.language", "localization.culture"];

    private static async Task RestorePackageAsync(string folder, string sourcePath, DatabaseGenerationCheckpoint captured,
        SqliteDatabaseGenerationJournal journal, DatabaseKeyBinding binding, AndroidGenerationKeyStore keys, SqliteConnection current)
    {
        var settings = new NativePreferenceStore();
        settings.Set(PackagePreferenceKeys[0], "fa"); settings.Set(PackagePreferenceKeys[1], "fa-IR");
        settings.Set("device.fictitious.security", "keep-device-state");
        var imported = new Vafadar.Core.Settings.InMemorySettingsStore();
        imported.Set(PackagePreferenceKeys[0], "en"); imported.Set(PackagePreferenceKeys[1], "de-DE");
        using var sourceServices = new ServiceCollection().AddZananceData(sourcePath).BuildServiceProvider();
        var sources = sourceServices.GetServices<Vafadar.Backup.IBackupSource>().Append(
            new Vafadar.Backup.SettingsBackupSource(imported, "display-settings.json", PackagePreferenceKeys));
        var environment = new Vafadar.Core.Hosting.StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0), "Independent fixture", "Fixture");
        var sourceBackup = new Vafadar.Backup.BackupService(sources, environment, imported, TimeProvider.System, new());
        var portable = await sourceBackup.CreatePackageAsync();
        var history = PackageHistory(folder, captured, binding);
        await history.CreateNewAsync(); await history.ReserveAsync(captured);
        var receiver = PackageReceiver(folder, captured, journal, binding, keys, new InterruptedPreferenceStore(settings, folder),
            _ => Task.FromResult(current), _ => throw new IOException("Package publication occurred before the interruption barrier."));
        var actualBackup = new Vafadar.Backup.BackupService([], [], environment, settings, TimeProvider.System, new(), receiver);
        try { await actualBackup.RestorePackageAsync(portable); }
        finally { CryptographicOperations.ZeroMemory(portable); }
    }

    private static SqlCipherPackageRestoreReceiver PackageReceiver(string folder, DatabaseGenerationCheckpoint captured,
        SqliteDatabaseGenerationJournal journal, DatabaseKeyBinding binding, AndroidGenerationKeyStore keys,
        Vafadar.Core.Settings.ISettingsStore settings, Func<CancellationToken, Task<SqliteConnection>> openCurrent,
        Func<CancellationToken, Task> publish, Func<CancellationToken, Task>? persist = null) => new(captured, journal, Path.Combine(folder, "package-restored.cipher.db"),
        binding, keys, new AndroidGenerationFileDurability(folder), settings, PackagePreferenceKeys, openCurrent,
        async (connection, ct) =>
        {
            var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false)
                .UseModel(ZananceDbContextModel.Instance).Options;
            await using var db = new ZananceDbContext(options); await db.Database.MigrateAsync(ct);
        }, publish, () => { if (!Directory.Exists(folder)) throw new IOException("Retired fixture lease."); }, 16 * 1024 * 1024, "4.19.0", persist ?? (ct => new NativePreferenceStore().PersistAsync(ct)), PackageHistory(folder, captured, binding));

    private static SqliteProfileRestoreHistory PackageHistory(string folder, DatabaseGenerationCheckpoint captured, DatabaseKeyBinding binding) =>
        new(Path.Combine(folder, "restore-history.sqlite"), new(binding.ProfileId, binding.InstallationId, captured.OriginalGeneration),
            () => { if (!Directory.Exists(folder)) throw new IOException("Retired fixture lease."); });

    private static async Task<string> ResumePackageAsync(string folder, bool applied = false)
    {
        var ids = (await File.ReadAllTextAsync(Path.Combine(folder, "restore-bindings.txt"))).Split('\n');
        var binding = new DatabaseKeyBinding(Guid.ParseExact(ids[0], "N"), Guid.ParseExact(ids[1], "N"), Guid.ParseExact(ids[3], "N"));
        var captured = new DatabaseGenerationCheckpoint(binding.ProfileId, Guid.ParseExact(ids[2], "N"), binding.GenerationId);
        var journal = new SqliteDatabaseGenerationJournal(Path.Combine(folder, "restore-selection.sqlite"), captured);
        var candidate = Path.Combine(folder, "package-restored.cipher.db"); var keys = new AndroidGenerationKeyStore(folder);
        using var services = new ServiceCollection().AddZananceData(candidate).BuildServiceProvider();
        var settings = new NativePreferenceStore();
        if (settings.Get(PackagePreferenceKeys[0]) != (applied ? "fr" : "en") || settings.Get(PackagePreferenceKeys[1]) != (applied ? "en-GB" : "fa-IR"))
            throw new IOException("Actual partial preference write did not survive interruption.");
        var history = PackageHistory(folder, captured, binding);
        var pending = (await history.ReadAsync())[^1];
        if (pending.Selected != applied || pending.CandidateGeneration != binding.GenerationId) throw new IOException("Interrupted restore reference did not retain its pending target.");
        var key = await keys.ReadAsync(binding, CancellationToken.None);
        try
        {
            var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var published = false;
            if (!applied)
            {
                var persistenceRequested = false;
                var failing = PackageReceiver(folder, captured, journal, binding, keys, settings,
                    _ => throw new IOException("Selected package must not open original."),
                    _ => throw new IOException("Failed persistence must not publish."),
                    _ => { persistenceRequested = true; throw new IOException("Fictitious preference persistence failure."); });
                try { await failing.ResumeAsync(); throw new IOException("Failed persistence unexpectedly completed."); }
                catch (IOException) when (persistenceRequested) { }
                var pendingCapsule = new SqlCipherRestorePreferences(candidate + ".preferences.cipher.db", binding, keys,
                    new AndroidGenerationFileDurability(folder), "4.19.0", PackagePreferenceKeys);
                if ((await pendingCapsule.ReadAsync(() => { })).Applied || (await history.ReadAsync())[^1].Selected)
                    throw new IOException("Failed preference persistence marked replay/reference complete.");
            }
            var receiver = PackageReceiver(folder, captured, journal, binding, keys, settings,
                _ => throw new IOException("Selected package restart requested old database access."),
                _ => throw new IOException("Cold retained owner must not invoke an old publication callback."),
                applied ? (_ => throw new IOException("Completed replay must not persist old preferences.")) : null);
            using var maintenance = await location.Access.AcquireExclusiveAsync();
            var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), maintenance.Validate);
            using var restored = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "",
                Path.Combine(folder, "fictitious-current.cipher.db"), history,
                reference =>
                {
                    if (reference.CandidateGeneration != binding.GenerationId || reference.OriginalGeneration != captured.OriginalGeneration)
                        throw new IOException("Current resolver changed retained restore reference.");
                    return receiver;
                }, keys, new AndroidGenerationFileDurability(folder), maintenance,
                () => { if (!Directory.Exists(folder)) throw new IOException("Retired fixture lease."); }, 16 * 1024 * 1024, "4.19.0");
            if (settings.Get(PackagePreferenceKeys[0]) != (applied ? "fr" : "en") || settings.Get(PackagePreferenceKeys[1]) != (applied ? "en-GB" : "de-DE"))
                throw new IOException("Retained owner preceded complete preference replay.");
            if (restored.Binding != binding || restored.Path != candidate) throw new IOException("Restored owner selected another generation.");
            restored.Publish(location, maintenance); published = true; maintenance.Dispose();
            var references = await history.ReadAsync();
            if (references.Count != 2 || !references[^1].Selected || references[^1].OriginalGeneration != captured.OriginalGeneration || references[^1].CandidateGeneration != binding.GenerationId)
                throw new IOException("Selected restore references changed or lost ancestry.");
            await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
            if (!published || (await db.Entries.SingleAsync()).Amount != 730 || settings.Get("device.fictitious.security") != "keep-device-state")
                throw new IOException("Actual restored package publication changed financial/device state.");
            var capsule = new SqlCipherRestorePreferences(candidate + ".preferences.cipher.db", binding, keys,
                new AndroidGenerationFileDurability(folder), "4.19.0", PackagePreferenceKeys);
            var packet = await capsule.ReadAsync(() => { });
            if (!packet.Applied) throw new IOException("Successful persistence did not commit replay marker.");
            if (packet.Original[PackagePreferenceKeys[0]] != "fa" || packet.Original[PackagePreferenceKeys[1]] != "fa-IR")
                throw new IOException("Original preference rollback packet was lost.");
            foreach (var path in new[] { candidate + ".preferences.cipher.db", candidate + ".before-restore.cipher.db" })
                if (!File.Exists(path) || (await File.ReadAllBytesAsync(path)).AsSpan().StartsWith("SQLite format 3"u8))
                    throw new IOException("Protected package rollback missing or plaintext.");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
        if (applied) return "{\"AppliedReplaySkippedAfterColdRestart\":true,\"LaterUserPreferencesRetained\":true,\"NoRepeatedPreferencePersistence\":true}";
        settings.Set(PackagePreferenceKeys[0], "fr"); settings.Set(PackagePreferenceKeys[1], "en-GB");
        return "{\"ActualPackageReceiver\":true,\"PartialPlatformPreferencesSurvivedKill\":true,\"DurableReplayBeforeProtectedPublication\":true,\"FinancialAndDeviceStateCorrect\":true,\"EncryptedPreferenceAndDatabaseRollbackRetained\":true,\"RestoreReferencesSurviveKillAndSelection\":true}";
    }

    private sealed class InterruptedPreferenceStore(Vafadar.Core.Settings.ISettingsStore inner, string folder) : Vafadar.Core.Settings.ISettingsStore
    {
        public string? Get(string key) => inner.Get(key);
        public void Remove(string key) => inner.Remove(key);
        public void Set(string key, string? value)
        {
            inner.Set(key, value);
            // Actual platform Set is not a disk-flush promise. External observation/restart verifies this fixture.
            File.WriteAllText(Path.Combine(folder, "package-partial.json"), "{\"ActualPartialPreferenceWrite\":true}");
            using var barrier = new ManualResetEvent(false); barrier.WaitOne();
        }
    }

    private sealed class NativePreferenceStore : Vafadar.Core.Settings.ISettingsStore
    {
        private readonly global::Android.Content.ISharedPreferences preferences = global::Android.App.Application.Context
            .GetSharedPreferences("fictitious-package-restore", global::Android.Content.FileCreationMode.Private)!;
        public Task PersistAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); using var editor = preferences.Edit()!;
            if (!editor.Commit()) throw new IOException("Fictitious native preference persistence failed.");
            return Task.CompletedTask;
        }
        public string? Get(string key) => preferences.GetString(key, null);
        public void Remove(string key) => Set(key, null);
        public void Set(string key, string? value)
        {
            using var editor = preferences.Edit()!;
            if (value is null) editor.Remove(key); else editor.PutString(key, value);
            if (!editor.Commit()) throw new IOException("Fictitious native preferences commit failed.");
        }
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
        var backup = services.GetRequiredService<Vafadar.Backup.IBackupSource>();
        using var output = new MemoryStream();
        await backup.WriteAsync(output, CancellationToken.None);
        output.Position = 0;
        await using (var portable = await SqliteMemorySnapshot.OpenAsync(output, 16 * 1024 * 1024))
        await using (var current = await factory.CreateDbContextAsync())
        {
            await current.Database.OpenConnectionAsync();
            await SqliteSnapshotVerifier.VerifyAsync(portable.Connection, (SqliteConnection)current.Database.GetDbConnection());
        }
        using var retiredOutput = new RetiringOutput(() => location.MoveToProtected(path, Create, "4.19.0"));
        refused = false;
        try { await backup.WriteAsync(retiredOutput, CancellationToken.None); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused || retiredOutput.Length is <= 0 or > 81920) throw new IOException("Retired portable export continued after its first output chunk.");
        using var restore = new MemoryStream([1, 2, 3]);
        refused = false;
        try { await backup.RestoreAsync(restore, CancellationToken.None); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused || restore.Position != 0) throw new IOException("Legacy keyed restore consumed input.");
        await first.DisposeAsync();
        var owner = await location.Access.AcquireExclusiveAsync();
        var owned = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner);
        if ((await owned.Entries.SingleAsync()).Amount != 250) throw new IOException("Maintenance factory changed money.");
        var waiting = factory.CreateDbContextAsync();
        if (waiting.IsCompleted) throw new IOException("Ordinary factory entered active maintenance.");
        owner.Dispose();
        if (waiting.IsCompleted) throw new IOException("Closing owner released its live native context.");
        refused = false;
        try { await owned.Entries.SingleAsync(); } catch (InvalidOperationException) { refused = true; }
        if (!refused) throw new IOException("Closed maintenance owner still issued native commands.");
        await owned.DisposeAsync();
        await using var released = await waiting;
        if ((await released.Entries.SingleAsync()).Amount != 250) throw new IOException("Native context admission did not recover.");
    }

    private sealed class RetiringOutput(Action retire) : MemoryStream
    {
        private bool retired;
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await base.WriteAsync(buffer, cancellationToken);
            if (!retired) { retired = true; retire(); }
        }
    }

    /// <summary>Creates only the original fictitious app model for independent lifecycle checks.</summary>
    internal static Task CreateOriginalAsync(string original) => Vafadar.SqlCipher.Compatibility.OriginalProfileFixture.CreateOriginalAsync(original);

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
