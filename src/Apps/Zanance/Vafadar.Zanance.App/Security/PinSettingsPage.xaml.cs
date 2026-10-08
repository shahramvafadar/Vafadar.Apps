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
        _viewModel.CloseAsync = () => Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.BackCommand.Execute(null);
        return true;
    }

    protected override void OnDisappearing()
    {
        _viewModel.Clear();
        base.OnDisappearing();
    }
}
