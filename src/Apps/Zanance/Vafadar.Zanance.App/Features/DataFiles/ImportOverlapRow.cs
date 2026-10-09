using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>An explicit per-aggregate import decision with the original amount, covered details and resulting amount.</summary>
public sealed partial class ImportOverlapRow : ObservableObject
{
    private readonly Translator _translator;
    private readonly string _remainder;
    private readonly Action _changed;

    /// <summary>Creates a row whose initial choice is deliberately unset.</summary>
    public ImportOverlapRow(AggregateOverlap overlap, string accountName, string categoryName, string currency,
        Translator translator, IDateFormatter dates, ILocalizationService localization, bool hasRefunds, Action changed)
    {
        Preview = overlap;
        _translator = translator;
        _changed = changed;
        CanLink = AggregatedEntries.CanLink(overlap.Aggregate) && !hasRefunds;
        Choices = CanLink ? [translator["Aggregate_Replace"], translator["Import_KeepBothShort"]] : [translator["Import_KeepBothShort"]];
        Title = overlap.Aggregate.Title ?? categoryName;
        Context = translator.Format("Import_AggregateContext", accountName, categoryName);
        Period = translator.Format("Import_AggregatePeriod", dates.Format(overlap.Aggregate.AggregatedFrom ?? overlap.Aggregate.Date),
            dates.Format(overlap.Aggregate.AggregatedTo ?? overlap.Aggregate.Date));
        Amounts = translator.Format("Import_AggregateAmounts", MoneyText.Format(overlap.Aggregate.Amount, currency, localization.CurrentCulture),
            overlap.Detailed.Count, MoneyText.Format(overlap.DetailedTotal, currency, localization.CurrentCulture));
        _remainder = MoneyText.Format(overlap.Remainder, currency, localization.CurrentCulture);
        Outcome = translator[CanLink ? "Import_AggregateChoose" : "Import_AggregateLinked"];
    }

    /// <summary>Gets the exact reviewed data, rechecked by the store before saving.</summary>
    public AggregateOverlap Preview { get; }

    /// <summary>Gets the aggregate's title or category.</summary>
    public string Title { get; }

    /// <summary>Gets the matching account and category.</summary>
    public string Context { get; }

    /// <summary>Gets the inclusive covered dates in the display calendar.</summary>
    public string Period { get; }

    /// <summary>Gets the original amount, detail count and total.</summary>
    public string Amounts { get; }

    /// <summary>Gets whether linking would preserve all unrelated financial relationships.</summary>
    public bool CanLink { get; }

    /// <summary>Gets the explicit choices; related financial metadata only permits keep-both here.</summary>
    public IReadOnlyList<string> Choices { get; }

    /// <summary>Gets or sets the chosen option; -1 means no decision yet.</summary>
    [ObservableProperty]
    public partial int SelectedIndex { get; set; } = -1;

    /// <summary>Gets or sets the visible effect of the current choice.</summary>
    [ObservableProperty]
    public partial string Outcome { get; set; }

    /// <summary>Gets a value indicating whether a valid option has been selected.</summary>
    public bool HasChoice => SelectedIndex >= 0 && SelectedIndex < Choices.Count;

    /// <summary>Gets the decision for the store, after <see cref="HasChoice"/> is checked.</summary>
    public ImportAggregateChoice Decision => new(Preview, CanLink && SelectedIndex == 0);

    partial void OnSelectedIndexChanged(int value)
    {
        Outcome = !HasChoice ? _translator[CanLink ? "Import_AggregateChoose" : "Import_AggregateLinked"]
            : Decision.Link ? Preview.Remainder == 0 ? _translator["Import_AggregateRemoved"]
                : _translator.Format("Import_AggregateRemaining", _remainder)
            : _translator["Import_AggregateKept"];
        _changed();
    }
}
