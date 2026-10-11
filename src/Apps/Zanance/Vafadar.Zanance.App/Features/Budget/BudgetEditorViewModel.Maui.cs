using CommunityToolkit.Mvvm.ComponentModel;

namespace Vafadar.Zanance.App.Features.Budget;

public sealed partial class LimitInput
{
    /// <summary>Gets the theme-aware native category color; application drafts never depend on native graphics.</summary>
    [ObservableProperty] public partial Color? Color { get; set; }
}

public sealed partial class BudgetEditorViewModel : IQueryAttributable
{
    /// <inheritdoc />
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try { await ApplyQueryAsync(query); }
        catch (Exception exception) when (exception is not OutOfMemoryException) { await _interaction.ShowFailureAsync(exception); }
    }
    partial void RefreshNativeColors()
    {
        if (_lookup is null) return;
        foreach (var row in Limits) row.Color = _lookup.Color(row.CategoryId);
    }
}
