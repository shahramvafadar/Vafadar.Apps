using Microsoft.EntityFrameworkCore;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
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
    /// <summary>Raised after data was written, e.g. to refresh reminders.</summary>
    public event EventHandler? Changed;

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
        OnChanged();
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

        OnChanged();
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

        OnChanged();
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

    /// <summary>
    /// Creates the default categories (CAT-01). Defaults added in later versions are created on the next start;
    /// existing ones – renamed or archived by the user – are never touched.
    /// </summary>
    public async Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = (await db.Categories.Where(c => c.SystemKey != null).Select(c => new { c.Kind, c.SystemKey }).ToListAsync(cancellationToken))
            .Select(c => (c.Kind, c.SystemKey!)).ToHashSet();

        var order = 0;
        foreach (var (kind, key, icon, color) in DefaultCategories.All)
        {
            if (!existing.Contains((kind, key)))
            {
                db.Categories.Add(new Category { Kind = kind, SystemKey = key, Icon = icon, Color = color, SortOrder = order });
            }

            order++;
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
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

        OnChanged();
    }

    /// <summary>Returns the budget of a month in a calendar and currency, if one exists.</summary>
    public async Task<Budget?> GetBudgetAsync(int year, int month, PeriodCalendar calendar, string currencyCode, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Budgets.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Year == year && b.Month == month && b.Calendar == calendar && b.CurrencyCode == currencyCode, cancellationToken);
    }

    /// <summary>Returns all budgets, newest period first.</summary>
    public async Task<List<Budget>> GetBudgetsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Budgets.AsNoTracking().OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).ToListAsync(cancellationToken);
    }

    /// <summary>Inserts or updates a budget including its category limits.</summary>
    public async Task SaveBudgetAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(budget);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Budgets.FirstOrDefaultAsync(b => b.Id == budget.Id, cancellationToken);
        if (existing is null)
        {
            db.Budgets.Add(budget);
        }
        else
        {
            // Owned limits are replaced as a whole; the tracked instance keeps EF's owned-entity bookkeeping right.
            existing.TotalLimit = budget.TotalLimit;
            existing.AccountIds = [.. budget.AccountIds];
            existing.AlertsEnabled = budget.AlertsEnabled;
            existing.CategoryLimits.Clear();
            existing.CategoryLimits.AddRange(budget.CategoryLimits.Select(l => new BudgetCategoryLimit { CategoryId = l.CategoryId, Limit = l.Limit }));
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Deletes a budget; entries are not affected.</summary>
    public async Task DeleteBudgetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Budgets.FirstOrDefaultAsync(b => b.Id == id, cancellationToken) is { } budget)
        {
            db.Budgets.Remove(budget);
            await db.SaveChangesAsync(cancellationToken);
            OnChanged();
        }
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

    /// <summary>Returns the refunds linked to a purchase.</summary>
    public async Task<List<LedgerEntry>> GetRefundsAsync(Guid purchaseId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AsNoTracking().Where(e => e.RefundOfId == purchaseId).OrderBy(e => e.Date).ToListAsync(cancellationToken);
    }

    /// <summary>Returns the entries sharing a group id (e.g. a transfer and its fee).</summary>
    public async Task<List<LedgerEntry>> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AsNoTracking().Where(e => e.GroupId == groupId).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Validates and saves an entry. Saving the same entry id twice updates it instead of creating a second entry, so
    /// repeated taps on Save cannot duplicate it (TX-06, AT-03).
    /// </summary>
    public Task<SaveResult> SaveEntryAsync(LedgerEntry entry, CancellationToken cancellationToken = default) =>
        SaveEntriesAsync([entry], [], cancellationToken);

    /// <summary>
    /// Validates and saves related entries in one transaction – e.g. a transfer and its fee – and deletes
    /// <paramref name="deleteIds"/> (a removed fee). Nothing is saved when any entry is invalid.
    /// </summary>
    public async Task<SaveResult> SaveEntriesAsync(
        IReadOnlyList<LedgerEntry> entries,
        IReadOnlyCollection<Guid> deleteIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(deleteIds);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var ids = entries.Select(e => e.Id).ToList();
        var existing = await db.Entries.AsNoTracking().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, cancellationToken);

        foreach (var entry in entries)
        {
            LedgerEntry? original = null;
            long otherRefunds = 0;
            if (entry.Kind == EntryKind.Refund && entry.RefundOfId is { } originalId)
            {
                original = await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == originalId, cancellationToken);
                otherRefunds = await db.Entries.Where(e => e.RefundOfId == originalId && e.Id != entry.Id).SumAsync(e => e.Amount, cancellationToken);
            }

            var errors = LedgerValidator.Validate(entry, accounts, categories, original, otherRefunds, isNew: !existing.ContainsKey(entry.Id));
            if (errors.Count > 0)
            {
                return new SaveResult(errors);
            }
        }

        foreach (var entry in entries)
        {
            if (entry.Kind != EntryKind.Transfer)
            {
                entry.ToAccountId = null;
                entry.ToAmount = null;
            }

            if (existing.TryGetValue(entry.Id, out var previous))
            {
                entry.CreatedAt = previous.CreatedAt;
                db.Entries.Update(entry);
            }
            else
            {
                db.Entries.Add(entry);
            }
        }

        if (deleteIds.Count > 0)
        {
            db.Entries.RemoveRange(await db.Entries.Where(e => deleteIds.Contains(e.Id) && !ids.Contains(e.Id)).ToListAsync(cancellationToken));
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
        return SaveResult.Success;
    }

    /// <summary>
    /// Deletes an entry together with the entries of its group (a transfer and its fee) and returns them, so that
    /// <see cref="RestoreEntriesAsync"/> can undo the deletion. Refunds linked to a deleted purchase are kept.
    /// </summary>
    public async Task<IReadOnlyList<LedgerEntry>> DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return [];
        }

        List<LedgerEntry> deleted = entry.GroupId is { } group
            ? await db.Entries.Where(e => e.GroupId == group).ToListAsync(cancellationToken)
            : [entry];

        db.Entries.RemoveRange(deleted);

        // A deleted settlement reopens its occurrence; automatic posting must not bring it back (REC-18, AT-31).
        foreach (var settled in deleted.Where(e => e.ScheduleId is not null))
        {
            var state = await db.OccurrenceStates.FirstOrDefaultAsync(
                s => s.ScheduleId == settled.ScheduleId && s.OriginalDate == settled.OccurrenceDate, cancellationToken);
            if (state is not null)
            {
                state.Status = OccurrenceStatus.Open;
                state.EntryId = null;
                state.AutoPostSuppressed = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
        return deleted;
    }

    /// <summary>Re-inserts deleted entries unchanged (undo of <see cref="DeleteEntryAsync"/>).</summary>
    public async Task RestoreEntriesAsync(IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        foreach (var entry in entries)
        {
            if (await db.Entries.AnyAsync(e => e.Id == entry.Id, cancellationToken))
            {
                continue;
            }

            db.Entries.Add(entry);
            if (entry.ScheduleId is { } scheduleId && entry.OccurrenceDate is { } original)
            {
                var state = await db.OccurrenceStates.FirstOrDefaultAsync(s => s.ScheduleId == scheduleId && s.OriginalDate == original, cancellationToken);
                if (state is null)
                {
                    state = new OccurrenceState { ScheduleId = scheduleId, OriginalDate = original };
                    db.OccurrenceStates.Add(state);
                }

                state.Status = OccurrenceStatus.Settled;
                state.EntryId = entry.Id;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
