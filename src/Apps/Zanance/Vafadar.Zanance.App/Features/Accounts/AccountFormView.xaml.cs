namespace Vafadar.Zanance.App.Features.Accounts;

public partial class AccountFormView : VerticalStackLayout
{
    public AccountFormView()
    {
        InitializeComponent();
    }

    // The labels are part of their checkbox's touch target.
    private void OnNegativeLabelTapped(object? sender, TappedEventArgs e) => NegativeBox.IsChecked = !NegativeBox.IsChecked;

    private void OnUnknownLabelTapped(object? sender, TappedEventArgs e) => UnknownBox.IsChecked = !UnknownBox.IsChecked;
}
