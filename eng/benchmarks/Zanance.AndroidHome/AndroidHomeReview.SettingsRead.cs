using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies complete original settings and maintenance reads on retained keyed reference/tenfold files.</summary>
    private static async Task<object> ReviewSettingsReadAsync(string root, string comparison, CancellationToken ct)
    {
        return await Task.Run(async () =>
        {
            var results = new List<object>();
            foreach (var shape in new[] { "reference", "tenfold" })
            {
                var folder = Path.Combine(root, shape); RequireRoot(folder);
                var original = Path.Combine(folder, "fixture.db");
                var identitiesPath = Path.Combine(folder, "identities.sqlite");
                if (File.Exists(original) || !File.Exists(identitiesPath) || !Directory.Exists(Path.Combine(folder, "keys")))
                    throw new ReviewCheckException("Retained protected settings workload required; no replacement data or keys.");
                using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
                var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
                SqlCipherProfileGeneration generation;
                using (var owner = await location.Access.AcquireExclusiveAsync(ct))
                {
                    var identities = new SqliteDatabaseIdentityCatalogue(identitiesPath, () => RequireRoot(folder));
                    var identity = (await identities.ReadAsync(ct)).Single();
                    var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
                    generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                        _ => throw new ReviewCheckException("No restored workload expected."), keys, new AndroidGenerationFileDurability(folder),
                        owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
                    generation.Publish(location, owner);
                }
                var store = data.GetRequiredService<ZananceStore>();
                using (generation)
                {
                    await using var before = await PortableAsync(data);
                    ZananceSettings expected;
                    var properties = typeof(ZananceSettings).GetProperties();
                    using (var owner = await location.Access.AcquireExclusiveAsync(ct))
                    {
                        var factory = (ILocalDbContextFactory<ZananceDbContext>)data.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
                        await using (var db = await factory.CreateDbContextAsync(owner, ct))
                            expected = await db.Settings.AsNoTracking().OrderBy(s => s.CreatedAt).FirstAsync(ct);
                        var maintenance = await store.GetSettingsForMaintenanceAsync(owner, ct);
                        RequireEqual(expected, maintenance);
                        location.Access.RequireDrainedOwner(owner);
                    }
                    RequireEqual(expected, await store.GetSettingsAsync(ct));
                    using (var cancelled = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        cancelled.Cancel(); var refused = false;
                        try { await store.GetSettingsAsync(cancelled.Token); }
                        catch (OperationCanceledException) { refused = true; }
                        if (!refused) throw new ReviewCheckException("Cancelled settings were published.");
                    }
                    await using var after = await PortableAsync(data);
                    await SqliteSnapshotVerifier.VerifyAsync(before.Connection, after.Connection, ct);
                    results.Add(new { Shape = shape, CompleteSettingsPropertiesMatched = properties.Length,
                        ActualExclusiveMaintenanceRead = true, CancellationRefused = true, CompleteSchemaAndTypedRowsUnchanged = true });

                    void RequireEqual(ZananceSettings oracle, ZananceSettings actual)
                    {
                        if (properties.Length != 25 || properties.Any(p => !Equals(p.GetValue(oracle), p.GetValue(actual))))
                            throw new ReviewCheckException("Complete original settings values differ.");
                    }
                }
                var retiredRefused = false;
                try { await store.GetSettingsAsync(ct); }
                catch (ObjectDisposedException) { retiredRefused = true; }
                if (!retiredRefused) throw new ReviewCheckException("Retired protected key owner was reused.");
            }
            return (object)new { Comparison = comparison, Results = results, RetiredProtectedKeyOwnersRefused = true,
                DataModuleVersionId = typeof(ZananceStore).Module.ModuleVersionId, CallerModuleVersionId = typeof(AndroidHomeReview).Module.ModuleVersionId,
                NoFinancialSaveOrReplacementKeys = true, MainPackageTouched = false,
                Boundary = "Original Release/AOT protected settings values and maintenance; no whole-device or startup distribution acceptance." };
        }, ct);

        async Task<SqliteMemorySnapshot> PortableAsync(IServiceProvider data)
        {
            using var bytes = new MemoryStream(); await data.GetRequiredService<IBackupSource>().WriteAsync(bytes, ct); bytes.Position = 0;
            return await SqliteMemorySnapshot.OpenAsync(bytes, 256 * 1024 * 1024, ct);
        }
    }
}
