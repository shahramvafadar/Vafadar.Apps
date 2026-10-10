using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Recognizes explicit principal closure from actual stored debt and the complete resulting ledger.</summary>
    private static async Task<Dictionary<Guid, HashSet<Guid>>> RetainedDebtClosingAsync(ZananceDbContext db,
        IReadOnlyList<LedgerEntry> entries, IReadOnlyCollection<Guid> deleteIds,
        IReadOnlyDictionary<Guid, LedgerEntry> existing, IReadOnlyDictionary<Guid, Account> accounts,
        IReadOnlyDictionary<Guid, Category> categories, IReadOnlyList<LedgerEntry> storedGroups,
        IReadOnlyCollection<Guid> holdingGroups, DateOnly today, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, HashSet<Guid>>();
        var units = new List<List<LedgerEntry>>();
        foreach (var transfer in entries.Where(e => e.Kind == EntryKind.Transfer && !existing.ContainsKey(e.Id)))
        {
            if (!ManualReviewed(transfer) || transfer.Amount <= 0 || transfer.Date > today
                || transfer.ToAccountId is not { } destination || destination == transfer.AccountId
                || !accounts.TryGetValue(transfer.AccountId, out var source) || source.IsArchived
                || !accounts.TryGetValue(destination, out var target) || target.IsArchived
                || transfer.Date < source.OpeningDate || transfer.Date < target.OpeningDate) continue;
            if (source.CurrencyCode == target.CurrencyCode
                ? transfer.ToAmount is { } same && same != transfer.Amount
                : transfer.ToAmount is null or <= 0) continue;

            List<LedgerEntry> unit = [transfer];
            if (transfer.GroupId is { } group)
            {
                // A retained closing unit cannot repurpose existing groups or holding payment relationships.
                if (holdingGroups.Contains(group) || storedGroups.Any(e => e.GroupId == group)) continue;
                unit = entries.Where(e => e.GroupId == group).ToList();
                var fees = unit.Where(e => e.Id != transfer.Id).ToList();
                if (fees.Any(e => existing.ContainsKey(e.Id) || !ManualReviewed(e) || e.Kind != EntryKind.Expense
                    || e.Amount <= 0 || e.Date != transfer.Date || (e.AccountId != source.Id && e.AccountId != target.Id)
                    || e.CategoryId is not { } category || !categories.TryGetValue(category, out var actual)
                    || actual.Kind != CategoryKind.Expense || actual.SystemKey != "Fees")
                    || fees.GroupBy(e => e.AccountId).Any(g => g.Count() > 1)) continue;
            }
            units.Add(unit);
        }

        var debtIds = units.SelectMany(u => u.Where(e => e.Kind == EntryKind.Transfer)
            .SelectMany(e => new[] { e.AccountId, e.ToAccountId!.Value }))
            .Distinct().Where(id => accounts[id].Type.IsDebt()).ToList();
        if (debtIds.Count == 0) return result;
        var stored = await db.Entries.AsNoTracking().Where(e => debtIds.Contains(e.AccountId)
            || (e.ToAccountId != null && debtIds.Contains(e.ToAccountId.Value))).ToListAsync(cancellationToken);
        var incomingIds = entries.Select(e => e.Id).ToHashSet();
        var final = stored.Where(e => !incomingIds.Contains(e.Id) && !deleteIds.Contains(e.Id)).Concat(entries).ToList();
        var closed = new HashSet<Guid>();
        foreach (var id in debtIds)
        {
            var account = accounts[id];
            if (!account.OpeningBalanceKnown || account.OpeningDate > today) continue;
            var baseline = LedgerCalculator.Balance(account, stored, today);
            if ((account.Type == AccountType.Loan ? baseline >= 0 : baseline <= 0)
                || baseline != LedgerCalculator.Balance(account, stored, today, confirmedOnly: true)) continue;
            var payments = units.Where(u => u.Any(e => e.Kind == EntryKind.Transfer
                && (account.Type == AccountType.Loan ? e.ToAccountId == id : e.AccountId == id))).ToList();
            if (payments.Count == 0 || payments.Any(u => LedgerCalculator.Balance(account, stored, u[0].Date) != baseline)) continue;
            // Widen before summing: closing Int64.MinValue may need more than one positive Int64 payment.
            var effect = payments.SelectMany(u => u).Sum(e => (decimal)e.EffectOn(id));
            var finalEffect = final.Where(e => e.Date >= account.OpeningDate && e.Date <= today)
                .Sum(e => (decimal)e.EffectOn(id));
            if (effect == -(decimal)baseline && (decimal)account.OpeningBalance + finalEffect == 0) closed.Add(id);
        }

        foreach (var unit in units)
        {
            var transfer = unit.Single(e => e.Kind == EntryKind.Transfer);
            if (!(closed.Contains(transfer.AccountId) && accounts[transfer.AccountId].Type == AccountType.Lent)
                && !(closed.Contains(transfer.ToAccountId!.Value) && accounts[transfer.ToAccountId.Value].Type == AccountType.Loan)) continue;
            // Existing cash funds may settle retained debt. Creating card debt or new lending still needs its own right.
            var allowed = new[] { transfer.AccountId, transfer.ToAccountId!.Value }.Where(id =>
                (closed.Contains(id) && (id == transfer.AccountId ? accounts[id].Type == AccountType.Lent : accounts[id].Type == AccountType.Loan))
                || accounts[id].Type is AccountType.Cash or AccountType.Checking or AccountType.Savings).ToHashSet();
            foreach (var row in unit) result[row.Id] = allowed;
        }
        return result;
    }

    /// <summary>Excludes automatic, imported and plan-marked drafts from manual retained closing rights.</summary>
    private static bool ManualReviewed(LedgerEntry entry) => entry.Source == EntrySource.Manual
        && entry.Review == ReviewState.Confirmed && entry.ScheduleId is null && entry.OccurrenceDate is null
        && !entry.IsPartialPayment && entry.ImportBatchId is null && entry.RefundOfId is null;
}
