using System.Collections.ObjectModel;
using System.Globalization;
using Vafadar.Zanance.App.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
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
public sealed partial class SplitPart(IReadOnlyList<SplitCategory> categories, Action changed, Func<string> currency, Func<CultureInfo> culture) : ObservableObject
{
    public IReadOnlyList<SplitCategory> Categories { get; } = categories;

    [ObservableProperty]
    public partial SplitCategory? Category { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    /// <summary>Gets the exact raw monetary interpretation for this original share.</summary>
    public MoneyDraftInput Input { get; } = new();
    /// <summary>Gets the original unit beside this share.</summary>
    [ObservableProperty] public partial string? UnitName { get; set; }
    /// <summary>Explains a retained original numeric format.</summary>
    [ObservableProperty] public partial string? FormatNote { get; set; }
    /// <summary>Gets complete field-local reentry feedback.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
    partial void OnAmountTextChanged(string value) { Input.ChangeText(value, currency(), culture()); changed(); }
    partial void OnCategoryChanged(SplitCategory? value) => changed();
}

/// <summary>
/// Splits one purchase or income across categories, or changes and joins an existing split (F2-TX-01). The parts must
/// add up exactly to the amount; the account balance changes by the same total.
/// </summary>
public sealed partial class SplitEditorViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IAppInteraction interaction) : ViewModelBase, Presentation.IUnsavedChanges, Presentation.IThemeAware
{
    private readonly SplitEditorSession _session = new(store);
    private bool _loading = true;
    private List<LedgerEntry> _parts = [];
    private Currency _currency = Currencies.Euro;
    private long _total;
    private long _partRevision;
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
    public partial bool CanSave { get; set; }

    [ObservableProperty]
    public partial bool IsSplit { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

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

    private void AddPart(Guid? categoryId, long amount)
    {
        var row = new SplitPart(_categories, PartChanged, () => _currency.Code, () => localization.CurrentCulture);
        row.Category = _categories.FirstOrDefault(c => c.Id == categoryId) ?? _categories[0];
        if (amount > 0) row.AmountText = row.Input.Format(amount, _currency.Code, localization.CurrentCulture);
        else row.Input.Initialize(string.Empty, _currency.Code, localization.CurrentCulture);
        Parts.Add(row); _partRevision++;
    }
    private void PartChanged() { _partRevision++; Update(); }

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
            Parts.Remove(part); _partRevision++;
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

    /// <summary>Observes only the visible original form's display changes without renewing raw money.</summary>
    public void SetMoneyDisplayActive(bool active)
    {
        localization.Changed -= OnMoneyDisplayChanged;
        if (active) localization.Changed += OnMoneyDisplayChanged;
    }
    private void OnMoneyDisplayChanged(object? sender, EventArgs args) => Update();
    partial void RefreshNativeColors(bool balanced);

    private void Update()
    {
        var assigned = 0L;
        var valid = true;
        foreach (var part in Parts)
        {
            part.UnitName = part.Input.UnitName(_currency.Code);
            part.FormatNote = part.Input.OriginalExample(localization.CurrentCulture) is { } example ? translator.Format("Plan_OriginalInputFormat", example) : null;
            part.Error = part.Input.NeedsReview(_currency.Code) ? translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(_currency.Code)) : null;
            if (part.Error is not null || !part.Input.TryParse(out var amount) || amount <= 0)
            {
                valid = false;
                continue;
            }

            assigned += amount;
        }

        var rest = _total - assigned;
        var culture = localization.CurrentCulture;
        TotalText = MoneyText.Format(_total, _currency.Code, culture); UnitNote = DisplayUnitNote.For(translator, _currency.Code);
        RemainingText = rest switch
        {
            > 0 => translator.Format("Split_Remaining", MoneyText.Format(rest, _currency.Code, culture)),
            < 0 => translator.Format("Split_TooMuch", MoneyText.Format(-rest, _currency.Code, culture)),
            _ when !valid => translator["Split_EnterAll"],
            _ => translator["Split_Balanced"],
        };
        RefreshNativeColors(rest == 0 && valid);
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
        var originalDraft = Snapshot(); var revision = _partRevision;
        Update();
        if ((!join && !CanSave) || revision != _partRevision || originalDraft != Snapshot()) return;
        var shares = join ? [] : Parts.Select(p =>
        {
            p.Input.TryParse(out var amount); return (p.Category?.Id, amount);
        }).ToArray();
        var inputRetired = false;
        bool InputCurrent()
        {
            var current = Snapshot() == originalDraft && revision == _partRevision && (join || Parts.All(p => !p.Input.NeedsReview(_currency.Code)));
            if (!current) inputRetired = true;
            return current;
        }
        IsBusy = true; Error = null; Update();
        try
        {
            var result = await _session.SaveAsync(shares, join, InputCurrent);
            if (result is null) return;
            if (!result.Succeeded)
            { Error = string.Join(Environment.NewLine, result.Errors.Select(e => translator[$"LedgerError_{e}"])); return; }
            _snapshot = Snapshot(); Update();
        }
        catch (CommittedWriteNotificationException<SaveResult>)
        { _snapshot = Snapshot(); Error = translator["Split_SavedNeedsReview"]; return; }
        catch (SplitReviewChangedException)
        {
            if (!inputRetired) _session.Retire();
            Error = translator[inputRetired ? "Amount_DraftChanged" : "Split_SourceChanged"]; return;
        }
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
    private async Task CancelAsync()
    {
        if (_session.Committed || !IsDirty || await interaction.ConfirmAsync(translator["Common_DiscardTitle"], translator["Common_DiscardMessage"],
            translator["Common_Discard"], translator["Common_KeepEditing"])) await interaction.NavigateAsync("..");
    }

    // The input as it was loaded or saved; leaving with a change asks first (CR12).
    private string? _snapshot;

    /// <inheritdoc />
    public bool IsDirty => !_session.Committed && _snapshot is not null && Snapshot() != _snapshot;

    private string Snapshot() => Presentation.DraftFingerprint.Create(
        string.Join(',', Parts.Select(p => $"{p.Category?.Id}:{p.AmountText}:{p.Input.Stamp}")));
}
