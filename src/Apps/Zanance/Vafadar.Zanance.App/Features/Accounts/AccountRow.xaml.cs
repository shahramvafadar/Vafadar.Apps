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
            await Shell.Current.GoToAsync(AppShell.AccountDetailRoute, new Dictionary<string, object> { ["id"] = item.Id });
        }
    }
}
