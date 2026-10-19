namespace Vafadar.Zanance.App.Features.Accounts;

public partial class AccountDetailPage : ContentPage
{
    private readonly AccountDetailViewModel _viewModel;

    public AccountDetailPage(AccountDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Shares the covered read when the original page or its retained Shell tab becomes visible.</summary>
    internal Task RefreshForNavigationAsync() => _viewModel.LoadAsync();
}
