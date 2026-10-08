using FluentIcons.Common;
using FluentIcons.Maui;
using Vafadar.Localization;
using Vafadar.Maui.Security;
using Vafadar.Zanance.Core.Security;
using Vafadar.Core.Text;

namespace Vafadar.Zanance.App.Security;

/// <summary>Covers the app while it is locked. It cannot be dismissed without authentication.</summary>
internal sealed class LockPage : ContentPage
{
    private readonly AppLockService _lock;
    private readonly Translator _translator;
    private readonly Label _message;
    private bool _promptOnAppearing;
    private bool _prompting;
    private readonly Entry _pin;
    private readonly TaskCompletionSource<bool>? _confirmation;

    /// <summary>Creates a persistent unlock cover, or a cancellable sensitive-operation confirmation.</summary>
    public LockPage(AppLockService appLock, Translator translator, bool promptOnAppearing,
        TaskCompletionSource<bool>? confirmation = null, string? reason = null)
    {
        _lock = appLock;
        _translator = translator;
        _promptOnAppearing = promptOnAppearing;
        _confirmation = confirmation;
        FlowDirection = Application.Current?.Windows.FirstOrDefault()?.Page?.FlowDirection ?? FlowDirection.MatchParent;

        _message = new Label { Text = appLock.PinUnavailable ? translator["Pin_Unavailable"] : reason ?? translator[appLock.PinEnabled ? "Pin_EnterHint" : "Lock_Message"], FontSize = 16, HorizontalTextAlignment = TextAlignment.Center };
        _pin = new Entry { IsPassword = true, Keyboard = Keyboard.Numeric, MaxLength = 4,
            FlowDirection = FlowDirection.LeftToRight, HorizontalTextAlignment = TextAlignment.Center,
            IsVisible = appLock.PinEnabled, AutomationId = "UnlockPin" };
        SemanticProperties.SetDescription(_pin, translator["Pin_Current"]);
        _pin.Completed += async (_, _) => await PromptAsync();
        var unlock = new Button
        {
            Text = translator["Lock_Unlock"],
            CornerRadius = 24,
            MinimumHeightRequest = 48,
            FontAttributes = FontAttributes.Bold,
            IsEnabled = !appLock.PinUnavailable,
        };

        // Theme colours as dynamic resources (the page background comes from the page style): the cover follows a theme
        // change while it is shown, and the button text keeps its contrast on the lighter blue of the dark theme.
        unlock.SetDynamicResource(Button.BackgroundColorProperty, "Primary");
        unlock.SetDynamicResource(Button.TextColorProperty, "OnPrimary");
        var symbol = new SymbolIcon { Symbol = Symbol.LockClosed, FontSize = 56, HorizontalOptions = LayoutOptions.Center };
        symbol.SetDynamicResource(SymbolIcon.ForegroundColorProperty, "Primary");
        unlock.Clicked += async (_, _) => await PromptAsync();

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(32),
            Spacing = 20,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                symbol,
                new Label { Text = translator["App_Name"], FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
                _message,
                _pin,
                unlock,
            },
        };
        if (appLock.PinEnabled || appLock.PinUnavailable)
        {
            var forgot = new Button { Text = translator["Pin_Forgot"], MinimumHeightRequest = 48 };
            forgot.SetDynamicResource(Button.TextColorProperty, "Primary");
            forgot.SetDynamicResource(Button.BackgroundColorProperty, "CardBackground");
            forgot.Clicked += async (_, _) =>
            {
                if (_prompting) { return; }
                _prompting = true;
                try
                {
                    if (await _lock.RecoverPinAsync(this)) { await CompleteAsync(true); }
                    else { _message.Text = translator["Pin_NoRecovery"]; }
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) { _message.Text = translator["Pin_Unavailable"]; }
                finally { _prompting = false; }
            };
            ((VerticalStackLayout)Content).Children.Add(forgot);
        }
        if (confirmation is not null)
        {
            var cancel = new Button { Text = translator["Common_Cancel"], MinimumHeightRequest = 48 };
            cancel.SetDynamicResource(Button.TextColorProperty, "Primary");
            cancel.SetDynamicResource(Button.BackgroundColorProperty, "CardBackground");
            cancel.Clicked += async (_, _) => await CompleteAsync(false);
            ((VerticalStackLayout)Content).Children.Add(cancel);
        }
    }

    /// <summary>Gets or sets a value indicating whether the app was already locked when it went to the background.</summary>
    public bool WasLockedBeforeSleep { get; set; }

    /// <summary>Checks the entered app PIN or prompts device authentication; failures leave the cover in place.</summary>
    public async Task PromptAsync()
    {
        if (_prompting)
        {
            return;
        }

        _prompting = true;
        try
        {
            if (_lock.PinUnavailable) { return; }
            if (_lock.PinEnabled)
            {
                var entered = Digits.ToAscii(_pin.Text ?? string.Empty);
                _pin.Text = string.Empty;
                var check = await _lock.Pin.VerifyAsync(entered);
                if (check.Outcome == PinOutcome.Success) { await CompleteAsync(true); }
                else
                {
                    _message.Text = check.Outcome == PinOutcome.Delayed
                        ? _translator.Format("Pin_Wait", (int)Math.Ceiling(check.Wait.TotalSeconds))
                        : _translator["Pin_Incorrect"];
                    SemanticScreenReader.Announce(_message.Text);
                }

                return;
            }

            switch (await _lock.Authenticator.AuthenticateAsync(_translator["Lock_Reason"]))
            {
                // No device lock any more: removing it required the device credential, and the app lock can never be
                // stronger than the device lock, so the owner must not be locked out for good (SEC-01 recovery path).
                case AuthenticationOutcome.Success or AuthenticationOutcome.NotAvailable:
                    await CompleteAsync(true);
                    break;
                default:
                    // Cancelled or not recognised: say so, so the cover does not look as if nothing happened.
                    _message.Text = _translator["Lock_NotUnlocked"];
                    SemanticScreenReader.Announce(_message.Text);
                    break;
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _pin.Text = string.Empty;
            _message.Text = _translator["Pin_Unavailable"];
        }
        finally
        {
            _prompting = false;
        }
    }

    private async Task CompleteAsync(bool success)
    {
        // A background lock can sit above a sensitive-operation prompt. Never pop that cover or grant the lower
        // prompt's operation while another modal page is on top (D-63).
        if (Navigation.ModalStack.LastOrDefault() != this) { return; }
        _pin.Text = string.Empty;
        if (_confirmation is not null)
        {
            await Navigation.PopModalAsync(animated: false);
            _confirmation.TrySetResult(success);
        }
        else if (success) { await _lock.UnlockedAsync(); }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_promptOnAppearing)
        {
            _promptOnAppearing = false;
            if (!_lock.PinEnabled) { await PromptAsync(); }
        }
    }

    protected override void OnDisappearing()
    {
        _pin.Text = string.Empty;
        base.OnDisappearing();
    }

    // The back button never uncovers the app.
    protected override bool OnBackButtonPressed()
    {
        if (_confirmation is not null && !_prompting) { _ = CompleteAsync(false); }
        return true;
    }
}
