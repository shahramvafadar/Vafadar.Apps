using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Profiles;

namespace Vafadar.Zanance.App.Features.Profiles;

/// <summary>One original openable profile with a separate unsaved device activity choice.</summary>
public sealed class ProfileRow(LocalProfile profile, bool isCurrent, ProfilesViewModel owner, Translator translator) : ObservableObject
{
    /// <summary>Gets the complete original identity/name; the main id remains empty.</summary>
    public LocalProfile Profile => profile;
    /// <summary>Gets the original name or live translated unnamed-main caption.</summary>
    public string Name => string.IsNullOrWhiteSpace(profile.Name) ? translator["Profile_Main"] : profile.Name;
    /// <summary>Gets whether this original is open, independently of new work.</summary>
    public bool IsCurrent => isCurrent;
    /// <summary>Gets the live current-profile caption.</summary>
    public string? Detail => IsCurrent ? translator["Profile_Open"] : null;
    /// <summary>Gets the in-memory original choice without saving or opening another profile.</summary>
    public bool IsSelected => owner.IsSelected(profile.Id);
    /// <summary>Gets whether the explicit draft can be toggled.</summary>
    public bool CanToggle => owner.CanChoose && owner.CanInteract;
    /// <summary>Gets selected/retained availability; originals remain openable for retained work.</summary>
    public string ChoiceText => translator[IsSelected ? "ActiveItems_Selected" : "ActiveItems_ReadOnly"];
    /// <summary>Gets saved original work availability, independent of an unsaved choice.</summary>
    public string WorkText => translator[owner.CanUseOriginal(profile.Id) ? "Profile_WorkAvailable" : "Profile_WorkRetained"];
    /// <summary>Gets a complete original navigation name.</summary>
    public string AccessibilityText => translator.Format("Profile_ActionsDescription", Name, string.Join(". ", new[] { Detail, WorkText }.Where(text => text is not null)));
    /// <summary>Gets a distinct live spoken name for changing only the unsaved activity choice.</summary>
    public string ChoiceDescription => translator.Format("Profile_ChoiceDescription", Name, ChoiceText);
    /// <summary>Refreshes captions/state without replacing the original or saving a draft.</summary>
    internal void Refresh()
    { foreach (var property in new[] { nameof(Name), nameof(Detail), nameof(IsSelected), nameof(CanToggle), nameof(ChoiceText), nameof(WorkText), nameof(AccessibilityText), nameof(ChoiceDescription) }) OnPropertyChanged(property); }
}

/// <summary>Reviewed original device catalogue; retained profiles stay openable and choices save explicitly.</summary>
public sealed partial class ProfilesViewModel(ProfileService profiles, Translator translator, IAppInteraction interaction, IAppFlowHost host) : ViewModelBase
{
    private ProfileCatalogueReview? _review;
    private HashSet<string> _selected = new(StringComparer.Ordinal), _baseline = new(StringComparer.Ordinal);
    private bool _retired, _discardApproved, _commandActive;
    private string? _errorKey;
    private bool _active = true, _rootRetired;
    private int _frame, _display;
    private Func<bool>? _rootCurrent;
    private bool OwnsPresentation => _active && !_rootRetired && _rootCurrent?.Invoke() != false;
    /// <summary>Binds the actual original native root without changing catalogue or saved choices.</summary>
    public void BindRootOwner(Func<bool> current) => _rootCurrent = current;
    /// <summary>Retires old prompt continuations on departure; returning cannot revive their captured frame.</summary>
    public void SetActive(bool active)
    {
        if (!active) { _active = false; _frame++; }
        else if (!_rootRetired) _active = true;
        Refresh();
    }
    /// <summary>Permanently retires the original root while retaining known catalogue effects and raw choices.</summary>
    public void RetireRoot() { _rootRetired = true; SetActive(false); }
    private Action CapturePresentation()
    {
        var frame = _frame; var display = _display;
        void Check()
        {
            if (!OwnsPresentation || frame != _frame || display != _display)
                throw new OperationCanceledException("The original profile presentation retired.");
        }
        Check(); return Check;
    }
    private static bool IsCurrent(Action check)
    { try { check(); return true; } catch (OperationCanceledException) { return false; } }
    /// <summary>Gets every original with independent real navigation and choice targets.</summary>
    public ObservableCollection<ProfileRow> Profiles { get; } = [];
    /// <summary>Gets complete publication cover and explicit failed-read retry.</summary>
    public SnapshotLoadState Loading { get; } = new();
    /// <summary>Gets translated pinned feedback without preference/provider details.</summary>
    [ObservableProperty]
    public partial string? Error { get; set; }
    /// <summary>Gets whether current actions can start outside publication/prompts.</summary>
    public bool CanInteract => OwnsPresentation && Loading.IsReady && !IsBusy && !_commandActive && !_retired;
    /// <summary>Gets whether verified device capacity supports explicit selection.</summary>
    public bool CanChoose => _review?.CanChoose == true;
    /// <summary>Gets whether the complete draft fits reviewed capacity.</summary>
    public bool CanSave => CanInteract && CanChoose && !OverCapacity;
    /// <summary>Gets original creation capacity separately from retained navigation.</summary>
    public bool CanAdd => CanInteract && _review?.CanCreate == true;
    /// <summary>Gets whether the unsaved subset exceeds its device allowance.</summary>
    public bool OverCapacity => _review?.Maximum is { } maximum && _selected.Count > maximum;
    /// <summary>Gets whether exact original choices differ from the published baseline.</summary>
    public bool IsDirty => !_discardApproved && !_selected.SetEquals(_baseline);
    /// <summary>Gets whether Back may leave outside publication and pending prompts.</summary>
    public bool CanLeave => !_commandActive && !IsBusy && !Loading.IsLoading;
    /// <summary>Gets allowance text without activating a commercial tier.</summary>
    public string AllowanceText => translator[CanChoose ? "Profile_WorkHint" : "ActiveItems_Unlimited"];
    /// <summary>Gets the device choice count using display digits only.</summary>
    public string Counter => NativeDigits.Apply(_review?.Maximum is { } maximum
        ? translator.Format("ActiveItems_Count", _selected.Count, maximum) : translator.Format("ActiveItems_AllCount", _selected.Count))!;
    /// <summary>Gets the inline capacity error without truncating retained rows.</summary>
    public string? CapacityError => OverCapacity ? NativeDigits.Apply(translator.Format("ActiveItems_Limit", _review?.Maximum)) : null;

    /// <summary>Reads once on appearance; returning from help retains exact unsaved choices.</summary>
    public Task EnsureLoadedAsync() => Loading.HasLoaded ? Task.CompletedTask : LoadAsync();
    /// <summary>Publishes a complete original review before enabling input.</summary>
    public async Task LoadAsync()
    {
        if (!OwnsPresentation || IsBusy || _commandActive) return; BeginCommand();
        try { await LoadCoreAsync(); } finally { EndCommand(); }
    }
    private async Task LoadCoreAsync()
    {
        if (!OwnsPresentation) return;
        var current = CapturePresentation();
        try
        {
            ProfileCatalogueReview? next = null;
            await Loading.RunAsync(() => { next = profiles.ReadWorkReview(); return Task.CompletedTask; }, () =>
            {
                current(); profiles.ValidateWorkReview(next!); _review = next; _retired = false; _discardApproved = false;
                _selected = next!.SelectedIds.ToHashSet(StringComparer.Ordinal); _baseline = _selected.ToHashSet(StringComparer.Ordinal);
                Profiles.Clear(); foreach (var original in next.Profiles)
                    Profiles.Add(new(original, original.Id == profiles.Current.Id, this, translator));
                SetError(null); Refresh();
            });
        }
        catch (Exception) { _retired = true; if (IsCurrent(current)) SetError("Profile_SourceChanged"); Refresh(); }
    }
    /// <summary>Checks an exact draft identity rather than a translated name.</summary>
    internal bool IsSelected(string id) => _selected.Contains(id);
    /// <summary>Checks only saved original device work availability.</summary>
    internal bool CanUseOriginal(string id) => _review?.CanUse(id) == true;
    [RelayCommand]
    private void Toggle(ProfileRow row)
    {
        if (!CanInteract || !CanChoose || !Profiles.Contains(row)) return;
        _discardApproved = false; if (!_selected.Remove(row.Profile.Id)) _selected.Add(row.Profile.Id); SetError(null); Refresh();
    }
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveChoiceAsync()
    {
        if (!CanSave || _review is null) return; var current = CapturePresentation(); BeginCommand(); var submitted = _selected.ToArray();
        try
        {
            try { profiles.SaveWorkChoice(_review, submitted); }
            catch (Exception) { _retired = true; if (IsCurrent(current)) SetError("Profile_SourceChanged"); return; }
            // Retire before refresh: Retry only reads when a committed choice cannot be published.
            _baseline = submitted.ToHashSet(StringComparer.Ordinal); _discardApproved = false; _retired = true; Refresh();
            if (IsCurrent(current)) await LoadCoreAsync();
        }
        finally { EndCommand(); }
    }
    [RelayCommand]
    private async Task ReloadAsync()
    {
        if (!OwnsPresentation || IsBusy || _commandActive) return; var current = CapturePresentation(); BeginCommand();
        try { if (await ConfirmDiscardCoreAsync(current)) { current(); await LoadCoreAsync(); } }
        catch (Exception) { if (IsCurrent(current)) SetError("Profile_SourceChanged"); }
        finally { EndCommand(); }
    }
    private async Task<bool> ConfirmDiscardCoreAsync(Action current)
    {
        if (!IsDirty) return true;
        var accepted = await interaction.ConfirmAsync(translator["Profile_Title"], translator["ActiveItems_DiscardMessage"],
            translator["ActiveItems_Discard"], translator["Common_Cancel"]);
        current(); if (accepted) _discardApproved = true; return accepted;
    }
    [RelayCommand]
    private async Task LeaveAsync()
    {
        if (!OwnsPresentation || !CanLeave) return; var current = CapturePresentation(); BeginCommand();
        try { if (await ConfirmDiscardCoreAsync(current)) { current(); await interaction.ReturnFromCommandAsync(this, current); } }
        catch (Exception) { if (IsCurrent(current)) SetError("Profile_SourceChanged"); }
        finally { EndCommand(); }
    }
    [RelayCommand]
    private async Task AddAsync()
    {
        if (!Loading.HasLoaded) await LoadAsync();
        if (!CanAdd || _review is null) return; var review = _review; var current = CapturePresentation(); BeginCommand();
        try
        {
            if (!await ConfirmDiscardCoreAsync(current)) return; current();
            var name = await interaction.PromptAsync(translator["Profile_Add"], translator["Profile_NamePrompt"], translator["Common_Save"], translator["Common_Cancel"], maxLength: ProfileService.MaxNameLength);
            current(); profiles.ValidateWorkReview(review); if (string.IsNullOrWhiteSpace(name)) return;
            if (IsTaken(review, name, null)) { SetError("Profile_NameTaken"); return; }
            var original = await profiles.CreateReviewedAsync(review, name, validateOriginal: current);
            _retired = true; current(); Refresh(); await LoadCoreAsync();
            if (_review is null || _retired) return;
            var createdReview = _review;
            if (await interaction.ConfirmAsync(translator["Profile_Add"], translator.Format("Profile_OpenNew", profiles.NameOf(original)), translator["Profile_OpenAction"], translator["Common_Cancel"]))
            { current(); await OpenProfileAsync(createdReview, original, current); }
        }
        catch (Exception) { _retired = true; if (IsCurrent(current)) SetError("Profile_SourceChanged"); }
        finally { EndCommand(); }
    }
    [RelayCommand]
    private async Task ChooseAsync(ProfileRow row)
    {
        if (!CanInteract || _review is null || !Profiles.Contains(row)) return; var review = _review; var current = CapturePresentation(); BeginCommand();
        try
        {
            if (!await ConfirmDiscardCoreAsync(current)) return; current();
            var open = translator["Profile_OpenAction"]; var rename = translator["Profile_Rename"]; var delete = translator["Common_Delete"];
            string[] actions = row.IsCurrent ? [rename] : row.Profile.IsMain ? [open, rename] : [open, rename, delete];
            var choice = await interaction.ChooseAsync(row.Name, translator["Common_Cancel"], actions); current(); profiles.ValidateWorkReview(review);
            if (choice == open) await OpenProfileAsync(review, row.Profile, current);
            else if (choice == rename)
            {
                var name = await interaction.PromptAsync(rename, translator["Profile_NamePrompt"], translator["Common_Save"], translator["Common_Cancel"], initialValue: row.Name, maxLength: ProfileService.MaxNameLength);
                current(); profiles.ValidateWorkReview(review); if (string.IsNullOrWhiteSpace(name)) return;
                if (IsTaken(review, name, row.Profile.Id)) { SetError("Profile_NameTaken"); return; }
                profiles.RenameReviewed(review, row.Profile, name); _retired = true; current(); await LoadCoreAsync();
            }
            else if (choice == delete)
            {
                // Both confirmations cover this exact original; no guessed same-id replacement is deleted.
                if (!await interaction.ConfirmAsync(translator["Profile_Delete"], translator.Format("Profile_DeleteMessage", row.Name), delete, translator["Common_Cancel"])) return;
                current(); profiles.ValidateWorkReview(review);
                if (!await interaction.ConfirmAsync(translator["Profile_Delete"], translator["Profile_DeleteFinal"], delete, translator["Common_Cancel"])) return;
                current(); await profiles.DeleteReviewedAsync(review, row.Profile, validateOriginal: current);
                _retired = true; current(); await LoadCoreAsync();
            }
        }
        catch (Exception) { _retired = true; if (IsCurrent(current)) SetError("Profile_SourceChanged"); }
        finally { EndCommand(); }
    }
    private bool IsTaken(ProfileCatalogueReview review, string name, string? exceptId)
    {
        var wanted = Vafadar.Core.Text.SearchText.Normalize(name);
        return review.Profiles.Any(p => p.Id != exceptId && string.Equals(Vafadar.Core.Text.SearchText.Normalize(profiles.NameOf(p)), wanted, StringComparison.CurrentCultureIgnoreCase));
    }
    private async Task OpenProfileAsync(ProfileCatalogueReview review, LocalProfile original, Action current)
    {
        SetError(null);
        current(); if (!await profiles.SwitchReviewedAsync(review, original, current)) { current(); SetError("Profile_Locked"); return; }
        _retired = true; current(); Refresh(); host.ShowCurrentProfile();
    }
    /// <summary>Refreshes captions/digits without replacing originals or saving choices.</summary>
    public void RefreshDisplay() { _display++; SetError(_errorKey); Refresh(); }
    // Native consent owns departure independently of shared status feedback (D-658).
    private void BeginCommand() { _commandActive = true; IsBusy = true; Refresh(); }
    private void EndCommand() { _commandActive = false; IsBusy = false; Refresh(); }
    private void SetError(string? key) { _errorKey = key; Error = key is null ? null : translator[key]; }
    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    { base.OnPropertyChanged(e); if (e.PropertyName == nameof(IsBusy)) Refresh(); }
    private void Refresh()
    {
        foreach (var property in new[] { nameof(CanInteract), nameof(CanChoose), nameof(CanSave), nameof(CanAdd), nameof(OverCapacity), nameof(IsDirty), nameof(CanLeave), nameof(AllowanceText), nameof(Counter), nameof(CapacityError) }) OnPropertyChanged(property);
        foreach (var row in Profiles) row.Refresh(); SaveChoiceCommand.NotifyCanExecuteChanged();
    }
}
