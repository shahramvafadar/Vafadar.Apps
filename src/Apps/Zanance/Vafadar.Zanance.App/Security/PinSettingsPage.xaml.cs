using Vafadar.Localization;

namespace Vafadar.Zanance.App.Security;

/// <summary>The explicit PIN setup/change/removal form (D-63).</summary>
public partial class PinSettingsPage : ContentPage
{
    private readonly PinSettingsViewModel _viewModel;
#if ANDROID
    private LockKeyboardViewport? _keyboardViewport;
#endif

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
#if ANDROID
        Loaded += (_, _) => UseKeyboardViewport();
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Activate();
#if ANDROID
        Dispatcher.Dispatch(UseKeyboardViewport);
#endif
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.BackCommand.Execute(null);
        return true;
    }

    protected override void OnDisappearing()
    {
#if ANDROID
        _keyboardViewport?.Dispose();
        _keyboardViewport = null;
#endif
        _viewModel.Deactivate();
        base.OnDisappearing();
    }

#if ANDROID
    private void UseKeyboardViewport()
    {
        if (_keyboardViewport is null && _viewModel.IsCurrent())
            _keyboardViewport = LockKeyboardViewport.TryAcquire(Handler?.PlatformView as Android.Views.View,
                PinContainer, PinViewport);
    }
#endif
}
