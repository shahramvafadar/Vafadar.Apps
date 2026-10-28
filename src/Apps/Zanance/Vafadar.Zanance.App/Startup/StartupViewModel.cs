using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Keeps startup covered through preparation failures and permits only one explicit retry at a time.</summary>
public partial class StartupViewModel(Func<Task> prepare, Func<Task<bool>>? recover = null, Func<Task<bool>>? restoreBackup = null) : ObservableObject
{
    private bool running;
    private bool completed;

    /// <summary>Gets whether preparation is running or the first startup attempt has not begun yet.</summary>
    [ObservableProperty] public partial bool IsBusy { get; set; } = true;

    /// <summary>Gets whether a covered retry state is visible, without exposing exception details or credentials.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowGenericFailure), nameof(ShowStorageFailure))]
    public partial bool HasFailed { get; set; }

    /// <summary>Gets whether the actual bounded-space guard refused preparation; no error text is inspected.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowGenericFailure), nameof(ShowStorageFailure),
        nameof(RecoveryAvailable), nameof(BackupRecoveryAvailable))]
    public partial bool HasInsufficientStorage { get; set; }

    /// <summary>Gets whether the original generic failure guidance applies.</summary>
    public bool ShowGenericFailure => HasFailed && !HasInsufficientStorage;

    /// <summary>Gets whether the safe storage guidance applies while all financial content remains covered.</summary>
    public bool ShowStorageFailure => HasFailed && HasInsufficientStorage;

    /// <summary>Gets whether this platform offers explicit retained-key recovery on a covered failure.</summary>
    public bool RecoveryAvailable => recover is not null && !HasInsufficientStorage;

    /// <summary>Gets whether covered startup offers an explicit portable backup recovery flow.</summary>
    public bool BackupRecoveryAvailable => restoreBackup is not null && !HasInsufficientStorage;

    /// <summary>Runs confirmed backup recovery while financial content and ordinary startup remain covered.</summary>
    [RelayCommand]
    private async Task RestoreBackupAsync() => await RunRecoveryAsync(restoreBackup);

    /// <summary>Requests explicit recovery without exposing financial content or bypassing the separate access gate.</summary>
    [RelayCommand]
    private async Task RecoverAsync() => await RunRecoveryAsync(recover);

    private async Task RunRecoveryAsync(Func<Task<bool>>? action)
    {
        // D-606: known low space needs the original retry after storage is freed, not credential recovery.
        if (running || completed || !HasFailed || HasInsufficientStorage || action is null) return;
        running = true; IsBusy = true; HasFailed = false;
        var restored = false;
        try { restored = await action(); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            HasInsufficientStorage = error is InsufficientDatabaseStorageException;
        }
        finally { running = false; IsBusy = false; HasFailed = true; }
        if (restored) await InitializeAsync();
    }

    /// <summary>Prepares once; a failure leaves content covered and requires a user retry.</summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (running || completed) return;
        running = true; IsBusy = true; HasFailed = false; HasInsufficientStorage = false;
        try { await prepare(); completed = true; }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            HasInsufficientStorage = error is InsufficientDatabaseStorageException;
            HasFailed = true;
            ObservePreparationFailure(error);
        }
        finally { running = false; IsBusy = false; }
    }

    /// <summary>Optional native-effect observation; absent in shipping builds, where the call is elided.</summary>
    partial void ObservePreparationFailure(Exception error);
}
