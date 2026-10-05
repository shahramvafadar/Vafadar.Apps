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
        Presentation.DigitPreferences.Apply(localization);
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
        reminders.WatchChanges();
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
            window.Page = CreateShell();
            OfferNotificationsSoon();
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
            window.Page = _services.GetRequiredService<OnboardingPage>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>());
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // The database is created or upgraded here, before the first page reads it, and not while the app is built:
        // Android builds the app in Application.onCreate, also when it starts the process only to deliver a notification,
        // and reports the app as not responding when that takes too long on a slow device (D-46).
        _services.MigrateLocalDatabase<ZananceDbContext>();

        // Display units (e.g. toman) of the open profile, before any amount is shown (FX-07, ZEX-P20).
        Presentation.DisplayUnitPreferences.Load(_services.GetRequiredService<ZananceStore>());
        var settings = _services.GetRequiredService<ZananceStore>().GetSettings();
        Page root = settings.OnboardingCompleted
            ? CreateShell()
            : _services.GetRequiredService<OnboardingPage>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>());

        var window = new Window(root) { Title = Translator.Instance["App_Name"] };
#if WINDOWS
        // The window title in the app's colours with the symbol in front of it (D-45). The default title keeps the
        // Windows theme: white on the light page when Windows is dark, black on the dark page when it is light.
        var titleBar = new TitleBar { Title = Translator.Instance["App_Name"], Icon = "zanance_symbol.png" };
        titleBar.SetDynamicResource(TitleBar.ForegroundColorProperty, "AmountText");
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
        Dispatcher.Dispatch(async () =>
        {
            await _services.GetRequiredService<AppLockService>().StartAsync();

            // A widget tap that started the app (D-30) opens its screen now that the shell exists.
            if (_pendingLink is { } link)
            {
                _pendingLink = null;
                OpenLink(link);
            }

            if (Windows.FirstOrDefault()?.Page is AppShell)
            {
                OfferNotificationsSoon();
            }
        });
        RunForegroundWork(starting: true);
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        Dispatcher.Dispatch(async () => await _services.GetRequiredService<AppLockService>().SleepAsync());
    }

    protected override void OnResume()
    {
        base.OnResume();
        Dispatcher.Dispatch(async () => await _services.GetRequiredService<AppLockService>().ResumeAsync());
        RunForegroundWork(starting: false);
    }

    // Due plan occurrences are recorded whenever the app comes to the foreground, then reminders are rebuilt;
    // correctness never depends on background execution (REC-22). Failures are not fatal: occurrences stay open.
    private void RunForegroundWork(bool starting)
    {
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
            app.Dispatcher.Dispatch(() => app._services.GetRequiredService<AppLockService>().RunWhenUnlockedAsync(() => app.OpenLinkAsync(link)));
        }
        else
        {
            _pendingLink = link;
        }
    }

    // A tapped reminder opens its occurrence; several taps or an old notification never record anything (REM-04, REM-06).
    private void OnReminderTapped(object? sender, string link) => Dispatcher.Dispatch(() =>
        _services.GetRequiredService<AppLockService>().RunWhenUnlockedAsync(() => OpenLinkAsync(link)));

    private async Task OpenLinkAsync(string link)
    {
        var parts = link.Split('|');
        if (Shell.Current is null)
        {
            return;
        }

        if (parts is ["occurrence", var plan, var date] && Guid.TryParse(plan, out var planId)
            && DateOnly.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var original))
        {
            await Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = planId, ["date"] = original });
        }
        else if (parts is ["plan", var id] && Guid.TryParse(id, out var scheduleId))
        {
            await Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = scheduleId });
        }
        else if (parts is ["plans"])
        {
            await Shell.Current.GoToAsync("//plans");
        }
        else if (parts is ["budget"])
        {
            await Shell.Current.GoToAsync(AppShell.BudgetRoute);
        }
        else if (parts is ["entry", var kind] && Enum.TryParse<Core.Ledger.EntryKind>(kind, out var entryKind)
                 && entryKind is Core.Ledger.EntryKind.Expense or Core.Ledger.EntryKind.Income or Core.Ledger.EntryKind.Transfer)
        {
            await Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["kind"] = entryKind.ToString() });
        }
    }

    // Set while a theme change waits for the user to leave the open pages.
    private bool _rebuildWhenBackOnTab;

    // On a tab's first page nothing can be lost, so the shell is rebuilt as for a language change: that also renews
    // colors that a trigger or visual state took over. With a detail page or an editor open, the open pages recompute
    // their colors in place (IThemeAware) and the rebuild waits until the user is back on a tab; the editor and what
    // was typed stay. Pages that are not shown reload when they appear.
    private void OnThemeChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(async () =>
    {
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
        // Reminder texts are translated when they are scheduled (REM-07).
        _services.GetRequiredService<ReminderService>().RefreshSoon();
        QueueRebuild();
    });

    private void QueueRebuild()
    {
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
        window.Page = CreateShell();
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
