using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>A category to choose for a split part; <see cref="Id"/> is <see langword="null"/> for "Uncategorized".</summary>
public sealed record SplitCategory(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>One part of a split.</summary>
public sealed partial class SplitPart(IReadOnlyList<SplitCategory> categories, Action changed) : ObservableObject
{
    public IReadOnlyList<SplitCategory> Categories { get; } = categories;

    [ObservableProperty]
    public partial SplitCategory? Category { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    partial void OnAmountTextChanged(string value) => changed();
}

/// <summary>
/// Splits one purchase or income across categories, or changes and joins an existing split (F2-TX-01). The parts must
/// add up exactly to the amount; the account balance changes by the same total.
/// </summary>
public sealed partial class SplitEditorViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IAppInteraction interaction) : ViewModelBase, IQueryAttributable, Presentation.IUnsavedChanges, Presentation.IThemeAware
{
    private readonly SplitEditorSession _session = new(store);
    private bool _loading = true;
    private List<LedgerEntry> _parts = [];
    private Currency _currency = Currencies.Euro;
    private long _total;
    private IReadOnlyList<SplitCategory> _categories = [];

    public ObservableCollection<SplitPart> Parts { get; } = [];

    [ObservableProperty]
    public partial string TotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? RemainingText { get; set; }

    // Amounts are typed in the currency's display unit when one is defined (FX-07).
    [ObservableProperty]
    public partial string? UnitNote { get; set; }

    [ObservableProperty]
    public partial Color RemainingColor { get; set; } = Palette.SecondaryText;

    [ObservableProperty]
    public partial bool CanSave { get; set; }

    [ObservableProperty]
    public partial bool IsSplit { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    /// <summary>Loads only the original coherent source packet before publishing editable shares.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue("id", out var value) || value is not Guid id) return;
        query.Clear();
        var review = await Task.Run(() => _session.LoadAsync(id));
        if (review is null) return;
        var entry = review.Inputs.Parts.Single(e => e.Id == id);
        _parts = review.Inputs.Parts.OrderBy(e => e.Id == id ? 0 : 1).ThenBy(e => e.CreatedAt).ThenBy(e => e.Id).Select(e => e.Copy()).ToList();
        var paybacks = review.Inputs.Paybacks;

        if (!EntryActions.CanSplit(_parts) || EntryActions.HasPaybacks(_parts, paybacks))
        {
            Error = translator["Split_NotPossible"];
            return;
        }

        var accounts = review.Inputs.Accounts;
        _currency = Currencies.TryGet(accounts.FirstOrDefault(a => a.Id == entry.AccountId)?.CurrencyCode ?? string.Empty, out var currency) ? currency : Currencies.Euro;
        _total = _parts.Sum(p => p.Amount);
        TotalText = MoneyText.Format(_total, _currency.Code, localization.CurrentCulture);
        UnitNote = DisplayUnitNote.For(translator, _currency.Code);
        IsSplit = _parts.Count > 1;

        var kind = entry.Kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense;
        var lookup = new CategoryLookup(review.Inputs.Categories, translator);
        _categories =
        [
            new SplitCategory(null, translator["Category_Uncategorized"]),
            .. lookup.All
                .Where(c => c.Kind == kind && !c.IsArchived)
                .OrderBy(c => lookup.Get(c.ParentId)?.SortOrder ?? c.SortOrder).ThenBy(c => c.ParentId is null ? 0 : 1).ThenBy(c => c.SortOrder)
                .Select(c => new SplitCategory(c.Id, c.ParentId is { } parent ? $"{lookup.Name(parent)} › {CategoryLookup.NameOf(c, translator)}" : CategoryLookup.NameOf(c, translator))),
        ];

        Parts.Clear();
        foreach (var part in _parts)
        {
            AddPart(part.CategoryId, part.Amount);
        }

        if (Parts.Count == 1)
        {
            AddPart(null, 0);
        }

        Update();
        _snapshot = Snapshot(); _loading = false; Update();
    }

    private void AddPart(Guid? categoryId, long amount) => Parts.Add(new SplitPart(_categories, Update)
    {
        Category = _categories.FirstOrDefault(c => c.Id == categoryId) ?? _categories[0],
        AmountText = amount > 0 ? MoneyText.ForInput(amount, _currency.Code, localization.CurrentCulture) : string.Empty,
    });

    [RelayCommand]
    private void Add()
    {
        AddPart(null, 0);
        Update();
    }

    [RelayCommand]
    private void Remove(SplitPart part)
    {
        if (Parts.Count > 2)
        {
            Parts.Remove(part);
            Update();
        }
    }

    // Shows how much is still to assign; saving is possible only when the parts add up exactly (F2-TX-01).
    // The remaining amount is recolored from the parts as they are; nothing typed changes.
    Task Presentation.IThemeAware.RefreshThemeAsync()
    {
        Update();
        return Task.CompletedTask;
    }

    private void Update()
    {
        var assigned = 0L;
        var valid = true;
        foreach (var part in Parts)
        {
            if (!MoneyText.TryParse(part.AmountText, _currency, localization.CurrentCulture, out var amount) || amount <= 0)
            {
                valid = false;
                continue;
            }

            assigned += amount;
        }

        var rest = _total - assigned;
        var culture = localization.CurrentCulture;
        RemainingText = rest switch
        {
            > 0 => translator.Format("Split_Remaining", MoneyText.Format(rest, _currency.Code, culture)),
            < 0 => translator.Format("Split_TooMuch", MoneyText.Format(-rest, _currency.Code, culture)),
            _ when !valid => translator["Split_EnterAll"],
            _ => translator["Split_Balanced"],
        };
        RemainingColor = rest == 0 && valid ? Palette.IncomeText : Palette.WarningText;
        CanSave = valid && rest == 0 && Parts.Count >= 2 && CanEdit && _session.Review is not null && !_session.Committed;
        OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(CanJoin)); OnPropertyChanged(nameof(CloseText));
        JoinCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Gets whether raw shares may still be edited; a known committed form never becomes writable again.</summary>
    public bool CanEdit => !_loading && !IsBusy && !_session.Committed;
    /// <summary>Gets whether the original still-reviewed split can be combined explicitly.</summary>
    public bool CanJoin => IsSplit && !IsBusy && !_session.Committed && _session.Review is not null;
    /// <summary>Gets the original Cancel caption, or Close after known acceptance.</summary>
    public string CloseText => translator[_session.Committed ? "Common_Close" : "Common_Cancel"];

    [RelayCommand]
    private Task SaveAsync() => CanSave ? SavePartsAsync(false) : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanJoin))]
    private Task JoinAsync() => CanJoin ? SavePartsAsync(true) : Task.CompletedTask;

    private async Task SavePartsAsync(bool join)
    {
        if (IsBusy || _session.Committed || _session.Review is null) return;
        var originalDraft = Snapshot();
        var shares = Parts.Select(p =>
        {
            MoneyText.TryParse(p.AmountText, _currency, localization.CurrentCulture, out var amount);
            return (p.Category?.Id, amount);
        }).ToArray();
        IsBusy = true; Error = null; Update();
        try
        {
            var result = await _session.SaveAsync(shares, join, () => Snapshot() == originalDraft);
            if (result is null) return;
            if (!result.Succeeded)
            { Error = string.Join(Environment.NewLine, result.Errors.Select(e => translator[$"LedgerError_{e}"])); return; }
            _snapshot = Snapshot(); Update();
        }
        catch (CommittedWriteNotificationException<SaveResult>)
        { _snapshot = Snapshot(); Error = translator["Split_SavedNeedsReview"]; return; }
        catch (SplitReviewChangedException)
        { _session.Retire(); Error = translator["Split_SourceChanged"]; return; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _session.Retire();
            Error = Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true);
            return;
        }
        finally { IsBusy = false; Update(); }

        try { await interaction.NavigateAsync(".."); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Error = translator["Editor_SavedReturnFailed"]; await interaction.ShowFailureAsync(exception); }
    }

    /// <summary>Retires a departed original form without replacing its raw draft.</summary>
    public void RetireReview() { _session.Retire(); Update(); }

    [RelayCommand]
    private Task CancelAsync() => _session.Committed ? interaction.NavigateAsync("..") : Presentation.UnsavedChanges.LeaveAsync(this);

    // The input as it was loaded or saved; leaving with a change asks first (CR12).
    private string? _snapshot;

    /// <inheritdoc />
    public bool IsDirty => !_session.Committed && _snapshot is not null && Snapshot() != _snapshot;

    private string Snapshot() => Presentation.UnsavedChanges.Fingerprint(
        string.Join(',', Parts.Select(p => $"{p.Category?.Id}:{p.AmountText}")));
}
