using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Core.Text;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Security;

/// <summary>Sets, changes or removes the device-wide app PIN; never persists entered text (D-63).</summary>
public sealed partial class PinSettingsViewModel(AppLockService appLock, Translator translator) : ObservableObject
{
    private bool _working;
    private bool _closing;
    private bool _active = true;
    private long _lifetime;
    private long _inputVersion;
    private long? _acceptedVersion;

    /// <summary>Checks the actual original modal and window root, independently of feedback.</summary>
    public Func<bool> IsCurrent { get; set; } = () => true;

    /// <summary>Activates a newly shown original form without reviving an older continuation.</summary>
    public void Activate() { _active = true; _lifetime++; OnPropertyChanged(nameof(HasPin)); }

    /// <summary>Retires outstanding original ownership and removes editable secret text.</summary>
    public void Deactivate() { _active = false; _lifetime++; Clear(); }

    partial void OnCurrentPinChanged(string value) => _inputVersion++;
    partial void OnNewPinChanged(string value) => _inputVersion++;
    partial void OnConfirmPinChanged(string value) => _inputVersion++;

    /// <summary>Gets whether a current PIN must be supplied.</summary>
    public bool HasPin => appLock.PinEnabled;
    /// <summary>Gets or sets the current PIN, held only while this form is open.</summary>
    [ObservableProperty] public partial string CurrentPin { get; set; } = string.Empty;
    /// <summary>Gets or sets the new PIN.</summary>
    [ObservableProperty] public partial string NewPin { get; set; } = string.Empty;
    /// <summary>Gets or sets the repeated new PIN.</summary>
    [ObservableProperty] public partial string ConfirmPin { get; set; } = string.Empty;
    /// <summary>Gets or sets the current PIN error.</summary>
    [ObservableProperty] public partial string? CurrentError { get; set; }
    /// <summary>Gets or sets the new PIN error.</summary>
    [ObservableProperty] public partial string? NewError { get; set; }
    /// <summary>Gets or sets the confirmation error.</summary>
    [ObservableProperty] public partial string? ConfirmError { get; set; }
    /// <summary>Gets or sets a storage failure message without exception details.</summary>
    [ObservableProperty] public partial string? PageError { get; set; }
    /// <summary>Gets or sets whether a protected storage operation is running.</summary>
    [ObservableProperty] public partial bool IsBusy { get; set; }
    /// <summary>Gets or sets the page's close callback.</summary>
    public Func<Task> CloseAsync { get; set; } = () => Task.CompletedTask;

    [RelayCommand] private Task SaveAsync() => SubmitAsync(remove: false);
    [RelayCommand] private Task RemoveAsync() => SubmitAsync(remove: true);

    private async Task SubmitAsync(bool remove)
    {
        if (_working || _closing || !_active || !IsCurrent()) { return; }
        // D-667: a known returned write is never retried because modal follow-through failed.
        if (_acceptedVersion == _inputVersion) { await CloseAcceptedAsync(_lifetime); return; }
        CurrentError = NewError = ConfirmError = PageError = null;
        var current = Digits.ToAscii(CurrentPin);
        var next = Digits.ToAscii(NewPin);
        var confirm = Digits.ToAscii(ConfirmPin);
        if (HasPin && !PinLock.IsValid(current)) { CurrentError = translator["Pin_FourDigits"]; }
        if (!remove)
        {
            if (!PinLock.IsValid(next)) { NewError = translator["Pin_FourDigits"]; }
            if (!PinLock.IsValid(confirm) || confirm != next) { ConfirmError = translator["Pin_Mismatch"]; }
        }

        if (CurrentError is not null || NewError is not null || ConfirmError is not null) { return; }
        var lifetime = _lifetime;
        Clear();
        var submittedVersion = _inputVersion;
        _working = true;
        IsBusy = true;
        void Current()
        {
            if (!_active || lifetime != _lifetime || !IsCurrent())
                throw new InvalidOperationException("The original PIN settings form retired.");
        }
        try
        {
            var result = remove ? await appLock.Pin.RemoveAsync(current, Current) : await appLock.Pin.SetAsync(current, next, Current);
            if (result.Outcome == PinOutcome.Success)
            {
                // Record only the original returned commit, before fallible navigation.
                _acceptedVersion = submittedVersion;
                OnPropertyChanged(nameof(HasPin));
                if (_active && lifetime == _lifetime && IsCurrent() && submittedVersion == _inputVersion)
                    await CloseAcceptedAsync(lifetime);
            }
            else
            {
                if (!_active || lifetime != _lifetime || !IsCurrent() || submittedVersion != _inputVersion) return;
                CurrentError = result.Outcome == PinOutcome.Delayed
                    ? translator.Format("Pin_Wait", (int)Math.Ceiling(result.Wait.TotalSeconds)) : translator["Pin_Incorrect"];
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (_active && lifetime == _lifetime && IsCurrent()) PageError = translator["Pin_Unavailable"];
        }
        finally { _working = false; IsBusy = false; }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (!_working && !_closing && _active && IsCurrent()) { Clear(); await CloseAcceptedAsync(_lifetime); }
    }

    private async Task CloseAcceptedAsync(long lifetime)
    {
        if (_closing || !_active || lifetime != _lifetime || !IsCurrent()) return;
        // Navigation remains owned even when this is only a known accepted-result follow-through.
        _closing = true;
        try { await CloseAsync(); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (_active && lifetime == _lifetime && IsCurrent())
                PageError = translator[_acceptedVersion == _inputVersion ? "ActiveItems_SavedReturnFailed" : "Error_Unexpected"];
        }
        finally { _closing = false; }
    }

    /// <summary>Removes all entered PIN text when the page closes.</summary>
    public void Clear() => CurrentPin = NewPin = ConfirmPin = string.Empty;
}
