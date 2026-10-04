using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>A slice of the composition with its share (ZEX-K13); the table is the alternative to the chart.</summary>
public sealed record CompositionRow(string Label, double Value, string ValueText, string ShareText, Color Color, Brush Brush);

/// <summary>R4 – holdings and net worth: net worth per currency, its composition, unvalued holdings and account movements.</summary>
public sealed partial class ReportsViewModel
{
    private static readonly string[] SliceColors = ["#1E5BD6", "#0E8A6A", "#C77700", "#7A3FD1", "#C2185B", "#00838F", "#5D4037", "#546E7A"];

    private NetWorthResult? _netWorth;

    public ObservableCollection<AmountLine> WealthLines { get; } = [];

    public ObservableCollection<CompositionRow> CompositionRows { get; } = [];

    public ObservableCollection<Brush> CompositionBrushes { get; } = [];

    public ObservableCollection<string> UnvaluedLines { get; } = [];

    public ObservableCollection<AccountReport> Accounts { get; } = [];

    /// <summary>Gets net worth per currency, e.g. "280.00 EUR · 4,000.00 USD" (ZEX-K10).</summary>
    [ObservableProperty]
    public partial string? NetWorthText { get; set; }

    [ObservableProperty]
    public partial string? NetWorthConverted { get; set; }

    [ObservableProperty]
    public partial bool HasComposition { get; set; }

    [ObservableProperty]
    public partial string? CompositionNote { get; set; }

    [ObservableProperty]
    public partial bool HasUnvalued { get; set; }

    private async Task BuildWealthAsync(IReadOnlyCollection<LedgerEntry> entries)
    {
        var today = Today;
        var types = await _holdings.GetTypesAsync();
        var text = new Holdings.HoldingText(_translator, _localization, _dates);
        _netWorth = NetWorthCalculator.Compute(_accounts, entries, types, await _holdings.GetEventsAsync(), await _holdings.GetValuationsAsync(), today);

        // K10 per currency with its groups; currencies are never added without a rate.
        NetWorthText = _netWorth.Totals.Count == 0 ? null : string.Join(" · ", _netWorth.Totals.Select(t => Money(t.Total, t.CurrencyCode)));
        WealthLines.Clear();
        foreach (var total in _netWorth.Totals)
        {
            void Line(string key, long value, bool negative = false)
            {
                if (value != 0)
                {
                    WealthLines.Add(new AmountLine(_translator[key], Money(negative ? -value : value, total.CurrencyCode), false, negative));
                }
            }

            Line("Report_WealthMoney", total.Money);
            Line("Report_WealthReceivables", total.Receivables);
            Line("Report_WealthAssets", total.ValuedAssets);
            Line("Report_WealthHoldings", total.Holdings);
            Line("Report_WealthDebts", total.Debts, negative: true);
            WealthLines.Add(new AmountLine(_translator.Format("Report_NetWorthIn", total.CurrencyCode), Money(total.Total, total.CurrencyCode), true, total.Total < 0));
        }

        UnvaluedLines.Clear();
        foreach (var holding in _netWorth.Unvalued)
        {
            UnvaluedLines.Add(_translator.Format("Report_NotValued", holding.AssetType.Name, text.Quantity(holding.Quantity, holding.AssetType)));
        }

        HasUnvalued = UnvaluedLines.Count > 0;

        // K13 (Advanced): shares of the known part in the valuation currency; what cannot be valued is listed.
        CompositionRows.Clear();
        CompositionBrushes.Clear();
        NetWorthConverted = null;
        CompositionNote = null;
        IReadOnlyCollection<RateInfo>? rates = null;
        IReadOnlyCollection<string>? missing = null;
        if (IsAdvanced && _settings.ValuationCurrencyEnabled)
        {
            var composition = NetWorthCalculator.Compose(_netWorth, new RateTable(await _store.GetRatesAsync()), _settings.ReportCurrencyCode, new RateFreshness(_settings.RateFreshnessDays));
            rates = composition.Rates;
            missing = composition.MissingRates;
            var index = 0;
            foreach (var slice in composition.Slices)
            {
                var color = Color.FromArgb(SliceColors[index++ % SliceColors.Length]);
                var brush = new SolidColorBrush(color);
                CompositionRows.Add(new CompositionRow(slice.Label, (double)slice.Value, Money(slice.Value, composition.CurrencyCode), slice.SharePercent.ToString("0.0", Culture) + " %", color, brush));
                CompositionBrushes.Add(brush);
            }

            if (composition.MissingRates.Count == 0 && _netWorth.Totals.Count > 1)
            {
                NetWorthConverted = _translator.Format("Report_Converted", Money(composition.Known - composition.Debts, composition.CurrencyCode));
            }

            CompositionNote = string.Join(" · ", new[]
            {
                composition.Debts > 0 ? _translator.Format("Report_DebtsApart", Money(composition.Debts, composition.CurrencyCode)) : null,
                composition.MissingRates.Count > 0 ? _translator.Format("Report_NoRate", string.Join(", ", composition.MissingRates)) : null,
                composition.Slices.Count < 2 ? _translator["Report_AddRatesForComposition"] : null,
            }.Where(s => s is not null));
            CompositionNote = CompositionNote.Length == 0 ? null : CompositionNote;
        }

        HasComposition = CompositionRows.Count > 1;
        BuildAccounts(entries);
        await BuildStatusAsync(entries, full: false, rates: rates, missingRates: missing, holdingsWithoutPrice: _netWorth.Unvalued.Count);
    }

    // Opening → movements → closing for each account of the scope in the period (REP-03).
    private void BuildAccounts(IReadOnlyCollection<LedgerEntry> entries)
    {
        Accounts.Clear();
        var scope = LedgerCalculator.InScope(_accounts, ScopeIds()).Where(a => !a.IsArchived || entries.Any(e => e.AccountId == a.Id && e.Date >= _from && e.Date <= _to));
        foreach (var movement in ReportCalculator.AccountMovements(scope, entries, _from, _to))
        {
            var account = _accounts.First(a => a.Id == movement.AccountId);
            string Format(long value, bool plus = false) => Money(value, movement.CurrencyCode, plus);
            var lines = new List<AmountLine> { new(_translator["Report_Opening"], Format(movement.Opening), true, movement.Opening < 0) };
            void Add(string key, long value, bool negative = false)
            {
                if (value != 0)
                {
                    lines.Add(new AmountLine(_translator[key], Format(negative ? -value : value, plus: true), false));
                }
            }

            Add("Report_OpeningBalance", movement.OpeningBalanceAdded);
            Add("KindFilter_Income", movement.Income);
            Add("Report_Refunds", movement.Refunds);
            Add("KindFilter_Expenses", movement.Expense, negative: true);
            Add("EntryKind_IncomeReversal", movement.IncomeReversals, negative: true);
            Add("Report_TransfersIn", movement.TransfersIn);
            Add("Report_TransfersOut", movement.TransfersOut, negative: true);
            Add("Report_Capital", movement.Capital);
            Add("Report_Adjustments", movement.Adjustments);
            lines.Add(new AmountLine(_translator["Report_Closing"], Format(movement.Closing), true, movement.Closing < 0));
            Accounts.Add(new AccountReport(account.Name, lines));
        }
    }

    [RelayCommand]
    private Task ExplainNetWorthAsync() => ExplainAsync("K10", NetWorthText ?? string.Empty, [.. WealthLines], () => Shell.Current.GoToAsync(AppShell.AccountsRoute));

    [RelayCommand]
    private Task ExplainCompositionAsync() => ExplainAsync("K13", NetWorthConverted ?? string.Empty,
        [.. CompositionRows.Select(r => new AmountLine($"{r.Label} · {r.ShareText}", r.ValueText, false))], () => Shell.Current.GoToAsync(AppShell.HoldingsRoute));

    [RelayCommand]
    private Task OpenHoldingsAsync() => Shell.Current.GoToAsync(AppShell.HoldingsRoute);
}
