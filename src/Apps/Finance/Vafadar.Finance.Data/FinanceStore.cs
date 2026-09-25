using Microsoft.EntityFrameworkCore;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Settings;

namespace Vafadar.Finance.Data;

/// <summary>The result of saving an entry.</summary>
/// <param name="Errors">Validation problems; empty when saved.</param>
public sealed record SaveResult(IReadOnlyList<LedgerError> Errors)
{
    /// <summary>Gets a value indicating whether the entry was saved.</summary>
    public bool Succeeded => Errors.Count == 0;

    /// <summary>A successful result.</summary>
    public static SaveResult Success { get; } = new([]);
}

/// <summary>
/// Data access for the Finance app. Every operation uses a short-lived context from the factory.
/// </summary>
public sealed class FinanceStore(IDbContextFactory<FinanceDbContext> contextFactory)
{
    /// <summary>
    /// Returns the settings synchronously – for startup code on the UI thread, where blocking on async code could
    /// deadlock. Returns defaults (not saved) when no settings exist yet.
    /// </summary>
    public FinanceSettings GetSettings()
    {
        using var db = contextFactory.CreateDbContext();
        return db.Settings.AsNoTracking().FirstOrDefault() ?? new FinanceSettings();
    }

    /// <summary>Returns the settings row, creating it on first use.</summary>
    public async Task<FinanceSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new FinanceSettings();
        db.Settings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    /// <summary>Saves the settings.</summary>
    public async Task SaveSettingsAsync(FinanceSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existingId = await db.Settings.Select(s => (Guid?)s.Id).FirstOrDefaultAsync(cancellationToken);
        if (existingId is null)
        {
            db.Settings.Add(settings);
        }
        else if (existingId == settings.Id)
        {
            db.Settings.Update(settings);
        }
        else
        {
            throw new InvalidOperationException("A different settings row already exists; load it with GetSettingsAsync first.");
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Returns all accounts ordered for display.</summary>
    public async Task<List<Account>> GetAccountsAsync(bool includeArchived = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Accounts.AsNoTracking()
            .Where(a => includeArchived || !a.IsArchived)
            .OrderBy(a => a.IsArchived).ThenBy(a => a.SortOrder).ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Inserts or updates an account. The currency of an account with entries cannot change (ACC-07).</summary>
    /// <returns><see langword="false"/> when the currency change was refused.</returns>
    public async Task<bool> SaveAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);
        if (existing is null)
        {
            db.Accounts.Add(account);
        }
        else
        {
            if (!string.Equals(existing.CurrencyCode, account.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                && await db.Entries.AnyAsync(e => e.AccountId == account.Id || e.ToAccountId == account.Id, cancellationToken))
            {
                return false;
            }

            db.Accounts.Update(account);
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Returns whether an account has any entries.</summary>
    public async Task<bool> HasEntriesAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AnyAsync(e => e.AccountId == accountId || e.ToAccountId == accountId, cancellationToken);
    }

    /// <summary>Returns all categories.</summary>
    public async Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().OrderBy(c => c.Kind).ThenBy(c => c.SortOrder).ToListAsync(cancellationToken);
    }

    /// <summary>Creates the default categories once (CAT-01); later calls do nothing.</summary>
    public async Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var order = 0;
        foreach (var (kind, key, icon, color) in DefaultCategories.All)
        {
            db.Categories.Add(new Category { Kind = kind, SystemKey = key, Icon = icon, Color = color, SortOrder = order++ });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Inserts or updates a category.</summary>
    public async Task SaveCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Categories.AnyAsync(c => c.Id == category.Id, cancellationToken))
        {
            db.Categories.Update(category);
        }
        else
        {
            db.Categories.Add(category);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Returns entries between two dates (inclusive), newest first.</summary>
    public async Task<List<LedgerEntry>> GetEntriesAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Entries.AsNoTracking();
        if (from is { } f)
        {
            query = query.Where(e => e.Date >= f);
        }

        if (to is { } t)
        {
            query = query.Where(e => e.Date <= t);
        }

        return await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToListAsync(cancellationToken);
    }

    /// <summary>Returns one entry.</summary>
    public async Task<LedgerEntry?> GetEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Validates and saves an entry. Saving the same entry id twice updates it instead of creating a second entry, so
    /// repeated taps on Save cannot duplicate it (TX-06, AT-03).
    /// </summary>
    public async Task<SaveResult> SaveEntryAsync(LedgerEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var existing = await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entry.Id, cancellationToken);

        LedgerEntry? original = null;
        long otherRefunds = 0;
        if (entry.Kind == EntryKind.Refund && entry.RefundOfId is { } originalId)
        {
            original = await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == originalId, cancellationToken);
            otherRefunds = await db.Entries.Where(e => e.RefundOfId == originalId && e.Id != entry.Id).SumAsync(e => e.Amount, cancellationToken);
        }

        var errors = LedgerValidator.Validate(entry, accounts, categories, original, otherRefunds, isNew: existing is null);
        if (errors.Count > 0)
        {
            return new SaveResult(errors);
        }

        if (entry.Kind != EntryKind.Transfer)
        {
            entry.ToAccountId = null;
            entry.ToAmount = null;
        }

        if (existing is null)
        {
            db.Entries.Add(entry);
        }
        else
        {
            entry.CreatedAt = existing.CreatedAt;
            db.Entries.Update(entry);
        }

        await db.SaveChangesAsync(cancellationToken);
        return SaveResult.Success;
    }

    /// <summary>Deletes an entry and returns it so that it can be restored by <see cref="RestoreEntryAsync"/> (undo).</summary>
    public async Task<LedgerEntry?> DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        db.Entries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    /// <summary>Re-inserts a deleted entry unchanged (undo of <see cref="DeleteEntryAsync"/>).</summary>
    public async Task RestoreEntryAsync(LedgerEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Entries.AnyAsync(e => e.Id == entry.Id, cancellationToken))
        {
            db.Entries.Add(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
