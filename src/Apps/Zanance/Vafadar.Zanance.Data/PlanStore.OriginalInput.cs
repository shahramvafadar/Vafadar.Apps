using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data;

/// <summary>The original schedule and its actual file, captured in the same existing schedule read.</summary>
/// <param name="Schedule">The original schedule, or null when it no longer exists.</param>
/// <param name="DatabasePath">The actual file that owns this original schedule.</param>
public sealed record OriginalScheduleRead(Schedule? Schedule, string DatabasePath);

public sealed partial class PlanStore
{
    /// <summary>Reads the occurrence's original plan and file without following another profile at Save time.</summary>
    public async Task<OriginalScheduleRead> ReadOriginalScheduleAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var plan = await db.Schedules.AsNoTracking().FirstOrDefaultAsync(row => row.Id == id, ct);
        return new(plan, DatabaseFileIdentity.ConfiguredPath(db));
    }
}

public sealed partial class ZananceStore
{
    /// <summary>Rechecks only a supplied original occurrence owner; existing non-form callers retain their contract.</summary>
    internal static void RequireOccurrenceInputOwner(ZananceDbContext db, Schedule original, string? path,
        string? currency, Func<bool>? isCurrent)
    {
        if (path is not null && path != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original occurrence file changed.");
        if (isCurrent?.Invoke() == false) throw new InvalidOperationException("The original occurrence input retired.");
        if (path is not null || currency is not null)
        {
            var target = (from plan in db.Schedules.AsNoTracking() where plan.Id == original.Id
                join account in db.Accounts.AsNoTracking() on plan.AccountId equals account.Id
                select new { plan.Kind, plan.AccountId, plan.ToAccountId, account.CurrencyCode }).SingleOrDefault();
            if (target is null || target.Kind != original.Kind || target.AccountId != original.AccountId || target.ToAccountId != original.ToAccountId
                || currency is not null && target.CurrencyCode != currency)
                throw new InvalidOperationException("The original occurrence financial target changed.");
        }
    }
}
