namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>Headless contract for original query delivery; actual native routing is qualified separately.</summary>
internal interface IQueryAttributable
{
    /// <summary>Passes the original requested identity unchanged.</summary>
    void ApplyQueryAttributes(IDictionary<string, object> query);
}

/// <summary>Refuses implicit native operations in headless checks; every test supplies its explicit interaction port.</summary>
internal sealed class MauiForecastInteraction : IForecastInteraction
{
    /// <inheritdoc />
    public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength, bool numeric = false) => throw new NotSupportedException("An explicit forecast interaction is required.");
    /// <inheritdoc />
    public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new NotSupportedException("An explicit forecast interaction is required.");
    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new NotSupportedException("An explicit forecast interaction is required.");
    /// <inheritdoc />
    public Task NavigateAsync(string route, IDictionary<string, object>? query = null) => throw new NotSupportedException("An explicit forecast interaction is required.");
}
