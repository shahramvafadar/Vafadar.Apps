using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Importing;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Captures only the current configured file identity before a native picker, without moving/opening SQL files.</summary>
    public async Task<string> CaptureImportFileAsync(CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        return DatabaseFileIdentity.ConfiguredPath(db);
    }
    /// <summary>Reads all original import sources in one snapshot and rejects a profile retired during the picker.</summary>
    public async Task<ImportReadReview> ReadImportReviewAsync(string expectedPath, CancellationToken ct = default, bool reviewAccess = true,
        Action? validateNative = null, bool includeCategoryRules = false)
    {
        ct.ThrowIfCancellationRequested(); validateNative?.Invoke();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (path != expectedPath) throw new InvalidOperationException("The original import profile changed.");
        var access = new CommercialFileAccess(commercialAccess, path);
        // Ordinary read-only CSV review remains available before purchase; its writer demands fresh
        // rights. Bank consent keeps its original reviewed-rights contract (D-405).
        if (reviewAccess) { access.EnsureSnapshotCurrent(); access.DemandFeature(CommercialFeature.CsvImport); }
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var inputs = await ImportReadReview.ReadInputsAsync(db, ct);
        var rules = includeCategoryRules ? (await db.CategoryRules.AsNoTracking().ToListAsync(ct))
            .OrderBy(rule => rule.Match, StringComparer.CurrentCultureIgnoreCase).ToList() : [];
        ct.ThrowIfCancellationRequested(); validateNative?.Invoke();
        var review = new ImportReadReview(path, access, inputs, reviewAccess, validateNative, rules);
        review.EnsureCurrent(); return review;
    }
}
