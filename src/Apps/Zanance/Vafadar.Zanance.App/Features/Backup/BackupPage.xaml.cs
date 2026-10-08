namespace Vafadar.Zanance.App.Features.Backup;

public partial class BackupPage : ContentPage
{
    private readonly BackupViewModel _viewModel;

    public BackupPage(BackupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.AttachPage(this);
    }

    /// <summary>Offers restore during first run, with a return to the unchanged onboarding draft (D-62).</summary>
    public void BeginOnboardingRestore(Func<Task> returnToOnboarding) => _viewModel.BeginOnboardingRestore(returnToOnboarding);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }

    /// <summary>Prevents a device Back action from closing first-run restore while data is being replaced.</summary>
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsRestoreOnly)
        {
            if (!_viewModel.IsBusy)
            {
                _viewModel.BackToOnboardingCommand.Execute(null);
            }

            return true;
        }

        return base.OnBackButtonPressed();
    }
}
