using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>One point of the net worth history; <see cref="Value"/> is in major units for the chart axis.</summary>
public sealed record WealthHistoryRow(string Label, double Value, string ValueText, bool IsIncomplete);

/// <summary>R5 – wealth change: net worth over time and whether it grew through saving or through prices and rates.</summary>
public sealed partial class ReportsViewModel
{
    private const int HistoryMonths = 12;
    private WealthChange? _change;

    public ObservableCollection<WealthHistoryRow> HistoryRows { get; } = [];

    public ObservableCollection<AmountLine> ChangeLines { get; } = [];

    public IReadOnlyList<string> ChangeRangeNames => [_translator["Report_LastMonth"], _translator["Report_ThisYear"], _translator["Report_Last12Months"]];

    /// <summary>Gets or sets the range of the decomposition: 0 last complete month, 1 this year, 2 the last twelve months.</summary>
    [ObservableProperty]
    public partial int ChangeRangeIndex { get; set; }

    [ObservableProperty]
    public partial string? HistoryNote { get; set; }

    [ObservableProperty]
    public partial string? ChangeTitle { get; set; }

    [ObservableProperty]
    public partial string? ChangeNote { get; set; }

    [ObservableProperty]
    public partial bool HasHistory { get; set; }

    partial void OnChangeRangeIndexChanged(int value) => Reload();

    // Net worth at the end of each month with prices and rates dated on or before it (ZEX-S0801), and the explanation
    // of its change (ZEX-S0802). Converted with the valuation currency when it is on, natively otherwise.
    private async Task BuildHistoryAsync(IReadOnlyCollection<LedgerEntry> entries)
    {
        var today = Today;
        var types = await _holdings.GetTypesAsync();
        var events = await _holdings.GetEventsAsync();
        var valuations = await _holdings.GetValuationsAsync();
        var rates = new RateTable(await _store.GetRatesAsync());
        var converted = _settings.ValuationCurrencyEnabled;
        var target = converted ? _settings.ReportCurrencyCode : _currency;
        var currency = Currencies.TryGet(target, out var known) ? known : Currencies.Euro;

        HistoryRows.Clear();
        foreach (var point in WealthHistory.MonthEnds(_accounts, entries, types, events, valuations, rates, target, today, HistoryMonths, Calendar, _startDay))
        {
            var value = converted ? point.Converted : point.PerCurrency.GetValueOrDefault(target);
            var label = point.Date == today ? _translator["Report_Today"] : _dates.Format(point.Date, DateFormatStyle.MonthYear);
            HistoryRows.Add(new WealthHistoryRow(label, value is { } v ? (double)MoneyText.ToDecimal(v, currency) : 0,
                value is { } amount ? Money(amount, target) + (point.IsIncomplete ? " *" : string.Empty) : "?", point.IsIncomplete || value is null));
        }

        HasHistory = HistoryRows.Count > 1;
        HistoryNote = HistoryRows.Any(r => r.IsIncomplete) ? _translator["Report_HistoryIncomplete"] : null;

        // The range of the decomposition ends today and starts at the end of the period before it.
        var current = PeriodMath.MonthOf(today, Calendar, _startDay);
        var (lastYear, lastMonth) = PeriodMath.Previous(current.Year, current.Month);
        var (from, to) = ChangeRangeIndex switch
        {
            1 => (PeriodMath.MonthRange(current.Year, 1, Calendar).First.AddDays(-1), today),
            2 => (today.AddMonths(-12), today),
            _ => (PeriodMath.MonthRange(lastYear, lastMonth, Calendar, _startDay).First.AddDays(-1), PeriodMath.MonthRange(lastYear, lastMonth, Calendar, _startDay).Last),
        };
        var changes = WealthHistory.Explain(_accounts, entries, types, events, valuations, from, to);
        IReadOnlyList<string> missing = [];
        if (converted)
        {
            (_change, missing) = WealthHistory.Convert(changes, rates, target);
        }
        else
        {
            _change = changes.FirstOrDefault(c => string.Equals(c.CurrencyCode, target, StringComparison.OrdinalIgnoreCase));
        }

        ChangeLines.Clear();
        ChangeTitle = _translator.Format("Report_ChangeRange", _dates.Format(from, DateFormatStyle.Short), _dates.Format(to, DateFormatStyle.Short));
        ChangeNote = null;
        if (_change is { } c)
        {
            void Line(string key, long value, bool total = false) => ChangeLines.Add(new AmountLine(_translator[key], Money(value, target, showPlus: true), total, value < 0, value > 0 && total));
            ChangeLines.Add(new AmountLine(_translator["Report_WealthStart"], Money(c.Start, target), false));
            Line("Report_Flows", c.Flows);
            Line("Report_PriceEffect", c.PriceEffect);
            if (converted)
            {
                Line("Report_FxEffect", c.FxEffect);
            }

            Line("Report_Corrections", c.Corrections);
            Line("Report_Remainder", c.Remainder);
            ChangeLines.Add(new AmountLine(_translator["Report_WealthEnd"], Money(c.End, target), true));
            Line("Report_WealthChange", c.Change, total: true);
            var causes = c.Causes.Select(cause => _translator[$"Report_Cause_{cause}"]).ToList();
            if (missing.Count > 0)
            {
                causes.Add(_translator.Format("Report_NoRate", string.Join(", ", missing)));
            }

            ChangeNote = causes.Count > 0 ? _translator.Format("Report_RemainderCauses", string.Join(" · ", causes)) : null;
        }

        await BuildStatusAsync(entries, full: false);
    }

    [RelayCommand]
    private Task ExplainChangeAsync() => ExplainAsync("K10H", _change is { } c ? Money(c.Change, c.CurrencyCode, showPlus: true) : string.Empty, [.. ChangeLines], () => Drill(KindFilter.All, null, null, _change?.From.AddDays(1), _change?.To));
}
