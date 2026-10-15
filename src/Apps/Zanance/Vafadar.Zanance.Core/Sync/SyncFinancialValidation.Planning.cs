using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Sync;

public static partial class SyncFinancialValidation
{
    /// <summary>Checks units and complete planning relationships that SQLite scalar constraints cannot express.</summary>
    private static IReadOnlyList<SyncGraphIssue> ValidatePlanning(SyncRecord[] live)
    {
        var issues = new List<SyncGraphIssue>();
        var accounts = Rows("Accounts").ToDictionary(Id);
        var goals = Rows("Goals").ToDictionary(Id);
        var settings = Rows("Settings").FirstOrDefault();
        var monthStart = settings is null ? 1 : checked((int)Number(settings.Values!.Value, "MonthStartDay"));
        var currencyColumns = SyncSchema.Manifest.Tables.ToDictionary(table => table.Name,
            table => table.Columns.Where(column => column.Name.EndsWith("CurrencyCode", StringComparison.Ordinal) ||
                column.Name == "EssentialEstimateCurrency").Select(column => column.Name).ToArray());
        var balanceOwners = new Dictionary<Guid, SyncRecordKey>();
        foreach (var record in live)
        {
            if (issues.Count >= 100) break;
            var row = record.Values!.Value;
            foreach (var column in currencyColumns[record.Record.Table])
                if (Text(row, column) is { } code && !Currencies.TryGet(code, out _))
                    Add(record, "InvalidCurrency", [column]);
            switch (record.Record.Table)
            {
                case "Goals":
                    if (string.IsNullOrWhiteSpace(Text(row, "Name"))) Add(record, "GoalNameRequired", ["Name"]);
                    if (Number(row, "TargetAmount") <= 0) Add(record, "GoalTargetMustBePositive", ["TargetAmount"]);
                    var kind = (GoalType)Number(row, "Type");
                    if (kind == GoalType.AccountBalance)
                    {
                        if (OptionalGuid(row, "AccountId") is not { } accountId || !accounts.TryGetValue(accountId, out var account))
                            Add(record, "GoalAccountRequired", ["AccountId"]);
                        else
                        {
                            if (!SameCurrency(row, "CurrencyCode", account.Values!.Value, "CurrencyCode"))
                                Add(record, "GoalAccountCurrencyMismatch", ["CurrencyCode", "AccountId"], account.Record);
                            if ((GoalState)Number(row, "State") is GoalState.Active or GoalState.Paused &&
                                !balanceOwners.TryAdd(accountId, record.Record))
                                Add(record, "AccountAlreadyHasBalanceGoal", ["AccountId", "State"], balanceOwners[accountId]);
                        }
                    }
                    // An archived/deleted holding type can remain referenced by retained goal history.
                    if (kind == GoalType.HoldingQuantity && OptionalGuid(row, "AssetTypeId") is not { })
                        Add(record, "GoalHoldingRequired", ["AssetTypeId"]);
                    if (kind != GoalType.Earmark && Number(row, "Protect") != 0)
                        Add(record, "OnlyEarmarksCanBeProtected", ["Type", "Protect"]);
                    break;
                case "GoalAllocations":
                    // Negative amounts are intentional releases, not malformed contributions.
                    if (Number(row, "Amount") == 0) Add(record, "GoalAllocationMustBeNonzero", ["Amount"]);
                    if (goals.TryGetValue(row.GetProperty("GoalId").GetGuid(), out var goal) &&
                        accounts.TryGetValue(row.GetProperty("AccountId").GetGuid(), out var funding) &&
                        !SameCurrency(goal.Values!.Value, "CurrencyCode", funding.Values!.Value, "CurrencyCode"))
                        Add(record, "GoalAllocationCurrencyMismatch", ["GoalId", "AccountId"], goal.Record);
                    break;
                case "ContributionPlans":
                    if (OptionalNumber(row, "Amount") is <= 0) Add(record, "ContributionMustBePositive", ["Amount"]);
                    if (OptionalNumber(row, "AssumedPricePerUnitMilli") is <= 0) Add(record, "AssumedPriceMustBePositive", ["AssumedPricePerUnitMilli"]);
                    if (Text(row, "Percent") is { } percent && decimal.Parse(percent, NumberStyles.Float, CultureInfo.InvariantCulture) is <= 0 or > 100)
                        Add(record, "ContributionPercentOutOfRange", ["Percent"]);
                    break;
                case "Budgets":
                    if (OptionalNumber(row, "TotalLimit") is < 0) Add(record, "BudgetLimitMustBeNonnegative", ["TotalLimit"]);
                    try
                    {
                        // Reuse the original calendar boundaries, including financial months and week overflow.
                        _ = Budgets.BudgetPeriods.Range(new Budgets.Budget
                        {
                            CurrencyCode = Text(row, "CurrencyCode")!,
                            Period = (Budgets.BudgetPeriod)Number(row, "Period"),
                            PeriodStart = Date(row, "PeriodStart"), Year = checked((int)Number(row, "Year")),
                            Month = checked((int)Number(row, "Month")), Calendar = (Budgets.PeriodCalendar)Number(row, "Calendar"),
                        }, monthStart);
                    }
                    catch (Exception error) when (error is ArgumentException or OverflowException)
                    {
                        Add(record, "InvalidBudgetPeriod", ["Period", "PeriodStart", "Year", "Month", "Calendar"]);
                    }
                    break;
                case "BudgetCategoryLimits":
                    if (Number(row, "Limit") < 0) Add(record, "BudgetLimitMustBeNonnegative", ["Limit"]);
                    break;
                case "Settings":
                    if (Number(row, "MonthStartDay") is < 1 or > Budgets.PeriodMath.MaxStartDay)
                        Add(record, "InvalidFinancialMonthStart", ["MonthStartDay"]);
                    if (OptionalNumber(row, "EssentialEstimate") is < 0)
                        Add(record, "EssentialEstimateMustBeNonnegative", ["EssentialEstimate"]);
                    break;
                case "ExchangeRates":
                    if (decimal.Parse(Text(row, "Rate")!, NumberStyles.Float, CultureInfo.InvariantCulture) <= 0)
                        Add(record, "ExchangeRateMustBePositive", ["Rate"]);
                    break;
                case "AssetValuations":
                    // The original total-value converter can round a tiny positive unit price to zero.
                    if (Number(row, "PricePerUnitMilli") < 0) Add(record, "HoldingPriceMustBeNonnegative", ["PricePerUnitMilli"]);
                    break;
            }
        }
        return issues;

        IEnumerable<SyncRecord> Rows(string table) => live.Where(record => record.Record.Table == table);
        void Add(SyncRecord record, string reason, string[] fields, SyncRecordKey? related = null)
        {
            if (issues.Count < 100) issues.Add(new(record.Record, reason, fields, related));
        }
    }

    private static Guid Id(SyncRecord record) => record.Values!.Value.GetProperty("Id").GetGuid();
    private static long? OptionalNumber(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : Number(row, name);
    private static bool SameCurrency(JsonElement left, string leftName, JsonElement right, string rightName) =>
        string.Equals(Text(left, leftName), Text(right, rightName), StringComparison.OrdinalIgnoreCase);
}
