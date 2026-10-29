namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>Headless query contract; original native Shell routing is qualified independently.</summary>
internal interface IQueryAttributable
{
    /// <summary>Delivers the original query without inventing native identity.</summary>
    void ApplyQueryAttributes(IDictionary<string, object> query);
}

/// <summary>Explicit asynchronous close boundary for original model checks, not a native navigation result.</summary>
internal sealed class Shell
{
    private static readonly AsyncLocal<Shell?> Slot = new();
    /// <summary>Gets or sets only this logical check's routing port.</summary>
    public static Shell Current { get => Slot.Value ?? throw new InvalidOperationException("An explicit original routing port is required."); set => Slot.Value = value; }
    /// <summary>Gets or sets the close effect supplied by the check.</summary>
    internal Func<string, Task> Navigate { get; init; } = _ => throw new NotSupportedException("No implicit native navigation.");
    /// <summary>Awaits the explicitly supplied original close boundary.</summary>
    public Task GoToAsync(string route) => Navigate(route);
}

/// <summary>Headless close boundary supplied through the explicit logical routing port, not a native result.</summary>
internal sealed class MauiKpiSheetInteraction : IKpiSheetInteraction
{
    /// <inheritdoc />
    public Task CloseAsync() => Shell.Current.GoToAsync("..");
}
