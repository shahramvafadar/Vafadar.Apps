using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Keeps startup covered through preparation failures and permits only one explicit retry at a time.</summary>
public partial class StartupViewModel(Func<Task> prepare) : ObservableObject
{
    private bool running;
    private bool completed;

    /// <summary>Gets whether preparation is running or the first startup attempt has not begun yet.</summary>
    [ObservableProperty] public partial bool IsBusy { get; set; } = true;

    /// <summary>Gets whether the generic retry state is visible, without exposing exception details or credentials.</summary>
    [ObservableProperty] public partial bool HasFailed { get; set; }

    /// <summary>Prepares once; a failure leaves content covered and requires a user retry.</summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (running || completed) return;
        running = true; IsBusy = true; HasFailed = false;
        try { await prepare(); completed = true; }
        catch (Exception error) when (error is not OutOfMemoryException) { HasFailed = true; }
        finally { running = false; IsBusy = false; }
    }
}
