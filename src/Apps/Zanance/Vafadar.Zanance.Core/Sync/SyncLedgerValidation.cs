using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Reuses original ledger validation on the final proposed graph, including all linked refunds.</summary>
public static class SyncLedgerValidation
{
    /// <summary>Checks structure first, then actual account currencies and original ledger invariants without posting money.</summary>
    public static IReadOnlyList<SyncGraphIssue> Validate(IReadOnlyCollection<SyncRecord> records, IReadOnlySet<Guid>? newEntryIds = null,
        SyncSchemaManifest? contract = null)
    {
        var structural = SyncGraphValidation.Validate(records, contract);
        if (structural.Count != 0) return structural;
        var live = records.Where(record => record.Values is not null).ToArray();
        var accounts = live.Where(record => record.Record.Table == "Accounts").ToDictionary(record => Id(record.Values!.Value), record =>
        {
            var row = record.Values!.Value;
            return new Account(Id(row)) { Name = row.GetProperty("Name").GetString()!, CurrencyCode = row.GetProperty("CurrencyCode").GetString()!,
                Type = (AccountType)Long(row, "Type"), IsArchived = Long(row, "IsArchived") != 0,
                OpeningBalance = Long(row, "OpeningBalance"), OpeningDate = Date(row, "OpeningDate") };
        });
        var categories = live.Where(record => record.Record.Table == "Categories").ToDictionary(record => Id(record.Values!.Value), record =>
        {
            var row = record.Values!.Value;
            return new Category(Id(row)) { Name = Text(row, "Name"), Kind = (CategoryKind)Long(row, "Kind"),
                ParentId = GuidOrNull(row, "ParentId"), IsArchived = Long(row, "IsArchived") != 0 };
        });
        var entries = live.Where(record => record.Record.Table == "Entries")
            .ToDictionary(record => Id(record.Values!.Value), record => ProjectEntry(record.Values!.Value));
        var entryKeys = live.Where(record => record.Record.Table == "Entries").ToDictionary(record => Id(record.Values!.Value), record => record.Record);
        var accountKeys = live.Where(record => record.Record.Table == "Accounts").ToDictionary(record => Id(record.Values!.Value), record => record.Record);
        var issues = new List<SyncGraphIssue>();
        foreach (var (id, account) in accounts)
        {
            if (!Money.Currencies.TryGet(account.CurrencyCode, out _)) issues.Add(new(accountKeys[id], "InvalidCurrency", ["CurrencyCode"]));
            if (issues.Count >= 100) return issues;
        }
        var refunds = entries.Values.Where(entry => entry.Kind == EntryKind.Refund && entry.RefundOfId is not null)
            .GroupBy(entry => entry.RefundOfId!.Value).ToDictionary(group => group.Key, group => group.Sum(entry => (decimal)entry.Amount));
        foreach (var entry in entries.Values)
        {
            if (issues.Count >= 100) break;
            LedgerEntry? original = null; long others = 0;
            if (entry.Kind == EntryKind.Refund && entry.RefundOfId is { } purchase)
            {
                if (!entries.TryGetValue(purchase, out original))
                { issues.Add(new(entryKeys[entry.Id], nameof(LedgerError.RefundOriginalMissing), ["RefundOfId"])); continue; }
                var otherTotal = refunds[purchase] - entry.Amount;
                if (otherTotal > long.MaxValue)
                { issues.Add(new(entryKeys[entry.Id], nameof(LedgerError.RefundExceedsPurchase), ["Amount"])); continue; }
                if (otherTotal < long.MinValue)
                { issues.Add(new(entryKeys[entry.Id], nameof(LedgerError.AmountMustBePositive), ["Amount"])); continue; }
                others = (long)otherTotal;
            }
            foreach (var error in LedgerValidator.Validate(entry, accounts, categories, original, others, newEntryIds?.Contains(entry.Id) == true))
                issues.Add(new(entryKeys[entry.Id], error.ToString(), []));
            // A changed purchase is checked against its final refunds too, matching the original whole-batch writer.
            if (refunds.TryGetValue(entry.Id, out var linked))
            {
                if (entry.Kind != EntryKind.Expense) issues.Add(new(entryKeys[entry.Id], nameof(LedgerError.RefundOriginalNotExpense), ["Kind"]));
                else if (linked > entry.Amount) issues.Add(new(entryKeys[entry.Id], nameof(LedgerError.RefundExceedsPurchase), ["Amount"]));
            }
            if (entry.Kind != EntryKind.Transfer && (entry.ToAccountId is not null || entry.ToAmount is not null))
                issues.Add(new(entryKeys[entry.Id], "UnexpectedTransferDestination", ["ToAccountId", "ToAmount"]));
        }
        return issues.Take(100).ToArray();
    }

    /// <summary>Original typed ledger projection reused after structural validation by the household cost validator.</summary>
    internal static LedgerEntry ProjectEntry(JsonElement row) => new(Id(row))
    {
        Kind = (EntryKind)Long(row, "Kind"), Date = Date(row, "Date"), AccountId = row.GetProperty("AccountId").GetGuid(),
        Amount = Long(row, "Amount"), ToAccountId = GuidOrNull(row, "ToAccountId"), ToAmount = LongOrNull(row, "ToAmount"),
        Direction = LongOrNull(row, "Direction") is { } direction ? (AdjustmentDirection)direction : null,
        CategoryId = GuidOrNull(row, "CategoryId"), Title = Text(row, "Title"), Payee = Text(row, "Payee"), Note = Text(row, "Note"),
        Review = (ReviewState)Long(row, "Review"), Source = (EntrySource)Long(row, "Source"),
        OriginalAmount = LongOrNull(row, "OriginalAmount"), OriginalCurrencyCode = Text(row, "OriginalCurrencyCode"),
        RefundOfId = GuidOrNull(row, "RefundOfId"), GroupId = GuidOrNull(row, "GroupId"),
        IsPartialPayment = Long(row, "IsPartialPayment") != 0, ReimbursableAmount = LongOrNull(row, "ReimbursableAmount"),
        ReimbursedBy = Text(row, "ReimbursedBy"), ReimbursementDueDate = DateOrNull(row, "ReimbursementDueDate"),
        IsAggregated = Long(row, "IsAggregated") != 0, AggregatedFrom = DateOrNull(row, "AggregatedFrom"), AggregatedTo = DateOrNull(row, "AggregatedTo"),
        ScheduleId = GuidOrNull(row, "ScheduleId"), OccurrenceDate = DateOrNull(row, "OccurrenceDate"), ImportBatchId = GuidOrNull(row, "ImportBatchId"),
    };

    private static Guid Id(JsonElement row) => row.GetProperty("Id").GetGuid();
    private static long Long(JsonElement row, string name) => row.GetProperty(name).GetInt64();
    private static long? LongOrNull(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : Long(row, name);
    private static string? Text(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : row.GetProperty(name).GetString();
    private static Guid? GuidOrNull(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : row.GetProperty(name).GetGuid();
    private static DateOnly Date(JsonElement row, string name) => DateOnly.ParseExact(row.GetProperty(name).GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DateOnly? DateOrNull(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : Date(row, name);
}
