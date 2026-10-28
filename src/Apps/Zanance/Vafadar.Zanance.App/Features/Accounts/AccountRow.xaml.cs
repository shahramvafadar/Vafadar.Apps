namespace Vafadar.Zanance.App.Features.Accounts;

public partial class AccountRow : Grid
{
    public AccountRow()
    {
        InitializeComponent();
    }

    private async void OnTapped(object? sender, EventArgs e)
    {
        if (BindingContext is AccountItem item)
        {
            // D-627: the actual owning page rejects old rows while covered, retired or bound to another file.
            for (Element? owner = this; owner is not null; owner = owner.Parent)
            {
                if (owner is AccountsPage page)
                {
                    await Presentation.Failures.GuardAsync(() => page.OpenAccountAsync(item));
                    return;
                }
                // The same original row also belongs to Home. Preserve that route without treating it as an Accounts owner.
                if (owner is Home.HomePage { BindingContext: Home.HomeViewModel home }
                    && home.Loading.IsReady && home.Accounts.Any(current => ReferenceEquals(current, item)))
                {
                    await Presentation.Failures.GuardAsync(() => Shell.Current.GoToAsync(AppShell.AccountDetailRoute,
                        new Dictionary<string, object> { ["id"] = item.Id }));
                    return;
                }
            }
        }
    }
}
