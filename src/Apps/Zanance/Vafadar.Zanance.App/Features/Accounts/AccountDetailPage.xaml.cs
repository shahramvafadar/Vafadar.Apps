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
        _viewModel.SetReconciliationActive(true);
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Retires pending original balance consent before another native page receives input.</summary>
    protected override void OnDisappearing()
    {
        _viewModel.SetReconciliationActive(false); base.OnDisappearing();
    }

    /// <summary>Shares the covered read when the original page or its retained Shell tab becomes visible.</summary>
    internal Task RefreshForNavigationAsync() => _viewModel.LoadAsync();
}
