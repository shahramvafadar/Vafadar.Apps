using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Measures the actual startup schema callback on unchanged retained protected files without copying its algorithm.</summary>
    private static async Task<object> ReviewSchemaConnectionAsync(string root, string comparison, CancellationToken ct)
    {
        var results = new List<object>();
        foreach (var name in new[] { "reference", "tenfold" })
        {
            var folder = Path.Combine(root, name); RequireRoot(folder);
            var original = Path.Combine(folder, "fixture.db"); var identityPath = Path.Combine(folder, "identities.sqlite");
            if (File.Exists(original) || !File.Exists(identityPath) || !Directory.Exists(Path.Combine(folder, "keys")))
                throw new ReviewCheckException("Original protected fixture required; never recreate it.");
            using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
            SqlCipherProfileGeneration? generation = null;
            var opens = 0; double elapsed; bool closed;
            try
            {
                using (var owner = await location.Access.AcquireExclusiveAsync(ct))
                {
                    var identities = new SqliteDatabaseIdentityCatalogue(identityPath, () => RequireRoot(folder));
                    var identity = (await identities.ReadAsync(ct)).Single();
                    var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
                    generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                        _ => throw new ReviewCheckException("No restored workload is expected."), keys, new AndroidGenerationFileDurability(folder),
                        owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
                    generation.Publish(location, owner);
                    await using var connection = generation.CreateConnection();
                    // D-558: unchanged-source probes must refuse pending migrations before invoking the callback.
                    await SchemaProbeAdmission.RequireSettledAsync(connection, ct);
                    connection.StateChange += (_, args) => { if (args.CurrentState == System.Data.ConnectionState.Open) opens++; };
                    var watch = Stopwatch.StartNew(); await ProtectedProfileStorage.UpgradeAsync(connection, ct); watch.Stop();
                    elapsed = watch.Elapsed.TotalMilliseconds; closed = connection.State == System.Data.ConnectionState.Closed;
                    location.Access.RequireDrainedOwner(owner);
                }
                var rawKey = await keys.ReadAsync(generation.Binding, ct);
                try
                {
                    await using var rollback = new SqliteConnection(new SqliteConnectionStringBuilder
                    { DataSource = generation.Path + ".rollback.db", Mode = SqliteOpenMode.ReadOnly, Pooling = false,
                        Password = Convert.ToBase64String(rawKey) }.ToString());
                    await rollback.OpenAsync(ct);
                    await using var current = await data.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                    await current.Database.OpenConnectionAsync(ct);
                    await SqliteSnapshotVerifier.VerifyAsync(rollback, (SqliteConnection)current.Database.GetDbConnection(), ct);
                }
                finally { CryptographicOperations.ZeroMemory(rawKey); }
            }
            // A refused precondition owns a generation too; do not leave its key alive until collection.
            finally { generation?.Dispose(); }
            if (!closed) throw new ReviewCheckException("Original schema callback leaked an open handle.");
            results.Add(new { Shape = name, NativeConnectionOpens = opens, CallbackMilliseconds = elapsed,
                ClosedCallerStateRestored = closed, CompleteOriginalRawSchemaAndTypedRowsUnchanged = true });
        }
        return new { Comparison = comparison, OriginalApplicationSchemaCallback = true, Results = results,
            CallerModuleVersionId = typeof(ProtectedProfileStorage).Module.ModuleVersionId, NoMainDataTouched = true,
            NoFinancialSaveOrKeyRecreation = true, KdfAndUnpooledPolicyUnchanged = true, ColdHomeOrPhoneAcceptance = false };
    }
}
