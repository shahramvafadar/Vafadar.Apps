using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Creates a process-local original handoff after validating the displayed source and activity choice.</summary>
    public async Task<TemplateVisit> CreateTemplateVisitAsync(QuickResourceReview review, Guid templateId, CancellationToken cancellationToken = default)
    {
        await ValidateQuickResourceReviewAsync(review, cancellationToken);
        if (!review.Templates.Any(template => template.Id == templateId) || !review.Work.CanApplyTemplate(templateId))
            throw new InvalidOperationException("The original template is unavailable for new draft activity.");
        return new(review, templateId);
    }

    /// <summary>Resolves the exact original source again at the destination, never another file's same-id template.</summary>
    public async Task<Core.Ledger.EntryTemplate> ResolveTemplateVisitAsync(TemplateVisit visit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visit);
        await ValidateQuickResourceReviewAsync(visit.Review, cancellationToken);
        if (!visit.Review.Work.CanApplyTemplate(visit.TemplateId)) throw new InvalidOperationException("Template activity is unavailable.");
        return visit.Review.Templates.Single(template => template.Id == visit.TemplateId);
    }

    /// <summary>Reads complete original quick definitions/references and an optional original entry without writing defaults.</summary>
    public async Task<QuickResourceReview> GetQuickResourceReviewAsync(Guid? sourceEntryId = null, CancellationToken cancellationToken = default)
    {
        var choices = new ResourceChoiceStore(contextFactory, commercialAccess);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        // D-274: one captured original file and native connection owns this complete read. Reopening
        // per choice repeated SQLCipher's unchanged key derivation and could capture a switched profile.
        var templates = await choices.ReadAsync(QuotaKind.QuickTemplates, db, cancellationToken);
        var filters = await choices.ReadAsync(QuotaKind.SavedFilters, db, cancellationToken);
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
        await db.Database.OpenConnectionAsync(cancellationToken);
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
