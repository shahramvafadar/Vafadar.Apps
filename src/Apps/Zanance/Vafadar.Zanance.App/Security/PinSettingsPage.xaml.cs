using Vafadar.Localization;

namespace Vafadar.Zanance.App.Security;

/// <summary>The explicit PIN setup/change/removal form (D-63).</summary>
public partial class PinSettingsPage : ContentPage
{
    private readonly PinSettingsViewModel _viewModel;

    /// <summary>Creates a PIN form without storing or displaying credentials.</summary>
    public PinSettingsPage(AppLockService appLock, Translator translator)
    {
        InitializeComponent();
        FlowDirection = Application.Current?.Windows.FirstOrDefault()?.Page?.FlowDirection ?? FlowDirection.MatchParent;
        BindingContext = _viewModel = new(appLock, translator);
        var root = Application.Current?.Windows.FirstOrDefault()?.Page;
        _viewModel.IsCurrent = () => root is not null
            && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, root)
            && ReferenceEquals(root.Navigation.ModalStack.LastOrDefault(), this);
        _viewModel.CloseAsync = async () =>
        {
            if (!_viewModel.IsCurrent()) return;
            await Navigation.PopModalAsync();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Activate();
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.BackCommand.Execute(null);
        return true;
    }

    protected override void OnDisappearing()
    {
        _viewModel.Deactivate();
        base.OnDisappearing();
    }
}
