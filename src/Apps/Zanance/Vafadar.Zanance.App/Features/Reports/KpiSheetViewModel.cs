using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>What the KPI sheet shows: the number, the scope it was computed with, its parts and the way to its entries.</summary>
/// <param name="Id">The KPI id of the catalog, e.g. "K05"; its texts are <c>Kpi_{Id}_Title</c>, <c>_Question</c>, <c>_Definition</c>, <c>_Included</c>, <c>_Excluded</c>.</param>
/// <param name="Value">The number as shown on the card.</param>
/// <param name="Scope">The scope, e.g. "October · EUR · Accounts in totals".</param>
/// <param name="Lines">The number in words: its parts.</param>
/// <param name="DataStatus">The data status of the view.</param>
/// <param name="ShowEntries">Opens the entries or the screen behind the number, or <see langword="null"/>.</param>
public sealed record KpiExplanation(string Id, string Value, string Scope, IReadOnlyList<AmountLine> Lines, string? DataStatus, Func<Task>? ShowEntries);

/// <summary>
/// The explanation of a KPI (ZEX-UI14, ZEX-S0609): question, definition in words, what is included and left out, the
/// data status and "Show entries". It names the scope the number was computed with; there is no accuracy score.
/// </summary>
public sealed partial class KpiSheetViewModel(Translator translator) : ViewModelBase, IQueryAttributable
{
    private Func<Task>? _showEntries;

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Value { get; set; }

    [ObservableProperty]
    public partial string? Scope { get; set; }

    [ObservableProperty]
    public partial string? Question { get; set; }

    [ObservableProperty]
    public partial string? Definition { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AmountLine> Lines { get; set; } = [];

    [ObservableProperty]
    public partial bool HasLines { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> Included { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<string> Excluded { get; set; } = [];

    [ObservableProperty]
    public partial string? DataStatus { get; set; }

    [ObservableProperty]
    public partial bool CanShowEntries { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("sheet", out var value) && value is KpiExplanation sheet)
        {
            Title = translator[$"Kpi_{sheet.Id}_Title"];
            Question = translator[$"Kpi_{sheet.Id}_Question"];
            Definition = translator[$"Kpi_{sheet.Id}_Definition"];
            Included = Split(translator[$"Kpi_{sheet.Id}_Included"]);
            Excluded = Split(translator[$"Kpi_{sheet.Id}_Excluded"]);
            Value = string.IsNullOrEmpty(sheet.Value) ? null : sheet.Value;
            Scope = sheet.Scope;
            Lines = sheet.Lines;
            HasLines = sheet.Lines.Count > 0;
            DataStatus = sheet.DataStatus;
            _showEntries = sheet.ShowEntries;
            CanShowEntries = _showEntries is not null;
        }

        query.Clear();
    }

    // Lines of a list are separated by "|" in the resources, so each language keeps its own count of items.
    private static string[] Split(string text) => [.. text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    [RelayCommand]
    private async Task ShowEntriesAsync()
    {
        await Shell.Current.GoToAsync("..");
        if (_showEntries is { } show)
        {
            await show();
        }
    }

    [RelayCommand]
    private Task CloseAsync() => Shell.Current.GoToAsync("..");
}
