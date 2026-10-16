using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Reuses original financial rules over a complete proposed graph and its actual prior state.</summary>
public static partial class SyncFinancialValidation
{
    /// <summary>Checks ledger/recurrence/holding history and prevents a currency edit from relabeling retained amounts.</summary>
    public static IReadOnlyList<SyncGraphIssue> Validate(IReadOnlyCollection<SyncRecord> before,
        IReadOnlyCollection<SyncRecord> after, IReadOnlySet<Guid>? newEntryIds = null, SyncSchemaManifest? contract = null)
    {
        var issues = SyncLedgerValidation.Validate(after, newEntryIds, contract).ToList();
        if (issues.Count != 0) return issues;
        var live = after.Where(row => row.Values is not null).ToArray();
        var previous = before.ToDictionary(row => row.Record);
        issues.AddRange(ValidatePlanning(live, contract));
        foreach (var record in live.Where(row => row.Record.Table is "Schedules" or "ContributionPlans"))
        {
            var row = record.Values!.Value;
            var rule = new RecurrenceRule
            {
                Frequency = (Frequency)Number(row, "Frequency"), Interval = checked((int)Number(row, "Interval")), Start = Date(row, "Start"),
                Calendar = (PeriodCalendar)Number(row, "Calendar"), DayRule = (MonthDayRule)Number(row, "DayRule"),
                MissingDay = (MissingDayPolicy)Number(row, "MissingDay"), End = (EndKind)Number(row, "EndKind"),
                EndDate = OptionalDate(row, "EndDate"), Count = OptionalInteger(row, "Count"), SecondDay = OptionalInteger(row, "SecondDay"),
                WeekendDays = checked((int)Number(row, "WeekendDays")), WeekendShift = (WeekendShift)Number(row, "WeekendShift"),
                HolidayRegion = Text(row, "HolidayRegion"),
            };
            if (Recurrence.Validate(rule) is { } error) issues.Add(new(record.Record, error, ["Interval", "SecondDay", "EndKind", "EndDate", "Count"]));
            if ((rule.WeekendDays & ~0x7f) != 0) issues.Add(new(record.Record, "InvalidWeekendMask", ["WeekendDays"]));
        }
        var events = live.Where(row => row.Record.Table == "AssetEvents").Select(record => (Record: record.Record, Event: Event(record.Values!.Value))).ToArray();
        foreach (var (key, item) in events)
        {
            if (item.Quantity <= 0) issues.Add(new(key, "QuantityMustBePositive", ["Quantity"]));
            if (item.Kind == AssetEventKind.LocationTransfer && (item.ToLocationId is null || item.ToLocationId == item.LocationId))
                issues.Add(new(key, "InvalidHoldingTransfer", ["LocationId", "ToLocationId"]));
        }
        if (issues.Count == 0)
        {
            try
            {
                if (HoldingsLedger.FindConflict(events.Select(item => item.Event)) is { } conflict)
                {
                    var cause = ChangedHolding(conflict.AssetTypeId) ?? events.First(item => item.Event.AssetTypeId == conflict.AssetTypeId).Record;
                    issues.Add(new(cause, "HoldingHistoryWouldBeNegative", ["Quantity", "Date"]));
                }
            }
            catch (OverflowException)
            {
                var cause = ChangedHolding(null) ?? events[0].Record;
                issues.Add(new(cause, "HoldingQuantityOverflow", ["Quantity"]));
            }
        }
        var grouped = live.GroupBy(row => row.Record.Table).ToDictionary(group => group.Key, group => group.ToArray());
        foreach (var account in live.Where(row => row.Record.Table == "Accounts"))
        {
            if (previous.GetValueOrDefault(account.Record)?.Values is not { } original ||
                string.Equals(Text(original, "CurrencyCode"), Text(account.Values!.Value, "CurrencyCode"), StringComparison.OrdinalIgnoreCase)) continue;
            var id = account.Values!.Value.GetProperty("Id").GetGuid();
            int CountReferences(string table, params string[] columns) => grouped.TryGetValue(table, out var rows)
                ? rows.Count(record => columns.Any(column => OptionalGuid(record.Values!.Value, column) == id)) : 0;
            var budgets = grouped.TryGetValue("Budgets", out var budgetRows) ? budgetRows.Count(record => IncludesGuid(record.Values!.Value, "AccountIds", id)) : 0;
            var locked = new AccountCurrencyLock(CountReferences("Entries", "AccountId", "ToAccountId"),
                CountReferences("Schedules", "AccountId", "ToAccountId"), CountReferences("Templates", "AccountId", "ToAccountId"),
                CountReferences("GoalAllocations", "AccountId"), budgets, account.Values!.Value.GetProperty("Installment").ValueKind != JsonValueKind.Null);
            if (locked.IsLocked) issues.Add(new(account.Record, "AccountCurrencyLocked", ["CurrencyCode"]));
        }
        if (contract?.Tables.Any(table => table.Name == "HouseholdCosts") == true)
            issues.AddRange(ValidateHousehold(before, after, newEntryIds is not null));
        return issues.Take(100).ToArray();

        SyncRecordKey? ChangedHolding(Guid? type)
        {
            foreach (var row in after.Where(row => row.Record.Table == "AssetEvents"))
            {
                var old = previous.GetValueOrDefault(row.Record)?.Values;
                var sample = row.Values ?? old;
                if (sample is null || type is { } expected && sample.Value.GetProperty("AssetTypeId").GetGuid() != expected) continue;
                if (old is null ? row.Values is not null : row.Values is null || !JsonElement.DeepEquals(old.Value, row.Values.Value)) return row.Record;
            }
            return null;
        }
    }

    private static AssetEvent Event(JsonElement row) => new(row.GetProperty("Id").GetGuid())
    {
        AssetTypeId = row.GetProperty("AssetTypeId").GetGuid(), LocationId = row.GetProperty("LocationId").GetGuid(),
        ToLocationId = OptionalGuid(row, "ToLocationId"), Kind = (AssetEventKind)Number(row, "Kind"), Date = Date(row, "Date"),
        Quantity = Number(row, "Quantity"), IsIncrease = Number(row, "IsIncrease") != 0,
    };
    private static long Number(JsonElement row, string name) => row.GetProperty(name).GetInt64();
    private static string? Text(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : row.GetProperty(name).GetString();
    private static Guid? OptionalGuid(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : row.GetProperty(name).GetGuid();
    private static int? OptionalInteger(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : checked((int)Number(row, name));
    private static DateOnly Date(JsonElement row, string name) => DateOnly.ParseExact(row.GetProperty(name).GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DateOnly? OptionalDate(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : Date(row, name);
    private static bool IncludesGuid(JsonElement row, string name, Guid id)
    {
        using var list = JsonDocument.Parse(row.GetProperty(name).GetString()!);
        return list.RootElement.EnumerateArray().Any(item => item.GetGuid() == id);
    }
}
