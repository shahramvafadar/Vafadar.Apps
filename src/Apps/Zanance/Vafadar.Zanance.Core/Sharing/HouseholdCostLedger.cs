using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Content-free household financial validation, separate from the original cash ledger's own rules.</summary>
public sealed record HouseholdCostIssue(Guid RecordId, string Code, string Table = "HouseholdCosts");

/// <summary>One contact/currency's exact cost allocation and private advance; never an inferred share of joint-account ownership.</summary>
public sealed record HouseholdMemberCostSummary(Guid ContactId, string CurrencyCode, decimal AttributedNetCost,
    decimal PrivatePaid, decimal PrivateSettlementNet, decimal PrivateAdvanceBalance);

/// <summary>One household/currency total; linked ledger expenses are not added again and membership cardinality is irrelevant.</summary>
public sealed record HouseholdCostTotal(string CurrencyCode, decimal NetCost, decimal PaidFromSharedAccounts,
    decimal PaidPrivately);

/// <summary>Explicit actual costs/repayments; never posts ledger rows, invents member loans or mixes currency units.</summary>
public static class HouseholdCostLedger
{
    /// <summary>Validates exact allocation sums, complete references, original linked movements and cumulative refund limits.</summary>
    public static IReadOnlyList<HouseholdCostIssue> Validate(IReadOnlyCollection<HouseholdContact> contacts,
        IReadOnlyCollection<HouseholdCost> costs, IReadOnlyCollection<HouseholdCostAllocation> allocations,
        IReadOnlyCollection<HouseholdSettlement> settlements, IReadOnlyDictionary<Guid, LedgerEntry> entries,
        IReadOnlyDictionary<Guid, Account> accounts, IReadOnlySet<Guid>? newCostIds = null,
        IReadOnlySet<Guid>? newSettlementIds = null, IReadOnlySet<(Guid CostId, Guid ContactId)>? newAllocations = null)
    {
        ArgumentNullException.ThrowIfNull(contacts); ArgumentNullException.ThrowIfNull(costs);
        ArgumentNullException.ThrowIfNull(allocations); ArgumentNullException.ThrowIfNull(settlements);
        ArgumentNullException.ThrowIfNull(entries); ArgumentNullException.ThrowIfNull(accounts);
        var issues = new List<HouseholdCostIssue>();
        if (contacts.Select(row => row.Id).Distinct().Count() != contacts.Count ||
            costs.Select(row => row.Id).Distinct().Count() != costs.Count ||
            settlements.Select(row => row.Id).Distinct().Count() != settlements.Count ||
            allocations.Select(row => (row.CostId, row.ContactId)).Distinct().Count() != allocations.Count)
            return [new(Guid.Empty, "DuplicateHouseholdIdentity")];
        var people = contacts.ToDictionary(row => row.Id); var actual = costs.ToDictionary(row => row.Id);
        var shares = allocations.GroupBy(row => row.CostId).ToDictionary(group => group.Key, group => group.ToArray());
        var linkedCosts = new HashSet<Guid>(); var linkedSettlements = new HashSet<Guid>();
        foreach (var contact in contacts)
        {
            if (contact.Id == Guid.Empty || string.IsNullOrWhiteSpace(contact.Name) || contact.Name.Length > 100 || contact.IdentityId?.Length > 128)
                Add(contact.Id, "InvalidHouseholdContact", "HouseholdContacts");
        }
        foreach (var share in allocations)
        {
            if (!actual.ContainsKey(share.CostId) || !people.ContainsKey(share.ContactId) || share.Amount < 0)
                Add(share.CostId, "InvalidHouseholdAllocation");
            if (newAllocations?.Contains((share.CostId, share.ContactId)) == true && people.GetValueOrDefault(share.ContactId)?.IsArchived != false)
                Add(share.CostId, "HouseholdContactArchived");
        }
        foreach (var cost in costs)
        {
            if (cost.Id == Guid.Empty || cost.Amount <= 0 || !ValidCurrency(cost.CurrencyCode) ||
                !Enum.IsDefined(cost.Kind) || !Enum.IsDefined(cost.PaymentKind) ||
                string.IsNullOrWhiteSpace(cost.Title) || cost.Title.Length > 200 || cost.Note?.Length > 4000)
                Add(cost.Id, "InvalidHouseholdCost");
            var rows = shares.GetValueOrDefault(cost.Id) ?? [];
            if (rows.Length == 0 || rows.Sum(row => (decimal)row.Amount) != cost.Amount)
                Add(cost.Id, "HouseholdAllocationSumMismatch");
            if (newCostIds?.Contains(cost.Id) == true && rows.Any(row => people.GetValueOrDefault(row.ContactId)?.IsArchived != false))
                Add(cost.Id, "HouseholdContactArchived");
            if (cost.PaymentKind == HouseholdPaymentKind.Private)
            {
                if (cost.EntryId is not null || cost.PaidByContactId is not { } payer || !people.ContainsKey(payer))
                    Add(cost.Id, "InvalidPrivateHouseholdPayment");
                else if (newCostIds?.Contains(cost.Id) == true && people[payer].IsArchived)
                    Add(cost.Id, "HouseholdContactArchived");
            }
            else if (cost.PaymentKind == HouseholdPaymentKind.SharedAccount)
            {
                if (cost.PaidByContactId is not null || cost.EntryId is not { } entryId || !linkedCosts.Add(entryId) ||
                    !entries.TryGetValue(entryId, out var entry) || !accounts.TryGetValue(entry.AccountId, out var account) ||
                    entry.Amount != cost.Amount || entry.Date != cost.Date || account.CurrencyCode != cost.CurrencyCode ||
                    entry.Kind != (cost.Kind == HouseholdCostKind.Expense ? EntryKind.Expense : EntryKind.Refund))
                    Add(cost.Id, "SharedHouseholdMovementMismatch");
            }
            if (cost.Kind == HouseholdCostKind.Expense && cost.RefundOfCostId is not null)
                Add(cost.Id, "UnexpectedHouseholdRefundReference");
            if (cost.Kind != HouseholdCostKind.Refund) continue;
            if (cost.RefundOfCostId is not { } originalId || !actual.TryGetValue(originalId, out var original) ||
                original.Kind != HouseholdCostKind.Expense || original.CurrencyCode != cost.CurrencyCode)
            { Add(cost.Id, "HouseholdRefundOriginalMissing"); continue; }
            if (cost.EntryId is { } refundEntryId && original.EntryId is { } originalEntryId &&
                entries.TryGetValue(refundEntryId, out var refundEntry) && refundEntry.RefundOfId != originalEntryId)
                Add(cost.Id, "HouseholdRefundMovementMismatch");
        }
        foreach (var group in costs.Where(row => row.Kind == HouseholdCostKind.Refund && row.RefundOfCostId is not null)
            .GroupBy(row => row.RefundOfCostId!.Value))
        {
            if (!actual.TryGetValue(group.Key, out var original)) continue;
            if (group.Sum(row => (decimal)row.Amount) > original.Amount) Add(original.Id, "HouseholdRefundExceedsCost");
            var originalShares = (shares.GetValueOrDefault(original.Id) ?? []).ToDictionary(row => row.ContactId, row => row.Amount);
            var refundedShares = group.SelectMany(row => shares.GetValueOrDefault(row.Id) ?? []).GroupBy(row => row.ContactId);
            foreach (var refunded in refundedShares)
                if (refunded.Sum(row => (decimal)row.Amount) > originalShares.GetValueOrDefault(refunded.Key))
                    Add(original.Id, "HouseholdRefundExceedsContactShare");
        }
        foreach (var settlement in settlements)
        {
            if (settlement.Id == Guid.Empty || settlement.Amount <= 0 || !ValidCurrency(settlement.CurrencyCode) ||
                settlement.FromContactId == settlement.ToContactId || !people.ContainsKey(settlement.FromContactId) ||
                !people.ContainsKey(settlement.ToContactId) || settlement.Note?.Length > 4000)
                Add(settlement.Id, "InvalidHouseholdSettlement", "HouseholdSettlements");
            if (newSettlementIds?.Contains(settlement.Id) == true &&
                (people.GetValueOrDefault(settlement.FromContactId)?.IsArchived != false || people.GetValueOrDefault(settlement.ToContactId)?.IsArchived != false))
                Add(settlement.Id, "HouseholdContactArchived", "HouseholdSettlements");
            if (settlement.EntryId is not { } entryId)
            {
                if (settlement.UsesDestinationAmount) Add(settlement.Id, "UnexpectedHouseholdSettlementSide", "HouseholdSettlements");
                continue;
            }
            if (!linkedSettlements.Add(entryId) || !entries.TryGetValue(entryId, out var entry) || entry.Kind != EntryKind.Transfer || entry.Date != settlement.Date)
            { Add(settlement.Id, "HouseholdSettlementMovementMismatch", "HouseholdSettlements"); continue; }
            var accountId = settlement.UsesDestinationAmount ? entry.ToAccountId : entry.AccountId;
            var amount = settlement.UsesDestinationAmount ? entry.ToAmount : entry.Amount;
            // The original ledger omits ToAmount for an unchanged same-currency transfer.
            // Use the source magnitude only with actual matching source/destination currency evidence.
            if (settlement.UsesDestinationAmount && amount is null && accountId is { } destination &&
                accounts.TryGetValue(destination, out var destinationAccount) && accounts.TryGetValue(entry.AccountId, out var sourceAccount) &&
                destinationAccount.CurrencyCode == sourceAccount.CurrencyCode) amount = entry.Amount;
            if (accountId is not { } actualAccount || !accounts.TryGetValue(actualAccount, out var account) ||
                amount != settlement.Amount || account.CurrencyCode != settlement.CurrencyCode)
                Add(settlement.Id, "HouseholdSettlementMovementMismatch", "HouseholdSettlements");
        }
        return issues;

        void Add(Guid id, string code, string table = "HouseholdCosts") { if (issues.Count < 100) issues.Add(new(id, code, table)); }
    }

    /// <summary>Counts each actual cost once, independent of its allocations, linked cash entry or number of members.</summary>
    public static IReadOnlyList<HouseholdCostTotal> Totals(IReadOnlyCollection<HouseholdCost> costs) => costs
        .GroupBy(row => row.CurrencyCode, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal)
        .Select(group => new HouseholdCostTotal(group.Key, group.Sum(Signed),
            group.Where(row => row.PaymentKind == HouseholdPaymentKind.SharedAccount).Sum(Signed),
            group.Where(row => row.PaymentKind == HouseholdPaymentKind.Private).Sum(Signed))).ToArray();

    /// <summary>
    /// Reports attribution separately from private advances. Joint-account costs do not invent an owner's/member's
    /// personal credit or infer who contributed opening cash; only explicit private payments/repayments affect advances.
    /// </summary>
    public static IReadOnlyList<HouseholdMemberCostSummary> Members(IReadOnlyCollection<HouseholdContact> contacts,
        IReadOnlyCollection<HouseholdCost> costs, IReadOnlyCollection<HouseholdCostAllocation> allocations,
        IReadOnlyCollection<HouseholdSettlement> settlements)
    {
        var byId = costs.ToDictionary(row => row.Id);
        var currencies = costs.Select(row => row.CurrencyCode).Concat(settlements.Select(row => row.CurrencyCode))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var results = new List<HouseholdMemberCostSummary>();
        foreach (var person in contacts.OrderBy(row => row.Id))
        foreach (var currency in currencies)
        {
            var assigned = allocations.Where(row => row.ContactId == person.Id && byId.TryGetValue(row.CostId, out var cost) && cost.CurrencyCode == currency).ToArray();
            var attribution = assigned.Sum(row => Sign(byId[row.CostId]) * row.Amount);
            var privateAttribution = assigned.Where(row => byId[row.CostId].PaymentKind == HouseholdPaymentKind.Private)
                .Sum(row => Sign(byId[row.CostId]) * row.Amount);
            var paid = costs.Where(row => row.CurrencyCode == currency && row.PaymentKind == HouseholdPaymentKind.Private && row.PaidByContactId == person.Id).Sum(Signed);
            var settlement = settlements.Where(row => row.CurrencyCode == currency).Sum(row =>
                row.FromContactId == person.Id ? (decimal)row.Amount : row.ToContactId == person.Id ? -(decimal)row.Amount : 0);
            results.Add(new(person.Id, currency, attribution, paid, settlement, paid - privateAttribution + settlement));
        }
        return results;
    }

    /// <summary>Rejects noncanonical/unknown currency codes instead of labelling amounts with a guessed default unit.</summary>
    private static bool ValidCurrency(string code) => code is { Length: 3 } && code == code.ToUpperInvariant() && Currencies.TryGet(code, out _);
    /// <summary>Uses decimal before arithmetic so Int64 aggregates do not silently overflow.</summary>
    private static decimal Signed(HouseholdCost row) => Sign(row) * row.Amount;
    /// <summary>Only actual refunds reverse a cost's sign.</summary>
    private static decimal Sign(HouseholdCost row) => row.Kind == HouseholdCostKind.Refund ? -1m : 1m;
}
