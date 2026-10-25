using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    private const string CurrentHomeMigration = "20261024115108_HomeFinancialRevision";

    /// <summary>Qualifies current native Home on encrypted copies of original rows without changing historical sources.</summary>
    private static async Task<object> ReviewCurrentHomeStagesAsync(App app, IServiceProvider services, string root,
        string comparison, string baseline, CancellationToken ct)
    {
        var resume = baseline != comparison;
        var clone = Path.Combine(Path.GetDirectoryName(root)!, "current-home-" + baseline, Path.GetFileName(root));
        if (Directory.Exists(clone) != resume) throw new ReviewCheckException("Explicit original clone required for resumption.");
        var previousProgress = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetStringExtra("review_current_progress");
        var hasAdoption = File.Exists(Path.Combine(clone, "adoption-proof.json"));
        var reportOnly = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_current_report", false) == true;
        if (previousProgress is not null && (!System.Text.RegularExpressions.Regex.IsMatch(previousProgress, "^[0-9a-f]{32}$") || !hasAdoption))
            throw new ReviewCheckException("Explicit adopted partial publication lineage required.");
        var failedAdoption = false;
        if (resume)
        {
            foreach (var file in Directory.EnumerateFiles(root, "failure-*.json"))
            {
                using var packet = JsonDocument.Parse(await File.ReadAllTextAsync(file, ct));
                failedAdoption |= packet.RootElement.GetProperty("Comparison").GetString() == baseline
                    && packet.RootElement.GetProperty("ErrorType").GetString() == typeof(SqliteException).FullName
                    && packet.RootElement.GetProperty("Stack").GetString()!.Contains("BackupDatabase", StringComparison.Ordinal)
                    && packet.RootElement.GetProperty("Stack").GetString()!.Contains("AdoptCurrentHomeSchemaAsync", StringComparison.Ordinal);
            }
            if (!hasAdoption && !failedAdoption || hasAdoption && previousProgress is null)
                throw new ReviewCheckException("Only the recorded failed adoption or its retained partial publication may continue.");
        }
        var originals = new[] { "reference", "tenfold" }.ToDictionary(name => name,
            name => CurrentHomeFileDigests(Path.Combine(root, name)), StringComparer.Ordinal);
        Directory.CreateDirectory(clone);
        if (!resume) File.Copy(Path.Combine(root, "fictitious-performance.marker"), Path.Combine(clone, "fictitious-performance.marker"));
        var adoptions = new List<object>();
        if (!hasAdoption) foreach (var name in new[] { "reference", "tenfold" })
        {
            var source = Path.Combine(root, name); RequireRoot(source);
            var destination = Path.Combine(clone, name);
            if (!Directory.Exists(destination))
            {
                CopyCurrentHomeSource(source, destination);
                if (!originals[name].SequenceEqual(CurrentHomeFileDigests(destination)))
                    throw new ReviewCheckException("Original encrypted source copy differs before adoption.");
            }
            else if (!resume) throw new ReviewCheckException("Never overwrite an existing copied shape.");
            adoptions.Add(await AdoptCurrentHomeSchemaAsync(destination, resume, ct));
        }
        var adopted = new[] { "reference", "tenfold" }.ToDictionary(name => name,
            name => CurrentHomeFileDigests(Path.Combine(clone, name)), StringComparer.Ordinal);
        if (!hasAdoption)
            await File.WriteAllTextAsync(Path.Combine(clone, "adoption-proof.json"), JsonSerializer.Serialize(adoptions), ct);
        else
        {
            using var recorded = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(clone, "adoption-proof.json"), ct));
            if (recorded.RootElement.GetArrayLength() != 2 || recorded.RootElement.EnumerateArray().Any(item =>
                !item.GetProperty("AllOriginalSchemaHeadersAndTypedRowsPreserved").GetBoolean()
                || !item.GetProperty("AdoptedEncryptedControlMatched").GetBoolean()))
                throw new ReviewCheckException("Original complete adoption receipts required.");
            adoptions.AddRange(recorded.RootElement.EnumerateArray().Select(item => (object)item.Clone()));
            var progressPath = Path.Combine(clone, "progress-" + previousProgress + "-home-stages.json");
            using var progress = JsonDocument.Parse(await File.ReadAllTextAsync(progressPath, ct));
            var count = progress.RootElement.GetArrayLength();
            if (count > 6 || (count == 6) != reportOnly)
                throw new ReviewCheckException("Completed native observations require report-only recovery, never replay.");
            File.Copy(progressPath, Path.Combine(clone, "progress-before-resume-" + comparison + ".json"));
        }
        var publicationComparison = previousProgress ?? comparison;
        var byteBaselinePath = Path.Combine(clone, "source-byte-baseline-" + publicationComparison + ".json");
        var durableByteBaseline = File.Exists(byteBaselinePath);
        var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime)
            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (durableByteBaseline)
        {
            // D-533: a resumed process must retain the first pre-measurement source, rather
            // than silently replacing its fingerprint after a native death or observer failure.
            using var recorded = JsonDocument.Parse(await File.ReadAllTextAsync(byteBaselinePath, ct));
            var packet = recorded.RootElement;
            if (!CurrentHomeByteIdentityMatches(packet, publicationComparison, day,
                typeof(ZananceStore).Module.ModuleVersionId, typeof(Core.Ledger.LedgerCalculator).Module.ModuleVersionId,
                typeof(Features.Home.HomePage).Module.ModuleVersionId, reportOnly))
                throw new ReviewCheckException("The original source/day/code fingerprint cannot be replaced during continuation.");
            string[] ReadFiles(string section, string name) => packet.GetProperty(section).GetProperty(name).EnumerateArray()
                .Select(item => item.GetString() ?? throw new ReviewCheckException("Incomplete stored source digest.")).ToArray();
            if (packet.GetProperty("Originals").EnumerateObject().Count() != 2
                || packet.GetProperty("Adopted").EnumerateObject().Count() != 2)
                throw new ReviewCheckException("Both original source shapes must remain in the retained fingerprint.");
            foreach (var name in new[] { "reference", "tenfold" })
            {
                var originalFiles = ReadFiles("Originals", name); var adoptedFiles = ReadFiles("Adopted", name);
                if (!originalFiles.SequenceEqual(originals[name])
                    || !CurrentHomeFilesPreserved(adoptedFiles, adopted[name], publicationComparison))
                    throw new ReviewCheckException("The source changed after the original pre-measurement fingerprint.");
                originals[name] = originalFiles; adopted[name] = adoptedFiles;
            }
        }
        else if (!reportOnly)
        {
            if (previousProgress is not null)
                throw new ReviewCheckException("Missing pre-measurement fingerprint; old observations cannot become new byte evidence.");
            await using (var output = new FileStream(byteBaselinePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await JsonSerializer.SerializeAsync(output, new { Comparison = publicationComparison, Day = day,
                    CapturedBeforeNativePublication = true, ProcessId = Environment.ProcessId,
                    DataModule = typeof(ZananceStore).Module.ModuleVersionId,
                    CoreModule = typeof(Core.Ledger.LedgerCalculator).Module.ModuleVersionId,
                    CallerModule = typeof(Features.Home.HomePage).Module.ModuleVersionId,
                    Originals = originals, Adopted = adopted }, cancellationToken: ct);
            var durability = new AndroidGenerationFileDurability(clone);
            await durability.SynchronizeFileAsync(byteBaselinePath, ct);
            // Zero completed contexts is a valid continuation point before the first native sample.
            var progressPath = Path.Combine(clone, "progress-" + publicationComparison + "-home-stages.json");
            await using (var progress = new FileStream(progressPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await JsonSerializer.SerializeAsync(progress, Array.Empty<object>(), cancellationToken: ct);
            await durability.SynchronizeFileAsync(progressPath, ct);
            durableByteBaseline = true;
        }
        if (reportOnly)
        {
            var audit = new List<object>();
            foreach (var name in new[] { "reference", "tenfold" })
            {
                audit.Add(await AuditCurrentHomeSourceAsync(Path.Combine(root, name), adopted: false, ct));
                audit.Add(await AuditCurrentHomeSourceAsync(Path.Combine(clone, name), adopted: true, ct));
            }
            await File.WriteAllTextAsync(Path.Combine(clone, "read-only-audit-" + comparison + ".json"), JsonSerializer.Serialize(audit), ct);
        }
        else
            await MeasureAsync(app, services, clone, "home-stages", publicationComparison, publicationComparison, ct, currentSchema: true);
        foreach (var name in new[] { "reference", "tenfold" })
            if (!originals[name].SequenceEqual(CurrentHomeFileDigests(Path.Combine(root, name)))
                || !CurrentHomeFilesPreserved(adopted[name], CurrentHomeFileDigests(Path.Combine(clone, name)), publicationComparison))
                throw new ReviewCheckException("An original source or adopted measurement source changed.");
        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(clone, "home-stages-" + publicationComparison + "-result.json"), ct));
        return new { Comparison = comparison, OriginalCloneBaseline = baseline, RetainedPublicationComparison = previousProgress,
            FailedAdoptionBaseline = resume && failedAdoption ? baseline : null, Adoptions = adoptions, CurrentHome = report.RootElement.Clone(),
            OriginalEncryptedSourcesUnchanged = true, AdoptedSourcesUnchangedDuringMeasurement = durableByteBaseline,
            CompletedNativeObservationsRecoveredWithoutReplay = reportOnly, FinalReadOnlyAuditSourceFilesUnchanged = reportOnly,
            HistoricalTimingExactByteEqualityAccepted = durableByteBaseline, OriginalPreMeasurementFingerprintRetained = durableByteBaseline,
            PriorByteGuardRefusalPreserved = reportOnly && !durableByteBaseline,
            OriginalFinancialRowsPreservedAcrossMigration = true, FinancialCommands = 0, PreparationOrKeyCreation = false,
            MainPackageTouched = false, Device = DeviceEvidence(), GeneralPerformanceOrStabilityAccepted = false };
    }

    /// <summary>Rejects a different source/code/day before continuing metrics; historical recovery adds no new UI observations.</summary>
    private static bool CurrentHomeByteIdentityMatches(JsonElement packet, string comparison, string day,
        Guid dataModule, Guid coreModule, Guid callerModule, bool reportOnly) =>
        packet.GetProperty("Comparison").GetString() == comparison
        && packet.GetProperty("CapturedBeforeNativePublication").GetBoolean()
        && packet.GetProperty("DataModule").GetGuid() == dataModule
        && packet.GetProperty("CoreModule").GetGuid() == coreModule
        && (reportOnly || packet.GetProperty("CallerModule").GetGuid() == callerModule
            && packet.GetProperty("Day").GetString() == day);

    /// <summary>Preserves every prior byte while permitting only the three exact newly captured presentation packets.</summary>
    private static bool CurrentHomeFilesPreserved(string[] before, string[] after, string comparison)
    {
        var old = before.ToDictionary(value => value[..value.LastIndexOf(':')], StringComparer.Ordinal);
        var current = after.ToDictionary(value => value[..value.LastIndexOf(':')], StringComparer.Ordinal);
        var allowed = new[] { "en", "fa", "de" }.Select(language => "presentation-" + comparison + "-" + language + ".json")
            .ToHashSet(StringComparer.Ordinal);
        return old.All(item => current.TryGetValue(item.Key, out var value) && value == item.Value)
            && current.Keys.Except(old.Keys, StringComparer.Ordinal).All(allowed.Contains);
    }

    /// <summary>Rechecks only existing encrypted source/control rows after a completed native cohort, without UI or metrics.</summary>
    private static async Task<object> AuditCurrentHomeSourceAsync(string folder, bool adopted, CancellationToken ct)
    {
        RequireRoot(folder);
        var original = Path.Combine(folder, "fixture.db");
        using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
        using var owner = await location.Access.AcquireExclusiveAsync(ct);
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => RequireRoot(folder));
        var identity = (await identities.ReadAsync(ct)).Single();
        var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
        using var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
            _ => throw new ReviewCheckException("Unexpected audit restore."), keys, new AndroidGenerationFileDurability(folder),
            owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
        if (Path.GetDirectoryName(generation.Path) != folder) throw new ReviewCheckException("Audit generation must remain in its exact captured directory.");
        var key = await keys.ReadAsync(generation.Binding, ct);
        try
        {
            var password = Convert.ToBase64String(key);
            await using var current = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = generation.Path, Mode = SqliteOpenMode.ReadOnly, Pooling = false, Password = password }.ToString());
            await using var control = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = generation.Path + (adopted ? ".current-home-control.db" : ".rollback.db"),
                Mode = SqliteOpenMode.ReadOnly, Pooling = false, Password = password }.ToString());
            await current.OpenAsync(ct); await control.OpenAsync(ct);
            await SqliteSnapshotVerifier.VerifyAsync(control, current, ct);
            await using var pending = current.CreateCommand(); pending.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId=$migration";
            pending.Parameters.AddWithValue("$migration", CurrentHomeMigration);
            var count = Convert.ToInt64(await pending.ExecuteScalarAsync(ct));
            if (count != (adopted ? 1 : 0)) throw new ReviewCheckException("Original and adopted schema boundaries differ.");
            return new { Shape = Path.GetFileName(folder), Adopted = adopted, OriginalOrCurrentControlMatchedAllSchemaHeadersAndTypedRows = true,
                ExactCapturedDirectory = true, ExistingOnlyReadOnlyConnections = true, NewMigrationPresent = adopted,
                UiOrTimingObservationsReplayed = false, FinancialCommands = 0 };
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    /// <summary>Applies only the known additive suffix with the production callback and independently preserves all old typed rows.</summary>
    private static async Task<object> AdoptCurrentHomeSchemaAsync(string folder, bool resume, CancellationToken ct)
    {
        RequireRoot(folder);
        var original = Path.Combine(folder, "fixture.db");
        if (File.Exists(original)) throw new ReviewCheckException("Existing protected source required.");
        using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
        using var owner = await location.Access.AcquireExclusiveAsync(ct);
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => RequireRoot(folder));
        var identity = (await identities.ReadAsync(ct)).Single();
        var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
        var ancestry = await history.ReadAsync(ct);
        var journal = await new SqliteDatabaseGenerationJournal(original + ".generation.sqlite", identity.InitialMigration).ReadAsync(ct);
        if (ancestry.Count != 1 || !ancestry[0].Selected || ancestry[0].Sequence != 0 || journal.Phase != DatabaseGenerationPhase.Completed)
            throw new ReviewCheckException("Completed selected original ancestry required before additive adoption.");
        using var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
            _ => throw new ReviewCheckException("Unexpected restored candidate."), keys, new AndroidGenerationFileDurability(folder),
            owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
        await using var current = generation.CreateConnection();
        await current.OpenAsync(ct);
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(current).UseModel(ZananceDbContextModel.Instance).Options;
        var alreadyApplied = false;
        await using (var db = new ZananceDbContext(options))
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
            alreadyApplied = pending.Length == 0 && resume
                && (await db.Database.GetAppliedMigrationsAsync(ct)).LastOrDefault() == CurrentHomeMigration;
            if (!alreadyApplied && !pending.SequenceEqual(new[] { CurrentHomeMigration }))
                throw new ReviewCheckException("Only the known additive Home revision suffix belongs to this qualification.");
        }
        var key = await keys.ReadAsync(generation.Binding, ct);
        try
        {
            var password = Convert.ToBase64String(key);
            await using var old = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = generation.Path + ".rollback.db", Mode = SqliteOpenMode.ReadOnly, Pooling = false, Password = password }.ToString());
            await old.OpenAsync(ct);
            if (!alreadyApplied)
            {
                await SqliteSnapshotVerifier.VerifyAsync(old, current, ct);
                await current.CloseAsync();
                await ProtectedProfileStorage.UpgradeAsync(current, ct);
                if (current.State != System.Data.ConnectionState.Closed)
                    throw new ReviewCheckException("Original schema callback must restore closed ownership.");
                location.Access.RequireDrainedOwner(owner);
                await current.OpenAsync(ct);
            }
            using var portableBytes = new MemoryStream();
            await SqlCipherPortableSnapshot.WriteAsync(current, portableBytes, 256 * 1024 * 1024, ct);
            portableBytes.Position = 0;
            await using var portable = await SqliteMemorySnapshot.OpenAsync(portableBytes, 256 * 1024 * 1024, ct);
            CryptographicOperations.ZeroMemory(portableBytes.GetBuffer().AsSpan(0, checked((int)portableBytes.Length)));
            // Normalize only a disposable memory copy's new bookkeeping objects. The original
            // ciphertext and every old schema/header/typed row must match the authenticated rollback.
            await using (var normalized = new SqliteConnection("Data Source=:memory:;Pooling=False"))
            {
                await normalized.OpenAsync(ct); portable.Connection.BackupDatabase(normalized);
                var triggers = new List<string>();
                await using (var command = normalized.CreateCommand())
                {
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='trigger' AND name GLOB 'HomeFinancialRevision_*' ORDER BY name";
                    await using var reader = await command.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct)) triggers.Add(reader.GetString(0));
                }
                var expected = new[] { "Entries", "Budgets", "BudgetCategoryLimits", "ExchangeRates", "AssetTypes", "AssetEvents" }
                    .SelectMany(table => new[] { "INSERT", "UPDATE", "DELETE" }.Select(operation => "HomeFinancialRevision_" + table + "_" + operation))
                    .Order(StringComparer.Ordinal).ToArray();
                if (!triggers.SequenceEqual(expected)) throw new ReviewCheckException("Complete original revision trigger set required.");
                foreach (var trigger in triggers)
                {
                    await using var drop = normalized.CreateCommand(); drop.CommandText = "DROP TRIGGER \"" + trigger + "\"";
                    await drop.ExecuteNonQueryAsync(ct);
                }
                await using var remove = normalized.CreateCommand();
                remove.CommandText = "DROP TABLE HomeFinancialRevision; DELETE FROM __EFMigrationsHistory WHERE MigrationId=$migration";
                remove.Parameters.AddWithValue("$migration", CurrentHomeMigration);
                if (await remove.ExecuteNonQueryAsync(ct) != 1) throw new ReviewCheckException("Exactly one new migration record required.");
                await SqliteSnapshotVerifier.VerifyAsync(old, normalized, ct);
            }
            var controlPath = generation.Path + ".current-home-control.db";
            if (File.Exists(controlPath)) throw new ReviewCheckException("Never overwrite an existing adopted control.");
            await portable.ExportEncryptedAsync(controlPath, password, ct);
            await using var control = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = controlPath, Mode = SqliteOpenMode.ReadOnly, Pooling = false, Password = password }.ToString());
            await control.OpenAsync(ct);
            await SqliteSnapshotVerifier.VerifyAsync(current, control, ct);
            return new { Shape = Path.GetFileName(folder), ProductionSchemaCallback = true, PendingSuffix = CurrentHomeMigration,
                ExistingCiphertextMatchedOriginalRollbackBeforeUpgrade = !alreadyApplied, OriginalAppliedMigrationNotRepeated = alreadyApplied,
                AllOriginalSchemaHeadersAndTypedRowsPreserved = true,
                ExactNewTriggerCount = 18, AdoptedEncryptedControlMatched = true, NoFinancialSaveOrNewKey = true };
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    /// <summary>Copies only a marked owned tree, refusing redirected children before following them.</summary>
    private static void CopyCurrentHomeSource(string source, string target)
    {
        RequireRoot(source); Directory.CreateDirectory(target);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new ReviewCheckException("Redirected source file refused.");
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new ReviewCheckException("Redirected source directory refused.");
            CopyCurrentHomeSourceFiles(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }

    /// <summary>Copies owned subordinate key-envelope files without requiring a second workload marker.</summary>
    private static void CopyCurrentHomeSourceFiles(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new ReviewCheckException("Redirected child refused.");
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
        foreach (var child in Directory.EnumerateDirectories(source))
        {
            if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0) throw new ReviewCheckException("Redirected child refused.");
            CopyCurrentHomeSourceFiles(child, Path.Combine(target, Path.GetFileName(child)));
        }
    }

    /// <summary>Retains secret-free comparison digests for the complete original encrypted source tree.</summary>
    private static string[] CurrentHomeFileDigests(string root)
    {
        RequireRoot(root);
        var files = new List<string>();
        void Walk(string directory)
        {
            foreach (var item in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(item);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new ReviewCheckException("Redirected digest source refused.");
                if ((attributes & FileAttributes.Directory) != 0) Walk(item);
                else { using var stream = File.OpenRead(item); files.Add(Path.GetRelativePath(root, item) + ":" + Convert.ToHexString(SHA256.HashData(stream))); }
            }
        }
        Walk(root); return files.Order(StringComparer.Ordinal).ToArray();
    }
}
