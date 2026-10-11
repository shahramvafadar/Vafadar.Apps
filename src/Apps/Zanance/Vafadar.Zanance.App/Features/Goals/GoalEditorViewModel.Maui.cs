namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Connects native navigation to the covered, platform-independent goal editor.</summary>
public sealed partial class GoalEditorViewModel : IQueryAttributable
{
    /// <inheritdoc />
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try { await ApplyQueryAsync(query); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { await _interaction.ShowFailureAsync(exception); }
    }
}
