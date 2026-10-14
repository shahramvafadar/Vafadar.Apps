using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads complete original budget form inputs without creating preferences or financial rows.</summary>
    public async Task<BudgetEditorReview> GetBudgetEditorReviewAsync(CancellationToken cancellationToken = default)
    {
        var choices = new BudgetChoiceStore(contextFactory, commercialAccess, time);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        // D-276: choices and form inputs keep the same captured file and keyed connection until validation ends.
        await db.Database.OpenConnectionAsync(cancellationToken);
        var periods = await choices.ReadAsync(db, cancellationToken);
        var accounts = await db.Accounts.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var result = new BudgetEditorReview(periods, accounts.AsReadOnly(), categories.AsReadOnly(), settings);
        await ValidateBudgetEditorReviewAsync(result, db, cancellationToken);
        return result;
    }

    /// <summary>Rejects a retired or caller-mutated form review without writing any row.</summary>
    public async Task ValidateBudgetEditorReviewAsync(BudgetEditorReview review, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await ValidateBudgetEditorReviewAsync(review, db, cancellationToken);
    }

    // D-152: the editor and writer use this same context overload; a separate connection cannot protect a Save.
    private async Task ValidateBudgetEditorReviewAsync(BudgetEditorReview review, ZananceDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(review);
        await new BudgetChoiceStore(contextFactory, commercialAccess, time).ValidateAsync(review.Choices, db, cancellationToken);
        var accounts = await db.Accounts.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (review.InputStamp != BudgetEditorReview.Capture(accounts, categories, settings)
            || review.InputStamp != BudgetEditorReview.Capture(review.Accounts, review.Categories, review.Settings))
            throw new InvalidOperationException("The reviewed budget references or preferences changed; reload before saving.");
        review.Choices.Access.EnsureSnapshotCurrent();
    }
}
