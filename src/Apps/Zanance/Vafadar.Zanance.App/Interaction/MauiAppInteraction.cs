using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Interaction;

/// <summary>Uses the active MAUI page for dialogs and the existing Shell routes for navigation.</summary>
internal sealed class MauiAppInteraction : IAppInteraction, IAppFlowHost
{
    /// <inheritdoc />
    public bool HouseholdRecoveryFilesAvailable => HouseholdRecoveryFiles.Available;
    /// <inheritdoc />
    public Task<byte[]?> PickHouseholdRecoveryAsync(string title, CancellationToken ct) => HouseholdRecoveryFiles.PickAsync(title, ct);
    /// <inheritdoc />
    public Task<bool> SaveHouseholdRecoveryAsync(string fileName, byte[] encrypted, CancellationToken ct) => HouseholdRecoveryFiles.SaveAsync(fileName, encrypted, ct);
    private static Page Page
    {
        get
        {
            var root = Application.Current?.Windows.FirstOrDefault()?.Page
                ?? throw new InvalidOperationException("The application window is not available.");
            return root.Navigation.ModalStack.LastOrDefault() ?? (root as Shell)?.CurrentPage ?? root;
        }
    }

    /// <inheritdoc />
    public Task AlertAsync(string title, string message, string cancel) => Page.DisplayAlertAsync(title, message, cancel);

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = Page;
#if WINDOWS
        return WindowsDialogTargets.ShowAsync(page, () => page.DisplayAlertAsync(title, message, accept, cancel));
#else
        return page.DisplayAlertAsync(title, message, accept, cancel);
#endif
    }

#if DEBUG && WINDOWS
    /// <summary>One public fictitious fingerprint input, never an answer or authority; the actual native prompt still requires explicit consent.</summary>
    internal (string Title, string Message, string Fingerprint)? ReviewPublicDevicePrompt { get; set; }
#endif
    /// <inheritdoc />
    public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1)
    {
#if DEBUG && WINDOWS
        // WinUI's native TextBox Value provider is not a projected managed
        // IValueProvider. Supply only this independently reviewed fictitious
        // public input through the existing initialValue API; retain the real
        // prompt and native Invoke confirmation. No desktop input or focus.
        if (ReviewPublicDevicePrompt is { } fixture && AppInfo.Current.PackageName == "pro.vafadar.zanance.householdlifecycle" &&
            FileSystem.AppDataDirectory.Contains("pro.vafadar.zanance.householdlifecycle", StringComparison.OrdinalIgnoreCase) &&
            initialValue is null && maxLength == 64 && title == fixture.Title && message == fixture.Message &&
            fixture.Fingerprint.Length == 64 && fixture.Fingerprint.All(Uri.IsHexDigit))
        { initialValue = fixture.Fingerprint; ReviewPublicDevicePrompt = null; }
#endif
        var page = Page;
#if WINDOWS
        return WindowsDialogTargets.ShowAsync(page, () => page.DisplayPromptAsync(title, message, accept, cancel, initialValue: initialValue, maxLength: maxLength));
#else
        return page.DisplayPromptAsync(title, message, accept, cancel, initialValue: initialValue, maxLength: maxLength);
#endif
    }

    /// <inheritdoc />
    public Task<string?> ChooseAsync(string title, string cancel, params string[] actions)
    {
        var page = Page;
#if WINDOWS
        return WindowsDialogTargets.ShowAsync(page, () => page.DisplayActionSheetAsync(title, cancel, null, actions));
#else
        return page.DisplayActionSheetAsync(title, cancel, null, actions);
#endif
    }

    /// <inheritdoc />
    public async Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (parameters is null) await Shell.Current.GoToAsync(route);
        else await Shell.Current.GoToAsync(route, parameters);
        // D-151: Shell applies a root tab's new query without Appearing when that tab is already visible.
        // Complete the exact destination after navigation rather than relying on a lifecycle reload.
        if (route == AppRoutes.BudgetRoute && parameters is not null
            && parameters.TryGetValue("budgetReminder", out var value) && value is Core.Reminders.BudgetReminderLink reminder)
        {
            if (Shell.Current.CurrentPage.BindingContext is not Features.Budget.BudgetViewModel budget)
                throw new InvalidOperationException("The original budget destination is not available.");
            await budget.ShowReminderAsync(reminder);
        }
    }

    /// <inheritdoc />
    public Task ShowFailureAsync(Exception exception) => Failures.ShowAsync(exception);

    /// <inheritdoc />
    public async Task ReturnFromCommandAsync(object originalOwner, Action validateOriginal)
    {
        validateOriginal(); var page = Page;
        if (Shell.Current is not AppShell root || !ReferenceEquals(page.BindingContext, originalOwner))
            throw new OperationCanceledException("The original native command owner changed.");
        using var scope = OwnedCommandReturn.Begin(root, page, originalOwner, validateOriginal);
        await root.GoToAsync(".."); scope.RequireReturned();
    }

    /// <inheritdoc />
    public Task RestoreOnboardingAsync() => Application.Current is App app ? app.ShowOnboardingRestoreAsync() : Task.CompletedTask;
    /// <inheritdoc />
    public bool PersonalSyncAvailable => Features.Sync.PersonalSyncBuild.IsAvailable;
    /// <inheritdoc />
    public Task RestoreSyncedOnboardingAsync() => Application.Current is App app ? app.ShowOnboardingSyncAsync() : Task.CompletedTask;
    /// <inheritdoc />
    public Task OpenHouseholdOnboardingAsync() => Application.Current is App app ? app.ShowOnboardingHouseholdAsync() : Task.CompletedTask;

    /// <inheritdoc />
    public void CompleteOnboarding()
    {
        // D-62: disconnecting the Windows form during its layout update can invalidate the visual tree.
        if (Application.Current is App app) { app.Dispatcher.Dispatch(app.ShowMainShell); }
    }

    /// <inheritdoc />
    public void ShowCurrentProfile() => (Application.Current as App)?.ShowCurrentProfile();
}
