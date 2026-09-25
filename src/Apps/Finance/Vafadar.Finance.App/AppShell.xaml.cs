using Vafadar.Finance.App.Features.Accounts;
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

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(AccountsRoute, typeof(AccountsPage));
        Routing.RegisterRoute(AccountEditorRoute, typeof(AccountEditorPage));
        Routing.RegisterRoute(SettingsRoute, typeof(SettingsPage));
    }
}
