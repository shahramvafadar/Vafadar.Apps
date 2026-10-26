using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Keeps startup covered through preparation failures and permits only one explicit retry at a time.</summary>
public partial class StartupViewModel(Func<Task> prepare, Func<Task<bool>>? recover = null, Func<Task<bool>>? restoreBackup = null) : ObservableObject
{
    private bool running;
    private bool completed;

    /// <summary>Gets whether preparation is running or the first startup attempt has not begun yet.</summary>
    [ObservableProperty] public partial bool IsBusy { get; set; } = true;

    /// <summary>Gets whether the generic retry state is visible, without exposing exception details or credentials.</summary>
    [ObservableProperty] public partial bool HasFailed { get; set; }

    /// <summary>Gets whether this platform offers explicit retained-key recovery on a covered failure.</summary>
    public bool RecoveryAvailable => recover is not null;

    /// <summary>Gets whether covered startup offers an explicit portable backup recovery flow.</summary>
    public bool BackupRecoveryAvailable => restoreBackup is not null;

    /// <summary>Runs confirmed backup recovery while financial content and ordinary startup remain covered.</summary>
    [RelayCommand]
    private async Task RestoreBackupAsync() => await RunRecoveryAsync(restoreBackup);

    /// <summary>Requests explicit recovery without exposing financial content or bypassing the separate access gate.</summary>
    [RelayCommand]
    private async Task RecoverAsync() => await RunRecoveryAsync(recover);

    private async Task RunRecoveryAsync(Func<Task<bool>>? action)
    {
        if (running || completed || !HasFailed || action is null) return;
        running = true; IsBusy = true; HasFailed = false;
        var restored = false;
        try { restored = await action(); }
        catch (Exception error) when (error is not OutOfMemoryException) { }
        finally { running = false; IsBusy = false; HasFailed = true; }
        if (restored) await InitializeAsync();
    }

    /// <summary>Prepares once; a failure leaves content covered and requires a user retry.</summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (running || completed) return;
        running = true; IsBusy = true; HasFailed = false;
        try { await prepare(); completed = true; }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            HasFailed = true;
            ObservePreparationFailure(error);
        }
        finally { running = false; IsBusy = false; }
    }

    /// <summary>Optional native-effect observation; absent in shipping builds, where the call is elided.</summary>
    partial void ObservePreparationFailure(Exception error);
}
