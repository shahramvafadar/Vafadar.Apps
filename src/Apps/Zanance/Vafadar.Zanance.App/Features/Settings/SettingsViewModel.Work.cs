using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Settings;

public sealed partial class SettingsViewModel
{
    private bool _active = true, _rootRetired;
    private Func<bool>? _rootOwner;
    private CancellationTokenSource _frame = new(), _read = new();
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private long _readRevision;
    private string? _originalFile;
    private int _pendingChanges;
    private bool _needsExplicitReload;
    private string? _publishedReminderDays, _feedbackKey;
    private ProfileResetReview? _acceptedReset;
    /// <summary>Gets current native input authority independently of stale editable controls.</summary>
    public bool CanInteract => OwnsPage && Loading.IsReady && !IsBusy && !_lock.IsLocked && !_lock.PinUnavailable && _acceptedReset is null;
    private bool OwnsPage => _active && !_rootRetired && (_rootOwner?.Invoke() ?? true);
    /// <summary>Gets current translated action feedback independently of the essential-estimate field error.</summary>
    [ObservableProperty] public partial string? ActionFeedback { get; private set; }
    /// <summary>Binds the actual original root without retaining a replacement root.</summary>
    internal void BindRootOwner(Func<bool> owner) { _rootOwner = owner ?? throw new ArgumentNullException(nameof(owner)); PublishWorkState(); }
    /// <summary>Retires original reads, queued preference changes and native consent continuations on actual departure.</summary>
    public void SetActive(bool active)
    {
        if (_active == active) return; _active = active;
        var old = _frame; _frame = new(); old.Cancel(); old.Dispose(); RetireRead(); PublishWorkState();
    }
    /// <summary>Permanently retires commands of a replaced application root.</summary>
    internal void RetireRoot() { _rootRetired = true; SetActive(false); PublishWorkState(); }
    private void RetireRead()
    {
        ++_readRevision; var old = _read; _read = CancellationTokenSource.CreateLinkedTokenSource(_frame.Token);
        Loading = new(); old.Cancel(); old.Dispose(); PublishWorkState();
    }
    private void PublishWorkState()
    { OnPropertyChanged(nameof(CanInteract)); DeleteAllDataCommand.NotifyCanExecuteChanged(); }
    private SettingsCommand? CaptureCommand()
    {
        if (!CanInteract || _snapshot?.WorkReview is not { } review) return null;
        var token = _frame.Token; var profile = _profiles.Current.Id;
        return new(review, token, () => OwnsPage && !_lock.IsLocked && !_lock.PinUnavailable && !token.IsCancellationRequested && profile == _profiles.Current.Id
            && _originalFile == review.DatabasePath && _acceptedReset is null);
    }
    private sealed record SettingsCommand(SettingsWorkSnapshot Review, CancellationToken Token, Func<bool> Current)
    {
        internal bool IsCurrent => !Token.IsCancellationRequested && Current();
        internal void RequireCurrent() { Token.ThrowIfCancellationRequested(); if (!IsCurrent) throw new InvalidOperationException("The original settings page retired."); }
    }
    private async Task ValidateCommandAsync(SettingsCommand command)
    {
        command.RequireCurrent(); await _store.ValidateSettingsWorkSnapshotAsync(command.Review, command.Token); command.RequireCurrent();
        if (!ReferenceEquals(_snapshot?.WorkReview, command.Review) || command.Review.Calendar != Presentation.Calendars.ToPeriod(_localization.CurrentCalendar))
            throw new InvalidOperationException("The original settings publication or calendar changed.");
    }
    private void Feedback(string key) { _feedbackKey = key; ActionFeedback = _translator[key]; }
    private async Task ReadOriginalAsync()
    {
        if (!OwnsPage) return;
        if (_acceptedReset is not null) { await FinishAcceptedResetAsync(); return; }
        RetireRead(); var revision = _readRevision; var token = _read.Token; var state = Loading;
        var entered = false;
        try
        {
            await _loadGate.WaitAsync(token); entered = true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
        try
        {
            bool Current() => OwnsPage && revision == _readRevision && ReferenceEquals(state, Loading) && !token.IsCancellationRequested;
            SettingsSnapshot? next = null;
            await state.RunAsync(async () =>
            {
                next = await SettingsSnapshot.ReadAsync(_store, _localization, _time,
                    _lock.Authenticator.IsAvailableAsync, NotificationsSupported, _reminders.Scheduler.AreEnabledAsync, token, path =>
                    { if (!Current()) return false; _originalFile ??= path; return _originalFile == path; });
                token.ThrowIfCancellationRequested();
                _originalFile ??= next.WorkReview.DatabasePath;
                if (_originalFile != next.WorkReview.DatabasePath) throw new InvalidOperationException("The original settings file changed.");
            }, () =>
            {
                if (!Current()) throw new OperationCanceledException(token);
                _snapshot = next; _needsExplicitReload = false;
                if (_feedbackKey == "Settings_SourceChanged") { _feedbackKey = null; ActionFeedback = null; }
                PresentSnapshot();
            });
        }
        catch (OperationCanceledException) when (!OwnsPage || token.IsCancellationRequested) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsPage && revision == _readRevision) { _needsExplicitReload = true; Feedback("Settings_SourceChanged"); } }
        finally { if (entered) _loadGate.Release(); PublishWorkState(); }
    }
    private async Task RefuseOriginalSourceAsync(Exception exception)
    {
        if (!OwnsPage) return;
        // A current source refusal retires all previously captured native effects; Retry does not replay their writes.
        var old = _frame; _frame = new(); old.Cancel(); old.Dispose(); RetireRead(); _needsExplicitReload = true;
        try { await Loading.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception observed) when (ReferenceEquals(observed, exception)) { }
        Feedback("Settings_SourceChanged"); PublishWorkState();
    }
    private async Task ChangeOriginalAsync(Action<Core.Settings.ZananceSettings> change)
    {
        if (CaptureCommand() is not { } command) return; ++_pendingChanges;
        try { await _store.UpdateSettingsWorkAsync(command.Review, change, () => command.IsCurrent, command.Token); }
        catch (CommittedWriteNotificationException<Guid>) { if (command.IsCurrent) Feedback("Settings_SavedFollowUpFailed"); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (command.IsCurrent) await RefuseOriginalSourceAsync(exception); }
        finally { if (--_pendingChanges == 0 && OwnsPage && !_needsExplicitReload) await LoadAsync(); }
    }
    private async Task NavigateOriginalAsync(string route)
    {
        if (CaptureCommand() is not { } command) return; IsBusy = true;
        try { await ValidateCommandAsync(command); await _interaction.NavigateAsync(route); }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (command.IsCurrent) await RefuseOriginalSourceAsync(exception); }
        finally { IsBusy = false; PublishWorkState(); }
    }
    /// <summary>Opens the original explicit recovery workflow only from the current source-bound native settings page.</summary>
    internal async Task OpenRecoverySetupAsync()
    {
        if (CaptureCommand() is not { } command) return; IsBusy = true;
        try { await ValidateCommandAsync(command); await ((App)Application.Current!).ShowRecoverySetupAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (command.IsCurrent) await RefuseOriginalSourceAsync(exception); }
        finally { IsBusy = false; PublishWorkState(); }
    }
    private bool _resetCacheCleared, _resetRemindersRefreshed;
    private Task? _resetFollowThrough;
    private Task FinishAcceptedResetAsync()
    {
        if (_resetFollowThrough is { IsCompleted: false } pending) return pending;
        return _resetFollowThrough = FinishAcceptedResetCoreAsync();
    }
    private async Task FinishAcceptedResetCoreAsync()
    {
        if (_acceptedReset is not { } receipt || !OwnsPage || _profiles.Current.Id != _profilesAtReset) return;
        var token = _frame.Token;
        bool Current() => OwnsPage && !token.IsCancellationRequested && _profiles.Current.Id == _profilesAtReset;
        try
        {
            // A rare interrupted follow-through may resume without repeating the committed destructive writer.
            var current = await _store.ReadSettingsWorkSnapshotAsync(Presentation.Calendars.ToPeriod(_localization.CurrentCalendar),
                path => Current() && path == receipt.DatabasePath, token);
            if (!Current()) return;
            if (current.Accounts.Count != 0 || current.Entries.Count != 0) throw new InvalidOperationException("New profile data must not be removed by old accepted follow-through.");
            if (!_resetCacheCleared) { Presentation.AttachmentFiles.ClearCache(); _resetCacheCleared = true; }
            if (!_resetRemindersRefreshed) { await _reminders.RefreshAsync(); _resetRemindersRefreshed = true; }
            if (!Current()) return;
            Presentation.DisplayUnitPreferences.Apply(string.Empty); await _lock.ReloadAsync();
            if (Current()) (Application.Current as App)?.ShowOnboarding();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) Feedback("Settings_DataDeleted"); }
    }

    private Core.Budgets.PeriodCalendar? _calendarReloadRequested;
    private void RequestCalendarSourceRefresh()
    {
        var calendar = Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);
        if (!OwnsPage || _snapshot is null || _snapshot.WorkReview.Calendar == calendar || _calendarReloadRequested == calendar) return;
        _calendarReloadRequested = calendar; _essentialSuggestion = null; EssentialSuggestionText = null;
        _ = LoadAsync();
    }

}
