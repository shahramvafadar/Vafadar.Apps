using FluentIcons.Common;
using FluentIcons.Maui;
using Vafadar.Localization;
using Vafadar.Maui.Security;

namespace Vafadar.Finance.App.Security;

/// <summary>Covers the app while it is locked. It cannot be dismissed without authentication.</summary>
internal sealed class LockPage : ContentPage
{
    private readonly AppLockService _lock;
    private readonly Translator _translator;
    private readonly Label _message;
    private bool _promptOnAppearing;
    private bool _prompting;

    public LockPage(AppLockService appLock, Translator translator, bool promptOnAppearing)
    {
        _lock = appLock;
        _translator = translator;
        _promptOnAppearing = promptOnAppearing;
        BackgroundColor = Color.FromArgb("#F6F7F6");
        FlowDirection = Application.Current?.Windows.FirstOrDefault()?.Page?.FlowDirection ?? FlowDirection.MatchParent;

        _message = new Label { Text = translator["Lock_Message"], FontSize = 16, HorizontalTextAlignment = TextAlignment.Center };
        var unlock = new Button
        {
            Text = translator["Lock_Unlock"],
            BackgroundColor = Color.FromArgb("#2E7D32"),
            TextColor = Colors.White,
            CornerRadius = 24,
            MinimumHeightRequest = 48,
            FontAttributes = FontAttributes.Bold,
        };
        unlock.Clicked += async (_, _) => await PromptAsync();

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(32),
            Spacing = 20,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new SymbolIcon { Symbol = Symbol.LockClosed, FontSize = 56, ForegroundColor = Color.FromArgb("#2E7D32"), HorizontalOptions = LayoutOptions.Center },
                new Label { Text = translator["App_Name"], FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
                _message,
                unlock,
            },
        };
    }

    /// <summary>Gets or sets a value indicating whether the app was already locked when it went to the background.</summary>
    public bool WasLockedBeforeSleep { get; set; }

    public async Task PromptAsync()
    {
        if (_prompting)
        {
            return;
        }

        _prompting = true;
        try
        {
            switch (await _lock.Authenticator.AuthenticateAsync(_translator["Lock_Reason"]))
            {
                // No device lock any more: removing it required the device credential, and the app lock can never be
                // stronger than the device lock, so the owner must not be locked out for good (SEC-01 recovery path).
                case AuthenticationOutcome.Success or AuthenticationOutcome.NotAvailable:
                    await _lock.UnlockedAsync();
                    break;
                default:
                    _message.Text = _translator["Lock_Message"];
                    break;
            }
        }
        finally
        {
            _prompting = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_promptOnAppearing)
        {
            _promptOnAppearing = false;
            await PromptAsync();
        }
    }

    // The back button never uncovers the app.
    protected override bool OnBackButtonPressed() => true;
}
