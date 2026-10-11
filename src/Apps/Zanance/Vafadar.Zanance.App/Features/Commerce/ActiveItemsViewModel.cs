using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>One retained item with independently translated original state and unsaved choice.</summary>
public sealed class ActiveItemRow(ResourceChoiceItem item, ActiveItemsViewModel owner, Translator translator) : ObservableObject
{
    /// <summary>Gets the immutable original identity, never its visual label.</summary>
    public Guid Id => item.Id;
    /// <summary>Gets the complete original user-written name.</summary>
    public string Name => item.Name;
    /// <summary>Gets whether the original state can occupy a slot; pauses count without resuming the plan.</summary>
    public bool IsEligible => item.State is QuotaItemState.Active or QuotaItemState.Paused;
    /// <summary>Gets the in-memory choice; only the explicit Save command persists it.</summary>
    public bool IsSelected => owner.IsSelected(Id);
    /// <summary>Gets whether the real row button can change this reviewed draft.</summary>
    public bool CanToggle => IsEligible && owner.CanChoose && owner.CanInteract;
    /// <summary>Gets the translated original state, independent of selection.</summary>
    public string StateText => translator[item.State switch
    {
        QuotaItemState.Paused => "Plan_Paused",
        QuotaItemState.Archived => "ActiveItems_Archived",
        QuotaItemState.Ended => "Plan_Ended",
        _ => "ActiveItems_Active",
    }];
    /// <summary>Gets identity details without displaying or changing money.</summary>
    public string? DetailText => item.AccountType is { } type
        ? translator.Format("ActiveItems_AccountDetail", translator[$"AccountType_{type}"], item.CurrencyCode)
        : item.EntryKind is { } kind ? translator[$"EntryKind_{kind}"] : null;
    /// <summary>Gets the separate availability caption; a historical state never becomes an active choice.</summary>
    public string ChoiceText => translator[!IsEligible ? "ActiveItems_History" : IsSelected ? "ActiveItems_Selected" : "ActiveItems_ReadOnly"];
    /// <summary>Gets a complete live spoken name for the real native target.</summary>
    public string AccessibilityText => translator.Format("ActiveItems_RowDescription", Name, DetailText ?? StateText, StateText, ChoiceText);

    /// <summary>Refreshes only captions and draft-dependent properties, retaining the original identity and state.</summary>
    internal void Refresh()
    {
        OnPropertyChanged(nameof(IsSelected)); OnPropertyChanged(nameof(CanToggle));
        OnPropertyChanged(nameof(StateText)); OnPropertyChanged(nameof(DetailText));
        OnPropertyChanged(nameof(ChoiceText)); OnPropertyChanged(nameof(AccessibilityText));
    }
}

/// <summary>Reviewed scoped account/Plan choices with covered reads, explicit Save and no commercial activation.</summary>
public sealed partial class ActiveItemsViewModel : ViewModelBase
{
    private readonly ResourceChoiceStore _store;
    private readonly Translator _translator;
    private readonly IAppInteraction _interaction;
    private ResourceChoiceSnapshot? _snapshot;
    private HashSet<Guid> _selected = [];
    private HashSet<Guid> _baseline = [];
    private bool _publishing;
    private bool _reviewRetired;
    private bool _discardApproved;

    /// <summary>Gets the pending native kind transition for same-page application coordination and review.</summary>
    internal Task PendingKindChange { get; private set; } = Task.CompletedTask;
    private int _publishedKindIndex;
    private string? _feedbackKey;

    /// <summary>Creates the empty covered form without reading the database or writing default choices.</summary>
    public ActiveItemsViewModel(ResourceChoiceStore store, Translator translator, IAppInteraction interaction)
    {
        _store = store; _translator = translator; _interaction = interaction;
        Loading.PropertyChanged += (_, _) => RefreshCapabilities();
        PropertyChanged += (_, args) => { if (args.PropertyName == nameof(IsBusy)) RefreshCapabilities(); };
        RefreshDisplay();
    }

    /// <summary>Gets initial/loading/failure publication state for the complete form.</summary>
    public SnapshotLoadState Loading { get; } = new();
    /// <summary>Gets retained rows, sorted for display without changing the stored identity set.</summary>
    public ObservableCollection<ActiveItemRow> Items { get; } = [];
    /// <summary>Gets or sets the native choice index; switching a dirty draft requires explicit discard.</summary>
    [ObservableProperty]
    public partial int KindIndex { get; set; }
    /// <summary>Gets the live translated native choice captions.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> KindNames { get; private set; } = [];
    /// <summary>Gets visible translated feedback, never raw storage/provider exceptions.</summary>
    [ObservableProperty]
    public partial string? Feedback { get; private set; }
    /// <summary>Gets whether one published review is currently interactive.</summary>
    public bool CanInteract => Loading.IsReady && !IsBusy;
    /// <summary>Gets whether the current review allows a bounded explicit choice.</summary>
    public bool CanChoose => _snapshot?.CanChoose == true && !_reviewRetired;
    /// <summary>Gets whether an eligible subset exceeds its current maximum.</summary>
    public bool OverCapacity => _snapshot?.Maximum is { } maximum && _selected.Count > maximum;
    /// <summary>Gets whether current draft choices differ from the last published or successfully saved baseline.</summary>
    public bool IsDirty => !_discardApproved && !_selected.SetEquals(_baseline);
    /// <summary>Gets whether the complete explicit draft can be submitted; empty is a valid explicit choice.</summary>
    public bool CanSave => CanInteract && CanChoose && !OverCapacity;
    /// <summary>Gets whether the covered state or a pending consent/write blocks leaving the form.</summary>
    public bool CanLeave => !IsBusy && !Loading.IsLoading;
    /// <summary>Gets whether the original list is empty.</summary>
    public bool IsEmpty => Items.Count == 0;
    /// <summary>Gets a complete translated counter with the user's current display digits.</summary>
    public string Counter => NativeDigits.Apply(_snapshot?.Maximum is { } maximum
        ? _translator.Format("ActiveItems_Count", _selected.Count, maximum)
        : _translator.Format("ActiveItems_AllCount", _selected.Count))!;
    /// <summary>Gets the current allowance explanation without inventing a product tier.</summary>
    public string AllowanceText => _translator[_snapshot?.Maximum switch
    {
        0 => "ActiveItems_Unavailable",
        null => "ActiveItems_Unlimited",
        _ => "ActiveItems_ChooseHint",
    }];
    /// <summary>Gets the inline capacity error beside the choices.</summary>
    public string? CapacityError => OverCapacity ? NativeDigits.Apply(_translator.Format("ActiveItems_Limit", _snapshot!.Maximum)) : null;

    /// <summary>Reads and publishes once on first appearance; returning from help keeps the exact unsaved draft.</summary>
    public Task EnsureLoadedAsync() => Loading.HasLoaded ? Task.CompletedTask : LoadAsync();

    /// <summary>Retries a covered read; user-requested reload/switch has already resolved any discard consent.</summary>
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await LoadCoreAsync(KindIndex); }
        finally { IsBusy = false; }
    }

    private async Task LoadCoreAsync(int index)
    {
        ResourceChoiceSnapshot? next = null;
        try
        {
            await Loading.RunAsync(async () => next = await _store.ReadAsync(Kind(index)), () =>
            {
                _snapshot = next!; _reviewRetired = false; _discardApproved = false; _publishedKindIndex = index;
                _selected = next!.SelectedIds.ToHashSet(); _baseline = _selected.ToHashSet();
                Items.Clear();
                foreach (var item in next.Items.OrderBy(item => item.Name, StringComparer.CurrentCulture).ThenBy(item => item.Id))
                    Items.Add(new(item, this, _translator));
                SetFeedback(null); RefreshCapabilities();
            });
        }
        catch (Exception) { SetFeedback("ActiveItems_LoadFailed"); }
    }

    partial void OnKindIndexChanged(int value)
    {
        if (_publishing) return;
        if (!CanInteract || value is not (0 or 1)) { RestoreIndex(); return; }
        PendingKindChange = SwitchAsync(value);
    }

    private async Task SwitchAsync(int requested)
    {
        IsBusy = true;
        try
        {
            if (!await ConfirmDiscardCoreAsync()) { RestoreIndex(); return; }
            await LoadCoreAsync(requested);
        }
        catch (Exception) { RestoreIndex(); SetFeedback("ActiveItems_LoadFailed"); }
        finally { IsBusy = false; }
    }

    private void RestoreIndex()
    {
        _publishing = true;
        try { KindIndex = _publishedKindIndex; }
        finally { _publishing = false; }
    }

    /// <summary>Requests discard through the existing native dialog; it never saves or clears financial data.</summary>
    public async Task<bool> ConfirmDiscardAsync()
    {
        if (!CanLeave) return false;
        IsBusy = true;
        try { return await ConfirmDiscardCoreAsync(); }
        catch (Exception) { SetFeedback("ActiveItems_LoadFailed"); return false; }
        finally { IsBusy = false; }
    }

    private async Task<bool> ConfirmDiscardCoreAsync()
    {
        if (!IsDirty) return true;
        var accepted = await _interaction.ConfirmAsync(_translator["ActiveItems_Title"], _translator["ActiveItems_DiscardMessage"],
            _translator["ActiveItems_Discard"], _translator["Common_Cancel"]);
        // A failed read/navigation does not revoke the already explicit discard decision or require it again.
        if (accepted) _discardApproved = true;
        return accepted;
    }

    [RelayCommand]
    private async Task LeaveAsync()
    {
        if (!CanLeave) return;
        IsBusy = true;
        try { if (await ConfirmDiscardCoreAsync()) await _interaction.NavigateAsync(".."); }
        catch (Exception) { SetFeedback("Error_Unexpected"); }
        finally { IsBusy = false; }
    }

    /// <summary>Gets the current in-memory identity choice for a retained row.</summary>
    internal bool IsSelected(Guid id) => _selected.Contains(id);

    [RelayCommand]
    private void Toggle(ActiveItemRow row)
    {
        if (!CanInteract || !CanChoose || !row.IsEligible || !Items.Contains(row)) return;
        _discardApproved = false;
        if (!_selected.Remove(row.Id)) _selected.Add(row.Id);
        SetFeedback(null); RefreshCapabilities();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!CanSave || _snapshot is not { } reviewed) return;
        IsBusy = true;
        var submitted = _selected.ToArray();
        try
        {
            try { await _store.SaveAsync(reviewed, submitted); }
            catch (Exception) { SetFeedback("ActiveItems_SaveFailed"); return; }
            // D-142: a committed review must not authorize another Save if post-commit navigation fails.
            _baseline = submitted.ToHashSet(); _discardApproved = false; _reviewRetired = true; SetFeedback(null); RefreshCapabilities();
            try { await _interaction.NavigateAsync(".."); }
            catch (Exception) { SetFeedback("ActiveItems_SavedReturnFailed"); }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { if (await ConfirmDiscardCoreAsync()) await LoadCoreAsync(KindIndex); }
        catch (Exception) { SetFeedback("ActiveItems_LoadFailed"); }
        finally { IsBusy = false; }
    }

    /// <summary>Refreshes live text/digits without reloading, replacing identities or discarding any selected draft.</summary>
    public void RefreshDisplay()
    {
        _publishing = true;
        try { KindNames = [_translator["Accounts_Title"], _translator["Tab_Plans"]]; }
        finally { _publishing = false; }
        SetFeedback(_feedbackKey); RefreshCapabilities();
    }

    private void SetFeedback(string? key) { _feedbackKey = key; Feedback = key is null ? null : _translator[key]; }

    private void RefreshCapabilities()
    {
        foreach (var name in new[] { nameof(CanInteract), nameof(CanChoose), nameof(CanSave), nameof(CanLeave), nameof(IsDirty),
            nameof(OverCapacity), nameof(Counter), nameof(AllowanceText), nameof(CapacityError), nameof(IsEmpty) }) OnPropertyChanged(name);
        foreach (var item in Items) item.Refresh();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private static QuotaKind Kind(int index) => index == 1 ? QuotaKind.RecurringPlans : QuotaKind.FinancialAccounts;
}
