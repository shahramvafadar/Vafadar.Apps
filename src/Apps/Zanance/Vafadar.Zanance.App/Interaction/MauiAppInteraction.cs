using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Interaction;

/// <summary>Uses the active MAUI page for dialogs and the existing Shell routes for navigation.</summary>
internal sealed class MauiAppInteraction : IAppInteraction, IAppFlowHost
{
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
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Page.DisplayAlertAsync(title, message, accept, cancel);

    /// <inheritdoc />
    public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) =>
        Page.DisplayPromptAsync(title, message, accept, cancel, initialValue: initialValue, maxLength: maxLength);

    /// <inheritdoc />
    public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => Page.DisplayActionSheetAsync(title, cancel, null, actions);

    /// <inheritdoc />
    public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => parameters is null
        ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, parameters);

    /// <inheritdoc />
    public Task ShowFailureAsync(Exception exception) => Failures.ShowAsync(exception);

    /// <inheritdoc />
    public Task RestoreOnboardingAsync() => Application.Current is App app ? app.ShowOnboardingRestoreAsync() : Task.CompletedTask;

    /// <inheritdoc />
    public void CompleteOnboarding()
    {
        // D-62: disconnecting the Windows form during its layout update can invalidate the visual tree.
        if (Application.Current is App app) { app.Dispatcher.Dispatch(app.ShowMainShell); }
    }

    /// <inheritdoc />
    public void ShowCurrentProfile() => (Application.Current as App)?.ShowCurrentProfile();
}
