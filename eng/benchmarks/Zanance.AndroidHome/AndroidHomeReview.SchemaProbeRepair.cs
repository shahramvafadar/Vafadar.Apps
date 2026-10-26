using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Repairs only D-558's authenticated diagnostic reference source, never Main or another retained cohort.</summary>
    private static async Task<object> RepairSchemaProbeAsync(string root, string comparison, CancellationToken ct)
    {
        const string run = "25b9dab64bf849fcb24e39246d773215";
        const string failedProbe = "748e3d9276624ec8b812ae5f37bf5aa1";
        const string completed = "5930bf93776340c1a8f1acecae11386d";
        var expectedRoot = Path.Combine(new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath, "android-home", run);
        if (Package != "pro.vafadar.zanance.androidhomereview" || Android.OS.Process.MyUid() != 10221
            || root != expectedRoot || Android.OS.Build.VERSION.SdkInt != Android.OS.BuildVersionCodes.Baklava
            || DeviceInfo.Current.DeviceType != DeviceType.Virtual)
            throw new ReviewCheckException("Only the original exact D558 emulator workload may be repaired.");
        RequireRoot(root);
        if (!await MainThread.InvokeOnMainThreadAsync(() => Platform.CurrentActivity?.Window?.DecorView?.HasWindowFocus == true))
            throw new ReviewCheckException("The original independent app must own its window.");
        var matchedFailure = false;
        foreach (var file in Directory.EnumerateFiles(root, "failure-*.json"))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0 || new FileInfo(file).Length > 512 * 1024)
                throw new ReviewCheckException("A bounded original failure record is required.");
            using var failure = JsonDocument.Parse(await File.ReadAllTextAsync(file, ct));
            matchedFailure |= failure.RootElement.GetProperty("Comparison").GetString() == failedProbe
                && failure.RootElement.GetProperty("Failed").GetBoolean()
                && failure.RootElement.GetProperty("ErrorType").GetString() == typeof(InvalidDataException).FullName
                && failure.RootElement.GetProperty("Stack").GetString()!.Contains("ReviewSchemaConnectionAsync", StringComparison.Ordinal);
        }
        if (!matchedFailure) throw new ReviewCheckException("The original post-maintenance schema-probe failure is required.");
        using var accepted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "home-current-stages-" + completed + "-result.json"), ct));
        if (accepted.RootElement.GetProperty("FinancialCommands").GetInt32() != 0
            || !accepted.RootElement.GetProperty("OriginalEncryptedSourcesUnchanged").GetBoolean()
            || accepted.RootElement.GetProperty("CurrentHome").GetProperty("Results").GetArrayLength() != 6)
            throw new ReviewCheckException("Retain the completed original native cohort without replay.");
        var clone = Path.Combine(Path.GetDirectoryName(root)!, "current-home-" + completed, run);
        using var baseline = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(clone, "source-byte-baseline-" + completed + ".json"), ct));
        var untouchedTenfold = baseline.RootElement.GetProperty("Originals").GetProperty("tenfold")
            .EnumerateArray().Select(value => value.GetString()!).ToArray();
        if (!untouchedTenfold.SequenceEqual(CurrentHomeFileDigests(Path.Combine(root, "tenfold"))))
            throw new ReviewCheckException("The failed first-reference probe must not have touched tenfold.");
        var repaired = await RepairOriginalSchemaProbeReferenceAsync(root, comparison, failedProbe, ct);
        var restoredReference = CurrentHomeFileDigests(Path.Combine(root, "reference"));
        var originalProbeRefused = false;
        try { await ReviewSchemaConnectionAsync(root, comparison, ct); }
        catch (InvalidDataException) { originalProbeRefused = true; }
        if (!originalProbeRefused
            || !restoredReference.SequenceEqual(CurrentHomeFileDigests(Path.Combine(root, "reference")))
            || !untouchedTenfold.SequenceEqual(CurrentHomeFileDigests(Path.Combine(root, "tenfold"))))
            throw new ReviewCheckException("The original guarded probe must refuse before changing either source.");
        var audits = new List<object>();
        foreach (var shape in new[] { "reference", "tenfold" })
        {
            var original = Path.Combine(root, shape); var adopted = Path.Combine(clone, shape);
            var beforeOriginal = CurrentHomeFileDigests(original); var beforeAdopted = CurrentHomeFileDigests(adopted);
            audits.Add(await AuditCurrentHomeSourceAsync(original, adopted: false, ct, requirePendingProbeRefusal: true));
            audits.Add(await AuditCurrentHomeSourceAsync(adopted, adopted: true, ct));
            if (!beforeOriginal.SequenceEqual(CurrentHomeFileDigests(original))
                || !beforeAdopted.SequenceEqual(CurrentHomeFileDigests(adopted)))
                throw new ReviewCheckException("Read-only audit or refused schema probe changed its source.");
        }
        return new { Comparison = comparison, FailedProbe = failedProbe, CompletedNativeComparison = completed,
            OnlyExactOwnedReferenceBookkeepingRepaired = true, Repaired = repaired, Audits = audits,
            OriginalAuthenticatedSchemaHeadersAndAllTypedRowsRestored = true, UntouchedOriginalTenfoldBytesVerified = true,
            ReferenceCipherBytesUnchangedSinceBeforeBadProbe = false, OriginalBadProbeAndByteMutationRetained = true,
            FourExistingProtectedSourcesReopenedWithoutNewKeys = true, ReadOnlyAuditSourceFilesUnchanged = true,
            PendingOriginalSchemaProbeRefusedBeforeUpgrade = true, FinancialCommands = 0, NativeHomeCasesReplayed = 0,
            OriginalSchemaProbeRefusalAndGenerationCleanupExercised = true,
            ShippingMainTouched = false, NoGeneralSecurityStabilityOrPerformanceAcceptance = true };
    }

    /// <summary>Proves the full authenticated rollback in memory before retiring only the two declared metadata migrations.</summary>
    private static async Task<ReadBlockSchemaRetirement.Result> RepairOriginalSchemaProbeReferenceAsync(
        string root, string comparison, string failedProbe, CancellationToken ct)
    {
        var folder = Path.Combine(root, "reference"); RequireRoot(folder);
        var original = Path.Combine(folder, "fixture.db");
        using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
        using var owner = await location.Access.AcquireExclusiveAsync(ct);
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => RequireRoot(folder));
        var identity = (await identities.ReadAsync(ct)).Single();
        var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
        using var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
            _ => throw new ReviewCheckException("Repair cannot create or select another generation."), keys,
            new AndroidGenerationFileDurability(folder), owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
        void Demand()
        {
            owner.Validate(); RequireRoot(folder);
            if (Path.GetDirectoryName(generation.Path) != folder)
                throw new ReviewCheckException("Repair must remain in the exact original reference generation.");
        }
        Demand();
        // The original protected factory binds the exclusive lease; a manually keyed handle has no such authority.
        generation.Publish(location, owner);
        await using var db = await data.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, ct);
        await db.Database.OpenConnectionAsync(ct);
        var source = (SqliteConnection)db.Database.GetDbConnection();
        ProtectedSqliteAccess.Require(source);
        var key = await keys.ReadAsync(generation.Binding, ct);
        try
        {
            await using var rollback = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = generation.Path + ".rollback.db", Mode = SqliteOpenMode.ReadOnly, Pooling = false,
                Password = Convert.ToBase64String(key) }.ToString());
            await rollback.OpenAsync(ct);
            using var bytes = new MemoryStream();
            try
            {
                await SqlCipherPortableSnapshot.WriteAsync(source, bytes, 256 * 1024 * 1024, Demand, ct);
                bytes.Position = 0;
                await using var before = await SqliteMemorySnapshot.OpenAsync(bytes, 256 * 1024 * 1024, ct);
                CryptographicOperations.ZeroMemory(bytes.GetBuffer().AsSpan(0, checked((int)bytes.Length)));
                var scope = string.Join("|", Package, Android.OS.Process.MyUid(), root, "reference", failedProbe,
                    typeof(ZananceStore).Module.ModuleVersionId, ReadBlockSchemaRetirement.Target);
                return await SchemaProbeAdmission.RepairAsync(db, before.Connection, rollback,
                    Path.Combine(root, "d558-reference-schema-probe-repair-request.json"), comparison, scope, Demand,
                    (path, json, token) => WriteNewDurableCpuOracleAsync(root, path, json, token), ct);
            }
            finally { CryptographicOperations.ZeroMemory(bytes.GetBuffer().AsSpan(0, checked((int)bytes.Length))); }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
