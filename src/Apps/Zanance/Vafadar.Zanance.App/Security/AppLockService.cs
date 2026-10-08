using Vafadar.Localization;
using Vafadar.Maui.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Security;

/// <summary>
/// The optional app lock (SEC-01, SEC-02). When enabled, the app is covered on start and when it goes to the
/// background, and opens again only after the app PIN matches or, without a PIN, the device owner authenticates. A tapped
/// notification is handled after unlocking (REM-06); export and backup ask again. The lock hides the app – it does not
/// encrypt the database (SEC-03).
/// </summary>
public sealed class AppLockService(IDeviceAuthenticator authenticator, ZananceStore store, Translator translator, TimeProvider time, PinLock pin)
{
    // A short switch to another app (e.g. to copy an IBAN) does not ask again.
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(30);

    private readonly List<Func<Task>> _pending = [];
    private DateTimeOffset? _sleptAt;
    private LockPage? _page;
    private bool _started;

    /// <summary>Gets a value indicating whether the lock is enabled.</summary>
    public bool IsEnabled => DeviceLockEnabled || PinEnabled || PinUnavailable;

    /// <summary>Gets the profile's separate device-authentication preference.</summary>
    public bool DeviceLockEnabled { get; private set; }

    /// <summary>Gets whether the device-wide app PIN is configured.</summary>
    public bool PinEnabled => pin.IsEnabled;

    /// <summary>Gets whether protected PIN state could not be read. This never falls back to an unlocked app.</summary>
    public bool PinUnavailable { get; private set; }

    /// <summary>Gets the testable PIN access gate.</summary>
    public PinLock Pin => pin;

    /// <summary>Resets a forgotten PIN after successful device authentication and an explicit removal confirmation.</summary>
    internal async Task<bool> RecoverPinAsync(Page page)
    {
        var recovered = await pin.RecoverAsync(async () =>
            await authenticator.AuthenticateAsync(translator["Pin_ResetReason"]) == AuthenticationOutcome.Success
            && await page.DisplayAlertAsync(translator["Pin_Setting"], translator["Pin_ResetConfirm"],
                translator["Pin_Remove"], translator["Common_Cancel"]));
        if (recovered) { PinUnavailable = false; }
        return recovered;
    }

    /// <summary>
    /// Gets a value indicating whether the app is covered – or has not read the lock setting yet, so that a link from a
    /// notification or the widget never opens a screen before the lock could cover it (REM-06).
    /// </summary>
    public bool IsLocked => _page is not null || !_started;

    /// <summary>Gets the authenticator.</summary>
    public IDeviceAuthenticator Authenticator => authenticator;

    /// <summary>Reads the setting and locks the app if it is enabled (app start).</summary>
    public async Task StartAsync()
    {
        DeviceLockEnabled = (await store.GetSettingsAsync()).AppLockEnabled;
        try { await pin.LoadAsync(); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            PinUnavailable = true;
            // Do not log secure-storage exception payloads, which can contain protected values.
        }
        ScreenProtection.Apply();
        _started = true;
        if (IsEnabled)
        {
            await ShowAsync(prompt: true);
        }

        if (!IsLocked)
        {
            await RunPendingAsync();
        }
    }

    /// <summary>Reads the setting again, e.g. after a restore brought another value.</summary>
    public async Task ReloadAsync() => DeviceLockEnabled = (await store.GetSettingsAsync()).AppLockEnabled;

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
        ScreenProtection.Apply();
        if (!IsEnabled || _page is null)
        {
            return;
        }

        // The cover may have been lost with a rebuilt window; put it back before asking.
        if (!IsOnScreen(_page))
        {
            _page = null;
            await ShowAsync(prompt: true);
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

    /// <summary>Confirms a sensitive operation with the app PIN, or the device lock when no app PIN is configured.</summary>
    public async Task<bool> ConfirmAsync(string reason)
    {
        if (PinUnavailable) { return false; }
        if (!IsEnabled) { return true; }
        if (!PinEnabled) { return await authenticator.AuthenticateAsync(reason) == AuthenticationOutcome.Success; }
        if (Application.Current?.Windows.FirstOrDefault()?.Page is not { } root) { return false; }
        var completion = new TaskCompletionSource<bool>();
        var page = new LockPage(this, translator, promptOnAppearing: false, completion, reason);
        await root.Navigation.PushModalAsync(page, animated: false);
        return await completion.Task;
    }

    /// <summary>Runs <paramref name="action"/> now, or after unlocking when the app is covered (REM-06).</summary>
    public Task RunWhenUnlockedAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!IsLocked)
        {
            return action();
        }

        lock (_pending)
        {
            _pending.Add(action);
        }

        return Task.CompletedTask;
    }

    /// <summary>Turns the lock on or off after the device owner confirmed it; returns the new state.</summary>
    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        var reason = translator[enabled ? "Lock_EnableReason" : "Lock_DisableReason"];
        if (await authenticator.AuthenticateAsync(reason) != AuthenticationOutcome.Success)
        {
            return DeviceLockEnabled;
        }

        var settings = await store.GetSettingsAsync();
        settings.AppLockEnabled = enabled;
        await store.SaveSettingsAsync(settings);
        DeviceLockEnabled = enabled;
        ScreenProtection.Apply();
        return DeviceLockEnabled;
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
        await RunPendingAsync();
    }

    // Links and rebuilds that waited for the unlock, in the order they came.
    private async Task RunPendingAsync()
    {
        List<Func<Task>> actions;
        lock (_pending)
        {
            actions = [.. _pending];
            _pending.Clear();
        }

        foreach (var action in actions)
        {
            try
            {
                await action();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // One link that cannot be opened (e.g. navigation refused while a dialog is open) must neither keep the
                // others from running nor end the app: these actions run from the lock page's unlock handler.
                System.Diagnostics.Debug.WriteLine($"Action after unlocking failed: {ex}");
            }
        }
    }

    private static bool IsOnScreen(Page page) =>
        Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation.ModalStack.Contains(page) == true;

    private async Task ShowAsync(bool prompt)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is not { } root)
        {
            return;
        }

        if (_page is not null && !IsOnScreen(_page))
        {
            _page = null;
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

}
