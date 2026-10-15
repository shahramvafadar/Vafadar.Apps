using System.Globalization;
using System.Text.Json;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Dashboard;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

public sealed partial class PersonalSyncReviewPresenter
{
    // Null means the ordinary typed formatter owns this value; false means this stored format is not reviewable.
    private bool? TryPortableValue(SyncTable table, SyncColumn column, JsonElement value, JsonElement body,
        IReadOnlyDictionary<SyncRecordKey, SyncRecord> references, out string text)
    {
        text = translator["Sync_ReviewUnavailable"];
        if (table.Name == "Attachments" && column.Name == "Data")
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("sha256", out var hash) ||
                !SyncProtocolDigest.IsDigest(hash.GetString()) || !value.TryGetProperty("length", out var size) ||
                !size.TryGetInt64(out var bytes) || bytes is < 1 or > Core.Ledger.EntryAttachment.MaxBytes) return false;
            text = NativeDigits.Apply(translator.Format("Sync_AttachmentBytes", bytes))!; return true;
        }
        if (column.Name == "Icon") return translator.TryGetString("Icon_" + value.GetString(), out text!);
        if (table.Name == "Categories" && column.Name == "SystemKey") return translator.TryGetString("Category_" + value.GetString(), out text!);
        if (table.Name == "Categories" && column.Name == "SpendingType")
            return translator.TryGetString("SpendingType_" + (SpendingType)value.GetInt64(), out text!);
        if (table.Name == "SavedFilters" && column.Name is "Period" or "Kind")
        {
            var keys = column.Name == "Period" ? new[] { "Period_ThisMonth", "Period_LastMonth", "Period_ThisYear", "Period_All" }
                : ["Sync_AllKinds", "EntryKind_Expense", "EntryKind_Income", "EntryKind_Transfer"];
            var index = value.GetInt64(); if (index < 0 || index >= keys.Length) return false;
            text = translator[keys[(int)index]]; return true;
        }
        if (table.Name == "ExchangeRates" && column.Name == "Rate")
        {
            if (!decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0 ||
                !Currencies.TryGet(body.GetProperty("FromCurrencyCode").GetString(), out var source) ||
                !Currencies.TryGet(body.GetProperty("ToCurrencyCode").GetString(), out var target)) return false;
            text = NativeDigits.Apply("\u2066\u200e1 " + source.Code + " = " + rate.ToString(localization.CurrentCulture) + " " + target.Code + "\u200e\u2069")!;
            return true;
        }
        if (table.Name == "Settings")
        {
            switch (column.Name)
            {
                case "Mode": return translator.TryGetString("Mode_" + (ExperienceMode)value.GetInt64(), out text!);
                case "BudgetCalendar": return translator.TryGetString("Calendar_" + (PeriodCalendar)value.GetInt64(), out text!);
                case "EssentialEstimatePeriod":
                    var period = value.GetInt64(); if (period < 0 || period > 2) return false;
                    text = translator[new[] { "Settings_PerDay", "Settings_PerWeek", "Settings_PerMonth" }[(int)period]]; return true;
                case "MonthStartDay":
                case "RateFreshnessDays":
                    text = column.Name == "RateFreshnessDays" && value.GetInt64() == 0 ? translator["Settings_FreshnessNever"] :
                        NativeDigits.Apply(value.GetInt64().ToString(localization.CurrentCulture))!; return true;
                case "DisplayUnits": return ReviewDisplayUnits(value.GetString()!, out text);
                case "HomeLayout": return ReviewHomeLayout(value.GetString()!, out text);
                case "ReviewProgress": return ReviewPeriodProgress(value.GetString()!, out text);
            }
        }
        if (table.Name == "ForecastSnapshots")
        {
            if (column.Name == "AccountIds")
            {
                var ids = value.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ids.Length == 0) { text = translator["Sync_ReviewEmpty"]; return true; }
                if (ids.Any(id => !Guid.TryParse(id, out _))) return false;
                return TryValue(table with { Name = "SavedFilters" }, column with { Kind = "json-guid" },
                    JsonSerializer.SerializeToElement(JsonSerializer.Serialize(ids)), body, references, out text);
            }
            if (column.Name == "UnknownCount") { text = NativeDigits.Apply(value.GetInt64().ToString(localization.CurrentCulture))!; return true; }
            if (column.Name == "Path")
            {
                if (!Currencies.TryGet(body.GetProperty("CurrencyCode").GetString(), out var currency) ||
                    !DateOnly.TryParseExact(body.GetProperty("BaseDate").GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                    !DateOnly.TryParseExact(body.GetProperty("Horizon").GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)) return false;
                var values = value.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 0)
                { text = translator["Sync_ReviewEmpty"]; return end == start; }
                if (values.Any(cell => !long.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) ||
                    values.Length != end.DayNumber - start.DayNumber + 1) return false;
                var snapshot = new ForecastSnapshot { Name = "", CurrencyCode = currency.Code, AccountIds = "", Path = value.GetString()!, BaseDate = start, Horizon = end };
                text = string.Join(Environment.NewLine, snapshot.Points().Select(point => dates.Format(point.Date) + " · " +
                    NativeDigits.Apply(MoneyText.Format(point.Balance, currency.Code, localization.CurrentCulture)))); return true;
            }
        }
        return null;
    }

    private bool ReviewDisplayUnits(string stored, out string text)
    {
        text = translator["Sync_ReviewUnavailable"]; var values = new List<string>(); var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in stored.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Split('|') is not [var code, var name, var exponent] ||
                !int.TryParse(exponent, NumberStyles.None, CultureInfo.InvariantCulture, out var power)) return false;
            var unit = new DisplayUnit(code, name, power); if (!unit.IsValid || !codes.Add(code)) return false;
            values.Add(translator.Format("Unit_Definition", unit.Name,
                NativeDigits.Apply("\u2066\u200e" + unit.Factor.ToString(localization.CurrentCulture) + " " + unit.CurrencyCode + "\u200e\u2069")));
        }
        text = values.Count == 0 ? translator["Sync_ReviewEmpty"] : string.Join(Environment.NewLine, values); return true;
    }

    private bool ReviewHomeLayout(string stored, out string text)
    {
        text = translator["Sync_ReviewUnavailable"]; var seen = new HashSet<HomeSection>();
        foreach (var token in stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (!Enum.TryParse<HomeSection>(token.TrimStart('-'), out var section) || !Enum.IsDefined(section) || !seen.Add(section)) return false;
        text = string.Join(Environment.NewLine, HomeLayout.Parse(stored).Sections.Select((section, index) =>
            NativeDigits.Apply((index + 1).ToString(localization.CurrentCulture)) + " · " + translator["HomeSection_" + section.Section] +
            " · " + translator[section.IsVisible ? "Sync_Shown" : "Sync_Hidden"])); return true;
    }

    private bool ReviewPeriodProgress(string stored, out string text)
    {
        text = translator["Sync_ReviewUnavailable"];
        if (stored.Length == 0) { text = translator["Sync_ReviewEmpty"]; return true; }
        if (stored.Length < 7 || stored[4] != '-' || !int.TryParse(stored.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
            !int.TryParse(stored.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month) || year < 1 || month < 1 || month > 12 ||
            stored.Length > 7 && stored[7] != ':') return false;
        if (stored.Length > 7 && stored[8..].Split(',', StringSplitOptions.RemoveEmptyEntries).Any(token =>
            !Enum.TryParse<ReviewStep>(token, out var step) || !Enum.IsDefined(step))) return false;
        var state = PeriodReview.Parse(stored, year, month);
        text = NativeDigits.Apply(state.Key) + " · " + translator[state.Finished ? "Sync_ReviewFinished" : "Sync_ReviewInProgress"] + Environment.NewLine +
            string.Join(Environment.NewLine, state.Done.Order().Select(step => translator["Review_" + step])); return true;
    }
}
