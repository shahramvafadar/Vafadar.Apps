namespace Vafadar.Finance.App.Features.Accounts;

public partial class AccountFormView : VerticalStackLayout
{
    public AccountFormView()
    {
        InitializeComponent();
    }

    // The label is part of the checkbox's touch target.
    private void OnNegativeLabelTapped(object? sender, TappedEventArgs e) => NegativeBox.IsChecked = !NegativeBox.IsChecked;
}
