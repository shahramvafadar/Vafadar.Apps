namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>Preserves actual Shell prompt keyboards, menus, consent and original route delivery.</summary>
internal sealed class MauiForecastInteraction : IForecastInteraction
{
    /// <inheritdoc />
    public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength, bool numeric = false) =>
        Shell.Current.DisplayPromptAsync(title, message, accept, cancel, initialValue: initialValue, maxLength: maxLength,
            keyboard: numeric ? Keyboard.Numeric : Keyboard.Default);
    /// <inheritdoc />
    public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => Shell.Current.DisplayActionSheetAsync(title, cancel, null, actions);
    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    /// <inheritdoc />
    public Task NavigateAsync(string route, IDictionary<string, object>? query = null) => query is null
        ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, query);
}
