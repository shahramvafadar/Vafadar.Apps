namespace Vafadar.Zanance.App.Features.Accounts;

public partial class AccountsPage : ContentPage
{
    private readonly AccountsViewModel _viewModel;

    /// <summary>Shares the original read when appearance and retained-tab navigation arrive together.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public AccountsPage(AccountsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Refreshes the original balances after another tab changed the ledger, without posting money.</summary>
    internal Task RefreshForNavigationAsync() => !PendingPublication.IsCompleted
        ? PendingPublication : PendingPublication = _viewModel.LoadAsync();
}
