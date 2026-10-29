using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads the complete original template definitions, rights and references in one actual-file worker transaction.</summary>
    public async Task<QuickResourceReview> ReadTemplateCenterAsync(Func<string, bool>? acceptsFile = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        // Bind the first file before any read can fail, so explicit Retry cannot silently adopt another profile.
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original template center cannot follow another file.");
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var review = await GetQuickResourceReviewAsync(db, sourceEntryId: null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested(); return review;
        }, cancellationToken);
    }

    /// <summary>Revalidates the original complete template-center packet on a worker before covered publication or native commands.</summary>
    public async Task ValidateTemplateCenterAsync(QuickResourceReview review, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        if (review.SourceEntry is not null) throw new InvalidOperationException("A template-center packet cannot borrow an entry draft.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        review.TemplateChoice.Access.EnsureSnapshotCurrent(); review.FilterChoice.Access.EnsureSnapshotCurrent();
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (review.TemplateChoice.DatabasePath != path || review.FilterChoice.DatabasePath != path)
            throw new InvalidOperationException("The original template-center file changed.");
        await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            await ValidateQuickResourceReviewAsync(review, db, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }
}
