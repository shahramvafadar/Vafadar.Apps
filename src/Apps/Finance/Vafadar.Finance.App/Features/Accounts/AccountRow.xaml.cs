namespace Vafadar.Finance.App.Features.Accounts;

public partial class AccountRow : Grid
{
    public AccountRow()
    {
        InitializeComponent();
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is AccountItem item)
        {
            await Shell.Current.GoToAsync(AppShell.AccountEditorRoute, new Dictionary<string, object> { ["id"] = item.Id });
        }
    }
}
