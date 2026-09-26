using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Security;

namespace Vafadar.Finance.App.Security;

/// <summary>
/// The optional app lock (SEC-01, SEC-02). When enabled, the app is covered on start and when it goes to the
/// background, and opens again only after the device owner authenticates (biometrics or device credential). A tapped
/// notification is handled after unlocking (REM-06); export and backup ask again. The lock hides the app – it does not
/// encrypt the database (SEC-03).
/// </summary>
public sealed class AppLockService(IDeviceAuthenticator authenticator, FinanceStore store, Translator translator, TimeProvider time)
{
    // A short switch to another app (e.g. to copy an IBAN) does not ask again.
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(30);

    private DateTimeOffset? _sleptAt;
    private Func<Task>? _pending;
    private LockPage? _page;

    /// <summary>Gets a value indicating whether the lock is enabled.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Gets a value indicating whether the app is currently covered.</summary>
    public bool IsLocked => _page is not null;

    /// <summary>Gets the authenticator.</summary>
    public IDeviceAuthenticator Authenticator => authenticator;

    /// <summary>Reads the setting and locks the app if it is enabled (app start).</summary>
    public async Task StartAsync()
    {
        IsEnabled = (await store.GetSettingsAsync()).AppLockEnabled;
        ApplySecureWindow();
        if (IsEnabled)
        {
            await ShowAsync(prompt: true);
        }
    }

    /// <summary>Covers the app when it leaves the foreground, so the recent-apps preview shows nothing (SEC-02).</summary>
    public async Task SleepAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        _sleptAt = time.GetUtcNow();
        await ShowAsync(prompt: false);
    }

    /// <summary>Asks for authentication after a longer absence; a short one uncovers the app directly.</summary>
    public async Task ResumeAsync()
    {
        ApplySecureWindow();
        if (!IsEnabled || _page is null)
        {
            return;
        }

        if (_sleptAt is { } slept && time.GetUtcNow() - slept < Grace && !_page.WasLockedBeforeSleep)
        {
            await UnlockedAsync();
        }
        else
        {
            await _page.PromptAsync();
        }
    }

    /// <summary>Asks the device owner to confirm a sensitive operation (export, backup, restore) when the lock is on.</summary>
    public async Task<bool> ConfirmAsync(string reason) =>
        !IsEnabled || await authenticator.AuthenticateAsync(reason) == AuthenticationOutcome.Success;

    /// <summary>Runs <paramref name="action"/> now, or after unlocking when the app is covered (REM-06).</summary>
    public Task RunWhenUnlockedAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!IsLocked)
        {
            return action();
        }

        _pending = action;
        return Task.CompletedTask;
    }

    /// <summary>Turns the lock on or off after the device owner confirmed it; returns the new state.</summary>
    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        var reason = translator[enabled ? "Lock_EnableReason" : "Lock_DisableReason"];
        if (await authenticator.AuthenticateAsync(reason) != AuthenticationOutcome.Success)
        {
            return IsEnabled;
        }

        var settings = await store.GetSettingsAsync();
        settings.AppLockEnabled = enabled;
        await store.SaveSettingsAsync(settings);
        IsEnabled = enabled;
        ApplySecureWindow();
        return IsEnabled;
    }

    /// <summary>Called by the lock page after a successful authentication.</summary>
    internal async Task UnlockedAsync()
    {
        if (_page is { } page)
        {
            _page = null;
            await page.Navigation.PopModalAsync(animated: false);
        }

        _sleptAt = null;
        if (Interlocked.Exchange(ref _pending, null) is { } pending)
        {
            await pending();
        }
    }

    private async Task ShowAsync(bool prompt)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is not { } root)
        {
            return;
        }

        if (_page is not null)
        {
            _page.WasLockedBeforeSleep = true;
            if (prompt)
            {
                await _page.PromptAsync();
            }

            return;
        }

        _page = new LockPage(this, translator, promptOnAppearing: prompt);
        await root.Navigation.PushModalAsync(_page, animated: false);
    }

    // Android: no screenshots and a blank recent-apps preview while the lock is on (SEC-02).
    private void ApplySecureWindow()
    {
#if ANDROID
        var window = Platform.CurrentActivity?.Window;
        if (IsEnabled)
        {
            window?.AddFlags(Android.Views.WindowManagerFlags.Secure);
        }
        else
        {
            window?.ClearFlags(Android.Views.WindowManagerFlags.Secure);
        }
#endif
    }
}
