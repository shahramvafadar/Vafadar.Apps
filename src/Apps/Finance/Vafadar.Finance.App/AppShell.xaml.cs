using Vafadar.Finance.App.Features.Accounts;
using Vafadar.Finance.App.Features.Categories;
using Vafadar.Finance.App.Features.Entries;
using Vafadar.Finance.App.Features.Settings;

namespace Vafadar.Finance.App;

public partial class AppShell : Shell
{
    /// <summary>Route of the account list.</summary>
    public const string AccountsRoute = "accounts";

    /// <summary>Route of the account editor (query: <c>id</c> for an existing account).</summary>
    public const string AccountEditorRoute = "account";

    /// <summary>Route of the settings page.</summary>
    public const string SettingsRoute = "settings";

    /// <summary>Route of the entry editor (query: <c>id</c>, <c>duplicate</c>, <c>refundOf</c> or <c>kind</c>).</summary>
    public const string EntryEditorRoute = "entry";

    /// <summary>Route of the entry details (query: <c>id</c>).</summary>
    public const string EntryDetailRoute = "entrydetail";

    /// <summary>Route of the category list.</summary>
    public const string CategoriesRoute = "categories";

    /// <summary>Route of the category editor (query: <c>id</c> for an existing category).</summary>
    public const string CategoryEditorRoute = "category";

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(AccountsRoute, typeof(AccountsPage));
        Routing.RegisterRoute(AccountEditorRoute, typeof(AccountEditorPage));
        Routing.RegisterRoute(SettingsRoute, typeof(SettingsPage));
        Routing.RegisterRoute(EntryEditorRoute, typeof(EntryEditorPage));
        Routing.RegisterRoute(EntryDetailRoute, typeof(EntryDetailPage));
        Routing.RegisterRoute(CategoriesRoute, typeof(CategoriesPage));
        Routing.RegisterRoute(CategoryEditorRoute, typeof(CategoryEditorPage));
    }
}
