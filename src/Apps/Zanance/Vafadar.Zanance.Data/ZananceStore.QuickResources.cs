using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads complete original quick definitions/references and an optional original entry without writing defaults.</summary>
    public async Task<QuickResourceReview> GetQuickResourceReviewAsync(Guid? sourceEntryId = null, CancellationToken cancellationToken = default)
    {
        var choices = new ResourceChoiceStore(contextFactory, commercialAccess);
        var templates = await choices.ReadAsync(QuotaKind.QuickTemplates, cancellationToken);
        var filters = await choices.ReadAsync(QuotaKind.SavedFilters, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var review = new QuickResourceReview(templates, filters,
            (await db.Templates.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(t => t.Id).ToArray(),
            (await db.SavedFilters.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(f => f.Id).ToArray(),
            (await db.Accounts.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(a => a.Id).ToArray(),
            (await db.Categories.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(c => c.Id).ToArray(),
            await db.Settings.AsNoTracking().SingleOrDefaultAsync(cancellationToken),
            sourceEntryId is { } id ? await db.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("The original template source entry is unavailable.") : null);
        await ValidateQuickResourceReviewAsync(review, db, cancellationToken);
        return review;
    }

    /// <summary>Rechecks the original published packet before an explicit native action without modifying any row.</summary>
    public async Task ValidateQuickResourceReviewAsync(QuickResourceReview review, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await ValidateQuickResourceReviewAsync(review, db, cancellationToken);
    }

    // D-154: all reviewed writes share this actual-context overload after acquiring their own SQLite writer.
    private async Task ValidateQuickResourceReviewAsync(QuickResourceReview review, ZananceDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(review);
        var choices = new ResourceChoiceStore(contextFactory, commercialAccess);
        await choices.ValidateAsync(review.TemplateChoice, db, cancellationToken);
        await choices.ValidateAsync(review.FilterChoice, db, cancellationToken);
        var stamp = QuickResourceReview.Capture(
            (await db.Templates.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(t => t.Id).ToArray(),
            (await db.SavedFilters.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(f => f.Id).ToArray(),
            (await db.Accounts.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(a => a.Id).ToArray(),
            (await db.Categories.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(c => c.Id).ToArray(),
            await db.Settings.AsNoTracking().SingleOrDefaultAsync(cancellationToken),
            review.SourceEntry is { } source ? await db.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == source.Id, cancellationToken) : null);
        if (stamp != review.InputStamp || review.InputStamp != QuickResourceReview.Capture(review.Templates, review.Filters,
            review.Accounts, review.Categories, review.Settings, review.SourceEntry))
            throw new InvalidOperationException("The original quick-resource definitions, source or references changed; reload them.");
        review.TemplateChoice.Access.EnsureSnapshotCurrent(); review.FilterChoice.Access.EnsureSnapshotCurrent();
    }
}
