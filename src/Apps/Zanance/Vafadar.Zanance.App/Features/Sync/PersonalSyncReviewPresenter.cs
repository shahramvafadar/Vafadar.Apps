using System.Globalization;
using System.Text.Json;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>One complete human-readable value, retaining monetary direction marks and the chosen date calendar.</summary>
public sealed record PersonalSyncReviewField(string Caption, string Value);
/// <summary>A submitted, remote or related immutable record; missing records are distinguished from deleted ones.</summary>
public sealed record PersonalSyncReviewVersion(string Heading, string State, IReadOnlyList<PersonalSyncReviewField> Fields);
/// <summary>A version-bound native choice; incomplete/ambiguous presentation cannot authorize a financial decision.</summary>
public sealed record PersonalSyncConflictItem(PersonalSyncConflictReview Review, string Title, string Reason,
    IReadOnlyList<PersonalSyncReviewVersion> Versions, bool CanChooseLocal, bool CanChooseRemote, string? Hint, string RemoteAction, string? LocalAction = null)
{
    /// <summary>Gets whether content must be previewed in addition to its metadata.</summary>
    public bool IsAttachment => Review.Conflict.Proposal.Record.Table == "Attachments";
    /// <summary>Gets whether a submitted file exists to preview.</summary>
    public bool CanPreviewLocal => IsAttachment && Review.Conflict.Proposal.Values is not null;
    /// <summary>Gets whether a service file exists to preview.</summary>
    public bool CanPreviewRemote => IsAttachment && Review.Conflict.Current?.Values is not null;
}

/// <summary>Formats original typed review values without raw JSON, internal identifiers or guessed currency units.</summary>
public sealed partial class PersonalSyncReviewPresenter(Translator translator, IDateFormatter dates, ILocalizationService localization)
{
    private static readonly IReadOnlyDictionary<string, string> Captions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Name"] = "Account_Name", ["Title"] = "Entry_Title", ["Date"] = "Entry_Date", ["Note"] = "Entry_Note",
        ["AccountId"] = "Entry_Account", ["ToAccountId"] = "Entry_ToAccount", ["CategoryId"] = "Entry_Category",
        ["Amount"] = "Entry_Amount", ["OriginalAmount"] = "Entry_ForeignAmount", ["ToAmount"] = "Sync_Field_ToAmount",
        ["Payee"] = "Entry_Payee", ["Tags"] = "Entry_Tags", ["OriginalCurrencyCode"] = "Sync_Field_OriginalCurrency",
        ["RefundOfId"] = "Sync_Field_OriginalPurchase", ["ReimbursableAmount"] = "Entry_ReimbursableAmount",
        ["ReimbursedBy"] = "Entry_ReimbursedBy", ["ReimbursementDueDate"] = "Sync_Field_RepaymentDate",
        ["AggregatedFrom"] = "Entry_AggregatedFrom", ["AggregatedTo"] = "Entry_AggregatedTo",
        ["IsAggregated"] = "Sync_Field_Aggregated", ["IsPartialPayment"] = "Sync_Field_Partial",
        ["ScheduleId"] = "Sync_Field_Plan", ["OccurrenceDate"] = "Sync_Field_OriginalDate",
        ["OriginalDate"] = "Sync_Field_OriginalDate", ["Review"] = "Sync_Field_Review", ["Source"] = "Sync_Field_Source",
        ["Direction"] = "Sync_Field_Direction", ["Kind"] = "Entry_Kind", ["Type"] = "Account_Type",
        ["CurrencyCode"] = "Account_Currency", ["CountryCode"] = "Account_Country", ["Counterparty"] = "Account_Counterparty",
        ["OpeningBalance"] = "Account_OpeningBalance", ["OpeningBalanceKnown"] = "Sync_Field_KnownBalance",
        ["OpeningDate"] = "Account_OpeningDate", ["IncludeInTotals"] = "Account_IncludeInTotals",
        ["UsableForPayments"] = "Account_Usable", ["IsArchived"] = "Sync_Field_Archived",
        ["DueDate"] = "Sync_Field_DueDate", ["LastReconciledOn"] = "Sync_Field_Reconciled",
        ["Installment"] = "Account_Installment", ["InterestRate"] = "Account_InterestRate", ["SortOrder"] = "Sync_Field_Order",
        ["Status"] = "Sync_Field_Status", ["EntryId"] = "Sync_Field_Settlement", ["PaidAmount"] = "Sync_Field_PaidAmount",
        ["AutoPostSuppressed"] = "Sync_Field_AutoSuppressed",
        ["AccountIds"] = "Budget_Accounts", ["BudgetId"] = "Budget_Title", ["TotalLimit"] = "Budget_TotalLimit",
        ["Limit"] = "Sync_Field_Limit", ["Calendar"] = "Plan_Calendar", ["Period"] = "Sync_Field_Period",
        ["PeriodStart"] = "Sync_Field_PeriodStart", ["Year"] = "Sync_Field_Year", ["Month"] = "Sync_Field_Month",
        ["Method"] = "Budget_Method", ["Rollover"] = "Rollover_Title", ["AlertsEnabled"] = "Budget_Alerts",
        ["AmountMode"] = "Plan_AmountMode", ["ActiveFrom"] = "Plan_ApplyFrom", ["ActiveUntil"] = "Sync_Field_ActiveUntil",
        ["PausedFrom"] = "Sync_Field_PausedFrom", ["PreviousScheduleId"] = "Sync_Field_PreviousPlan", ["State"] = "Sync_Field_Status",
        ["AutoPost"] = "Plan_AutoPost", ["AutoPostFrom"] = "Sync_Field_AutoPostFrom", ["Start"] = "Plan_Start",
        ["MissingDay"] = "Plan_MissingDay", ["ReminderEnabled"] = "Plan_Reminder",
        ["ReminderDaysBefore"] = "Plan_DaysBefore", ["ReminderTime"] = "Plan_ReminderTime", ["ReminderOnDueDate"] = "Plan_ReminderOnDueDate",
        ["ContractProvider"] = "Contract_Provider", ["ContractReference"] = "Contract_Reference", ["ContractEnd"] = "Contract_End",
        ["ContractRenews"] = "Contract_Renews", ["CancellationDeadline"] = "Contract_Deadline", ["ReviewDate"] = "Contract_Review",
        ["WeekendDays"] = "Sync_Field_WeekendDays", ["WeekendShift"] = "Weekend_Title", ["HolidayRegion"] = "Sync_Field_HolidayRegion",
        ["Frequency"] = "Plan_Repeat", ["Interval"] = "Plan_Every", ["DayRule"] = "Plan_Day", ["SecondDay"] = "Plan_SecondDayNumber",
        ["EndKind"] = "Plan_End", ["EndDate"] = "Sync_Field_EndDate", ["Count"] = "Plan_Times",
        ["AssetTypeId"] = "AssetType_Name", ["LocationId"] = "AssetEvent_Location", ["ToLocationId"] = "AssetEvent_To",
        ["Quantity"] = "AssetEvent_Quantity", ["BasisAmount"] = "Sync_Field_Basis", ["ProceedsAmount"] = "AssetEvent_Received",
        ["IsIncrease"] = "AssetEvent_Increase", ["Reason"] = "AssetEvent_Reason", ["Dimension"] = "AssetType_MeasuredBy",
        ["Metal"] = "AssetType_Metal", ["PurityPer10000"] = "AssetType_Purity", ["UnitWeightMg"] = "AssetType_UnitWeight",
        ["CountUnitName"] = "AssetType_CountUnit", ["Divisible"] = "AssetType_Divisible", ["PriceCurrencyCode"] = "AssetType_PriceCurrency",
        ["PricePerUnitMilli"] = "Holding_Prices",
        ["GoalId"] = "Goal_Details", ["TargetAmount"] = "Goal_Target", ["TargetDate"] = "Goal_HasDate",
        ["Priority"] = "Goal_Priority", ["Protect"] = "Goal_Protect", ["HomePin"] = "Sync_Field_HomePosition",
        ["PausedAt"] = "Sync_Field_PausedAt", ["CompletedAt"] = "Sync_Field_CompletedAt",
        ["Percent"] = "Goal_Percent", ["CategoryIds"] = "Goal_CutCategory", ["AssumedPricePerUnitMilli"] = "Sync_Field_AssumedPrice",
        ["ParentId"] = "Category_Parent", ["SystemKey"] = "Sync_Field_DefaultCategory", ["SpendingType"] = "SpendingType_Title",
        ["IsEssential"] = "Sync_Field_Essential", ["Match"] = "Rule_Match", ["Color"] = "Category_Color", ["Icon"] = "Entry_Icon",
        ["CategoryName"] = "Category_Name", ["From"] = "Sync_Field_PeriodStart", ["To"] = "Sync_Field_EndDate", ["Search"] = "Tx_Search",
        ["InTotalsOnly"] = "Report_ScopeInTotals", ["UnreviewedOnly"] = "Tx_Unreviewed", ["IsEstimate"] = "Rates_Estimate",
        ["FromCurrencyCode"] = "Rates_From", ["ToCurrencyCode"] = "Rates_To", ["Rate"] = "Rates_Rate",
        ["BudgetCalendar"] = "Plan_Calendar", ["DefaultAccountId"] = "Settings_DefaultAccount", ["DefaultCurrencyCode"] = "Settings_DefaultCurrency",
        ["DisplayUnits"] = "Unit_Title", ["HomeLayout"] = "HomeLayout_Title", ["ReportCurrencyCode"] = "Settings_ReportCurrency",
        ["ValuationCurrencyEnabled"] = "Settings_ShowConverted", ["HomeBudgetCurrencyCode"] = "Sync_Field_HomeBudgetCurrency",
        ["MonthStartDay"] = "Settings_MonthStartDay", ["RateFreshnessDays"] = "Settings_RateFreshness", ["Mode"] = "Settings_Experience",
        ["FortnightStart"] = "Sync_Field_FortnightStart", ["ReviewProgress"] = "Review_Progress", ["EssentialEstimate"] = "Settings_Essential",
        ["EssentialEstimateCurrency"] = "Settings_EssentialCurrency", ["EssentialEstimatePeriod"] = "Sync_Field_EstimatePeriod",
        ["BaseDate"] = "Sync_Field_BaseDate", ["Horizon"] = "Sync_Field_EndDate", ["Minimum"] = "Sync_Field_Minimum",
        ["MinimumDate"] = "Sync_Field_MinimumDate", ["UnknownCount"] = "Sync_Field_UnknownCount", ["Assumptions"] = "Snapshot_Assumptions",
        ["AppVersion"] = "Sync_Field_AppVersion", ["Path"] = "Forecast_Path",
        ["FileName"] = "Sync_AttachmentFileName", ["ContentType"] = "Sync_AttachmentType", ["Data"] = "Sync_AttachmentContent",
        ["IdentityId"] = "HouseholdCost_ContactIdentity", ["PaymentKind"] = "HouseholdCost_PaymentSource",
        ["PaidByContactId"] = "HouseholdCost_Payer", ["RefundOfCostId"] = "HouseholdCost_RefundOriginal",
        ["CostId"] = "HouseholdCost_Title", ["ContactId"] = "HouseholdCost_Contacts",
        ["FromContactId"] = "HouseholdCost_Payer", ["ToContactId"] = "HouseholdCost_Recipient",
        ["UsesDestinationAmount"] = "HouseholdCost_Destination",
    };
    private static readonly HashSet<string> MoneyFields = new(StringComparer.Ordinal)
    { "Entries.Amount", "Entries.ToAmount", "Entries.OriginalAmount", "Entries.ReimbursableAmount", "Accounts.OpeningBalance",
        "Accounts.Installment", "OccurrenceStates.Amount", "OccurrenceStates.PaidAmount", "Budgets.TotalLimit", "BudgetCategoryLimits.Limit",
        "Schedules.Amount", "Schedules.ToAmount", "AssetEvents.BasisAmount", "AssetEvents.ProceedsAmount", "GoalAllocations.Amount",
        "Templates.Amount", "ForecastSnapshots.Minimum", "Settings.EssentialEstimate", "HouseholdCosts.Amount", "HouseholdAllocations.Amount", "HouseholdSettlements.Amount" };
    private static readonly HashSet<string> Booleans = new(StringComparer.Ordinal)
    { "IsAggregated", "IsPartialPayment", "IncludeInTotals", "UsableForPayments", "IsArchived", "OpeningBalanceKnown", "AutoPostSuppressed", "AlertsEnabled",
        "AutoPost", "ReminderEnabled", "ReminderOnDueDate", "ContractRenews", "IsIncrease", "Divisible", "Protect", "IsEssential",
        "InTotalsOnly", "UnreviewedOnly", "IsEstimate", "ValuationCurrencyEnabled", "UsesDestinationAmount" };

    /// <summary>Builds only from immutable reviewed payloads and actual prepared local relationship snapshots.</summary>
    public PersonalSyncConflictItem Create(PersonalSyncConflictReview review, IReadOnlyList<SyncRecord> remoteRecords)
    {
        if (review.Conflict.LinkedGroup is { } group)
        {
            var members = new[] { review }.Concat(review.GroupMembers ?? []).ToArray();
            var items = members.Select(member => CreateSingle(member, remoteRecords)).ToArray();
            var keys = members.Select(member => member.Conflict.Proposal.Record).ToHashSet();
            var unknownRemote = members.SelectMany(member => member.Conflict.Context ?? []).Any(record =>
                SyncConflictGrouping.InGroup(record.Record, record.Values, review.Conflict.Proposal.Record, group) && !keys.Contains(record.Record));
            var versions = items.SelectMany(item => item.Versions.Select(version => version with { Heading = item.Title + " · " + version.Heading })).ToList();
            // A remote-only member is displayed, but cannot be silently retained as part of a local-version choice.
            foreach (var record in members.SelectMany(member => member.Conflict.Context ?? []).Where(record =>
                SyncConflictGrouping.InGroup(record.Record, record.Values, review.Conflict.Proposal.Record, group) && !keys.Contains(record.Record)).DistinctBy(record => record.Record))
            {
                var context = members.SelectMany(member => member.Conflict.Context ?? []).DistinctBy(row => row.Record).ToArray();
                var snapshot = review with { Conflict = new(new(record.Record, record.Version, record.Values), record, record, [], "LinkedGroupReview", Context: context, LinkedGroup: group),
                    GroupMembers = null, LocalContext = context };
                var item = CreateSingle(snapshot, remoteRecords);
                versions.AddRange(item.Versions.Skip(1).Select(version => version with { Heading = item.Title + " · " + version.Heading }));
                if (!item.CanChooseRemote) return new(review, translator["Sync_LinkedGroup"], translator["Sync_GroupReview"], versions,
                    false, false, translator["Sync_ReviewIncomplete"], translator["Sync_KeepRemoteGroup"], translator["Sync_KeepLocalGroup"]);
            }
            return new(review, translator["Sync_LinkedGroup"], translator["Sync_GroupReview"], versions,
                !unknownRemote && items.All(item => item.CanChooseLocal), items.All(item => item.CanChooseRemote),
                items.Select(item => item.Hint).FirstOrDefault(hint => hint is not null), translator["Sync_KeepRemoteGroup"], translator["Sync_KeepLocalGroup"]);
        }
        return CreateSingle(review, remoteRecords);
    }

    private PersonalSyncConflictItem CreateSingle(PersonalSyncConflictReview review, IReadOnlyList<SyncRecord> remoteRecords)
    {
        var conflict = review.Conflict;
        var references = (review.LocalContext ?? []).Where(row => row.Values is not null).ToDictionary(row => row.Record);
        foreach (var record in remoteRecords.Where(row => row.Values is not null)) references.TryAdd(record.Record, record);
        if (conflict.RelatedRecord is { Values: not null } related) references[related.Record] = related;
        // The service snapshot owns remote captions and units. A newly edited local account or
        // holding type cannot relabel the remote amount while the owner reviews it.
        var remoteReferences = conflict.Context is null ? references : conflict.Context.Where(row => row.Values is not null).ToDictionary(row => row.Record);
        var comparison = conflict.Current?.Values ?? (conflict.RelatedRecord?.Record.Table == conflict.Proposal.Record.Table ? conflict.RelatedRecord.Values : null);
        var complete = true;
        var versions = new List<PersonalSyncReviewVersion>
        {
            Version(conflict.Proposal.Record, conflict.Proposal.Values, comparison, translator["Sync_ReviewSubmitted"], true),
            Version(conflict.Proposal.Record, conflict.Current?.Values, conflict.Proposal.Values, translator["Sync_Remote"], conflict.Current is not null, true),
        };
        if (conflict.RelatedRecord is { } counterpart)
            versions.Add(Version(counterpart.Record, counterpart.Values, conflict.Proposal.Record.Table == counterpart.Record.Table ? conflict.Proposal.Values : null,
                translator["Sync_ReviewRelated"], true, true));
        var actual = review.LocalContext?.FirstOrDefault(row => row.Record == conflict.Proposal.Record);
        var changed = actual is null || !Same(actual.Values, conflict.Proposal.Values);
        var duplicate = conflict.Current?.Values is null && conflict.RelatedRecord?.Record.Table == conflict.Proposal.Record.Table &&
            (conflict.ExistingSettlement is not null || conflict.Reason == "UniqueRelationship");
        var reason = translator[conflict.Reason switch
        {
            "ReviewStale" => "Sync_ReasonStale", "OccurrenceAlreadySettled" => "Sync_ReasonSettlement",
            "CompetingChanges" => "Sync_ReasonChanges", _ => "Sync_ReasonRelated",
        }];
        var hint = changed ? translator["Sync_ReviewLocalChanged"] : !complete ? translator["Sync_ReviewIncomplete"]
            : duplicate ? translator["Sync_ReviewDuplicateHint"] : null;
        var title = translator[conflict.Proposal.Record.Table switch
        { "Accounts" => "Entry_Account", "Entries" => "Sync_RecordEntry", "OccurrenceStates" => "Occurrence_Title",
            "Budgets" => "Budget_Title", "BudgetCategoryLimits" => "Budget_Categories", "Schedules" => "Plan_Details",
            "AssetTypes" => "AssetType_Name", "AssetEvents" => "AssetEvent_Kind", "AssetValuations" => "Holding_Prices",
            "AssetLocations" => "AssetEvent_Location", "Goals" => "Goal_Details", "GoalAllocations" => "Goal_History",
            "ContributionPlans" => "Goal_PlanTitle", "Templates" => "Templates_Title", "SavedFilters" => "Filter_Title",
            "Categories" => "Entry_Category", "CategoryRules" => "Rules_Title", "ExchangeRates" => "Rates_Title",
            "Settings" => "Settings_MoneyMonths", "ForecastSnapshots" => "Snapshot_Detail", "Attachments" => "Attachment_Title",
            "HouseholdContacts" => "HouseholdCost_Contacts", "HouseholdCosts" or "HouseholdAllocations" or "HouseholdSettlements" => "HouseholdCost_Title", _ => "Sync_Record" }];
        return new(review, title, reason, versions, complete && !changed && !duplicate, complete && !changed, hint,
            translator[duplicate ? "Sync_KeepExisting" : "Sync_KeepRemote"], translator["Sync_KeepLocal"]);

        PersonalSyncReviewVersion Version(SyncRecordKey key, JsonElement? body, JsonElement? other, string heading, bool existed, bool remoteVersion = false)
        {
            if (body is null) return new(heading, translator[existed ? "Sync_Deleted" : "Sync_ReviewAbsent"], []);
            var table = SyncSchema.Describe(key.Table); var fields = new List<PersonalSyncReviewField>();
            if (table.Name is "Schedules" or "ContributionPlans")
            {
                var rule = ReviewRule(body.Value);
                if (Recurrence.Validate(rule) is not null) complete = false;
                else fields.Add(new(translator["Plan_Repeat"], NativeDigits.Apply(new PlanText(translator, dates, localization).Rule(rule))!));
            }
            foreach (var column in table.Columns)
            {
                if (column.Name is "Id" or "CreatedAt") continue;
                if (!body.Value.TryGetProperty(column.Name, out var value)) { complete = false; continue; }
                if (column.Name == "GroupId" && key.Table is "Entries" or "AssetEvents")
                {
                    fields.Add(new(translator["Sync_LinkedGroup"], translator[value.ValueKind == JsonValueKind.Null ? "Sync_ReviewEmpty" : "Sync_GroupMember"]));
                    if (value.ValueKind != JsonValueKind.Null && review.Conflict.LinkedGroup != value.GetGuid()) complete = false;
                    continue;
                }
                JsonElement? compared = other is { } otherBody && otherBody.TryGetProperty(column.Name, out var cell) ? cell : null;
                if (!Captions.TryGetValue(column.Name, out var caption) || !translator.TryGetString(caption, out var label))
                { if (value.ValueKind != JsonValueKind.Null && (Critical(column.Name) || !Same(value, compared))) complete = false; continue; }
                if (table.Name == "ContributionPlans" && column.Name == "Method") label = translator["Goal_Method"];
                if (table.Name == "Goals" && column.Name == "Type") label = translator["Goal_TypeQuestion"];
                if (table.Name == "Goals" && column.Name == "Frequency") label = translator["Goal_Frequency"];
                if (table.Name.StartsWith("Household", StringComparison.Ordinal) && column.Name == "EntryId") label = translator["HouseholdCost_SharedCash"];
                if (TryValue(table, column, value, body.Value, remoteVersion ? remoteReferences : references, out var text)) fields.Add(new(label, text));
                else if (Critical(column.Name) || column.Kind is "guid" or "json-guid" || !Same(value, compared)) complete = false;
            }
            return new(heading, "", fields);
        }
    }

    private bool TryValue(SyncTable table, SyncColumn column, JsonElement value, JsonElement body,
        IReadOnlyDictionary<SyncRecordKey, SyncRecord> references, out string text)
    {
        text = translator["Sync_ReviewUnavailable"];
        if (value.ValueKind == JsonValueKind.Null) { text = translator["Sync_ReviewEmpty"]; return true; }
        if (TryPortableValue(table, column, value, body, references, out var portable) is { } supported)
        { text = portable; return supported; }
        if (table.Name == "Goals" && column.Name == "TargetAmount" || table.Name == "ContributionPlans" && column.Name is "Amount" or "AssumedPricePerUnitMilli")
        {
            JsonElement goal;
            if (table.Name == "Goals") goal = body;
            else if (references.TryGetValue(Key("Goals", body.GetProperty("GoalId").GetString()!), out var parent) && parent.Values is { } goalBody) goal = goalBody;
            else return false;
            if ((GoalType)goal.GetProperty("Type").GetInt64() == GoalType.HoldingQuantity)
            {
                if (!references.TryGetValue(Key("AssetTypes", goal.GetProperty("AssetTypeId").GetString()!), out var record) || record.Values is not { } typeBody) return false;
                var type = ReviewedType(typeBody); var holding = new HoldingText(translator, localization, dates);
                if (column.Name == "AssumedPricePerUnitMilli") return ExactPrice(value.GetInt64(), type.PriceCurrencyCode, type, out text);
                if (type.Dimension == AssetDimension.Count && !type.Divisible && value.GetInt64() % Quantities.PerGramOrUnit != 0) return false;
                text = holding.Quantity(value.GetInt64(), type); return true;
            }
            if (column.Name == "AssumedPricePerUnitMilli" || !Currencies.TryGet(goal.GetProperty("CurrencyCode").GetString(), out var currency)) return false;
            text = NativeDigits.Apply(MoneyText.Format(value.GetInt64(), currency.Code, localization.CurrentCulture))!; return true;
        }
        if (table.Name == "AssetTypes" && column.Name is "PurityPer10000" or "UnitWeightMg")
        {
            var type = ReviewedType(body); var holding = new HoldingText(translator, localization, dates);
            text = column.Name == "PurityPer10000" ? holding.Purity(type)!
                : holding.Quantity(value.GetInt64(), new AssetType { Name = "", Dimension = AssetDimension.Mass, PriceCurrencyCode = type.PriceCurrencyCode });
            return text is not null;
        }
        if (table.Name is "AssetEvents" or "AssetValuations" && column.Name is "Quantity" or "PricePerUnitMilli")
        {
            if (!references.TryGetValue(Key("AssetTypes", body.GetProperty("AssetTypeId").GetString()!), out var record) || record.Values is not { } typeBody) return false;
            var type = ReviewedType(typeBody); var holding = new HoldingText(translator, localization, dates);
            if (column.Name == "Quantity")
            {
                if (type.Dimension == AssetDimension.Count && !type.Divisible && value.GetInt64() % Quantities.PerGramOrUnit != 0) return false;
                text = holding.Quantity(value.GetInt64(), type); return true;
            }
            // Stored prices have three more decimal places than ordinary minor-unit money.
            // HoldingText.Price intentionally rounds for overview cards; consent must retain every stored digit.
            return ExactPrice(value.GetInt64(), body.GetProperty("CurrencyCode").GetString(), type, out text);
        }
        if (MoneyFields.Contains(table.Name + "." + column.Name))
        {
            var currency = Currency(table.Name, column.Name, body, references);
            if (!Currencies.TryGet(currency, out var known)) return false;
            text = NativeDigits.Apply(MoneyText.Format(value.GetInt64(), known.Code, CultureInfo.CurrentCulture))!; return true;
        }
        if (column.Kind == "date")
        {
            if (!DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return false;
            text = dates.Format(date); return true;
        }
        if (column.Kind == "time")
        {
            if (!TimeOnly.TryParseExact(value.GetString(), ["HH:mm:ss", "HH:mm:ss.FFFFFFF"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var when)) return false;
            text = NativeDigits.Apply(when.ToString("T", localization.CurrentCulture))!; return true;
        }
        if (column.Kind == "ticks")
        {
            var stamp = new DateTimeOffset(value.GetInt64(), TimeSpan.Zero);
            text = dates.Format(DateOnly.FromDateTime(stamp.UtcDateTime)) + " · " + NativeDigits.Apply(stamp.ToString("HH:mm:ss.fffffff", localization.CurrentCulture)) + " UTC";
            return true;
        }
        if (column.Kind == "guid")
        {
            var target = table.ForeignKeys.FirstOrDefault(key => key.Columns.Length == 1 && key.Columns[0] == column.Name)?.PrincipalTable
                ?? (column.Name is "ScheduleId" or "PreviousScheduleId" ? "Schedules" : column.Name == "EntryId" ? "Entries"
                    : column.Name == "DefaultAccountId" ? "Accounts" : column.Name == "ParentId" ? "Categories"
                    : column.Name == "CategoryId" ? "Categories" : column.Name == "GoalId" ? "Goals"
                    : column.Name is "AccountId" or "ToAccountId" ? "Accounts"
                    : column.Name == "AssetTypeId" ? "AssetTypes" : column.Name is "LocationId" or "ToLocationId" ? "AssetLocations" : null);
            if (target is null || !references.TryGetValue(Key(target, value.GetString()!), out var record) || record.Values is not { } row) return false;
            if (target == "Budgets")
            {
                if (!TryValue(SyncSchema.Manifest.Tables.Single(item => item.Name == target),
                    SyncSchema.Manifest.Tables.Single(item => item.Name == target).Columns.Single(item => item.Name == "Period"),
                    row.GetProperty("Period"), row, references, out var period)) return false;
                text = translator["Budget_Title"] + " · " + period + " · " + row.GetProperty("CurrencyCode").GetString();
                var calendarKey = "Calendar_" + (PeriodCalendar)row.GetProperty("Calendar").GetInt64();
                if (!translator.TryGetString(calendarKey, out var calendar)) return false;
                text += " · " + calendar;
                if (row.GetProperty("Period").GetInt64() == 0)
                    text += " · " + NativeDigits.Apply(row.GetProperty("Year").GetInt64().ToString(CultureInfo.CurrentCulture)) + "/" +
                        NativeDigits.Apply(row.GetProperty("Month").GetInt64().ToString(CultureInfo.CurrentCulture));
                else if (DateOnly.TryParseExact(row.GetProperty("PeriodStart").GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                    text += " · " + dates.Format(start);
                return true;
            }
            if (row.TryGetProperty("Name", out var name) && !string.IsNullOrWhiteSpace(name.GetString())) text = name.GetString()!;
            else if (target == "Categories" && row.TryGetProperty("SystemKey", out var systemKey) && systemKey.GetString() is { } categoryKey &&
                translator.TryGetString("Category_" + categoryKey, out var categoryName)) text = categoryName;
            else if (row.TryGetProperty("Title", out var title) && !string.IsNullOrWhiteSpace(title.GetString())) text = title.GetString()!;
            else if (target == "Entries") text = translator["Sync_RecordEntry"];
            else return false;
            if (row.TryGetProperty("CurrencyCode", out var code) && code.GetString() is { } iso) text += " · " + iso;
            if (row.TryGetProperty("Date", out var when) && DateOnly.TryParseExact(when.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                text += " · " + dates.Format(date);
            return true;
        }
        if (column.Kind == "json-string")
        {
            using var list = JsonDocument.Parse(value.GetString()!);
            if (list.RootElement.ValueKind != JsonValueKind.Array) return false;
            text = string.Join(" ", list.RootElement.EnumerateArray().Select(item => EntryTags.Display(item.GetString()!))); return true;
        }
        if (column.Kind == "json-guid" && column.Name is "AccountIds" or "CategoryIds")
        {
            using var list = JsonDocument.Parse(value.GetString()!); var names = new List<string>();
            foreach (var id in list.RootElement.EnumerateArray())
            {
                var target = column.Name == "AccountIds" ? "Accounts" : "Categories";
                if (!references.TryGetValue(Key(target, id.GetString()!), out var account) || account.Values is not { } accountBody) return false;
                var name = accountBody.GetProperty("Name").GetString();
                if (string.IsNullOrWhiteSpace(name) && target == "Categories" && accountBody.GetProperty("SystemKey").GetString() is { } systemKey)
                    name = translator.TryGetString("Category_" + systemKey, out var translated) ? translated : null;
                if (string.IsNullOrWhiteSpace(name)) return false;
                names.Add(name + (target == "Accounts" ? " · " + accountBody.GetProperty("CurrencyCode").GetString() : ""));
            }
            text = names.Count == 0 ? translator[column.Name == "AccountIds" ? "Sync_AllBudgetAccounts" : "Sync_ReviewEmpty"] : string.Join(Environment.NewLine, names); return true;
        }
        if (column.Kind == "decimal")
        {
            if (!decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)) return false;
            text = NativeDigits.Apply(number.ToString(CultureInfo.CurrentCulture))! + (column.Name == "Percent" ? " %" : ""); return true;
        }
        if (column.Kind == "integer")
        {
            var number = value.GetInt64();
            if (column.EnumValues is { } allowed && !column.Flags && !allowed.Contains(number)) return false;
            if (Booleans.Contains(column.Name)) { text = translator[number == 0 ? "Sync_No" : "Sync_Yes"]; return true; }
            var key = (table.Name, column.Name) switch
            {
                ("Accounts", "Type") => "AccountType_" + (AccountType)number,
                ("Templates", "Kind") => "EntryKind_" + (EntryKind)number,
                ("Categories", "Kind") or ("CategoryRules", "Kind") => number == 0 ? "EntryKind_Expense" : "EntryKind_Income",
                ("Entries", "Kind") => "EntryKind_" + (EntryKind)number,
                ("HouseholdCosts", "Kind") => number == 1 ? "EntryKind_Expense" : "EntryKind_Refund",
                ("HouseholdCosts", "PaymentKind") => number == 1 ? "HouseholdCost_PrivatePaid" : "HouseholdCost_SharedPaid",
                ("Entries", "Review") => number == 0 ? "Sync_Confirmed" : "Sync_Unreviewed",
                ("Entries", "Direction") => number == 0 ? "Sync_Increase" : "Sync_Decrease",
                ("Entries", "Source") => number switch { 0 => "Sync_SourceManual", 1 => "Sync_SourcePlan", _ => "Sync_SourceImport" },
                ("OccurrenceStates", "Status") => number switch { 0 => "Sync_StatusOpen", 1 => "Sync_StatusSettled", _ => "Sync_StatusSkipped" },
                ("Budgets", "Method") => "Budget_Method" + (BudgetMethod)number,
                ("Budgets", "Period") => "Budget_Period" + (BudgetPeriod)number,
                ("Budgets", "Calendar") => "Calendar_" + (PeriodCalendar)number,
                ("Budgets", "Rollover") => number switch { 0 => "Rollover_None", 1 => "Rollover_Surplus", _ => "Rollover_Both" },
                ("Schedules", "Kind") => "EntryKind_" + (EntryKind)number,
                ("Schedules", "AmountMode") => "AmountMode_" + (AmountMode)number,
                ("Schedules", "Calendar") => "Calendar_" + (PeriodCalendar)number,
                ("Schedules", "MissingDay") => number == 0 ? "MissingDay_LastValid" : "MissingDay_Skip",
                ("Schedules", "WeekendShift") => number switch { 0 => "Weekend_Keep", 1 => "Weekend_Before", _ => "Weekend_After" },
                ("Schedules", "State") => number switch { 0 => "Sync_PlanActive", 1 => "Plan_Paused", _ => "Plan_Ended" },
                ("Schedules", "Frequency") => number == 1 ? "Rule_Daily" : "Repeat_" + (Frequency)number,
                ("Schedules", "EndKind") => "End_" + (EndKind)number,
                ("Schedules", "DayRule") => "Sync_DayRule_" + (MonthDayRule)number,
                ("AssetTypes", "Kind") => "AssetKind_" + (AssetKind)number,
                ("AssetTypes", "Metal") => "Metal_" + (Metal)number,
                ("AssetTypes", "Dimension") => number == 0 ? "AssetType_ByWeight" : "AssetType_ByCount",
                ("AssetEvents", "Kind") => "AssetEvent_" + (AssetEventKind)number,
                ("AssetValuations", "Source") => number == 0 ? "Sync_SourceManual" : "AssetEvent_Purchase",
                ("Goals", "Type") => "GoalType_" + (GoalType)number,
                ("Goals", "Priority") => "GoalPriority_" + (GoalPriority)number,
                ("Goals", "State") => "GoalState_" + (GoalState)number,
                ("Goals", "Frequency") => number == 0 ? "Goal_Monthly" : "Goal_Weekly",
                ("ContributionPlans", "Method") => number switch { 0 => "Contribution_Fixed", 1 => "Contribution_ShareOfIncome", _ => "Contribution_SpendingCut" },
                _ => "",
            };
            if (key.Length == 0 && table.Name == "ContributionPlans" && column.Name is "Frequency" or "EndKind" or "DayRule" or "Calendar" or "MissingDay" or "WeekendShift")
                return TryValue(table with { Name = "Schedules" }, column, value, body, references, out text);
            if (key.Length != 0 && translator.TryGetString(key, out var label)) { text = label; return true; }
            if (table.Name is "Schedules" or "ContributionPlans" && column.Name == "WeekendDays")
            {
                if ((number & ~127L) != 0) return false;
                text = number == 0 ? translator["Sync_ReviewEmpty"] : string.Join(", ", Enum.GetValues<DayOfWeek>()
                    .Where(day => (number & (1 << (int)day)) != 0).Select(localization.CurrentCulture.DateTimeFormat.GetDayName));
                return true;
            }
            if (table.Name is "Schedules" or "ContributionPlans" && column.Name is "ReminderDaysBefore" or "Interval" or "SecondDay" or "Count" || table.Name == "Goals" && column.Name == "HomePin")
            { text = NativeDigits.Apply(number.ToString(localization.CurrentCulture))!; return true; }
            if (column.Name == "SortOrder" || table.Name == "Budgets" && (column.Name is "Year" or "Month"))
            { text = NativeDigits.Apply(number.ToString(CultureInfo.CurrentCulture))!; return true; }
            return false;
        }
        if (column.Kind == "text") { text = value.GetString() ?? ""; return true; }
        return false;
    }

    private static string? Currency(string table, string column, JsonElement body, IReadOnlyDictionary<SyncRecordKey, SyncRecord> references)
    {
        if (column == "OriginalAmount") return body.GetProperty("OriginalCurrencyCode").GetString();
        if (table == "Accounts") return body.GetProperty("CurrencyCode").GetString();
        if (table is "HouseholdCosts" or "HouseholdSettlements") return body.GetProperty("CurrencyCode").GetString();
        if (table == "HouseholdAllocations")
            return references.TryGetValue(Key("HouseholdCosts", body.GetProperty("CostId").GetString()!), out var cost) && cost.Values is { } costBody
                ? costBody.GetProperty("CurrencyCode").GetString() : null;
        if (table == "Budgets") return body.GetProperty("CurrencyCode").GetString();
        if (table == "ForecastSnapshots") return body.GetProperty("CurrencyCode").GetString();
        if (table == "Settings" && column == "EssentialEstimate") return body.GetProperty("EssentialEstimateCurrency").GetString();
        if (table == "AssetEvents")
            return references.TryGetValue(Key("AssetTypes", body.GetProperty("AssetTypeId").GetString()!), out var type) && type.Values is { } typeBody
                ? typeBody.GetProperty("PriceCurrencyCode").GetString() : null;
        if (table == "BudgetCategoryLimits")
            return references.TryGetValue(Key("Budgets", body.GetProperty("BudgetId").GetString()!), out var budget) && budget.Values is { } budgetBody
                ? budgetBody.GetProperty("CurrencyCode").GetString() : null;
        if (table == "OccurrenceStates")
        {
            if (!references.TryGetValue(Key("Schedules", body.GetProperty("ScheduleId").GetString()!), out var plan) || plan.Values is not { } values) return null;
            body = values;
        }
        var account = column == "ToAmount" ? "ToAccountId" : "AccountId";
        return body.TryGetProperty(account, out var id) && id.ValueKind == JsonValueKind.String &&
            references.TryGetValue(Key("Accounts", id.GetString()!), out var row) && row.Values is { } source
            ? source.GetProperty("CurrencyCode").GetString() : null;
    }
    private static SyncRecordKey Key(string table, string id) => new(table, JsonSerializer.Serialize(new[] { Guid.Parse(id).ToString("D").ToUpperInvariant() }));
    private bool ExactPrice(long stored, string? code, AssetType type, out string text)
    {
        text = translator["Sync_ReviewUnavailable"];
        if (!Currencies.TryGet(code, out var currency)) return false;
        var exact = stored / (currency.MinorFactor * 1000m);
        text = NativeDigits.Apply("\u2066\u200e" + exact.ToString("N" + (currency.MinorDigits + 3), localization.CurrentCulture) +
            " " + currency.Code + "\u200e\u2069 " + new HoldingText(translator, localization, dates).PerUnit(type))!; return true;
    }
    private static AssetType ReviewedType(JsonElement row) => new(row.GetProperty("Id").GetGuid())
    {
        Name = row.GetProperty("Name").GetString()!, PriceCurrencyCode = row.GetProperty("PriceCurrencyCode").GetString()!,
        Kind = (AssetKind)row.GetProperty("Kind").GetInt32(), Dimension = (AssetDimension)row.GetProperty("Dimension").GetInt32(),
        Metal = (Metal)row.GetProperty("Metal").GetInt32(), Divisible = row.GetProperty("Divisible").GetInt64() != 0,
        CountUnitName = row.GetProperty("CountUnitName").GetString(),
        PurityPer10000 = row.GetProperty("PurityPer10000").ValueKind == JsonValueKind.Null ? null : row.GetProperty("PurityPer10000").GetInt32(),
        UnitWeightMg = row.GetProperty("UnitWeightMg").ValueKind == JsonValueKind.Null ? null : row.GetProperty("UnitWeightMg").GetInt64(),
    };
    private static RecurrenceRule ReviewRule(JsonElement row)
    {
        DateOnly Date(string name) => DateOnly.ParseExact(row.GetProperty(name).GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        int? Number(string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : row.GetProperty(name).GetInt32();
        return new() { Start = Date("Start"), Frequency = (Frequency)row.GetProperty("Frequency").GetInt32(), Interval = row.GetProperty("Interval").GetInt32(),
            Calendar = (PeriodCalendar)row.GetProperty("Calendar").GetInt32(), DayRule = (MonthDayRule)row.GetProperty("DayRule").GetInt32(),
            MissingDay = (MissingDayPolicy)row.GetProperty("MissingDay").GetInt32(), SecondDay = Number("SecondDay"), End = (EndKind)row.GetProperty("EndKind").GetInt32(),
            EndDate = row.GetProperty("EndDate").ValueKind == JsonValueKind.Null ? null : Date("EndDate"), Count = Number("Count"),
            WeekendDays = row.GetProperty("WeekendDays").GetInt32(), WeekendShift = (WeekendShift)row.GetProperty("WeekendShift").GetInt32(),
            HolidayRegion = row.GetProperty("HolidayRegion").GetString() };
    }
    private static bool Critical(string name) => name.Contains("Amount", StringComparison.Ordinal) || name.Contains("Limit", StringComparison.Ordinal) ||
        name.Contains("Price", StringComparison.Ordinal) || name is "OpeningBalance" or "Installment" or "Quantity" or "Rate" or "Percent";
    private static bool Same(JsonElement? first, JsonElement? second) => first is null ? second is null || second.Value.ValueKind == JsonValueKind.Null
        : second is null ? first.Value.ValueKind == JsonValueKind.Null : JsonElement.DeepEquals(first.Value, second.Value);
}
