using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Retires only the normal empty independent caller's derived schema before restoring its original APK.</summary>
    private static async Task<object> RetireCallerReadBlocksAsync(IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var uid = Android.OS.Process.MyUid();
        if (!string.Equals(Package, "pro.vafadar.zanance.androidhomereview", StringComparison.Ordinal) || AppInfo.Current.PackageName != Package
            || Platform.CurrentActivity?.Intent?.GetIntExtra("review_retire_uid", -1) != uid
            || Platform.CurrentActivity?.Intent?.GetStringExtra("review_retire_target") != ReadBlockSchemaRetirement.Target
            || Platform.CurrentActivity?.Intent?.GetStringExtra("review_retire_original_apk") != "b1d5d3343ca54a002279c412b205fc9a21f70b7c181cd9d4bc4badf8c3206757")
            throw new ReviewCheckException("Only the explicit original independent caller UID can retire its derived schema.");
        var profile = services.GetRequiredService<ProfileService>();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var ownFiles = new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath;
        var selectedPath = new Java.IO.File(location.Path).CanonicalPath;
        if (!profile.Current.IsMain || !location.IsProtected || !location.RequiresProtection
            || Path.GetDirectoryName(selectedPath) != ownFiles)
            throw new ReviewCheckException("Normal protected independent Main is required; workload trees and shipping Main are excluded.");
        if (!await MainThread.InvokeOnMainThreadAsync(() => Platform.CurrentActivity?.Window?.DecorView?.HasWindowFocus == true))
            throw new ReviewCheckException("The independent caller must own the current native window.");
        var publication = location.CaptureValidation();
        using var owner = await location.Access.AcquireExclusiveAsync(ct);
        void Demand()
        {
            owner.Validate(); publication();
            if (!profile.Current.IsMain || new Java.IO.File(location.Path).CanonicalPath != selectedPath)
                throw new ReviewCheckException("Original independent Main selection changed.");
        }
        Demand();
        await using var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, ct);
        await db.Database.OpenConnectionAsync(ct);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        ProtectedSqliteAccess.Require(connection);
        if (await db.Entries.AsNoTracking().CountAsync(ct) != 0)
            throw new ReviewCheckException("Normal independent Main must have no accepted financial entries to retire transport.");
        using var bytes = new MemoryStream();
        await SqlCipherPortableSnapshot.WriteAsync(connection, bytes, 256 * 1024 * 1024, Demand, ct);
        bytes.Position = 0;
        await using var portable = await SqliteMemorySnapshot.OpenAsync(bytes, 256 * 1024 * 1024, ct);
        CryptographicOperations.ZeroMemory(bytes.GetBuffer().AsSpan(0, checked((int)bytes.Length)));
        await using var before = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await before.OpenAsync(ct); portable.Connection.BackupDatabase(before);
        var scope = string.Join("|", Package, uid, typeof(AndroidHomeReview).Module.ModuleVersionId, "normal-main", ReadBlockSchemaRetirement.Target,
            "b1d5d3343ca54a002279c412b205fc9a21f70b7c181cd9d4bc4badf8c3206757");
        var result = await ReadBlockSchemaRetirement.RetireAsync(db, before, Path.Combine(root, "retire-request.json"),
            comparison, scope, Demand, (path, json, token) => WriteNewDurableCpuOracleAsync(root, path, json, token), ct);
        return new { Comparison = comparison, CallerUid = uid, CallerPackage = Package, SelectedNormalMain = true,
            OnlyDeclaredHomeBookkeepingRetired = true, OldCallerSchemaSupported = true, SourceAndKeysNotReplaced = true,
            ShippingMainTouched = false, WorkloadSourcesTouched = false, Result = result };
    }
}
