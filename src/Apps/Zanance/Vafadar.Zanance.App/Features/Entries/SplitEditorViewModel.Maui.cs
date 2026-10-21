using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Connects original split input to native navigation and semantic colors.</summary>
public sealed partial class SplitEditorViewModel : IQueryAttributable
{
    /// <summary>Gets the current balanced/error semantic color without putting native graphics in monetary ownership.</summary>
    [ObservableProperty] public partial Color RemainingColor { get; set; } = Palette.SecondaryText;
    /// <inheritdoc />
    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Failures.GuardAsync(() => ApplyQueryAsync(query));
    partial void RefreshNativeColors(bool balanced) => RemainingColor = balanced ? Palette.IncomeText : Palette.WarningText;
}
