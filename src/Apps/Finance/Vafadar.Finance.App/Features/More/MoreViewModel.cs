using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Core.Hosting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.More;

/// <summary>An entry of the More menu.</summary>
public sealed record MoreItem(string Title, Symbol Icon, string Route);

/// <summary>The More tab (docs/03-ux-design.md §2): everything that is not used daily.</summary>
public sealed partial class MoreViewModel : ViewModelBase
{
    private readonly Translator _translator;
    private readonly IAppEnvironment _app;

    public MoreViewModel(Translator translator, IAppEnvironment app)
    {
        _translator = translator;
        _app = app;
        VersionText = string.Empty;
        Items = [];
    }

    [ObservableProperty]
    public partial IReadOnlyList<MoreItem> Items { get; set; }

    [ObservableProperty]
    public partial string VersionText { get; set; }

    public void Refresh()
    {
        Items =
        [
            new(_translator["Accounts_Title"], Symbol.Wallet, AppShell.AccountsRoute),
            new(_translator["Budget_Title"], Symbol.Target, AppShell.BudgetRoute),
            new(_translator["Report_Title"], Symbol.DataPie, AppShell.ReportsRoute),
            new(_translator["Forecast_Title"], Symbol.DataTrending, AppShell.ForecastRoute),
            new(_translator["Categories_Title"], Symbol.Tag, AppShell.CategoriesRoute),
            new(_translator["Templates_Title"], Symbol.Flash, AppShell.TemplatesRoute),
            new(_translator["Rates_Title"], Symbol.ArrowSwap, AppShell.RatesRoute),
            new(_translator["ImportExport_Title"], Symbol.DocumentTable, AppShell.ImportExportRoute),
            new(_translator["Backup_Title"], Symbol.ShieldCheckmark, AppShell.BackupRoute),
            new(_translator["Settings_Title"], Symbol.Settings, AppShell.SettingsRoute),
        ];
        VersionText = _translator.Format("Settings_Version", _app.Version);
    }

    [RelayCommand]
    private Task OpenAsync(MoreItem item) => Shell.Current.GoToAsync(item.Route);
}
