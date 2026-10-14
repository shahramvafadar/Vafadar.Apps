using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private Page? _startupPage;
    private Startup.StartupViewModel? _startup;
    private bool _databaseReady;
    private readonly TaskCompletionSource _startupReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Lets diagnostic routes wait for actual storage and access initialization instead of racing migration.</summary>
    internal Task WaitForStartupAsync() => _startupReady.Task;
#if DEBUG
    private int _startupWindowThread;
    private int _startupStorageThread;
    /// <summary>Reports actual window/storage thread separation for the requested native startup review.</summary>
    internal bool StartupStorageRanOffWindowThread => _startupStorageThread != 0 && _startupStorageThread != _startupWindowThread;
#endif

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();

        // Wide windows show every page as a centred, readable column (D-41).
        Presentation.ReadableWidth.Attach(this);

        // Copies of opened receipts do not outlive the session that opened them (F2-TX-04).
        Presentation.AttachmentFiles.ClearCache();

        // Light or dark theme (UX-08, D-22). Colors are dynamic resources; screens with computed colors recolor in place
        // (IThemeAware). The shell is not rebuilt: in "System" the device may turn dark on its own while an editor is
        // open, and a rebuild would close the editor and lose what was typed.
        var theme = services.GetRequiredService<Presentation.ThemeService>();
        theme.Initialize(this);
        theme.Changed += OnThemeChanged;
        var localization = services.GetRequiredService<ILocalizationService>();

        // Persian digits in the Persian interface (D-27). The translator refreshes the texts of open pages before the
        // service raises Changed, and a page that stays open (onboarding) would keep the old digits. This handler is
        // registered before any page binds to the translator, so it runs first.
        Presentation.DigitPreferences.Initialize(localization);
        Translator.Instance.PropertyChanged += (_, _) => Presentation.DigitPreferences.Apply(localization);
        Presentation.DigitPreferences.Changed += OnLocalizationChanged;
        localization.Changed += OnLocalizationChanged;
#if ANDROID
        // Android 13+ per-app language (D-32).
        AppLocales.Initialize(localization);
#endif

        // Progress bars are drawn left to right on every platform; in right-to-left languages they are mirrored, so they
        // fill in the reading direction.
        Resources["ReadingScaleX"] = localization.IsRightToLeft ? -1d : 1d;
        localization.Changed += (_, _) => Resources["ReadingScaleX"] = localization.IsRightToLeft ? -1d : 1d;

        // Persian text in Vazirmatn; English and German in Figtree with Urbanist (the wordmark's face) for titles and
        // large amounts (D-27).
        ApplyFonts(localization);
        localization.Changed += (_, _) => ApplyFonts(localization);
        this.ApplyToModalPages(localization);

        var reminders = services.GetRequiredService<ReminderService>();
        reminders.Scheduler.Tapped += OnReminderTapped;
    }

    private void ApplyFonts(ILocalizationService localization)
    {
        var persian = localization.CurrentCulture.TwoLetterISOLanguageName == "fa";
        Resources["AppFont"] = persian ? "Vazirmatn" : "Figtree";
        Resources["DisplayFont"] = persian ? "VazirmatnBold" : "UrbanistBold";
#if WINDOWS
        // What Windows draws itself – dialogs (alerts, confirmations, help), picker lists, the date picker – takes the
        // system font otherwise (Segoe UI, also for Persian); it uses the app's font as well (D-47).
        if (_services.GetService<IFontManager>() is { } fonts && Microsoft.UI.Xaml.Application.Current is { } platformApp)
        {
            platformApp.Resources["ContentControlThemeFontFamily"] = fonts.GetFontFamily(Microsoft.Maui.Font.OfSize(persian ? "Vazirmatn" : "Figtree", 14));
        }
#endif
    }

    /// <summary>Replaces the root page of the main window, e.g. after onboarding.</summary>
    public void ShowMainShell()
    {
        if (Windows.FirstOrDefault() is { } window)
        {
            ReplaceRoot(window, CreateShell());
            OfferNotificationsSoon();
            if (_pendingLink is { } link)
            {
                _pendingLink = null;
                Dispatcher.Dispatch(() => _ = Presentation.Failures.GuardAsync(() => OpenLinkAsync(link)));
            }
        }
    }

    // Reminders are offered once per device after the first start (D-38): after onboarding, or on the next start of an
    // installation that finished onboarding before; only once the app is unlocked and the new screens are shown.
    private void OfferNotificationsSoon()
    {
#if DEBUG
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOTS") is { Length: > 0 })
        {
            return;
        }
#endif
        Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(1), () => _ = Presentation.Failures.GuardAsync(() =>
            _services.GetRequiredService<AppLockService>().RunWhenUnlockedAsync(() =>
                Presentation.PermissionPrompts.OfferNotificationsAsync(_services.GetRequiredService<ReminderService>(), Translator.Instance))));
    }

    /// <summary>
    /// Shows the profile that was just opened (§3): its screens, or onboarding for a new profile; its due plans are
    /// posted and its reminders replace those of the previous profile.
    /// </summary>
    public void ShowCurrentProfile()
    {
        // Each profile has its own display units (ZEX-P20).
        Presentation.DisplayUnitPreferences.Load(_services.GetRequiredService<ZananceStore>());
        if (_services.GetRequiredService<ZananceStore>().GetSettings().OnboardingCompleted)
        {
            ShowMainShell();
        }
        else
        {
            ShowOnboarding();
        }

        RunForegroundWork(starting: false);
    }

    /// <summary>Shows onboarding again, e.g. after all data was deleted.</summary>
    public void ShowOnboarding()
    {
        if (Windows.FirstOrDefault() is { } window)
        {
            ReplaceRoot(window, _services.GetRequiredService<OnboardingPage>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>()));
        }
    }

    /// <summary>Opens the existing restore flow before a first account is created (D-62).</summary>
    public async Task ShowOnboardingRestoreAsync()
    {
        if (Windows.FirstOrDefault()?.Page is OnboardingPage onboarding)
        {
            var page = _services.GetRequiredService<Features.Backup.BackupPage>()
                .WithFlowDirection(_services.GetRequiredService<ILocalizationService>());
            // The modal keeps the wizard and its unsaved draft attached until restore succeeds or is cancelled (D-62).
            page.BeginOnboardingRestore(() => onboarding.Navigation.PopModalAsync(animated: false));
            await onboarding.Navigation.PushModalAsync(page, animated: false);
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
#if DEBUG
        // Independent protected reviews must fail before the covered startup worker can touch a main installation.
        // MSBuild can retain stale ApplicationId metadata when only a global property changes (D-274).
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") is "home-balance-reload" or "holiday-catalog" or "home-loading"
            && AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new InvalidOperationException("Independent review application identity required before startup.");
        _startupWindowThread = Environment.CurrentManagedThreadId;
#endif
        // Android builds the app in Application.onCreate, including notification-only process starts (D-46).
        // Window construction must not migrate or open a financial context on that thread.
        // D-222: the first window contains no financial page. Native key/migration work is deferred to a worker.
#if ANDROID || WINDOWS
        _startup = new Startup.StartupViewModel(PrepareStartupAsync, RecoverStartupKeysAsync, RecoverStartupBackupAsync);
#else
        _startup = new Startup.StartupViewModel(PrepareStartupAsync);
#endif
        var cover = new Startup.StartupPage(_startup).WithFlowDirection(_services.GetRequiredService<ILocalizationService>());
        var window = new Window(cover) { Title = Translator.Instance["App_Name"] };
        var startup = _startup;
        // Use the actual window dispatcher after native creation, including Android activity recreation.
        // The view model excludes duplicate preparation from the compatibility OnStart callback.
        window.Created += (_, _) => window.Dispatcher.Dispatch(async () => await startup.InitializeAsync());
#if WINDOWS
        // The window title in the app's colours with the symbol in front of it (D-45). The default title keeps the
        // Windows theme: white on the light page when Windows is dark, black on the dark page when it is light.
        var titleBar = new TitleBar { Title = Translator.Instance["App_Name"], Icon = "zanance_symbol.png" };
        titleBar.SetDynamicResource(TitleBar.ForegroundColorProperty, "AmountText");
        titleBar.SetDynamicResource(TitleBar.BackgroundColorProperty, "PageBackground");
        window.TitleBar = titleBar;

        // Never narrower than a small phone, the width every layout is made for (D-47).
        window.MinimumWidth = 360;
        window.MinimumHeight = 520;
#endif
#if DEBUG
        Diagnostics.DebugSnapshots.StartIfRequested(this, _services, window);
#endif
        return window;
    }

    protected override void OnStart()
    {
        base.OnStart();
        Dispatcher.Dispatch(async () => { if (_startup is not null) await _startup.InitializeAsync(); });
    }

#if ANDROID || WINDOWS
    /// <summary>Opens device-local optional recovery setup from the unlocked Settings form.</summary>
    internal async Task ShowRecoverySetupAsync()
    {
        var page = Windows.FirstOrDefault()?.Page ?? throw new InvalidOperationException("Application window is unavailable.");
        await page.Navigation.PushModalAsync(new RecoverySetupPage(
            _services.GetRequiredService<ProtectedStorageHost>(), Translator.Instance));
    }

    /// <summary>Restores an explicitly chosen portable backup without removing unreadable original data or the device PIN.</summary>
    private async Task<bool> RecoverStartupBackupAsync()
    {
        if (!await _services.GetRequiredService<AppLockService>().ConfirmStartupRecoveryAsync(Translator.Instance["Lock_ConfirmRestore"])) return false;
        var root = Windows.FirstOrDefault()?.Page ?? throw new InvalidOperationException("Startup cover is unavailable.");
        var page = new StartupBackupPage(_services.GetRequiredService<ProtectedBackupRecovery>(), Translator.Instance)
            { FlowDirection = root.FlowDirection };
        await root.Navigation.PushModalAsync(page, animated: false);
        var restored = await page.Completion;
        if (restored)
        {
            _services.GetRequiredService<ILocalizationService>().Initialize();
            _services.GetRequiredService<Presentation.ThemeService>().Initialize();
        }
        return restored;
    }

    /// <summary>Accepts the user's recorded code only in the covered startup surface; app access remains a separate gate.</summary>
    private async Task<bool> RecoverStartupKeysAsync()
    {
        if (!await _services.GetRequiredService<AppLockService>().ConfirmStartupRecoveryAsync(Translator.Instance["Lock_ConfirmRestore"])) return false;
        var page = Windows.FirstOrDefault()?.Page ?? throw new InvalidOperationException("Startup cover is unavailable.");
        var text = Translator.Instance;
        var code = await page.DisplayPromptAsync(text["Recovery_Title"], text["Recovery_CodePrompt"],
            text["Recovery_Confirm"], text["Common_Cancel"], maxLength: 64);
        if (code is null) return false;
        try
        {
            await Task.Run(() => _services.GetRequiredService<ProtectedStorageHost>()
                .RecoverDeviceKeysAsync(code, CancellationToken.None));
            return true;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            await page.DisplayAlertAsync(text["Recovery_Title"], text["Recovery_Failed"], text["Common_Ok"]);
            return false;
        }
        finally { code = string.Empty; }
    }
#endif

    private async Task PrepareStartupAsync()
    {
        var window = Windows.FirstOrDefault() ?? throw new InvalidOperationException("Startup window is unavailable.");
        var store = _services.GetRequiredService<ZananceStore>();
        var legacyUnits = Preferences.Default.Get("display_units", string.Empty);
        var settings = await Task.Run(async () =>
        {
#if DEBUG
            _startupStorageThread = Environment.CurrentManagedThreadId;
#endif
            await _services.GetRequiredService<Startup.IAppDatabaseStartup>().PrepareAsync();
            var prepared = await store.GetSettingsAsync();
            if (prepared.DisplayUnits is null)
            {
                prepared.DisplayUnits = legacyUnits; await store.SaveSettingsAsync(prepared);
            }
            return prepared;
        });
        if (!Windows.Contains(window)) throw new InvalidOperationException("Startup window closed.");
        Presentation.DisplayUnitPreferences.Apply(settings.DisplayUnits!);
        _startupPage = settings.OnboardingCompleted ? CreateShell()
            : _services.GetRequiredService<OnboardingPage>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>());
        await _services.GetRequiredService<AppLockService>().StartAsync();
        // A rejected deep link is a navigation failure after secure initialization, not a database-startup failure.
        await Presentation.Failures.GuardAsync(() => _services.GetRequiredService<AppLinkRouter>().StartAsync(() =>
            {
                if (_startupPage is { } page && Windows.FirstOrDefault() is { } window)
                {
                    _startupPage = null;
                    window.Page = page;
                }

                // The startup PIN cover precedes the Shell: consume the link only after the real page is visible (D-73).
                string? link = null;
                if (Windows.FirstOrDefault()?.Page is AppShell)
                {
                    link = _pendingLink;
                    _pendingLink = null;
                    OfferNotificationsSoon();
                }
                return Task.FromResult(link);
            }));
        _databaseReady = true;
        // The initial settings write must not schedule a competing reminder database read during preparation.
        _services.GetRequiredService<ReminderService>().WatchChanges(_services.GetRequiredService<Profiles.ProfileService>());
        _startupReady.TrySetResult();
        RunForegroundWork(starting: true);
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        if (_databaseReady) Dispatcher.Dispatch(async () => await _services.GetRequiredService<AppLockService>().SleepAsync());
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (_databaseReady) Dispatcher.Dispatch(async () => await _services.GetRequiredService<AppLockService>().ResumeAsync());
        RunForegroundWork(starting: false);
    }

    // Due plan occurrences are recorded whenever the app comes to the foreground, then reminders are rebuilt;
    // correctness never depends on background execution (REC-22). Failures are not fatal: occurrences stay open.
    private void RunForegroundWork(bool starting)
    {
        if (!_databaseReady) return;
        var processor = _services.GetRequiredService<AutoPostProcessor>();
        var reminders = _services.GetRequiredService<ReminderService>();
        var store = _services.GetRequiredService<ZananceStore>();
        var today = DateOnly.FromDateTime(_services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        _ = Task.Run(async () =>
        {
            try
            {
                await processor.RunAsync(today);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine($"Automatic posting failed: {ex}");
            }

            await reminders.RefreshAsync();

            // Receipts of deleted entries are removed at start only: undo is kept in memory, so after a restart no
            // deleted entry can come back, while on resume an undo may still be pending (F2-TX-04).
            if (!starting)
            {
                return;
            }

            try
            {
                await store.PurgeOrphanAttachmentsAsync();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine($"Attachment cleanup failed: {ex}");
            }
        });
    }

    private static string? _pendingLink;

    /// <summary>
    /// Opens an in-app link, e.g. <c>entry|Expense</c> from the quick add widget (D-30), after the app lock; before the
    /// shell exists the link waits for the start.
    /// </summary>
    public static void OpenLink(string link)
    {
        if (Current is App app && Shell.Current is not null)
        {
            app.Dispatcher.Dispatch(() => app.OpenLinkAsync(link));
        }
        else
        {
            _pendingLink = link;
        }
    }

    // A tapped reminder opens its occurrence; several taps or an old notification never record anything (REM-04, REM-06).
    private void OnReminderTapped(object? sender, string link) => OpenLink(link);

    private Task OpenLinkAsync(string link) => _services.GetRequiredService<AppLinkRouter>().OpenAsync(link);

    // Set while a theme change waits for the user to leave the open pages.
    private bool _rebuildWhenBackOnTab;

    // On a tab's first page nothing can be lost, so the shell is rebuilt as for a language change: that also renews
    // colors that a trigger or visual state took over. With a detail page or an editor open, the open pages recompute
    // their colors in place (IThemeAware) and the rebuild waits until the user is back on a tab; the editor and what
    // was typed stay. Pages that are not shown reload when they appear.
    private void OnThemeChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(async () =>
    {
        if (!_databaseReady) return;
        if (Windows.FirstOrDefault()?.Page is not AppShell shell || IsOnTab(shell))
        {
            _rebuildWhenBackOnTab = false;
            QueueRebuild();
            return;
        }

        _rebuildWhenBackOnTab = true;
        foreach (var page in OpenPages())
        {
            if (page.BindingContext is Presentation.IThemeAware aware)
            {
                await Presentation.Failures.GuardAsync(aware.RefreshThemeAsync);
            }
        }
    });

    private static bool IsOnTab(Shell shell) => shell.Navigation.NavigationStack.Count <= 1 && shell.Navigation.ModalStack.Count == 0;

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        if (_rebuildWhenBackOnTab && sender is Shell shell && IsOnTab(shell))
        {
            _rebuildWhenBackOnTab = false;
            Dispatcher.Dispatch(QueueRebuild);
        }
    }

    private IEnumerable<Page> OpenPages()
    {
        if (Windows.FirstOrDefault()?.Page is not { } root)
        {
            return [];
        }

        IEnumerable<Page?> pages = root is Shell shell
            ? [shell.CurrentPage, .. shell.Navigation.NavigationStack, .. shell.Navigation.ModalStack]
            : [root, .. root.Navigation.ModalStack];
        return pages.Select(p => p is NavigationPage navigation ? navigation.CurrentPage : p).OfType<Page>().Distinct();
    }

    private bool _rebuildQueued;

    // Pages cache formatted numbers, dates and icons, and flipping the flow direction of a live visual tree is not
    // reliable on every platform. Rebuilding the shell gives a clean result. While the app is locked the rebuild waits,
    // so the lock cover is never lost with the old window content.
    private void OnLocalizationChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(() =>
    {
        if (!_databaseReady) return;
        // Reminder texts are translated when they are scheduled (REM-07).
        _services.GetRequiredService<ReminderService>().RefreshSoon();
        // Keep open forms and the live regional preview intact; rebuild cached tab content after returning (D-67).
        if (Windows.FirstOrDefault()?.Page is AppShell shell && !IsOnTab(shell))
        {
            _rebuildWhenBackOnTab = true;
            return;
        }

        QueueRebuild();
    });

    private void QueueRebuild()
    {
        if (!_databaseReady) return;
        if (_rebuildQueued)
        {
            return;
        }

        _rebuildQueued = true;
        _ = _services.GetRequiredService<AppLockService>().RunWhenUnlockedAsync(RebuildShellAsync);
    }

    /// <summary>Raised when a rebuilt shell is shown on its tab (after a language, calendar, digit or theme change).</summary>
    internal event EventHandler? ShellRebuilt;

    private async Task RebuildShellAsync()
    {
        try
        {
            await RebuildShellCoreAsync();
        }
        finally
        {
            ShellRebuilt?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task RebuildShellCoreAsync()
    {
        _rebuildQueued = false;
        if (Windows.FirstOrDefault() is not { Page: AppShell shell } window)
        {
            return;
        }

        // Back to the current tab only: a detail or editor page cannot be rebuilt without its query (an editor would
        // come back empty and save a copy).
        var tab = TabRoute(shell);
        var replacement = CreateShell();
        ReplaceRoot(window, replacement);
        if (tab is not null && Shell.Current is { } current)
        {
            try
            {
                await current.GoToAsync(tab, animate: false);
            }
            catch (ArgumentException)
            {
                // The route no longer resolves; staying on the first tab is fine.
            }
        }
    }

    /// <summary>Retires the old native navigation graph before replacing an already published root.</summary>
    private void ReplaceRoot(Window window, Page replacement)
    {
        // D-82/D-139: profile/onboarding replacement also disconnects Android Shell renderers. Retained title
        // bindings must not abort later translation notifications by accessing a missing native MauiContext.
        if (window.Page is AppShell retired)
        {
            retired.RetireNavigationBindings();
            retired.Navigated -= OnShellNavigated;
        }
        window.Page = replacement;
    }

    private static string? TabRoute(Shell shell)
    {
        if (shell.CurrentItem?.CurrentItem is not { } section || section.CurrentItem is not { } content)
        {
            return null;
        }

        return section.Items.Count > 1 ? $"//{section.Route}/{content.Route}" : $"//{content.Route}";
    }

    private AppShell CreateShell()
    {
        var shell = _services.GetRequiredService<AppShell>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>());
        shell.Navigated += OnShellNavigated;
        return shell;
    }
}
