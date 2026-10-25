using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.RetainedReader;

/// <summary>Reads only the already selected independent encrypted generations, with actual platform keys and original sources.</summary>
internal static class RetainedReadStages
{
    /// <summary>The original target-specific workload prepared before D-470, not a new financial fixture.</summary>
#if RETAINED_READER_EMULATOR
    internal const string ExpectedRun = "25b9dab64bf849fcb24e39246d773215";
#else
    internal const string ExpectedRun = "d53542fe57d6446ab52b82a757ee7f6e";
#endif

    /// <summary>Separates reader/core stages and requires immutable original source and all original raw typed rows.</summary>
    internal static async Task<object> RunAsync(string files, string evidence, string comparison, CancellationToken ct)
    {
        var source = Path.Combine(files, "android-home", ExpectedRun);
        RequireRoot(source);
        var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var results = new List<object>();
        foreach (var (name, rows) in new[] { ("reference", 10000), ("tenfold", 100000) })
        {
            var folder = Path.Combine(source, name);
            RequireRoot(folder);
            var original = Path.Combine(folder, "fixture.db");
            var identityPath = Path.Combine(folder, "identities.sqlite");
            var keyDirectory = Path.Combine(folder, "keys");
            if (File.Exists(original) || !File.Exists(identityPath) || !Directory.Exists(keyDirectory))
                throw new RetainedReadRefusal("Only the original retained encrypted workload is allowed; no creation or recovery.");
            var before = SourceFiles(folder);
            using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keys = new ExistingKeys(new AndroidGenerationKeyStore(keyDirectory));
            var durability = new ExistingDurability(new AndroidGenerationFileDurability(folder));
            SqlCipherProfileGeneration generation;
            using (var owner = await location.Access.AcquireExclusiveAsync(ct))
            {
                var identities = new SqliteDatabaseIdentityCatalogue(identityPath, () => RequireRoot(folder));
                var identity = (await identities.ReadAsync(ct)).Single();
                var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey,
                    () => RequireRoot(folder));
                // OpenCurrent can resume unfinished original migration. Admit only the already
                // completed, selected original generation before calling that production entry point.
                var ancestry = await history.ReadAsync(ct);
                var checkpoint = await new SqliteDatabaseGenerationJournal(original + ".generation.sqlite",
                    identity.InitialMigration).ReadAsync(ct);
                if (ancestry.Count != 1 || ancestry[0].Sequence != 0 || !ancestry[0].Selected
                    || checkpoint.Phase != DatabaseGenerationPhase.Completed)
                    throw new RetainedReadRefusal("Only a completed, already selected original generation may be read.");
                generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                    _ => throw new RetainedReadRefusal("No restored workload belongs to this original source."),
                    keys, durability, owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
                generation.Publish(location, owner);
            }
            object stages;
            using (generation)
            {
                var rollbackPath = generation.Path + ".rollback.db";
                if (!File.Exists(rollbackPath)) throw new RetainedReadRefusal("Original authenticated rollback is missing.");
                stages = await LedgerStageDiagnostics.MeasureAsync(data.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),
                    rows, day, ct);
                var rawKey = await keys.ReadAsync(generation.Binding, ct);
                try
                {
                    await using var expected = new SqliteConnection(new SqliteConnectionStringBuilder
                    { DataSource = rollbackPath, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
                        Password = Convert.ToBase64String(rawKey) }.ToString());
                    await expected.OpenAsync(ct);
                    await using var actual = generation.CreateConnection();
                    await actual.OpenAsync(ct);
                    await using (var queryOnly = actual.CreateCommand())
                    { queryOnly.CommandText = "PRAGMA query_only = ON"; await queryOnly.ExecuteNonQueryAsync(ct); }
                    await SqliteSnapshotVerifier.VerifyAsync(expected, actual, ct);
                }
                finally { CryptographicOperations.ZeroMemory(rawKey); }
            }
            if (!before.SequenceEqual(SourceFiles(folder)))
                throw new RetainedReadRefusal("An original source, metadata, envelope or file set changed; preserve the difference.");
            if (keys.PreparationAttempts != 0 || durability.SynchronizationAttempts != 0)
                throw new RetainedReadRefusal("Read-only source admission attempted a preparation effect.");
            results.Add(new { Shape = name, Stages = stages, CompleteOriginalRawSchemaAndTypedRowsMatchRollback = true,
                OriginalSourceFileSetAndBytesUnchanged = true, KeyPreparationAttempts = keys.PreparationAttempts,
                DurabilitySynchronizationAttempts = durability.SynchronizationAttempts });
            // Report progress outside the retained financial source. A host timeout never authorizes
            // a new process or replacement source; original terminal chunks can be read later.
            await File.WriteAllTextAsync(Path.Combine(evidence, name + "-complete.json"),
                System.Text.Json.JsonSerializer.Serialize(results[^1]), ct);
        }
        return new { Comparison = comparison, Run = ExpectedRun, ProcessId = Environment.ProcessId, Results = results,
            CapturedDay = day, OriginalApplicationStartupExecuted = false, SourceMigrationPerformed = false,
            SeedOrFinancialCommandsDispatched = false, MainPackageTouched = false,
            Api = (int)Android.OS.Build.VERSION.SdkInt, Runtime = Environment.Version.ToString(),
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            PageBytes = Environment.SystemPageSize, WholeHomeOrColdOrStabilityAccepted = false };
    }

    /// <summary>Refuses missing or redirected source roots without creating markers, databases or keys.</summary>
    private static void RequireRoot(string folder)
    {
        if (!File.Exists(Path.Combine(folder, "fictitious-performance.marker")))
            throw new RetainedReadRefusal("Original independent workload marker is missing.");
        for (var item = new DirectoryInfo(folder); item is not null; item = item.Parent)
            if ((item.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new RetainedReadRefusal("Redirected original workload root refused.");
    }

    /// <summary>Captures only this original source folder; ciphertext/envelope bytes and names never enter public output.</summary>
    private static SortedDictionary<string, string> SourceFiles(string folder)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new RetainedReadRefusal("Redirected retained file refused.");
            using var input = File.OpenRead(file);
            result.Add(Path.GetRelativePath(folder, file), Convert.ToHexString(SHA256.HashData(input)));
        }
        return result;
    }

    /// <summary>Original platform unwrapping is allowed; preparation cannot silently repair a missing key.</summary>
    private sealed class ExistingKeys(AndroidGenerationKeyStore original) : IDatabaseGenerationKeyStore
    {
        /// <summary>Counts refused effects; no existing successful setup is repeated.</summary>
        internal int PreparationAttempts { get; private set; }

        /// <inheritdoc />
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        { PreparationAttempts++; throw new RetainedReadRefusal("Key preparation is forbidden in this retained read."); }

        /// <inheritdoc />
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken) => original.ReadAsync(binding, cancellationToken);
    }

    /// <summary>Exposes original volume checks while refusing synchronization effects belonging to an unfinished migration.</summary>
    private sealed class ExistingDurability(AndroidGenerationFileDurability original) : IDatabaseGenerationFileDurability
    {
        /// <summary>Counts refused migration effects.</summary>
        internal int SynchronizationAttempts { get; private set; }

        /// <inheritdoc />
        public long AvailableBytes => original.AvailableBytes;

        /// <inheritdoc />
        public Task SynchronizeFileAsync(string path, CancellationToken cancellationToken)
        { SynchronizationAttempts++; throw new RetainedReadRefusal("Migration file synchronization is forbidden in this retained read."); }

        /// <inheritdoc />
        public Task SynchronizeDirectoryAsync(CancellationToken cancellationToken)
        { SynchronizationAttempts++; throw new RetainedReadRefusal("Migration directory synchronization is forbidden in this retained read."); }
    }
}
