using System.Text.Json;
using System.Text.Json.Nodes;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Explicit one-time source selection. Personal-source edits are not an implicit live household link.</summary>
public sealed record HouseholdProjectionSelection(IReadOnlyList<Guid> AccountIds, bool IncludeHistory, bool IncludeAttachments, string ExcludedCounterpartName)
{
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdProjectionSelection);
}

/// <summary>Only selected financial rows and indispensable dependencies, with fresh independent destination identities.</summary>
public sealed class HouseholdProjectedGraph
{
    internal HouseholdProjectedGraph(SyncRecord[] records, int selectedAccounts, int transfers, int detachedRefunds)
    { Records = Array.AsReadOnly(records); SelectedAccounts = selectedAccounts; PrivateTransfers = transfers; DetachedRefunds = detachedRefunds; }
    /// <summary>Complete immutable destination rows; no source settings, device keys, outboxes or membership authority.</summary>
    public IReadOnlyList<SyncRecord> Records { get; }
    /// <summary>Number of explicitly chosen source accounts, excluding neutral counterparts.</summary>
    public int SelectedAccounts { get; }
    /// <summary>Transfers whose private side was replaced by a neutral excluded counterpart in the selected side's exact units.</summary>
    public int PrivateTransfers { get; }
    /// <summary>Refunds retained without the unselected original purchase reference.</summary>
    public int DetachedRefunds { get; }
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdProjectedGraph);
}

/// <summary>Projects reviewed selected history without guessing private counterpart balances, currencies or financial relationships.</summary>
public static class HouseholdProjection
{
    /// <summary>Consumes only allowlisted source rows. Every actual destination row is checked by the original whole-graph financial validator.</summary>
    public static HouseholdProjectedGraph Create(HouseholdProjectionSelection selection, IReadOnlyCollection<SyncRecord> source,
        IReadOnlyDictionary<Guid, string>? privateTransferCurrencies = null)
    {
        ArgumentNullException.ThrowIfNull(selection); ArgumentNullException.ThrowIfNull(source);
        if (selection.AccountIds.Count > SyncGraphValidation.MaximumRows || selection.AccountIds.Any(id => id == Guid.Empty) ||
            selection.AccountIds.Distinct().Count() != selection.AccountIds.Count || selection.IncludeAttachments && !selection.IncludeHistory ||
            string.IsNullOrWhiteSpace(selection.ExcludedCounterpartName)) throw Refusal();
        var allowed = new HashSet<string>(["Accounts", "Entries", "Categories", "Attachments"], StringComparer.Ordinal);
        var schemas = SyncSchema.Manifest.Tables.Where(table => allowed.Contains(table.Name)).ToDictionary(table => table.Name, StringComparer.Ordinal);
        if (source.Count > SyncGraphValidation.MaximumRows) throw new SyncProfileBoundException();
        foreach (var item in source)
            if (!schemas.TryGetValue(item.Record.Table, out var table) || item.Values is not { } row ||
                SyncGraphValidation.ValidateRow(table, item.Record, row).Count != 0) throw Refusal();
        var grouped = source.GroupBy(item => item.Record.Table).ToDictionary(group => group.Key,
            group => group.ToDictionary(item => item.Values!.Value.GetProperty("Id").GetGuid()));
        Dictionary<Guid, SyncRecord> Rows(string table) => grouped.GetValueOrDefault(table) ?? [];
        var chosen = selection.AccountIds.ToHashSet(); var accounts = Rows("Accounts"); var entries = Rows("Entries");
        var categories = Rows("Categories"); var attachments = Rows("Attachments");
        if (!chosen.SetEquals(accounts.Keys) || !selection.IncludeHistory && (entries.Count != 0 || categories.Count != 0 || attachments.Count != 0) ||
            !selection.IncludeAttachments && attachments.Count != 0) throw Refusal();
        foreach (var row in entries.Values)
        {
            var value = row.Values!.Value;
            if (!chosen.Contains(value.GetProperty("AccountId").GetGuid()) &&
                !(OptionalGuid(value, "ToAccountId") is { } destination && chosen.Contains(destination))) throw Refusal();
        }
        var neededCategories = new HashSet<Guid>();
        foreach (var item in entries.Values)
            if (OptionalGuid(item.Values!.Value, "CategoryId") is { } category) AddCategory(category, []);
        if (!neededCategories.SetEquals(categories.Keys) || attachments.Values.Any(item => !entries.ContainsKey(item.Values!.Value.GetProperty("EntryId").GetGuid()))) throw Refusal();
        var accountIds = accounts.Keys.ToDictionary(id => id, _ => Guid.CreateVersion7());
        var entryIds = entries.Keys.ToDictionary(id => id, _ => Guid.CreateVersion7());
        var categoryIds = categories.Keys.ToDictionary(id => id, _ => Guid.CreateVersion7());
        var groups = entries.Values.Select(item => OptionalGuid(item.Values!.Value, "GroupId")).Where(id => id is not null)
            .GroupBy(id => id!.Value).Where(group => group.Count() > 1).ToDictionary(group => group.Key, _ => Guid.CreateVersion7());
        var neutral = new Dictionary<string, Guid>(StringComparer.Ordinal); var result = new List<SyncRecord>();
        foreach (var (id, item) in accounts) { var row = Node(item); row["Id"] = accountIds[id]; Add("Accounts", row); }
        foreach (var (id, item) in categories)
        {
            var row = Node(item); row["Id"] = categoryIds[id];
            row["ParentId"] = OptionalGuid(item.Values!.Value, "ParentId") is { } parent ? categoryIds[parent] : null; Add("Categories", row);
        }
        var transferCount = 0; var detachedRefunds = 0;
        foreach (var (id, item) in entries)
        {
            var original = item.Values!.Value; var row = Node(item); row["Id"] = entryIds[id];
            var sourceId = original.GetProperty("AccountId").GetGuid(); var destination = OptionalGuid(original, "ToAccountId");
            var selectedSource = chosen.Contains(sourceId); var selectedDestination = destination is { } target && chosen.Contains(target);
            if ((EntryKind)original.GetProperty("Kind").GetInt64() == EntryKind.Transfer)
            {
                if (destination is null || !selectedSource && !selectedDestination) throw Refusal();
                if (selectedSource && selectedDestination) { row["AccountId"] = accountIds[sourceId]; row["ToAccountId"] = accountIds[destination.Value]; }
                else
                {
                    transferCount++;
                    var selected = selectedSource ? sourceId : destination.Value;
                    var units = accounts[selected].Values!.Value.GetProperty("CurrencyCode").GetString()!;
                    var amount = original.GetProperty("Amount").GetInt64();
                    if (!selectedSource)
                    {
                        if (original.GetProperty("ToAmount").ValueKind != JsonValueKind.Null) amount = original.GetProperty("ToAmount").GetInt64();
                        else if (privateTransferCurrencies?.GetValueOrDefault(sourceId) != units) throw Refusal();
                    }
                    var counterpart = Counterpart(units, accounts[selected]);
                    row["AccountId"] = selectedSource ? accountIds[selected] : counterpart;
                    row["ToAccountId"] = selectedSource ? counterpart : accountIds[selected];
                    row["Amount"] = amount; row["ToAmount"] = amount;
                    // A private FX side is deliberately absent. Equal units on the neutral excluded
                    // endpoint preserve only the selected account's exact movement/classification.
                    row["OriginalAmount"] = null; row["OriginalCurrencyCode"] = null;
                }
            }
            else
            {
                if (!selectedSource) throw Refusal(); row["AccountId"] = accountIds[sourceId];
            }
            row["CategoryId"] = OptionalGuid(original, "CategoryId") is { } category ? categoryIds[category] : null;
            var refund = OptionalGuid(original, "RefundOfId");
            row["RefundOfId"] = refund is { } purchase && entryIds.TryGetValue(purchase, out var copiedPurchase) ? copiedPurchase : null;
            if (refund is not null && row["RefundOfId"] is null) detachedRefunds++;
            row["GroupId"] = OptionalGuid(original, "GroupId") is { } group && groups.TryGetValue(group, out var copiedGroup) ? copiedGroup : null;
            // A selected cash history is independent of source plans/import journals. It cannot
            // settle a private occurrence or revive an Undo batch on another device.
            row["ScheduleId"] = null; row["OccurrenceDate"] = null; row["IsPartialPayment"] = 0; row["ImportBatchId"] = null;
            Add("Entries", row);
        }
        foreach (var item in attachments.Values)
        { var row = Node(item); row["Id"] = Guid.CreateVersion7(); row["EntryId"] = entryIds[item.Values!.Value.GetProperty("EntryId").GetGuid()]; Add("Attachments", row); }
        var issues = SyncFinancialValidation.Validate([], result);
        if (issues.Count != 0) throw new InvalidDataException("Household projection graph refusal: " + string.Join(',', issues.Select(issue => issue.Record.Table + ":" + issue.Reason + ":" + string.Join('/', issue.Fields)).Distinct().Take(8)));
        return new(result.ToArray(), accounts.Count, transferCount, detachedRefunds);

        void AddCategory(Guid id, HashSet<Guid> ancestry)
        {
            if (!ancestry.Add(id) || !categories.TryGetValue(id, out var item)) throw Refusal();
            if (neededCategories.Add(id) && OptionalGuid(item.Values!.Value, "ParentId") is { } parent) AddCategory(parent, ancestry);
        }
        Guid Counterpart(string currency, SyncRecord template)
        {
            if (neutral.TryGetValue(currency, out var existing)) return existing;
            var id = Guid.CreateVersion7(); neutral.Add(currency, id); var row = Node(template);
            row["Id"] = id; row["Name"] = selection.ExcludedCounterpartName; row["CurrencyCode"] = currency;
            row["OpeningBalance"] = 0; row["OpeningBalanceKnown"] = 0; row["Type"] = 0;
            row["IncludeInTotals"] = 0; row["UsableForPayments"] = 0; row["IsArchived"] = 0; row["SortOrder"] = int.MaxValue;
            foreach (var name in new[] { "Counterparty", "CountryCode", "DueDate", "Icon", "Installment", "InterestRate", "LastReconciledOn" }) row[name] = null;
            Add("Accounts", row); return id;
        }
        void Add(string table, JsonObject row)
        {
            // The actual SQLite/EF financial contract uses uppercase Guid cells/keys. JsonNode's
            // default Guid representation is lowercase and must not create unqueryable copies.
            foreach (var column in schemas[table].Columns.Where(column => column.Kind == "guid"))
                if (row[column.Name] is { } cell) row[column.Name] = cell.GetValue<Guid>().ToString("D").ToUpperInvariant();
            using var bytes = new MemoryStream();
            using (var writer = new Utf8JsonWriter(bytes)) row.WriteTo(writer);
            using var document = JsonDocument.Parse(bytes.ToArray()); var value = document.RootElement.Clone();
            result.Add(new(new(table, SyncGraphValidation.KeyOf(value, schemas[table].PrimaryKey)), 0, value));
        }
    }
    private static JsonObject Node(SyncRecord item) => JsonNode.Parse(item.Values!.Value.GetRawText())!.AsObject();
    private static Guid? OptionalGuid(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : row.GetProperty(name).GetGuid();
    private static InvalidDataException Refusal() => new("The explicit household account projection is invalid.");
}
