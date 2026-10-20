namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>Connects actual native query delivery to the original platform-independent type editor.</summary>
public sealed partial class AssetTypeEditorViewModel : IQueryAttributable
{
    /// <inheritdoc />
    public async void ApplyQueryAttributes(IDictionary<string, object> query) =>
        await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));
}
