using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.More;

/// <summary>An entry of the More menu: title, one line on what it holds, icon and route.</summary>
public sealed record MoreItem(string Title, string Subtitle, Symbol Icon, string Route);

/// <summary>A titled group of the More menu.</summary>
public sealed record MoreGroup(string Title, IReadOnlyList<MoreItem> Items);

/// <summary>The More tab (docs/03-ux-design.md §2): everything that is not used daily, in five groups (D-27, D-39). Budget,
/// reports, forecast and goals live in the Insights tab.</summary>
public sealed partial class MoreViewModel(Translator translator) : ViewModelBase
{
    [ObservableProperty]
    public partial IReadOnlyList<MoreGroup> Groups { get; set; } = [];

    public void Refresh()
    {
        MoreItem Item(string title, string subtitle, Symbol icon, string route) => new(translator[title], translator[subtitle], icon, route);
        Groups =
        [
            new(translator["More_Money"],
            [
                Item("Accounts_Title", "More_AccountsHint", Symbol.Wallet, AppShell.AccountsRoute),
                Item("Reimbursements_Title", "More_ReimbursementsHint", Symbol.ArrowUndo, AppShell.ReimbursementsRoute),
            ]),
            new(translator["More_Organize"],
            [
                Item("Categories_Title", "More_CategoriesHint", Symbol.Tag, AppShell.CategoriesRoute),
                Item("Templates_Title", "More_TemplatesHint", Symbol.Flash, AppShell.TemplatesRoute),
                Item("Rules_Title", "More_RulesHint", Symbol.TextBulletList, AppShell.RulesRoute),
            ]),
            new(translator["More_Currency"],
            [
                Item("Rates_Title", "More_RatesHint", Symbol.ArrowSwap, AppShell.RatesRoute),
                Item("Unit_Title", "More_UnitsHint", Symbol.Money, AppShell.DisplayUnitsRoute),
            ]),
            new(translator["More_DataSecurity"],
            [
                Item("Backup_Title", "More_BackupHint", Symbol.ShieldCheckmark, AppShell.BackupRoute),
                Item("ImportExport_Title", "More_ImportExportHint", Symbol.DocumentTable, AppShell.ImportExportRoute),
                Item("Profile_Title", "More_ProfilesHint", Symbol.PeopleSwap, AppShell.ProfilesRoute),
            ]),
            new(translator["More_App"],
            [
                Item("Settings_Title", "More_SettingsHint", Symbol.Settings, AppShell.SettingsRoute),
                Item("About_Title", "More_AboutHint", Symbol.Info, AppShell.AboutRoute),
            ]),
        ];
    }

    [RelayCommand]
    private Task OpenAsync(MoreItem item) => Shell.Current.GoToAsync(item.Route);
}