namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>Connects original native query delivery to the actual platform-independent holding editor.</summary>
public sealed partial class AssetEventEditorViewModel : IQueryAttributable
{
    /// <inheritdoc />
    public async void ApplyQueryAttributes(IDictionary<string, object> query) =>
        await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));
}
