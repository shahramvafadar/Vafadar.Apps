using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Core.Sync;

public static partial class SyncFinancialValidation
{
    /// <summary>Projects only already structurally validated household scalars into the original exact cost rules.</summary>
    private static IReadOnlyList<SyncGraphIssue> ValidateHousehold(IReadOnlyCollection<SyncRecord> before,
        IReadOnlyCollection<SyncRecord> after, bool creating)
    {
        var live = after.Where(row => row.Values is not null).ToArray();
        IEnumerable<JsonElement> Rows(string table) => live.Where(row => row.Record.Table == table).Select(row => row.Values!.Value);
        var contacts = Rows("HouseholdContacts").Select(row => new HouseholdContact(row.GetProperty("Id").GetGuid())
        { Name = Text(row, "Name")!, IdentityId = Text(row, "IdentityId"), IsArchived = Number(row, "IsArchived") != 0 }).ToArray();
        var costs = Rows("HouseholdCosts").Select(row => new HouseholdCost(row.GetProperty("Id").GetGuid())
        {
            Kind = (HouseholdCostKind)Number(row, "Kind"), Date = Date(row, "Date"), Amount = Number(row, "Amount"),
            CurrencyCode = Text(row, "CurrencyCode")!, PaidByContactId = OptionalGuid(row, "PaidByContactId"),
            PaymentKind = (HouseholdPaymentKind)Number(row, "PaymentKind"), EntryId = OptionalGuid(row, "EntryId"),
            RefundOfCostId = OptionalGuid(row, "RefundOfCostId"), Title = Text(row, "Title")!, Note = Text(row, "Note"),
        }).ToArray();
        var allocations = Rows("HouseholdAllocations").Select(row => new HouseholdCostAllocation
        { CostId = row.GetProperty("CostId").GetGuid(), ContactId = row.GetProperty("ContactId").GetGuid(), Amount = Number(row, "Amount") }).ToArray();
        var settlements = Rows("HouseholdSettlements").Select(row => new HouseholdSettlement(row.GetProperty("Id").GetGuid())
        {
            FromContactId = row.GetProperty("FromContactId").GetGuid(), ToContactId = row.GetProperty("ToContactId").GetGuid(),
            Date = Date(row, "Date"), Amount = Number(row, "Amount"), CurrencyCode = Text(row, "CurrencyCode")!,
            EntryId = OptionalGuid(row, "EntryId"), UsesDestinationAmount = Number(row, "UsesDestinationAmount") != 0, Note = Text(row, "Note"),
        }).ToArray();
        var entries = Rows("Entries").Select(SyncLedgerValidation.ProjectEntry).ToDictionary(row => row.Id);
        var accounts = Rows("Accounts").Select(row => new Account(row.GetProperty("Id").GetGuid())
        { Name = Text(row, "Name")!, CurrencyCode = Text(row, "CurrencyCode")! }).ToDictionary(row => row.Id);
        var existingCosts = before.Where(row => row.Record.Table == "HouseholdCosts" && row.Values is not null).Select(row => row.Values!.Value.GetProperty("Id").GetGuid()).ToHashSet();
        var existingSettlements = before.Where(row => row.Record.Table == "HouseholdSettlements" && row.Values is not null).Select(row => row.Values!.Value.GetProperty("Id").GetGuid()).ToHashSet();
        var existingAllocations = before.Where(row => row.Record.Table == "HouseholdAllocations" && row.Values is not null)
            .Select(row => (row.Values!.Value.GetProperty("CostId").GetGuid(), row.Values!.Value.GetProperty("ContactId").GetGuid())).ToHashSet();
        return HouseholdCostLedger.Validate(contacts, costs, allocations, settlements, entries, accounts,
            creating ? costs.Where(row => !existingCosts.Contains(row.Id)).Select(row => row.Id).ToHashSet() : null,
            creating ? settlements.Where(row => !existingSettlements.Contains(row.Id)).Select(row => row.Id).ToHashSet() : null,
            creating ? allocations.Where(row => !existingAllocations.Contains((row.CostId, row.ContactId))).Select(row => (row.CostId, row.ContactId)).ToHashSet() : null)
            .Select(issue => new SyncGraphIssue(new(issue.Table, JsonSerializer.Serialize(new[] { issue.RecordId.ToString("D").ToUpperInvariant() })), issue.Code, []))
            .ToArray();
    }
}
