using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Core.Hosting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.More;

/// <summary>An entry of the More menu.</summary>
public sealed record MoreItem(string Title, Symbol Icon, string Route);

/// <summary>A titled group of the More menu.</summary>
public sealed record MoreGroup(string Title, IReadOnlyList<MoreItem> Items);

/// <summary>The More tab (docs/03-ux-design.md §2): everything that is not used daily, in four groups (D-27). Budget, reports,
/// forecast and goals live in the Insights tab.</summary>
public sealed partial class MoreViewModel : ViewModelBase
{
    private readonly Translator _translator;
    private readonly IAppEnvironment _app;

    public MoreViewModel(Translator translator, IAppEnvironment app)
    {
        _translator = translator;
        _app = app;
        VersionText = string.Empty;
        Groups = [];
    }

    [ObservableProperty]
    public partial IReadOnlyList<MoreGroup> Groups { get; set; }

    [ObservableProperty]
    public partial string VersionText { get; set; }

    public void Refresh()
    {
        Groups =
        [
            new(_translator["More_Money"],
            [
                new(_translator["Accounts_Title"], Symbol.Wallet, AppShell.AccountsRoute),
                new(_translator["Reimbursements_Title"], Symbol.ArrowUndo, AppShell.ReimbursementsRoute),
            ]),
            new(_translator["More_Organize"],
            [
                new(_translator["Categories_Title"], Symbol.Tag, AppShell.CategoriesRoute),
                new(_translator["Templates_Title"], Symbol.Flash, AppShell.TemplatesRoute),
                new(_translator["Rules_Title"], Symbol.TextBulletList, AppShell.RulesRoute),
            ]),
            new(_translator["More_Currency"],
            [
                new(_translator["Rates_Title"], Symbol.ArrowSwap, AppShell.RatesRoute),
                new(_translator["Unit_Title"], Symbol.Money, AppShell.DisplayUnitsRoute),
            ]),
            new(_translator["More_DataSecurity"],
            [
                new(_translator["Backup_Title"], Symbol.ShieldCheckmark, AppShell.BackupRoute),
                new(_translator["Profile_Title"], Symbol.PeopleSwap, AppShell.ProfilesRoute),
                new(_translator["ImportExport_Title"], Symbol.DocumentTable, AppShell.ImportExportRoute),
                new(_translator["Settings_Title"], Symbol.Settings, AppShell.SettingsRoute),
            ]),
        ];
        VersionText = _translator.Format("Settings_Version", _app.Version);
    }

    [RelayCommand]
    private Task OpenAsync(MoreItem item) => Shell.Current.GoToAsync(item.Route);
}
