namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Connects original native routing to the portable retained-bill application lifetime.</summary>
public sealed partial class SettlementViewModel : IQueryAttributable
{
    /// <inheritdoc />
    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(async () =>
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue("id", out var value) || value is not Guid id) return;
        query.Clear(); await LoadAsync(id);
    });
}
