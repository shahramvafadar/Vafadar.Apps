namespace Vafadar.Zanance.App.Interaction;

/// <summary>Platform dialogs and navigation used by application flows; no view or global window is required by the caller.</summary>
public interface IAppInteraction
{
    /// <summary>Shows information beside the active application flow.</summary>
    Task AlertAsync(string title, string message, string cancel);

    /// <summary>Requests an explicit confirmation; cancellation is false.</summary>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);

    /// <summary>Requests text; cancellation produces no input.</summary>
    Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1);

    /// <summary>Offers the supplied actions; cancellation produces no action.</summary>
    Task<string?> ChooseAsync(string title, string cancel, params string[] actions);

    /// <summary>Opens a registered application route without posting financial data.</summary>
    Task NavigateAsync(string route, IDictionary<string, object>? parameters = null);

    /// <summary>Reports a failed operation while preserving its form.</summary>
    Task ShowFailureAsync(Exception exception);
}

/// <summary>Window transitions at the end of profile/onboarding flows.</summary>
public interface IAppFlowHost
{
    /// <summary>Opens first-run restore without creating an account or committing the draft preferences.</summary>
    Task RestoreOnboardingAsync();

    /// <summary>Completes onboarding after layout work has finished.</summary>
    void CompleteOnboarding();

    /// <summary>Rebuilds the window for the successfully selected profile.</summary>
    void ShowCurrentProfile();
}
