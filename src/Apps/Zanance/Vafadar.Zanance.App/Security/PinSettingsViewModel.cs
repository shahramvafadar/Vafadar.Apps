using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Core.Text;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Security;

/// <summary>Sets, changes or removes the device-wide app PIN; never persists entered text (D-63).</summary>
public sealed partial class PinSettingsViewModel(AppLockService appLock, Translator translator) : ObservableObject
{
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
        if (IsBusy) { return; }
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
        IsBusy = true;
        try
        {
            var result = remove ? await appLock.Pin.RemoveAsync(current) : await appLock.Pin.SetAsync(current, next);
            if (result.Outcome == PinOutcome.Success)
            {
                Clear();
                await CloseAsync();
            }
            else
            {
                CurrentPin = string.Empty;
                CurrentError = result.Outcome == PinOutcome.Delayed
                    ? translator.Format("Pin_Wait", (int)Math.Ceiling(result.Wait.TotalSeconds)) : translator["Pin_Incorrect"];
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Clear();
            PageError = translator["Pin_Unavailable"];
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (!IsBusy) { Clear(); await CloseAsync(); }
    }

    /// <summary>Removes all entered PIN text when the page closes.</summary>
    public void Clear() => CurrentPin = NewPin = ConfirmPin = string.Empty;
}
