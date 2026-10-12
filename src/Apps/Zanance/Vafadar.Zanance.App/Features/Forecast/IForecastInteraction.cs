namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>Original native prompts/menus/consent/navigation for forecast drafts, separate from financial algorithms.</summary>
public interface IForecastInteraction
{
    /// <summary>Requests the original name/date/amount input with its established length and keyboard policy.</summary>
    Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength, bool numeric = false);
    /// <summary>Offers the original row actions without changing a scenario on cancellation.</summary>
    Task<string?> ChooseAsync(string title, string cancel, params string[] actions);
    /// <summary>Requests explicit original deletion consent.</summary>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
    /// <summary>Opens the existing route without creating financial rows.</summary>
    Task NavigateAsync(string route, IDictionary<string, object>? query = null);
}
