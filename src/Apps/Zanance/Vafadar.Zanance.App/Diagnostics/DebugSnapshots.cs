#if DEBUG
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Localization;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>
/// Development aid (Debug builds only, never in Release): when the environment variable
/// <c>VAFADAR_SNAPSHOTS</c> points to a folder, the app walks through its main screens in English and Persian and
/// renders each page to a PNG file, then closes. Used to review layout and right-to-left rendering without a device.
/// </summary>
internal static class DebugSnapshots
{
    private static void SetSize(Window window)
    {
        var size = Environment.GetEnvironmentVariable("VAFADAR_WINDOW_SIZE")?.Split('x');
        window.Width = size is [var w, _] && double.TryParse(w, System.Globalization.CultureInfo.InvariantCulture, out var width) ? width : 412;
        window.Height = size is [_, var h] && double.TryParse(h, System.Globalization.CultureInfo.InvariantCulture, out var height) ? height : 892;
    }

    public static void StartIfRequested(App app, IServiceProvider services, Window window)
    {
#if ANDROID
        if (DebugImportLinks.StartIfRequested(app, services)) { return; }
        if (DebugReviewReminders.StartIfRequested(app, services)) { return; }
        if (DebugGoalReminders.StartIfRequested(app, services)) { return; }
#endif
        // VAFADAR_START_ROUTE opens one screen with the real window chrome (navigation bar, back button) for a check
        // of the whole window; the app stays open.
        if (Environment.GetEnvironmentVariable("VAFADAR_START_ROUTE") is { Length: > 0 } route)
        {
            SetSize(window);
            app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
            {
                if (Shell.Current is { } shell)
                {
                    foreach (var step in route.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        await shell.GoToAsync(step);
                    }
                }
#if WINDOWS
                if (Environment.GetEnvironmentVariable("VAFADAR_CAPTURE_WINDOW") is { Length: > 0 } file)
                {
                    // VAFADAR_CAPTURE_DELAY leaves time to open a dialog first, which is saved next to the window.
                    var delay = int.TryParse(Environment.GetEnvironmentVariable("VAFADAR_CAPTURE_DELAY"), out var seconds) ? seconds : 2;
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                    await CaptureWindowAsync(window, file);
                }
#endif
            });
        }

        var folder = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOTS");
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        // Phone-like size so that the layout matches the primary target; VAFADAR_WINDOW_SIZE (e.g. 1280x820) checks wide windows.
        SetSize(window);
        Directory.CreateDirectory(folder);
#if WINDOWS
        services.GetService<DebugFontScale>()?.WriteEvidence(folder);
#endif

        // A crash outside the walk-through's own code (an event handler, a binding) ends the process without a trace;
        // the exception is written next to the screenshots like the walk-through's own errors.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            File.AppendAllText(Path.Combine(folder, "error.txt"), $"Unhandled: {e.ExceptionObject}{Environment.NewLine}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
            File.AppendAllText(Path.Combine(folder, "error.txt"), $"Unobserved: {e.Exception}{Environment.NewLine}");
        Presentation.Failures.Observed = ex =>
            File.AppendAllText(Path.Combine(folder, "error.txt"), $"Shown as a failure: {ex}{Environment.NewLine}");
#if WINDOWS
        if (Microsoft.UI.Xaml.Application.Current is { } xaml)
        {
            xaml.UnhandledException += (_, e) =>
                File.AppendAllText(Path.Combine(folder, "error.txt"), $"Unhandled (UI): {e.Exception}{Environment.NewLine}");
        }
#endif
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            try
            {
#if WINDOWS
                // PowerShell WindowStyle.Hidden does not hide a WinUI AppWindow. Requested snapshots render their
                // own root directly; keep the review window hidden so it cannot interrupt or receive desktop input (D-86).
                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow) { nativeWindow.AppWindow.Hide(); }
#endif
                await RunAsync(app, services, folder);
            }
            catch (Exception ex)
            {
                await File.WriteAllTextAsync(Path.Combine(folder, "error.txt"), ex.ToString());
            }
            finally
            {
                app.Quit();
            }
        });
    }

    private static async Task CaptureImportChoicesAsync(App app, IServiceProvider services, Features.DataFiles.ImportExportPage page,
        Features.DataFiles.ImportExportViewModel vm, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var account = (await store.GetAccountsAsync()).FirstOrDefault(a => a.Name == "Import fixture EUR");
        if (account is null)
        {
            account = new Account { Name = "Import fixture EUR", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 8, 1) };
            await store.SaveAccountAsync(account);
        }
        var aggregate = (await store.GetEntriesAsync()).SingleOrDefault(e => e.AccountId == account.Id && e.IsAggregated);
        if (aggregate is null)
        {
            var category = (await store.GetCategoriesAsync()).First(c => c.Kind == CategoryKind.Expense && c.SystemKey == "Food");
            aggregate = new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense, Amount = 41200,
                IsAggregated = true, Date = new DateOnly(2026, 9, 30), AggregatedFrom = new DateOnly(2026, 9, 1), AggregatedTo = new DateOnly(2026, 9, 30) };
            await store.SaveEntryAsync(aggregate);
        }
        await vm.PreviewFixtureAsync(DebugImportFixture.Details(aggregate));
        // New bindable rows need a layout pass before their position is usable for a rendered check.
        await Task.Delay(300);
        if (FindScrollView(page) is { } scroll)
        {
            double offset = 0;
            for (Element? element = page.FindByName<VerticalStackLayout>("OverlapSection"); element is not null && element != scroll.Content; element = element.Parent)
            {
                if (element is VisualElement visual) { offset += visual.Y; }
            }
            await scroll.ScrollToAsync(0, offset, animated: false);
            await Task.Delay(300);
        }
        await CaptureAsync(app, folder, $"{language}-import-overlap-pending");
        var row = vm.Overlaps.Single();
        if (vm.CanImport) { throw new InvalidOperationException("An unset aggregate decision must not permit import."); }
        row.SelectedIndex = 0;
        if (!vm.CanImport) { throw new InvalidOperationException("The explicit link decision should permit this fictitious import."); }
        await Task.Delay(300);
        await CaptureAsync(app, folder, $"{language}-import-overlap-link");
        row.SelectedIndex = 1;
        await Task.Delay(300);
        await CaptureAsync(app, folder, $"{language}-import-overlap-keep");
#if WINDOWS
        await CaptureHelpAsync(app, folder, language, ["ImportAggregates"]);
#endif
        row.SelectedIndex = -1;
    }

    // Fictitious destination feedback only; no account sign-in or cloud upload is performed by this fixture.
    private static async Task CaptureCloudStatesAsync(App app, Features.Backup.BackupViewModel vm, string folder, string language)
    {
        var translator = Translator.Instance;
        vm.HasCloud = true;
        vm.CloudAccounts.Clear();
        var row = new Features.Backup.CloudAccountRow(Vafadar.Authentication.ExternalIdentityProvider.Microsoft, translator["Cloud_OneDrive"])
        {
            Account = "backup-review@example.invalid",
            Status = translator["Cloud_NoBackups"],
        };
        vm.CloudAccounts.Add(row);
        vm.UsePassword = false;
        vm.LocalBackups.Clear();
        vm.HasLocalBackups = false;
        await Task.Delay(300);
        if (FindScrollView(app.Windows[0].Page) is { } scroll)
        {
            if (Shell.Current?.CurrentPage?.FindByName<Border>("CloudSection") is { } card)
            {
                await scroll.ScrollToAsync(card, ScrollToPosition.Start, animated: false);
            }
        }
        await CaptureAsync(app, folder, $"{language}-backup-cloud-empty");
        row.Status = null;
        row.Error = translator.Format("Cloud_Error_Offline", row.Title);
        await CaptureAsync(app, folder, $"{language}-backup-cloud-error");
        row.Error = null;
        var files = new[] { "", "~old-device" }.Select(profile =>
            new Vafadar.Backup.BackupFileInfo(profile, $"pro.vafadar.zanance{profile}_20261008T120000Z.vbak", 123456,
                DateTimeOffset.Parse("2026-10-08T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture))).ToArray();
        vm.ApplyCloudFiles(row, files);
        if (!row.HasBackups || row.Backups.Count != 2)
        {
            throw new InvalidOperationException("Discovered cloud files did not become visible.");
        }
        await Task.Delay(300);
        await CaptureAsync(app, folder, $"{language}-backup-cloud-files");
        vm.CloudAccounts.Clear();
        vm.HasCloud = false;
    }

    private static async Task RunAsync(App app, IServiceProvider services, string folder)
    {
        var settings = services.GetRequiredService<Vafadar.Core.Settings.ISettingsStore>();
        var saved = LocalizationService.PortableKeys.ToDictionary(key => key, settings.Get);
        var passwordChoice = Preferences.Default.Get("backup.usePassword", true);
        try
        {
            await RunCoreAsync(app, services, folder);
        }
        finally
        {
            Preferences.Default.Set("backup.usePassword", passwordChoice);
            foreach (var (key, value) in saved)
            {
                settings.Set(key, value);
            }
        }
    }

    private static async Task RunCoreAsync(App app, IServiceProvider services, string folder)
    {
        var localization = services.GetRequiredService<ILocalizationService>();
        var languages = (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_LANGUAGES") ?? "en,fa").Split(',');

        // VAFADAR_SNAPSHOT_THEME=dark shoots the dark theme; the choice is saved, so light runs set it back.
        var dark = string.Equals(Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_THEME"), "dark", StringComparison.OrdinalIgnoreCase);
        services.GetRequiredService<Presentation.ThemeService>().Set(dark ? Presentation.ThemeChoice.Dark : Presentation.ThemeChoice.Light);

        // VAFADAR_SNAPSHOT_DIGITS=latin shows Persian with Latin digits; the choice is saved, so other runs set the default
        // (Persian digits) back.
        localization.SetDigits(string.Equals(Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_DIGITS"), "latin", StringComparison.OrdinalIgnoreCase)
            ? DigitStyle.Latin : DigitStyle.LanguageDefault);
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY")?.Contains("regional", StringComparison.Ordinal) == true)
        {
            localization.SetFormattingCulture("de-DE");
            localization.SetRegion("DE");
            localization.SetCalendar(CalendarSystem.Gregorian);
        }

        // VAFADAR_SNAPSHOT_CALENDAR=Hijri (or Persian, Gregorian) shoots every language in that calendar, budget included
        // (onboarding takes the current calendar). The choice is saved, so runs without it remove it again and the
        // calendar follows the language.
        if (Enum.TryParse<CalendarSystem>(Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_CALENDAR"), ignoreCase: true, out var calendar))
        {
            localization.SetCalendar(calendar);
        }
        else
        {
            services.GetRequiredService<Vafadar.Core.Settings.ISettingsStore>().Remove("localization.calendar");
        }

        if (app.Windows[0].Page is OnboardingPage onboarding && onboarding.BindingContext is OnboardingViewModel vm)
        {
            // The snapshot theme is selected after the first page is created; show that choice in its picker too.
            vm.ThemeIndex = (int)services.GetRequiredService<Presentation.ThemeService>().Choice;
            foreach (var language in languages)
            {
                vm.Step = 1;
                vm.SelectedLanguage = localization.SupportedLanguages.First(l => l.CultureName == language);
                await Task.Delay(500);
                await CaptureAsync(app, folder, $"{language}-onboarding-1");

                // D-62: restore is available before any account exists, and Back preserves the wizard draft.
#if WINDOWS
                await ReviewOnboardingRestoreAsync(app, services, onboarding, vm, folder, language);
#else
                await vm.RestoreBackupCommand.ExecuteAsync(null);
                await Task.Delay(500);
                await CaptureAsync(app, folder, $"{language}-onboarding-restore");
                if (onboarding.Navigation.ModalStack.LastOrDefault()?.BindingContext is Features.Backup.BackupViewModel restore)
                {
                    await restore.BackToOnboardingCommand.ExecuteAsync(null);
                }
#endif

                for (var step = 2; step <= OnboardingViewModel.StepCount; step++)
                {
                    await vm.NextCommand.ExecuteAsync(null);
                    await Task.Delay(500);
                    if (!ReferenceEquals(app.Windows[0].Page, onboarding))
                    { throw new InvalidOperationException("The reviewed onboarding page was retired before its step capture."); }
                    await CaptureAsync(app, folder, $"{language}-onboarding-{step}");
                    if (FindScrollView(onboarding) is { } scroll)
                    {
                        await scroll.ScrollToAsync(0, scroll.ContentSize.Height, animated: false);
                        await Task.Delay(300);
                        await CaptureAsync(app, folder, $"{language}-onboarding-{step}-end");
                        await scroll.ScrollToAsync(0, 0, animated: false);
                    }
#if WINDOWS
                    if (step == OnboardingViewModel.StepCount)
                    { await ReviewOnboardingRestoreAsync(app, services, onboarding, vm, folder, language); }
#endif
                }
            }
            // The focused restore-action review ends before the fixture account is created or other routes are seeded.
            if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "onboarding-actions") { return; }
            vm.Account.OpeningText = Core.Money.MoneyText.ForInput(125050, "EUR", localization.CurrentCulture);
            await vm.NextCommand.ExecuteAsync(null);
            await Task.Delay(1500);
        }

        // VAFADAR_SNAPSHOT_EMPTY=1: the first days of a new user – one account, nothing recorded yet – to check every
        // empty state (UX-05).
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_EMPTY") == "1")
        {
            var emptyScreens = new (string Name, string Route)[]
            {
                ("home", "//home"), ("transactions", "//transactions"), ("plans", "//plans"), ("budget", AppShell.BudgetRoute),
                ("reports", AppShell.ReportsRoute), ("forecast", AppShell.ForecastRoute), ("goals", AppShell.GoalsRoute),
                ("accounts", AppShell.AccountsRoute), ("templates", AppShell.TemplatesRoute), ("rules", AppShell.RulesRoute),
                ("reimbursements", AppShell.ReimbursementsRoute), ("rates", AppShell.RatesRoute), ("display-units", AppShell.DisplayUnitsRoute),
                ("backup", AppShell.BackupRoute), ("entry-new", AppShell.EntryEditorRoute),
            };
            foreach (var language in languages)
            {
                await SetLanguageAsync(app, localization, language);
                foreach (var (name, route) in emptyScreens)
                {
                    await Shell.Current.GoToAsync(route);
                    await Task.Delay(1500);
                    await CaptureAsync(app, folder, $"{language}-empty-{name}");
                    if (!route.StartsWith("//", StringComparison.Ordinal))
                    {
                        await Shell.Current.GoToAsync("//home");
                        await Task.Delay(500);
                    }
                }
            }

            return;
        }

        var (expenseId, foodId) = await SeedAsync(services);
        // The ordinary expense fixture has a payback and deliberately cannot split. Review a real eligible income too.
        var splitIncomeId = (await services.GetRequiredService<ZananceStore>().GetEntriesAsync())
            .First(entry => entry.Kind == EntryKind.Income && EntryActions.CanSplit([entry])).Id;
        await Presentation.DisplayUnitPreferences.SaveAsync(services.GetRequiredService<ZananceStore>(), [new Core.Money.DisplayUnit("IRR", "Toman", 1)]);
        var (planId, planDate) = await SeedPlansAsync(services);
        await SeedBudgetAsync(services, foodId);
        var goalId = (await services.GetRequiredService<GoalStore>().GetGoalsAsync()).First(g => g.Type == Core.Goals.GoalType.AccountBalance).Id;
        var quantityGoalId = (await services.GetRequiredService<GoalStore>().GetGoalsAsync()).First(g => g.Type == Core.Goals.GoalType.HoldingQuantity).Id;
        var snapshotId = (await services.GetRequiredService<ZananceStore>().GetForecastSnapshotsAsync()).First().Id;
        var accountId = (await services.GetRequiredService<ZananceStore>().GetAccountsAsync()).First(a => a.Type == AccountType.Checking).Id;
        var weekdayPlanId = (await services.GetRequiredService<PlanStore>().GetSchedulesAsync()).First(s => s.Rule.DayRule == MonthDayRule.LastWeekday).Id;
        var loanId = (await services.GetRequiredService<ZananceStore>().GetAccountsAsync()).First(a => a.Type == AccountType.Loan).Id;
        // Fictitious legacy value account for the actual editor confirmation route (D-74 / AT-81).
        var confirmationAsset = new Account { Name = "Fictitious car", Type = AccountType.Asset, CurrencyCode = "EUR",
            OpeningBalance = 800000, OpeningDate = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime).AddDays(-30) };
        await services.GetRequiredService<ZananceStore>().SaveAccountAsync(confirmationAsset);
        var holdingTypes = await services.GetRequiredService<HoldingStore>().GetTypesAsync();
        var goldId = holdingTypes.First(t => t.Dimension == Core.Holdings.AssetDimension.Mass).Id;
        var coinId = holdingTypes.First(t => t.Dimension == Core.Holdings.AssetDimension.Count).Id;
        await services.GetRequiredService<Vafadar.Backup.IBackupService>().CreateBackupAsync(
            new Vafadar.Backup.Storage.LocalFolderBackupStorage(Path.Combine(FileSystem.AppDataDirectory, "backups")), "snapshot-password");
        var screens = new (string Name, string Route, Dictionary<string, object>? Query)[]
        {
            ("home", "//home", null),
            ("transactions", "//transactions", null),
            ("entry-new", AppShell.EntryEditorRoute, null),
            ("entry-tags", AppShell.EntryEditorRoute, null),
            ("entry-details", AppShell.EntryEditorRoute, null),
            ("entry-validation", AppShell.EntryEditorRoute, new() { ["kind"] = "Transfer" }),
            ("entry-fee-retention", AppShell.EntryEditorRoute, null),
            ("headers", AppShell.SettingsRoute, null),
            ("entry-asset-income", AppShell.EntryEditorRoute, new() { ["kind"] = nameof(EntryKind.Income), ["account"] = confirmationAsset.Id }),
            ("entry-asset-expense", AppShell.EntryEditorRoute, new() { ["kind"] = nameof(EntryKind.Expense), ["account"] = confirmationAsset.Id }),
            ("entry-edit", AppShell.EntryEditorRoute, new() { ["id"] = expenseId }),
            ("entry-detail", AppShell.EntryDetailRoute, new() { ["id"] = expenseId }),
            ("receipt-found", AppShell.EntryEditorRoute, new() { ["kind"] = nameof(EntryKind.Expense), ["account"] = accountId,
                ["receipt"] = Core.Receipts.ReceiptParser.Parse("Market Example\n28.09.2026\nSUMME 4,10 EUR (inkl. MwSt. 0,27)") }),
            ("receipt-review", AppShell.EntryEditorRoute, new() { ["kind"] = nameof(EntryKind.Expense), ["account"] = accountId,
                ["receipt"] = Core.Receipts.ReceiptParser.Parse("Market Example\nTOTAL 12,34 EUR\nTOTAL 15,00 EUR") }),
            ("receipt-damaged", AppShell.EntryEditorRoute, new() { ["kind"] = nameof(EntryKind.Expense), ["account"] = accountId,
                ["receipt"] = Core.Receipts.ReceiptParser.Parse("Market Example\nTOTAL 12,O9") }),
            ("receipt-missing", AppShell.EntryEditorRoute, new() { ["kind"] = nameof(EntryKind.Expense), ["account"] = accountId,
                ["receipt"] = Core.Receipts.ReceiptParser.Parse("Market Example\n28.09.2026\nWater 1,20\nBread 2,50") }),
            ("receipt-conflict", AppShell.EntryEditorRoute, new() { ["kind"] = nameof(EntryKind.Expense), ["account"] = accountId,
                ["receipt"] = Core.Receipts.ReceiptParser.Parse("Market Example\nTOTAL 12,34 USD") }),
            ("receipt-reread-missing", AppShell.EntryEditorRoute, new() { ["id"] = expenseId,
                ["receipt"] = Core.Receipts.ReceiptParser.Parse("Market Example\n28.09.2026\nWater 1,20\nBread 2,50") }),
            ("split", AppShell.SplitRoute, new() { ["id"] = expenseId }),
            ("split-readable", AppShell.SplitRoute, new() { ["id"] = splitIncomeId }),
            ("reimbursements", AppShell.ReimbursementsRoute, null),
            ("rules", AppShell.RulesRoute, null),
            ("plans", "//plans", null),
            ("plan-new", AppShell.PlanEditorRoute, null),
            ("plan-debt-reminder", AppShell.PlanEditorRoute, new() { ["fromDebt"] = loanId }),
            ("debt-new", AppShell.AccountEditorRoute, new() { ["debt"] = true }),
            ("receivable-new", AppShell.AccountEditorRoute, new() { ["debt"] = AccountType.Lent }),
            ("plan-edit", AppShell.PlanEditorRoute, new() { ["id"] = planId }),
            ("plan-detail", AppShell.PlanDetailRoute, new() { ["id"] = planId }),
            ("plan-weekday", AppShell.PlanDetailRoute, new() { ["id"] = weekdayPlanId }),
            ("settlement", AppShell.SettlementRoute, new() { ["id"] = planId }),
            ("settlement-feedback", AppShell.SettlementRoute, new() { ["id"] = planId }),
            ("occurrence", AppShell.OccurrenceRoute, new() { ["plan"] = planId, ["date"] = planDate }),
            ("accounts", AppShell.AccountsRoute, null),
            ("modal-headers", AppShell.AccountsRoute, null),
            ("plan-validation", AppShell.PlanEditorRoute, null),
            ("account-detail", AppShell.AccountDetailRoute, new() { ["id"] = accountId }),
            ("loan-detail", AppShell.AccountDetailRoute, new() { ["id"] = loanId }),
            ("loan-actions", AppShell.AccountDetailRoute, new() { ["id"] = loanId }),
            ("date-inputs", AppShell.AccountDetailRoute, new() { ["id"] = accountId }),
            ("loan-schedule", AppShell.LoanScheduleRoute, new() { ["id"] = loanId }),
            ("loan-edit", AppShell.AccountEditorRoute, new() { ["id"] = loanId }),
            ("account", AppShell.AccountEditorRoute, null),
            ("categories", AppShell.CategoriesRoute, null),
            ("category", AppShell.CategoryEditorRoute, new() { ["id"] = foodId }),
            ("insights-tabs", AppShell.BudgetRoute, null),
            ("budget-periods", AppShell.BudgetRoute, null),
            ("budget-readouts", AppShell.BudgetRoute, null),
            ("budget", AppShell.BudgetRoute, null),
            ("forecast", AppShell.ForecastRoute, null),
            ("rates", AppShell.RatesRoute, null),
            ("display-units", AppShell.DisplayUnitsRoute, null),
            ("home-layout", AppShell.HomeLayoutRoute, null),
            ("templates", AppShell.TemplatesRoute, null),
            ("goals", AppShell.GoalsRoute, null),
            ("goal-detail", AppShell.GoalDetailRoute, new() { ["id"] = goalId }),
            ("goal-edit", AppShell.GoalEditorRoute, new() { ["id"] = goalId }),
            ("goal-quantity", AppShell.GoalDetailRoute, new() { ["id"] = quantityGoalId }),
            ("goal-quantity-edit", AppShell.GoalEditorRoute, new() { ["id"] = quantityGoalId }),
            ("forecast-snapshot", AppShell.SnapshotRoute, new() { ["id"] = snapshotId }),
            ("holdings", AppShell.HoldingsRoute, null),
            ("holding-detail", AppShell.HoldingDetailRoute, new() { ["id"] = goldId }),
            ("holding-coin", AppShell.HoldingDetailRoute, new() { ["id"] = coinId }),
            ("asset-type-edit", AppShell.AssetTypeEditorRoute, new() { ["id"] = coinId }),
            ("asset-type-new", AppShell.AssetTypeEditorRoute, null),
            ("asset-purchase", AppShell.AssetEventEditorRoute, new() { ["type"] = goldId, ["kind"] = "Purchase" }),
            ("asset-move", AppShell.AssetEventEditorRoute, new() { ["type"] = goldId, ["kind"] = "LocationTransfer" }),
            ("importexport", AppShell.ImportExportRoute, null),
            ("import-overlap", AppShell.ImportExportRoute, null),
            ("reports", AppShell.ReportsRoute, null),
            ("report-commitments", AppShell.ReportsRoute, new() { ["report"] = 1 }),
            ("report-goals", AppShell.ReportsRoute, new() { ["report"] = 2 }),
            ("report-wealth", AppShell.ReportsRoute, new() { ["report"] = 3 }),
            ("report-history", AppShell.ReportsRoute, new() { ["report"] = 4 }),
            ("report-status", AppShell.ReportsRoute, new() { ["report"] = 5 }),
            ("report-kpi", AppShell.KpiSheetRoute, new() { ["sheet"] = new Features.Reports.KpiExplanation("K05", "+700.00 EUR", "October · EUR · Accounts in totals", [new("Income", "3,000.00 EUR", false), new("= Surplus", "+700.00 EUR", true)], null, null) }),
            ("report-review", AppShell.ReviewRoute, null),
            ("backup", AppShell.BackupRoute, null),
            ("settings", AppShell.SettingsRoute, null),
            ("settings-display", AppShell.SettingsRoute, null),
            ("settings-reopened", AppShell.SettingsRoute, null),
            ("settings-actions", AppShell.SettingsRoute, null),
            ("regional-settings", AppShell.SettingsRoute, null),
            ("review-reminder", AppShell.SettingsRoute, null),
            ("profiles", AppShell.ProfilesRoute, null),
            ("about", AppShell.AboutRoute, null),
            ("notices", AppShell.NoticesRoute, null),
            ("more", "//more", null),
        };

        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
#if WINDOWS
            // VAFADAR_SNAPSHOT_HELP=1 opens every "?" help (Help_{Topic}_Title) instead of the screens and saves the window
            // with the dialog as <language>-help-<topic>-popup1.png, to review the texts at the chosen size and theme.
            if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_HELP") == "1")
            {
                await CaptureHelpAsync(app, folder, language);
                continue;
            }
#endif
            // VAFADAR_SNAPSHOT_ONLY=report,holding shoots only the screens whose name starts with one of the prefixes.
            var only = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
            // A filter that matches nothing (e.g. "home budget", a list joined with spaces) must not end as an empty run.
            if (only is { Length: > 0 } && !screens.Any(s => only.Any(o => s.Name.StartsWith(o, StringComparison.Ordinal))))
            {
                throw new ArgumentException($"VAFADAR_SNAPSHOT_ONLY '{string.Join(',', only)}' matches no screen.");
            }

            foreach (var (name, route, query) in screens.Where(s => only is null || only.Any(o => s.Name.StartsWith(o, StringComparison.Ordinal))))
            {
                var splitEntries = name == "split-readable" ? System.Text.Json.JsonSerializer.Serialize(
                    await services.GetRequiredService<ZananceStore>().GetEntriesAsync()) : null;
                // AT-106 prepares only the walk-through's fictitious transfer; ordinary editor launches never seed fees.
                var actualQuery = name == "entry-fee-retention" ? await PrepareDestinationFeeReviewAsync(services) : query;
                await (actualQuery is null ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, actualQuery));
                await Task.Delay(1500);
                if (name.StartsWith("receipt-", StringComparison.Ordinal)
                    && (app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() ?? Shell.Current.CurrentPage)?.BindingContext
                        is Features.Entries.EntryEditorViewModel receiptEditor)
                {
                    await VerifyReceiptEditorAsync(services, name, receiptEditor, expenseId);
                }

                if (name == "import-overlap" && Shell.Current.CurrentPage is Features.DataFiles.ImportExportPage importPage
                    && importPage.BindingContext is Features.DataFiles.ImportExportViewModel importVm)
                {
                    await CaptureImportChoicesAsync(app, services, importPage, importVm, folder, language);
                }

                await CaptureAsync(app, folder, $"{language}-{name}");
                if (name == "backup" && Shell.Current!.CurrentPage?.BindingContext is Features.Backup.BackupViewModel backupVm)
                {
                    await CaptureCloudStatesAsync(app, backupVm, folder, language);
                }
#if WINDOWS
                if (name.StartsWith("entry-asset-", StringComparison.Ordinal)
                    && Shell.Current.CurrentPage?.BindingContext is Features.Entries.EntryEditorViewModel assetEditor)
                {
                    await CaptureAssetConfirmationAsync(app, services, assetEditor, folder, $"{language}-{name}", confirmationAsset.Id);
                }

                if (name == "budget-periods" && Shell.Current.CurrentPage is Features.Budget.BudgetPage budgetPeriods)
                {
                    await ReviewBudgetPeriodsAsync(app, services, budgetPeriods, folder, language);
                }

                if (name == "budget-readouts" && Shell.Current.CurrentPage is Features.Budget.BudgetPage budgetReadouts)
                {
                    await ReviewBudgetReadoutsAsync(app, services, budgetReadouts, folder, language);
                }

                if (name == "insights-tabs")
                {
                    await ReviewInsightsTabsAsync(app, services, folder, language);
                }

                if (name == "regional-settings")
                {
                    await CaptureHelpAsync(app, folder, language, ["RegionalFormat"]);
                }

                if (name == "review-reminder" && Shell.Current.CurrentPage is Features.Settings.SettingsPage reviewSettings
                    && FindScrollView(reviewSettings) is { } reviewScroll)
                {
                    await reviewScroll.ScrollToAsync(reviewSettings.FindByName<Border>("NotificationCard"), ScrollToPosition.Start, animated: false);
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-notifications");
                    await CaptureHelpAsync(app, folder, language, ["ReviewReminder"]);
                }

                if (name == "goal-edit" && Shell.Current.CurrentPage is Features.Goals.GoalEditorPage goalPage)
                {
                    if (FindScrollView(goalPage) is { } goalScroll)
                    {
                        await goalScroll.ScrollToAsync(goalPage.FindByName<Switch>("ContributionReminderSwitch"), ScrollToPosition.Center, animated: false);
                        await Task.Delay(500);
                        await CaptureAsync(app, folder, $"{language}-{name}-reminder");
                    }
                    await CaptureHelpAsync(app, folder, language, ["GoalReminder"]);
                }

                if (name == "settings-display" && Shell.Current.CurrentPage is Features.Settings.SettingsPage displaySettings)
                {
                    await ReviewSettingsDisplayAsync(app, services, displaySettings, folder, language);
                }

                if (name == "headers" && Shell.Current.CurrentPage is Features.Settings.SettingsPage headerPage)
                {
                    await ReviewHeadersAsync(app, services, headerPage, folder, language);
                }

                if (name == "settings-reopened")
                {
                    await ReviewReopenedSettingsAsync(app, services, folder, language);
                }

#if WINDOWS
                if (name == "entry-details" && (app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() ?? Shell.Current.CurrentPage)
                    is Features.Entries.EntryEditorPage detailsPage)
                { await ReviewEntryDetailsAsync(app, services, detailsPage, folder, language); }
                if (name == "entry-tags" && (app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() ?? Shell.Current.CurrentPage)
                    is Features.Entries.EntryEditorPage tagPage)
                { await ReviewTagChoicesAsync(app, services, tagPage, folder, language); }
                if ((app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() ?? Shell.Current.CurrentPage) is ContentPage readoutPage)
                {
                    await ReviewAmountReadoutsAsync(app, readoutPage, folder, language + "-" + name);
                }

                if (name == "date-inputs" && Shell.Current.CurrentPage is ContentPage datePage)
                {
                    await ReviewDateInputsAsync(app, services, datePage, folder, language);
                }

                if (name is "loan-actions" or "settings-actions" && Shell.Current.CurrentPage is ContentPage actionPage)
                {
                    await CaptureWrappingActionsAsync(app, actionPage, folder, language + "-" + name);
                }
#endif

                if (name == "settings" && Shell.Current.CurrentPage is Features.Settings.SettingsPage settingsPage)
                {
                    await CaptureSecurityAsync(app, services, settingsPage, folder, language);
                }
#endif
                if (FindScrollView(app.Windows[0].Page) is { } scroll && scroll.ContentSize.Height > scroll.Height + 40)
                {
#if WINDOWS
                    await ScrollToEndIfNeededAsync(scroll);
#else
                    await scroll.ScrollToAsync(0, scroll.ContentSize.Height, animated: false);
#endif
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-end");
                }

                if (splitEntries is not null)
                {
                    var splitPage = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as Features.Entries.SplitEditorPage;
                    if (splitPage?.BindingContext is not Features.Entries.SplitEditorViewModel { Error: null } splitVm
                        || string.IsNullOrEmpty(splitVm.TotalText) || splitVm.Parts.Count < 2
                        || splitEntries != System.Text.Json.JsonSerializer.Serialize(
                            await services.GetRequiredService<ZananceStore>().GetEntriesAsync()))
                    { throw new InvalidOperationException("The eligible split review lacks its real value or wrote ledger entries."); }
                    await File.WriteAllTextAsync(Path.Combine(folder, language + "-split-readable-proof.json"),
                        "{\"ActualEligibleModal\":true,\"CompleteAmountPacket\":true,\"StoredEntriesUnchanged\":true}");
                }

#if WINDOWS
                if (name == "settlement-feedback")
                { await ReviewSettlementFeedbackAsync(app, services, folder, language); }
                if (name == "entry-fee-retention")
                { await ReviewDestinationFeeRetentionAsync(app, services, folder, language); }
                if (name == "entry-validation")
                { await ReviewEntryValidationAsync(app, services, folder, language); }
                if (name == "plan-validation")
                { await ReviewPlanValidationAsync(app, services, folder, language); }
                if (name == "home" && Shell.Current.CurrentPage is Features.Home.HomePage { BindingContext: Features.Home.HomeViewModel } homePage)
                {
                    await ReviewHomeSnapshotAsync(services, homePage, folder, language);
                    await ReviewHomeAccountVisibilityAsync(app, services, homePage, folder, language);
                }
                if (name == "home-layout" && Shell.Current.CurrentPage is Features.Home.HomeLayoutPage layoutPage)
                { await ReviewHomeLayoutActionsAsync(app, services, layoutPage, folder, language); }
                if (name == "accounts" && Shell.Current.CurrentPage is Features.Accounts.AccountsPage accountsPage)
                { await ReviewDebtEntryActionAsync(app, services, accountsPage, folder, language); }
                if (name == "modal-headers" && Shell.Current.CurrentPage is Features.Accounts.AccountsPage modalParent)
                {
                    // Match the real budget Edit command contract; a budget editor has no meaningful month without it.
                    var modalBudget = (await services.GetRequiredService<ZananceStore>().GetBudgetsAsync()).First();
                    await ReviewModalHeadersAsync(app, services, modalParent,
                    [
                        ("account", AppShell.AccountEditorRoute, null),
                        ("debt", AppShell.AccountEditorRoute, new() { ["debt"] = true }),
                        ("receivable", AppShell.AccountEditorRoute, new() { ["debt"] = AccountType.Lent }),
                        ("entry", AppShell.EntryEditorRoute, null),
                        ("plan", AppShell.PlanEditorRoute, null),
                        ("category", AppShell.CategoryEditorRoute, new() { ["id"] = foodId }),
                        ("budget", AppShell.BudgetEditorRoute, new() { ["year"] = modalBudget.Year, ["month"] = modalBudget.Month,
                            ["calendar"] = modalBudget.Calendar, ["currency"] = modalBudget.CurrencyCode, ["period"] = modalBudget.Period,
                            ["start"] = Core.Budgets.PeriodMath.MonthRange(modalBudget.Year, modalBudget.Month, modalBudget.Calendar).First }),
                        ("split", AppShell.SplitRoute, new() { ["id"] = splitIncomeId }),
                        ("goal", AppShell.GoalEditorRoute, new() { ["id"] = goalId }),
                        ("settlement", AppShell.SettlementRoute, new() { ["id"] = planId }),
                        ("asset-type", AppShell.AssetTypeEditorRoute, new() { ["id"] = coinId }),
                        ("asset-event", AppShell.AssetEventEditorRoute, new() { ["type"] = goldId, ["kind"] = "Purchase" }),
                        ("kpi", AppShell.KpiSheetRoute, new() { ["sheet"] = new Features.Reports.KpiExplanation("K05",
                            Core.Money.MoneyText.Format(1250, "EUR", localization.CurrentCulture), Translator.Instance["Accounts_Title"], [], null, null) }),
                    ], folder, language);
                }
#endif

                // The PDF of the reports screen (REP-07), written next to the screenshots.
                if (name is "plan-new" or "plan-debt-reminder" or "debt-new" or "receivable-new" or "loan-edit")
                {
                    await ReviewPlanDebtDraftAsync(app, folder, $"{language}-{name}", name);
                }

                if (name == "reports" && Shell.Current.CurrentPage?.BindingContext is Features.Reports.ReportsViewModel reports)
                {
                    await File.WriteAllBytesAsync(Path.Combine(folder, $"{language}-report.pdf"), await reports.CreatePdfAsync());
                    await reports.LoadAsync();
                }

                // The same budget read as envelopes (§10.3).
                if (name == "budget" && Shell.Current.CurrentPage?.BindingContext is Features.Budget.BudgetViewModel budget)
                {
                    await budget.SetMethodAsync(Core.Budgets.BudgetMethod.Envelopes);
                    await Task.Delay(800);
                    await CaptureAsync(app, folder, $"{language}-{name}-envelopes");
                    await budget.SetMethodAsync(Core.Budgets.BudgetMethod.Flex);
                    await Task.Delay(800);
                    await CaptureAsync(app, folder, $"{language}-{name}-flex");
                    await budget.SetMethodAsync(Core.Budgets.BudgetMethod.Limits);
                }

                // A what-if with a higher amount of the first plan item (FOR-10).
                if (Shell.Current.CurrentPage?.BindingContext is Features.Forecast.ForecastViewModel forecast
                    && forecast.Cards.SelectMany(c => c.Rows).FirstOrDefault(r => r.PlanAmount is > 0) is { PlanAmount: { } planned } row)
                {
                    await forecast.AssumeAmountAsync(row, planned * 6 / 5);
                    await Task.Delay(800);
                    await CaptureAsync(app, folder, $"{language}-{name}-scenario");
                    await forecast.ResetScenarioCommand.ExecuteAsync(null);
                }

                // A theme change while an editor is open (the device may turn dark on its own) keeps the editor and what
                // was typed; the colors change in place. The run fails otherwise.
                if (name == "entry-new" && Shell.Current.CurrentPage is { BindingContext: Features.Entries.EntryEditorViewModel editor } editorPage)
                {
                    var (amount, note) = (editor.AmountText, editor.Note);
                    editor.AmountText = "12.34";
                    editor.Note = "Theme check";
                    var themes = services.GetRequiredService<Presentation.ThemeService>();
                    themes.Set(dark ? Presentation.ThemeChoice.Light : Presentation.ThemeChoice.Dark);
                    await Task.Delay(1500);
                    if (FindScrollView(app.Windows[0].Page) is { } top)
                    {
                        await top.ScrollToAsync(0, 0, animated: false);
                        await Task.Delay(300);
                    }

                    await CaptureAsync(app, folder, $"{language}-{name}-theme-switched");
                    var kept = ReferenceEquals(Shell.Current.CurrentPage, editorPage) && editor.AmountText == "12.34" && editor.Note == "Theme check";
                    themes.Set(dark ? Presentation.ThemeChoice.Dark : Presentation.ThemeChoice.Light);
                    await Task.Delay(500);
                    (editor.AmountText, editor.Note) = (amount, note);
                    if (!kept)
                    {
                        throw new InvalidOperationException("A theme change closed the open editor or lost its input.");
                    }

                    // Back on a tab, the waiting rebuild renews the shell; there a theme change rebuilds it at once.
                    var shellBefore = app.Windows[0].Page;
                    await Shell.Current.GoToAsync("//home");
                    await Task.Delay(1500);
                    var renewedAfterEditing = !ReferenceEquals(app.Windows[0].Page, shellBefore);
                    shellBefore = app.Windows[0].Page;
                    themes.Set(dark ? Presentation.ThemeChoice.Light : Presentation.ThemeChoice.Dark);
                    await Task.Delay(1500);
                    var renewedOnTab = !ReferenceEquals(app.Windows[0].Page, shellBefore);
                    themes.Set(dark ? Presentation.ThemeChoice.Dark : Presentation.ThemeChoice.Light);
                    await Task.Delay(1500);
                    if (!renewedAfterEditing || !renewedOnTab)
                    {
                        throw new InvalidOperationException($"The shell was not renewed (after editing: {renewedAfterEditing}, on a tab: {renewedOnTab}).");
                    }
                }

                // The bulk selection of the transactions list (F2-TX-04).
                if (Shell.Current.CurrentPage?.BindingContext is Features.Transactions.TransactionsViewModel transactions)
                {
                    var unchangedEntries = System.Text.Json.JsonSerializer.Serialize(await services.GetRequiredService<ZananceStore>().GetEntriesAsync());
                    // D-76: the real page bindings show covered/loading, failure/retry and restored snapshots.
                    // All data belongs to this fictitious walk-through; these states never write a ledger entry.
                    await transactions.LoadAsync();
                    var delayedRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var covered = transactions.Loading.RunAsync(() => delayedRead.Task, () => { });
                    try
                    {
                        await Task.Delay(500);
                        await CaptureAsync(app, folder, $"{language}-{name}-loading");
                    }
                    finally
                    {
                        delayedRead.TrySetResult();
                        await covered;
                    }

                    try
                    {
                        await transactions.Loading.RunAsync(() => Task.FromException(new IOException("Fictitious snapshot read failure")), () => { });
                    }
                    catch (IOException) { /* The explicit fictitious failure is the state being reviewed. */ }
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-load-failed");
#if WINDOWS
                    await InvokeSnapshotRetryAsync(transactions);
#else
                    await transactions.LoadAsync();
#endif
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-reloaded");

                    transactions.StartSelectingCommand.Execute(null);
#if WINDOWS
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-select-empty");
                    InvokeWrappingAction(Shell.Current.CurrentPage, transactions.SelectAllCommand);
                    await Task.Delay(100);
                    if (!transactions.HasSelection) { throw new InvalidOperationException("Native Select all did not reach the existing command."); }
#else
                    transactions.SelectAllCommand.Execute(null);
#endif
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-select");
#if WINDOWS
                    InvokeWrappingAction(Shell.Current.CurrentPage, transactions.StopSelectingCommand);
                    await Task.Delay(100);
                    if (transactions.IsSelecting) { throw new InvalidOperationException("Native Cancel did not close selection."); }
#else
                    transactions.StopSelectingCommand.Execute(null);
#endif

                    // D-78: render the Undo notice without deleting a fixture entry or claiming an Undo workflow test.
                    var undoWasVisible = transactions.ShowUndo;
                    try
                    {
                        transactions.ShowUndo = true;
                        await Task.Delay(500);
                        await CaptureAsync(app, folder, $"{language}-{name}-undo-preview");
                    }
                    finally { transactions.ShowUndo = undoWasVisible; }

                    // A saved filter applied with one tap (REP-08).
                    if (transactions.SavedFilters.FirstOrDefault() is { } saved)
                    {
                        transactions.ApplySavedFilterCommand.Execute(saved);
                        await Task.Delay(800);
                        await CaptureAsync(app, folder, $"{language}-{name}-filter");
                    }
                    if (unchangedEntries != System.Text.Json.JsonSerializer.Serialize(await services.GetRequiredService<ZananceStore>().GetEntriesAsync()))
                    { throw new InvalidOperationException("Selection/layout inspection wrote financial entries."); }
                }

                if (!route.StartsWith("//", StringComparison.Ordinal))
                {
                    await Shell.Current.GoToAsync("//home");
                    await Task.Delay(500);
                }
            }
        }
    }

    // D-65: real rendered draft states and binding-independent domain checks; no fixture below saves a user draft.
    private static async Task ReviewPlanDebtDraftAsync(App app, string folder, string file, string name)
    {
        var page = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() ?? Shell.Current.CurrentPage;
        if (page is Features.Plans.PlanEditorPage planPage && page.BindingContext is Features.Plans.PlanEditorViewModel plan)
        {
            if (name == "plan-debt-reminder")
            {
                if (!plan.IsTransfer || plan.AmountModeIndex != (int)AmountMode.Unknown || plan.AutoPost || !plan.ReminderEnabled)
                {
                    throw new InvalidOperationException("A repayment reminder draft must not post an estimated installment.");
                }
            }
            else
            {
                if (plan.PresetIndex != 0)
                {
                    throw new InvalidOperationException("A blank plan must start as a single occurrence.");
                }

                var (year, month, _) = Vafadar.Localization.CalendarDates.Parts(plan.Start, plan.RuleCalendar);
                if (!Vafadar.Localization.CalendarDates.TryCreate(year, month, 20, plan.RuleCalendar, out var start))
                {
                    throw new InvalidOperationException("The sample monthly date is invalid.");
                }

                plan.Start = start;
                plan.PresetIndex = 3;
                plan.PresetIndex = 5;
                if (plan.UnitIndex != 2 || plan.IntervalText != "1")
                {
                    throw new InvalidOperationException("Custom repetition must preserve the monthly unit and interval.");
                }

                plan.PresetIndex = 3;
                plan.Name = "Monthly example";
                plan.AmountText = "25";
                plan.SelectedCategory = plan.Categories.FirstOrDefault();
                plan.EndIndex = (int)EndKind.AfterCount;
                plan.CountText = "3";
                if (!plan.ShowEnd || plan.Preview.Count != 3 || plan.Name != "Monthly example" || plan.AmountText != "25")
                {
                    throw new InvalidOperationException("The common count/category choices changed the draft or lost its preview.");
                }
            }

            if (FindScrollView(app.Windows[0].Page) is { } scroll)
            {
                await scroll.ScrollToAsync(planPage.FindByName<Border>("ScheduleCard"), ScrollToPosition.Start, animated: false);
                await Task.Delay(400);
                await CaptureAsync(app, folder, file + "-schedule");
                if (name == "plan-new")
                {
                    plan.CalendarIndex = 0;
                    plan.Start = new DateOnly(2027, 1, 31);
                    plan.EndIndex = 0;
                    plan.ShowRuleOptions = true;
                    await Task.Delay(400);
                    await scroll.ScrollToAsync(planPage.FindByName<Border>("ScheduleCard"), ScrollToPosition.Start, animated: false);
                    await Task.Delay(400);
                    await CaptureAsync(app, folder, file + "-month-end");
                }
            }
        }
        else if (page is Features.Accounts.AccountEditorPage accountPage && page.BindingContext is Features.Accounts.AccountEditorViewModel account)
        {
            if (!account.Form.IsDebtType || account.Form.ShowNegative)
            {
                throw new InvalidOperationException("The debt form must express the direction without a manual sign.");
            }

            var form = accountPage.FindByName<Features.Accounts.AccountFormView>("AccountForm");
            if (FindScrollView(app.Windows[0].Page) is { } scroll)
            {
                await scroll.ScrollToAsync(form.FindByName<Label>("OpeningAmountLabel"), ScrollToPosition.Start, animated: false);
                await Task.Delay(400);
                await CaptureAsync(app, folder, file + "-amount");
                account.Form.ShowDebtTerms = true;
                account.Form.ExpandedAccountDetails = true;
                if (name == "receivable-new")
                {
                    account.Form.HasDueDate = true;
                }

                await Task.Delay(400);
                await scroll.ScrollToAsync(form.FindByName<Label>("OpeningAmountLabel"), ScrollToPosition.Start, animated: false);
                await Task.Delay(400);
                await CaptureAsync(app, folder, file + "-options");
            }
        }
    }

    // A language change rebuilds the shell, which then returns to the previous tab; a navigation before that ends would
    // be undone (e.g. "More" instead of Home), so the walk-through waits for the rebuild instead of a fixed pause.
    private static async Task SetLanguageAsync(App app, ILocalizationService localization, string language)
    {
        var rebuilt = new TaskCompletionSource();
        void OnRebuilt(object? sender, EventArgs e) => rebuilt.TrySetResult();
        var waits = app.Windows[0].Page is AppShell && localization.CurrentLanguage.CultureName != language;
        app.ShellRebuilt += OnRebuilt;
        try
        {
            localization.SetLanguage(localization.SupportedLanguages.First(l => l.CultureName == language));
            if (waits)
            {
                await Task.WhenAny(rebuilt.Task, Task.Delay(10_000));
            }
        }
        finally
        {
            app.ShellRebuilt -= OnRebuilt;
        }

        await Task.Delay(1000);
    }

    /// <summary>Prepares a destination fee exclusively in the fictitious snapshot database for AT-106.</summary>
    private static async Task<Dictionary<string, object>> PrepareDestinationFeeReviewAsync(IServiceProvider services)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var entries = await store.GetEntriesAsync();
        var transfer = entries.Single(entry => entry.Kind == EntryKind.Transfer);
        var category = (await store.GetCategoriesAsync()).Single(item => item.SystemKey == DefaultCategories.Fees).Id;
        var fee = EntryActions.SyncDestinationFee(transfer, EntryActions.FindDestinationFee(transfer, entries), 75, category)!;
        var result = await store.SaveEntriesAsync([transfer, fee], []);
        if (!result.Succeeded) { throw new InvalidOperationException("The fictitious destination fee could not be prepared."); }
        return new() { ["id"] = transfer.Id };
    }

    // Sample data so that lists and details are not empty: a second account, income, expenses, a transfer with a
    // fee and a partial refund.
    private static async Task<(Guid ExpenseId, Guid FoodId)> SeedAsync(IServiceProvider services)
    {
        var store = services.GetRequiredService<ZananceStore>();
        await store.EnsureDefaultCategoriesAsync();
        var categories = await store.GetCategoriesAsync();
        Guid Category(string key) => categories.First(c => c.SystemKey == key).Id;
        var checking = (await store.GetAccountsAsync()).First(a => a.Type == AccountType.Checking);

        // The onboarding account opens today; open it earlier so the sample entries of the last days count everywhere.
        checking.OpeningDate = checking.OpeningDate.AddMonths(-3);
        await store.SaveAccountAsync(checking);
        var savings = new Account { Name = "Savings", Type = AccountType.Savings, CurrencyCode = checking.CurrencyCode, OpeningDate = checking.OpeningDate.AddDays(-7), OpeningBalance = 300_00 };
        await store.SaveAccountAsync(savings);
        await store.SaveAccountAsync(new Account { Name = "Car loan", Type = AccountType.Loan, CurrencyCode = checking.CurrencyCode, OpeningDate = checking.OpeningDate, OpeningBalance = -4_000_00, IncludeInTotals = false, Counterparty = "Bank", InterestRate = 4.9m, Installment = 185_00 });

        var today = DateOnly.FromDateTime(DateTime.Today);
        var groceries = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 4_380, Date = today, CategoryId = Category("Food"), Title = "Groceries", Payee = "Market", Tags = ["home"] };
        var salary = new LedgerEntry { Kind = EntryKind.Income, AccountId = checking.Id, Amount = 250_000, Date = today, CategoryId = Category("Salary") };
        var rent = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 95_000, Date = today.AddDays(-1), CategoryId = Category("Housing"), Title = "Rent" };
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = checking.Id, ToAccountId = savings.Id, Amount = 20_000, Date = today.AddDays(-1) };
        var fee = EntryActions.SyncTransferFee(transfer, null, 150, Category(DefaultCategories.Fees))!;
        var hotel = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 18_000, Date = today.AddDays(-2), CategoryId = Category("Other"), Title = "Hotel", ReimbursableAmount = 18_000, ReimbursedBy = "Employer", Tags = ["work trip"] };
        await store.SaveEntriesAsync([groceries, salary, rent, transfer, fee, hotel], []);
        await store.SaveEntryAsync(EntryActions.CreateRefund(groceries, 1_200, checking.Id, today));
        await store.AddAttachmentAsync(new EntryAttachment { EntryId = groceries.Id, FileName = "Market receipt.pdf", ContentType = "application/pdf", Data = new byte[48_000] });
        await store.SaveTemplateAsync(EntryTemplate.From(groceries, "Groceries", keepAmount: false));
        await store.SaveSavedFilterAsync(new SavedFilter { Name = "Food", Kind = 1, CategoryIds = [Category("Food")] });
        await store.SaveSavedFilterAsync(new SavedFilter { Name = "Work trip", Period = 3, Search = "#work trip" });
        await store.SaveCategoryRuleAsync(new Core.Categories.CategoryRule { Match = "Market", CategoryId = Category("Food"), Kind = Core.Categories.CategoryKind.Expense });
        var goalStore = services.GetRequiredService<GoalStore>();
        var travel = new Core.Goals.Goal { Name = "Travel", TargetAmount = 1_200_00, CurrencyCode = checking.CurrencyCode, TargetDate = today.AddMonths(5), Icon = "Airplane" };
        var insurance = new Core.Goals.Goal { Name = "Car insurance", TargetAmount = 720_00, CurrencyCode = checking.CurrencyCode, TargetDate = today.AddMonths(2), Priority = Core.Goals.GoalPriority.High, Icon = "VehicleCar" };
        await goalStore.SaveGoalAsync(travel);
        await goalStore.SaveGoalAsync(insurance);
        await goalStore.AddAllocationAsync(new Core.Goals.GoalAllocation { GoalId = insurance.Id, AccountId = savings.Id, Amount = 180_00, Date = today });
        await goalStore.AddAllocationAsync(new Core.Goals.GoalAllocation { GoalId = travel.Id, AccountId = savings.Id, Amount = 300_00, Date = today });

        // A balance goal on the savings account with a monthly plan, pinned to Home (ZEX phase 2).
        var emergency = new Core.Goals.Goal
        {
            Name = "Emergency fund", TargetAmount = 5_000_00, CurrencyCode = savings.CurrencyCode, Icon = "BuildingBank",
            Type = Core.Goals.GoalType.AccountBalance, AccountId = savings.Id, HomePin = 1,
        };
        await goalStore.SaveGoalAsync(emergency);
        await goalStore.SaveContributionPlanAsync(emergency.Id, new Core.Goals.ContributionPlan
        {
            Method = Core.Goals.ContributionMethod.FixedAmount,
            Amount = 250_00,
            ReminderEnabled = true,
            Rule = new Core.Plans.RecurrenceRule { Frequency = Core.Plans.Frequency.Monthly, Start = today.AddDays(5) },
        });
        await SeedHoldingsAsync(services, checking, today);
        await store.SaveTemplateAsync(new EntryTemplate { Name = "Coffee", Kind = EntryKind.Expense, AccountId = checking.Id, CategoryId = Category("Food"), Amount = 350 });
        return (groceries.Id, Category("Food"));
    }

    // Holdings (ZEX phase 3): 18k gold already owned, a purchase with a fee, a move to the bank box and a price; three
    // coins received as a gift without a price, so the value is shown as unknown.
    private static async Task SeedHoldingsAsync(IServiceProvider services, Account checking, DateOnly today)
    {
        var holdings = services.GetRequiredService<HoldingStore>();
        var gold = new Core.Holdings.AssetType { Name = "18k gold", PriceCurrencyCode = checking.CurrencyCode, Metal = Core.Holdings.Metal.Gold, PurityPer10000 = 7500 };
        var coin = new Core.Holdings.AssetType
        {
            Name = "Bahar Azadi coin", Kind = Core.Holdings.AssetKind.CoinOrBar, Dimension = Core.Holdings.AssetDimension.Count, Metal = Core.Holdings.Metal.Gold,
            PurityPer10000 = 9000, UnitWeightMg = 8_133, CountUnitName = "coins", PriceCurrencyCode = checking.CurrencyCode, SortOrder = 1,
        };
        await holdings.SaveTypeAsync(gold);
        await holdings.SaveTypeAsync(coin);
        var safe = await holdings.EnsureDefaultLocationAsync("Home safe");
        var bank = new Core.Holdings.AssetLocation { Name = "Bank box", SortOrder = 1 };
        await holdings.SaveLocationAsync(bank);
        await holdings.SaveEventAsync(new Core.Holdings.AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = Core.Holdings.AssetEventKind.Opening, Quantity = 20_000, BasisAmount = 1_000_00, Date = today.AddDays(-40) }, []);
        var purchase = new Core.Holdings.AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = Core.Holdings.AssetEventKind.Purchase, Quantity = 10_000, BasisAmount = 1_050_00, Date = today.AddDays(-3) };
        await holdings.SaveEventAsync(purchase,
        [
            new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = checking.Id, Amount = 1_050_00, Date = purchase.Date, Title = gold.Name },
            new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 20_00, Date = purchase.Date, CategoryId = await holdings.FeesCategoryAsync(), Title = "Fee: 18k gold" },
        ]);
        await holdings.SaveEventAsync(new Core.Holdings.AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, ToLocationId = bank.Id, Kind = Core.Holdings.AssetEventKind.LocationTransfer, Quantity = 5_000, Date = today.AddDays(-1) }, []);
        await holdings.SaveValuationAsync(new Core.Holdings.AssetValuation { AssetTypeId = gold.Id, CurrencyCode = gold.PriceCurrencyCode, Date = purchase.Date, PricePerUnitMilli = 105_00_000, Source = Core.Holdings.ValuationSource.Purchase });
        await holdings.SaveValuationAsync(new Core.Holdings.AssetValuation { AssetTypeId = gold.Id, CurrencyCode = gold.PriceCurrencyCode, Date = today, PricePerUnitMilli = 110_00_000 });
        await holdings.SaveEventAsync(new Core.Holdings.AssetEvent { AssetTypeId = coin.Id, LocationId = safe.Id, Kind = Core.Holdings.AssetEventKind.GiftReceived, Quantity = 3_000, Date = today.AddDays(-20), Note = "Wedding gift" }, []);

        // A quantity goal of 50 g with 2 g per month (ZEX phase 5) and a forecast saved two weeks ago.
        var goals = services.GetRequiredService<GoalStore>();
        var fifty = new Core.Goals.Goal { Name = "50 g gold", CurrencyCode = gold.PriceCurrencyCode, Type = Core.Goals.GoalType.HoldingQuantity, AssetTypeId = gold.Id, TargetAmount = 50_000, TargetDate = today.AddMonths(18), Icon = "Diamond" };
        await goals.SaveGoalAsync(fifty);
        await goals.SaveContributionPlanAsync(fifty.Id, new Core.Goals.ContributionPlan { Method = Core.Goals.ContributionMethod.FixedAmount, Amount = 2_000, AssumedPricePerUnitMilli = 105_00_000, Rule = new Core.Plans.RecurrenceRule { Frequency = Core.Plans.Frequency.Monthly, Start = today.AddDays(10) } });
        var store = services.GetRequiredService<ZananceStore>();
        var baseDate = today.AddDays(-14);
        var forecast = Core.Forecasts.ForecastCalculator.Compute(await store.GetAccountsAsync(), await store.GetEntriesAsync(), await services.GetRequiredService<PlanStore>().GetSchedulesAsync(), [], baseDate, baseDate.AddDays(30)).First();
        var snapshot = Core.Forecasts.ForecastSnapshot.From("Before October", forecast, [checking.Id], baseDate, null, "snapshot");
        snapshot.CreatedAt = DateTimeOffset.Now.AddDays(-14);
        await store.SaveForecastSnapshotAsync(snapshot);
    }

    private static async Task SeedBudgetAsync(IServiceProvider services, Guid foodId)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var settings = await store.GetSettingsAsync();
        var (year, month) = Core.Budgets.PeriodMath.MonthOf(DateOnly.FromDateTime(DateTime.Today), settings.BudgetCalendar);
        var (py, pm) = Core.Budgets.PeriodMath.Previous(year, month);
        await store.SaveBudgetAsync(new Core.Budgets.Budget { Year = py, Month = pm, Calendar = settings.BudgetCalendar, CurrencyCode = settings.ReportCurrencyCode, TotalLimit = 15_000 });
        var budget = new Core.Budgets.Budget { Year = year, Month = month, Calendar = settings.BudgetCalendar, CurrencyCode = settings.ReportCurrencyCode, TotalLimit = 120_000, Rollover = Core.Budgets.BudgetRollover.Surplus };
        budget.CategoryLimits.Add(new Core.Budgets.BudgetCategoryLimit { CategoryId = foodId, Limit = 4_000 });
        await store.SaveBudgetAsync(budget);

        // A weekly budget of the current week: Simple shows the period choice because it exists (ZEX-S0502).
        var week = Core.Budgets.BudgetPeriods.StartOf(Core.Budgets.BudgetPeriod.Week, DateOnly.FromDateTime(DateTime.Today), DayOfWeek.Monday);
        await store.SaveBudgetAsync(new Core.Budgets.Budget { Period = Core.Budgets.BudgetPeriod.Week, PeriodStart = week, CurrencyCode = settings.ReportCurrencyCode, TotalLimit = 30_000 });

        // Snapshots show the Advanced screens unless VAFADAR_SNAPSHOT_MODE=simple; Simple hides options but not data.
        settings.Mode = string.Equals(Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_MODE"), "simple", StringComparison.OrdinalIgnoreCase)
            ? Core.Settings.ExperienceMode.Simple
            : Core.Settings.ExperienceMode.Advanced;
        await store.SaveSettingsAsync(settings);
    }

    // A monthly rent with an overdue occurrence, an estimated phone bill and a salary that posts automatically.
    private static async Task<(Guid PlanId, DateOnly OverdueDate)> SeedPlansAsync(IServiceProvider services)
    {
        var ledger = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var categories = await ledger.GetCategoriesAsync();
        Guid Category(string key) => categories.First(c => c.SystemKey == key).Id;
        var checking = (await ledger.GetAccountsAsync()).First(a => a.Type == AccountType.Checking);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rentStart = today.AddDays(-3).AddMonths(-2);

        var rent = new Schedule
        {
            Name = "Rent",
            AccountId = checking.Id,
            CategoryId = Category("Housing"),
            Amount = 95_000,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = rentStart },
            ReminderEnabled = true,
        };
        var phone = new Schedule
        {
            ContractProvider = "Mobile Co.",
            CancellationDeadline = today.AddDays(20),
            ReviewDate = today.AddMonths(4),
            Name = "Phone bill",
            AccountId = checking.Id,
            CategoryId = Category("Communication"),
            AmountMode = AmountMode.Estimated,
            Amount = 2_990,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = today.AddDays(9), DayRule = MonthDayRule.LastWeekday },
        };
        var salary = new Schedule
        {
            Name = "Salary",
            Kind = EntryKind.Income,
            AccountId = checking.Id,
            CategoryId = Category("Salary"),
            Amount = 250_000,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = today.AddDays(20), DayRule = MonthDayRule.LastDayOfMonth },
            AutoPost = true,
            AutoPostFrom = today,
        };
        await plans.SaveSchedulesAsync([rent, phone, salary]);

        // Settle the first rent so that the plan has history.
        var states = await plans.GetStatesAsync(rent.Id);
        var first = Occurrences.Between(rent, states, rentStart, rentStart, today).Single();
        await plans.SettleAsync(first, Occurrences.CreateEntry(first, 95_000, rentStart, ReviewState.Confirmed));
        return (rent.Id, rentStart.AddMonths(1));
    }

    /// <summary>Checks receipt draft behaviour on fictitious data; saving remains an explicit user action.</summary>
    private static async Task VerifyReceiptEditorAsync(IServiceProvider services, string name,
        Features.Entries.EntryEditorViewModel editor, Guid expenseId)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var before = (await store.GetEntriesAsync()).Count;
        var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
        if (name == "receipt-found")
        {
            if (!Core.Money.MoneyText.TryParse(editor.AmountText, "EUR", culture, out var amount) || amount != 410)
            {
                throw new InvalidOperationException("A clear receipt did not fill the purchase total.");
            }

            var units = Core.Money.DisplayUnits.All;
            try
            {
                Core.Money.DisplayUnits.Set(units.Append(new Core.Money.DisplayUnit("EUR", "Euro x10", 1)));
                await editor.SaveCommand.ExecuteAsync(null);
                if (editor.SaveError is null || (await store.GetEntriesAsync()).Count != before)
                {
                    throw new InvalidOperationException("A changed display unit silently reinterpreted a receipt.");
                }
            }
            finally
            {
                Core.Money.DisplayUnits.Set(units);
                editor.UseReceiptAmountCommand.Execute(editor.ReceiptChoices[0]);
                editor.SaveError = null;
            }
        }
        else if (name == "receipt-review")
        {
            var original = editor.AmountText;
            if (!string.IsNullOrWhiteSpace(original) || editor.ReceiptChoices.Count != 2)
            {
                throw new InvalidOperationException("Conflicting totals filled a draft automatically.");
            }

            editor.UseReceiptAmountCommand.Execute(editor.ReceiptChoices[0]);
            if (!Core.Money.MoneyText.TryParse(editor.AmountText, "EUR", culture, out var chosen) || chosen != 1234)
            {
                throw new InvalidOperationException("A manual receipt choice did not fill the form.");
            }

            editor.AmountText = original;
        }
        else if (name == "receipt-reread-missing")
        {
            var existing = await store.GetEntryAsync(expenseId);
            if (!Core.Money.MoneyText.TryParse(editor.AmountText, "EUR", culture, out var amount) || existing?.Amount != amount)
            {
                throw new InvalidOperationException("A receipt without a total erased the existing amount.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(editor.AmountText))
        {
            throw new InvalidOperationException("Unsafe receipt evidence filled the draft automatically.");
        }

        if ((await store.GetEntriesAsync()).Count != before)
        {
            throw new InvalidOperationException("Reviewing a receipt changed the ledger before confirmation.");
        }
    }

    private static ScrollView? FindScrollView(Page? root)
    {
        var page = root is Shell shell ? shell.CurrentPage : root;
        if (page?.Navigation.ModalStack.LastOrDefault() is { } modal)
        {
            page = modal;
        }

        return page is ContentPage content ? Find(content.Content) : null;

        static ScrollView? Find(IView? view) => view switch
        {
            ScrollView scroll when scroll.IsVisible => scroll,
            Layout layout => layout.Children.Select(Find).FirstOrDefault(s => s is not null),
            ContentView contentView => Find(contentView.Content),
            _ => null,
        };
    }

    private static async Task CaptureAsync(App app, string folder, string name)
    {
        var rootPage = app.Windows[0].Page;
        var page = rootPage?.Navigation.ModalStack.LastOrDefault() ?? rootPage;
#if WINDOWS
        if (name.EndsWith("-home", StringComparison.Ordinal) && page is Shell { CurrentPage: Features.Home.HomePage home })
        {
            DebugFontScale.CheckQuickActions(home, folder, name);
        }
#endif
        var visible = page is Shell shell ? (shell.CurrentPage as VisualElement) ?? shell : page as VisualElement;
#if WINDOWS
        if (visible is ContentPage content)
        {
            try { DebugLayoutChecks.Check(content, folder, name); }
            catch
            {
                // Keep the actual own-window negative evidence alongside geometry; do not turn a failed check into a pass.
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, name + "-layout-failure-window.png"));
                throw;
            }
        }
#endif
        if (visible?.Parent is Shell && page is VisualElement root)
        {
            visible = root;
        }

        var result = visible is null ? null : await visible.CaptureAsync();
        if (result is null)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, name + ".txt"), "capture returned null");
            return;
        }

        await using var source = await result.OpenReadAsync(ScreenshotFormat.Png);
        await using var target = File.Create(Path.Combine(folder, name + ".png"));
        await source.CopyToAsync(target);
#if WINDOWS
        if ((page is Shell currentShell ? currentShell.CurrentPage : page) is ContentPage checkedPage
            && (checkedPage.FindByName<VisualElement>("ContentViewport") is not null || checkedPage is Features.Settings.SettingsPage or Features.Accounts.AccountDetailPage
                || DebugLayoutChecks.AppliesTo(checkedPage)
                || Shell.GetPresentationMode(checkedPage) is PresentationMode.Modal or PresentationMode.ModalAnimated or PresentationMode.ModalNotAnimated))
        {
            // D-78: include persistent actions and navigation in the app's own native-window rendering.
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, name + "-window.png"));
        }
#endif
    }

#if WINDOWS
    // VAFADAR_CAPTURE_WINDOW=<file.png> with VAFADAR_START_ROUTE: the whole window content, including the title area
    // and the navigation, rendered by the app itself. A capture from outside is blank while other windows cover it.
    // Open dialogs and menus are saved as <file>-popup1.png, <file>-popup2.png, ...
    private static async Task CaptureWindowAsync(Window window, string path)
    {
        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content: Microsoft.UI.Xaml.UIElement root })
        {
            return;
        }

        await RenderAsync(root, path);
        if (root.XamlRoot is not null)
        {
            var number = 0;
            foreach (var popup in Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot))
            {
                // Some layers of a menu have no size; rendering them fails with an invalid argument.
                if (popup.Child is Microsoft.UI.Xaml.FrameworkElement { ActualWidth: > 0, ActualHeight: > 0 } child)
                {
                    try
                    {
                        await RenderAsync(child, Path.ChangeExtension(path, null) + $"-popup{++number}.png");
                    }
                    catch (Exception exception)
                    {
                        await File.WriteAllTextAsync(Path.ChangeExtension(path, null) + $"-popup{number}.txt", exception.Message);
                    }
                }
            }
        }
    }

    // Opens the help of every topic that has a title, saves the window with the open dialog and closes the dialog again.
    private static async Task CaptureHelpAsync(App app, string folder, string language, IEnumerable<string>? selectedTopics = null)
    {
        var topics = Resources.Strings.AppStrings.ResourceManager
            .GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!
            .Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(k => k.StartsWith("Help_", StringComparison.Ordinal) && k.EndsWith("_Title", StringComparison.Ordinal))
            .Select(k => k["Help_".Length..^"_Title".Length])
            .Order(StringComparer.Ordinal);
        foreach (var topic in topics.Where(t => selectedTopics is null || selectedTopics.Contains(t, StringComparer.Ordinal)))
        {
            var shown = Vafadar.Maui.Controls.HelpButton.ShowAsync(topic);
            await Task.Delay(700);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-help-{topic}.png"));
            if (app.Windows[0].Handler?.PlatformView is Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
            {
                foreach (var popup in Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
                {
                    if (popup.Child is Microsoft.UI.Xaml.Controls.ContentDialog dialog)
                    {
                        dialog.Hide();
                    }
                }
            }

            await shown;
            await Task.Delay(200);
        }
    }

    // D-74: drive the actual native dialog through its UI Automation Invoke pattern, never desktop input/focus.
    private static async Task CaptureAssetConfirmationAsync(App app, IServiceProvider services,
        Features.Entries.EntryEditorViewModel editor, string folder, string file, Guid assetId)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var before = await store.GetEntriesAsync();
        var accountBefore = (await store.GetAccountsAsync()).Single(a => a.Id == assetId);
        editor.AmountText = 12.34m.ToString("0.00", services.GetRequiredService<ILocalizationService>().CurrentCulture);
        editor.Note = "Fictitious QA04 draft";
        var text = editor.AmountText;
        var shown = editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(700);
        await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, file + "-consent.png"));
        if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
        {
            throw new InvalidOperationException("The native confirmation window is unavailable.");
        }

        var dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
            .Select(p => p.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().Single();
        var cancel = services.GetRequiredService<Translator>()["Common_Cancel"];
        var button = FindNativeButton(dialog, cancel) ?? throw new InvalidOperationException("The confirmation cancel button is unavailable.");
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(button);
        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        {
            throw new InvalidOperationException("The confirmation button has no Invoke pattern.");
        }
        invoke.Invoke();
        await shown;
        var after = await store.GetEntriesAsync();
        var accountAfter = (await store.GetAccountsAsync()).Single(a => a.Id == assetId);
        if (after.Count != before.Count || !after.Select(e => e.Id).Order().SequenceEqual(before.Select(e => e.Id).Order())
            || accountAfter.OpeningBalance != accountBefore.OpeningBalance || editor.AmountText != text
            || editor.Note != "Fictitious QA04 draft" || editor.IsBusy || editor.SaveError is not null)
        {
            throw new InvalidOperationException("Cancelling valued-asset consent changed the ledger, value or draft.");
        }
        // Navigation to the next fictitious screen must not trigger an unrelated unsaved-draft dialog.
        editor.AmountText = string.Empty;
        editor.Note = string.Empty;
    }

    // D-81: WinUI emits no ViewChanged event for an already reached/clamped position; a redundant MAUI await can hang.
    // Compare actual native offsets first. The helpers only move the fictitious review's own scroll view.
    private static async Task ScrollToEndIfNeededAsync(ScrollView scroll)
    {
        if (scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native)
        { throw new InvalidOperationException("The real native review viewport is missing."); }
        if (native.ScrollableHeight <= 1 || native.VerticalOffset >= native.ScrollableHeight - 1) { return; }
        NativeScrollProvider(native).SetScrollPercent(-1, 100);
        await Task.Delay(300);
    }

    private static async Task ScrollToViewIfNeededAsync(ScrollView scroll, VisualElement target)
    {
        if (scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native
            || target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement element)
        { throw new InvalidOperationException("The actual native review target/viewport is missing."); }
        if (native.ScrollableHeight <= 1) { return; }
        var point = element.TransformToVisual(native).TransformPoint(new Windows.Foundation.Point(0, 0));
        var wanted = Math.Clamp(native.VerticalOffset + point.Y, 0, native.ScrollableHeight);
        if (Math.Abs(wanted - native.VerticalOffset) <= 1) { return; }
        NativeScrollProvider(native).SetScrollPercent(-1, wanted / native.ScrollableHeight * 100);
        await Task.Delay(300);
    }

    private static Microsoft.UI.Xaml.Automation.Provider.IScrollProvider NativeScrollProvider(Microsoft.UI.Xaml.Controls.ScrollViewer native)
    {
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(native);
        return peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll) as Microsoft.UI.Xaml.Automation.Provider.IScrollProvider
            ?? throw new InvalidOperationException("The actual native viewport has no Scroll pattern.");
    }

    // D-81: scroll the real native value to both ends through its Scroll pattern; no mouse/keyboard/focus input.
    private static async Task ReviewAmountReadoutsAsync(App app, ContentPage page, string folder, string name)
    {
        var evidence = new List<object>();
        var index = 0;
        foreach (var readout in VisualDescendants(page).OfType<Presentation.AmountReadout>())
        {
            var visible = true;
            for (Element? parent = readout; parent is not null; parent = parent.Parent)
            { if (parent is VisualElement { IsVisible: false }) { visible = false; break; } }
            if (!visible) { continue; }
            var scroll = VisualDescendants(readout).OfType<ScrollView>().Single();
            if (scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native) { continue; }
            if (native.ScrollableWidth <= 1) { continue; }
            if (FindScrollView(page) is { } outer && !ReferenceEquals(outer, scroll))
            { await ScrollToViewIfNeededAsync(outer, readout); }
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(native);
            if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll) is not Microsoft.UI.Xaml.Automation.Provider.IScrollProvider provider)
            { throw new InvalidOperationException("An oversized value has no real native Scroll pattern."); }
            provider.SetScrollPercent(100, -1);
            await Task.Delay(400);
            var end = native.HorizontalOffset;
            if (end < native.ScrollableWidth - 1) { throw new InvalidOperationException("Native amount scrolling did not reach the full unit/end."); }
            await CaptureAsync(app, folder, name + "-amount-end-" + ++index);
            provider.SetScrollPercent(0, -1);
            await Task.Delay(400);
            if (native.HorizontalOffset > 1) { throw new InvalidOperationException("Native amount scrolling did not restore the sign/start."); }
            await CaptureAsync(app, folder, name + "-amount-start-" + index);
            evidence.Add(new { readout.AmountText, native.ViewportWidth, native.ExtentWidth, native.ScrollableWidth,
                offsetAtEnd = end, offsetAtStart = native.HorizontalOffset, nativeScrollPattern = true });
        }
        if (evidence.Count > 0)
        { await File.WriteAllTextAsync(Path.Combine(folder, name + "-amount-scroll-proof.json"), System.Text.Json.JsonSerializer.Serialize(evidence)); }
    }

    // D-81: measure the actual date boxes with a programmatic draft, without focus, Save or ledger writes; inspect all supported calendars.
    private static async Task ReviewDateInputsAsync(App app, IServiceProvider services, ContentPage page, string folder, string language)
    {
        var field = VisualDescendants(page).OfType<Vafadar.Maui.Controls.DateField>().Single();
        var originalDate = field.Date;
        var originalCalendar = field.CalendarOverride;
        var unchanged = System.Text.Json.JsonSerializer.Serialize(await services.GetRequiredService<ZananceStore>().GetEntriesAsync());
        try
        {
            field.CalendarOverride = CalendarSystem.Gregorian;
            var boxes = VisualDescendants(field).OfType<Entry>().ToArray();
            Entry Box(string part) => boxes.Single(box => SemanticProperties.GetDescription(box) == Translator.Instance["DateField_" + part]);
            void Set(Entry box, string value) => box.Text = value;
            Set(Box("Year"), "2027");
            Set(Box("Month"), "02");
            Set(Box("Day"), "28");
            await Task.Delay(100);
            if (field.Date != new DateOnly(2027, 2, 28)) { throw new InvalidOperationException("Programmatic date input did not reach its real binding."); }
            Set(Box("Year"), "2");
            await Task.Delay(100);
            if (field.Date != new DateOnly(2027, 2, 28) || Box("Year").Text != "2")
            { throw new InvalidOperationException("Partial year input rewrote the stored date or the draft."); }
            if (FindScrollView(page) is not { } scroll) { throw new InvalidOperationException("Date review needs its real scroll viewport."); }
            await ScrollToViewIfNeededAsync(scroll, field);
            await Task.Delay(300);
            await CaptureAsync(app, folder, language + "-date-inputs-partial-year");
            Set(Box("Year"), "2027");
            foreach (var calendar in new[] { CalendarSystem.Gregorian, CalendarSystem.Persian, CalendarSystem.Hijri })
            {
                field.CalendarOverride = calendar;
                await Task.Delay(200);
                var (year, month, day) = CalendarDates.Parts(field.Date, calendar);
                if (Box("Year").Text != year.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    || Box("Month").Text != month.ToString("00", System.Globalization.CultureInfo.InvariantCulture)
                    || Box("Day").Text != day.ToString("00", System.Globalization.CultureInfo.InvariantCulture)
                    || field.Date != new DateOnly(2027, 2, 28))
                { throw new InvalidOperationException("A calendar redraw changed the bound date or its displayed parts."); }
                await CaptureAsync(app, folder, language + "-date-inputs-" + calendar);
            }
        }
        finally { field.CalendarOverride = originalCalendar; field.Date = originalDate; }
        if (unchanged != System.Text.Json.JsonSerializer.Serialize(await services.GetRequiredService<ZananceStore>().GetEntriesAsync()))
        { throw new InvalidOperationException("Native date review wrote ledger entries."); }
        await File.WriteAllTextAsync(Path.Combine(folder, language + "-date-inputs-proof.json"),
            "{\"ProgrammaticDraftInput\":true,\"PartialYearPreserved\":true,\"ThreeCalendarParts\":true,\"StoredEntriesUnchanged\":true}");
    }

    private static IEnumerable<VisualElement> VisualDescendants(IVisualTreeElement root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is VisualElement visual) { yield return visual; }
            foreach (var descendant in VisualDescendants(child)) { yield return descendant; }
        }
    }

    // D-80: scroll every currently visible growing action into the real viewport; never invoke its command.
    private static async Task CaptureWrappingActionsAsync(App app, ContentPage page, string folder, string name)
    {
        if (page.BindingContext is Features.Settings.SettingsViewModel settings) { await settings.LoadAsync(); }
        if (FindScrollView(page) is not { } scroll) { throw new InvalidOperationException("Action review needs its real scroll viewport."); }
        var index = 0;
        foreach (var action in WrappingActions(page))
        {
            var visible = true;
            for (Element? parent = action; parent is not null; parent = parent.Parent)
            {
                if (parent is VisualElement { IsVisible: false }) { visible = false; break; }
            }
            if (!visible) { continue; }
            await scroll.ScrollToAsync(action, ScrollToPosition.Start, animated: false);
            await Task.Delay(300);
            await CaptureAsync(app, folder, $"{name}-action-{++index}");
        }
        if (index == 0) { throw new InvalidOperationException("No visible growing action was reviewed."); }
    }

    private static IEnumerable<Presentation.WrappingAction> WrappingActions(IVisualTreeElement root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is Presentation.WrappingAction action) { yield return action; }
            foreach (var descendant in WrappingActions(child)) { yield return descendant; }
        }
    }

    // D-80: select/cancel are non-writing real commands; invoke their actual transparent native button.
    private static void InvokeWrappingAction(Page page, System.Windows.Input.ICommand command)
    {
        var action = WrappingActions(page).Single(a => ReferenceEquals(a.Command, command));
        if (action.Content is not Grid grid || grid.Children.LastOrDefault() is not Button button
            || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native)
        { throw new InvalidOperationException("The real growing action button is unavailable."); }
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native);
        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        { throw new InvalidOperationException("The growing action has no native Invoke pattern."); }
        invoke.Invoke();
    }

    // D-79: actual form publication, native retry and live captions, using fictitious data without PIN operations.
    private static async Task ReviewSettingsDisplayAsync(App app, IServiceProvider services,
        Features.Settings.SettingsPage page, string folder, string language)
    {
        var vm = (Features.Settings.SettingsViewModel)page.BindingContext;
        await vm.LoadAsync();
        var store = services.GetRequiredService<ZananceStore>();
        var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var before = System.Text.Json.JsonSerializer.Serialize(await store.GetSettingsAsync());
        var entries = System.Text.Json.JsonSerializer.Serialize(await store.GetEntriesAsync());
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loading = vm.Loading.RunAsync(() => held.Task, vm.RefreshDisplay);
        var form = page.FindByName<ScrollView>("SettingsContent");
        await Task.Delay(300);
        if (form.IsVisible || form.IsEnabled || vm.Loading.IsReady) { throw new InvalidOperationException("Settings input is exposed during its read."); }
        await CaptureAsync(app, folder, $"{language}-settings-display-loading");
        held.SetResult(); await loading;
        try { await vm.Loading.RunAsync(() => Task.FromException(new IOException("Fictitious settings read failure")), vm.RefreshDisplay); }
        catch (IOException) { }
        await Task.Delay(300);
        if (form.IsVisible || form.IsEnabled || vm.Loading.IsReady) { throw new InvalidOperationException("Failed Settings input is exposed."); }
        await CaptureAsync(app, folder, $"{language}-settings-display-failure");
        if (page.FindByName<Button>("RetrySettingsButton")?.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button retry)
        { throw new InvalidOperationException("The Settings retry button is unavailable."); }
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(retry);
        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        { throw new InvalidOperationException("Settings retry has no Invoke pattern."); }
        invoke.Invoke();
        for (var i = 0; i < 100 && !vm.Loading.IsReady; i++) { await Task.Delay(50); }
        if (!vm.Loading.IsReady || !form.IsVisible || !form.IsEnabled) { throw new InvalidOperationException("Settings retry did not publish."); }
        await CaptureAsync(app, folder, $"{language}-settings-display-retry");
        var draft = vm.EssentialText; var period = vm.EssentialPeriodIndex;
        var choices = (vm.ModeIndex, vm.ThemeIndex, vm.FreshnessIndex, vm.StartDayIndex, vm.DefaultAccount?.Id);
        try
        {
            vm.EssentialText = "17.25"; vm.EssentialPeriodIndex = 2;
            foreach (var target in new[] { "de", "fa", "en", language })
            {
                vm.SelectedLanguage = vm.Languages.First(l => l.CultureName == target);
                await Task.Delay(300);
                if (localization.CurrentLanguage.CultureName != target || vm.SelectedLanguage?.CultureName != target
                    || vm.ModeNames[0] != translator["Mode_Simple"] || vm.ThemeNames[0] != translator["Theme_System"]
                    || vm.EssentialPeriodNames[2] != translator["Settings_PerMonth"]
                    || vm.EssentialText != "17.25" || vm.EssentialPeriodIndex != 2
                    || choices != (vm.ModeIndex, vm.ThemeIndex, vm.FreshnessIndex, vm.StartDayIndex, vm.DefaultAccount?.Id))
                { throw new InvalidOperationException("A live Settings display change lost its language, choices or unsaved draft."); }
                await form.ScrollToAsync(page.FindByName<Border>("AppearanceCard"), ScrollToPosition.Start, animated: false);
                await Task.Delay(200);
                await CaptureAsync(app, folder, $"{language}-settings-display-live-{target}");
            }
        }
        finally { vm.EssentialText = draft; vm.EssentialPeriodIndex = period; }
        if (before != System.Text.Json.JsonSerializer.Serialize(await store.GetSettingsAsync())
            || entries != System.Text.Json.JsonSerializer.Serialize(await store.GetEntriesAsync()))
        { throw new InvalidOperationException("Settings display/retry changed preferences or ledger data."); }
        File.WriteAllText(Path.Combine(folder, language + "-settings-display-proof.json"),
            System.Text.Json.JsonSerializer.Serialize(new { FormCoveredUntilPublication = true, NativeRetry = true,
                LiveChoiceCaptions = true, UnsavedInputPreserved = true, StoredPreferencesAndEntriesUnchanged = true }));
    }

    // D-82: keep old shells alive so collection cannot conceal a translated title still reaching a retired renderer.
    private static async Task ReviewReopenedSettingsAsync(App app, IServiceProvider services, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var preferences = System.Text.Json.JsonSerializer.Serialize(await store.GetSettingsAsync());
        var entries = System.Text.Json.JsonSerializer.Serialize(await store.GetEntriesAsync());
        var accounts = System.Text.Json.JsonSerializer.Serialize(await store.GetAccountsAsync());
        var retired = new List<(AppShell Shell, (BaseShellItem Item, string Title)[] Titles)>();
        var changes = new List<object>();
        foreach (var target in new[] { "de", "fa", "en", language })
        {
            if (Shell.Current.CurrentPage is not Features.Settings.SettingsPage page
                || page.BindingContext is not Features.Settings.SettingsViewModel vm)
            { throw new InvalidOperationException("Reopened Settings is unavailable."); }
            for (var i = 0; i < 100 && !vm.Loading.IsReady; i++) { await Task.Delay(50); }
            if (!vm.Loading.IsReady) { throw new InvalidOperationException("Reopened Settings did not publish."); }
            var oldShell = (AppShell)app.Windows[0].Page!;
            var changed = localization.CurrentLanguage.CultureName != target;
            var draft = vm.EssentialText; var period = vm.EssentialPeriodIndex;
            var choices = (vm.ModeIndex, vm.ThemeIndex, vm.FreshnessIndex, vm.StartDayIndex, vm.DefaultAccount?.Id);
            try
            {
                vm.EssentialText = "17.25"; vm.EssentialPeriodIndex = 2;
                var picker = VisualDescendants(page).OfType<Picker>().Single(p => ReferenceEquals(p.ItemsSource, vm.Languages));
                await SelectSnapshotLanguageAsync(picker, vm.Languages.IndexOf(vm.Languages.First(l => l.CultureName == target)));
                await Task.Delay(300);
                if (!ReferenceEquals(Shell.Current.CurrentPage, page) || !ReferenceEquals(app.Windows[0].Page, oldShell)
                    || localization.CurrentLanguage.CultureName != target || vm.SelectedLanguage?.CultureName != target
                    || !Equals(picker.SelectedItem, vm.SelectedLanguage) || page.Title != translator["Settings_Title"]
                    || vm.ModeNames[0] != translator["Mode_Simple"] || vm.ThemeNames[0] != translator["Theme_System"]
                    || vm.EssentialText != "17.25" || vm.EssentialPeriodIndex != 2
                    || choices != (vm.ModeIndex, vm.ThemeIndex, vm.FreshnessIndex, vm.StartDayIndex, vm.DefaultAccount?.Id))
                { throw new InvalidOperationException("Reopened Settings lost its translated choices or open draft."); }
                if (retired.SelectMany(r => r.Titles).Any(t => t.Item.Title != t.Title))
                { throw new InvalidOperationException("A retired navigation title still receives translations."); }
                await CaptureAsync(app, folder, $"{language}-settings-reopened-live-{target}");
            }
            finally { vm.EssentialText = draft; vm.EssentialPeriodIndex = period; }

            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnRebuilt(object? sender, EventArgs e) => rebuilt.TrySetResult();
            app.ShellRebuilt += OnRebuilt;
            try
            {
                InvokeSnapshotBack(page);
                if (changed) { await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
                else { await Task.Delay(300); }
            }
            finally { app.ShellRebuilt -= OnRebuilt; }
            if (changed)
            {
                if (ReferenceEquals(app.Windows[0].Page, oldShell))
                { throw new InvalidOperationException("Returning from Settings did not replace the old shell."); }
                var titles = oldShell.Items.SelectMany(item => new BaseShellItem[] { item }
                    .Concat(item.Items.SelectMany(section => new BaseShellItem[] { section }.Concat(section.Items))))
                    .Select(item => (item, item.Title)).ToArray();
                retired.Add((oldShell, titles));
            }
            var current = (AppShell)app.Windows[0].Page!;
            var home = current.Items.SelectMany(i => i.Items).SelectMany(s => s.Items).Single(c => c.Route == "home");
            if (home.Title != translator["Tab_Home"])
            { throw new InvalidOperationException("Current navigation titles did not translate."); }
            // ShellRebuilt reports navigation completion, not completion of Home's async read or native arrangement.
            if (current.CurrentPage.BindingContext is Features.Home.HomeViewModel homeVm)
            { await homeVm.LoadAsync().WaitAsync(TimeSpan.FromSeconds(10)); }
            await WaitForReopenedLayoutAsync(current.CurrentPage);
            await CaptureAsync(app, folder, $"{language}-settings-reopened-return-{target}");
            await current.GoToAsync(AppShell.SettingsRoute, animate: false);
            await Task.Delay(500);
            changes.Add(new { Language = target, ActualNativeSelection = true, LiveDraftPreserved = true,
                ShellRebuiltOnReturn = changed, RetiredShellsHeld = retired.Count });
        }
        var currentPreferences = System.Text.Json.JsonSerializer.Serialize(await store.GetSettingsAsync());
        var entriesUnchanged = entries == System.Text.Json.JsonSerializer.Serialize(await store.GetEntriesAsync());
        var accountsUnchanged = accounts == System.Text.Json.JsonSerializer.Serialize(await store.GetAccountsAsync());
        if (preferences != currentPreferences || !entriesUnchanged || !accountsUnchanged)
        {
            using var before = System.Text.Json.JsonDocument.Parse(preferences);
            using var after = System.Text.Json.JsonDocument.Parse(currentPreferences);
            var changedFields = before.RootElement.EnumerateObject().Where(p =>
                p.Value.GetRawText() != after.RootElement.GetProperty(p.Name).GetRawText()).Select(p => p.Name).ToArray();
            File.WriteAllText(Path.Combine(folder, language + "-settings-reopened-data-failure.json"),
                System.Text.Json.JsonSerializer.Serialize(new { ChangedPreferenceFields = changedFields, entriesUnchanged, accountsUnchanged }));
            throw new InvalidOperationException("Reopened Settings changed stored preferences or financial data.");
        }
        File.WriteAllText(Path.Combine(folder, language + "-settings-reopened-proof.json"),
            System.Text.Json.JsonSerializer.Serialize(new { Changes = changes, RetiredTitlesFrozen = true,
                StoredPreferencesEntriesAccountsUnchanged = true, NoFinancialSave = true }));
        GC.KeepAlive(retired);
    }

    // Wait for native arrangement; the following geometry checks still reject small or overlapping targets.
    private static async Task WaitForReopenedLayoutAsync(Page page)
    {
        static bool Visible(VisualElement element)
        {
            for (Element? parent = element; parent is not null; parent = parent.Parent)
            { if (parent is VisualElement { IsVisible: false }) { return false; } }
            return true;
        }
        for (var attempt = 0; attempt < 100; attempt++)
        {
            await Task.Delay(50);
            var heading = page.FindByName<Button>("BalanceHeadingAction");
            var rows = VisualDescendants(page).OfType<Grid>().Where(row => row.BindingContext is (Presentation.EntryRow or Features.Accounts.AccountItem)
                && row.Handler is not null && Visible(row));
            if (page.Width > 0 && (heading is null || heading.Width > 0 && heading.Height > 0)
                && rows.All(row => row.Width > 0 && row.Height > 0)) { return; }
        }
        throw new InvalidOperationException("The replacement page did not finish its native arrangement.");
    }

    // Select through the native data peer; this also covers choices virtualized inside the popup.
    private static async Task SelectSnapshotLanguageAsync(Picker picker, int index)
    {
        if (picker.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ComboBox combo)
        { throw new InvalidOperationException("The native language picker is unavailable."); }
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ComboBoxAutomationPeer(combo);
        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.ExpandCollapse)
            is not Microsoft.UI.Xaml.Automation.Provider.IExpandCollapseProvider expand)
        { throw new InvalidOperationException("The language picker has no native expand pattern."); }
        expand.Expand();
        await Task.Delay(200);
        if (index < 0 || index >= combo.Items.Count)
        { throw new InvalidOperationException("The native language choice is unavailable."); }
        // WinUI owns selection on the data peer; a valid choice need not have a realized popup container yet.
        var itemPeer = new Microsoft.UI.Xaml.Automation.Peers.ComboBoxItemDataAutomationPeer(combo.Items[index], peer);
        if (itemPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.SelectionItem)
            is not Microsoft.UI.Xaml.Automation.Provider.ISelectionItemProvider selection)
        { throw new InvalidOperationException("The language choice has no native select pattern."); }
        selection.Select(); expand.Collapse();
    }

    // AT-99: invoke the actual customization controls using only the walk-through's fictitious profile.
    private static async Task ReviewHomeLayoutActionsAsync(App app, IServiceProvider services,
        Features.Home.HomeLayoutPage page, string folder, string language)
    {
        var vm = (Features.Home.HomeLayoutViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> FinancialAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Budgets = await store.GetBudgetsAsync() });
        static string OtherPreferences(Core.Settings.ZananceSettings settings)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(settings))!.AsObject();
            node.Remove(nameof(settings.HomeLayout)); node.Remove(nameof(settings.UpdatedAt));
            return node.ToJsonString();
        }
        var before = await FinancialAsync(); var preferences = OtherPreferences(await store.GetSettingsAsync());
        var expected = Core.Dashboard.HomeLayout.Parse((await store.GetSettingsAsync()).HomeLayout);
        var first = vm.Rows[0].Section;
        var scroll = FindScrollView(page) ?? throw new InvalidOperationException("Home customization has no viewport.");
        Grid Row(Core.Dashboard.HomeSection section) => VisualDescendants(page).OfType<Grid>().Single(row =>
            row.BindingContext is Features.Home.HomeSectionRow model && model.Section == section && row.Children.OfType<Switch>().Any());
        async Task SavedAsync()
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if ((await store.GetSettingsAsync()).HomeLayout == expected.ToString()) { return; }
                await Task.Delay(25);
            }
            throw new InvalidOperationException("The native customization action did not persist its exact layout.");
        }
        async Task MoveAsync(int delta)
        {
            var row = Row(first); await scroll.ScrollToAsync(row, ScrollToPosition.Center, animated: false); await Task.Delay(200);
            var button = row.Children.OfType<Button>().Single(candidate => candidate.Command == (delta < 0 ? vm.MoveUpCommand : vm.MoveDownCommand));
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetPattern(
                    Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
            { throw new InvalidOperationException("The actual section reorder target has no native Invoke pattern."); }
            invoke.Invoke(); expected.Move(first, delta); await SavedAsync();
            if (!vm.Rows.Select(candidate => candidate.Section).SequenceEqual(expected.Sections.Select(state => state.Section)))
            { throw new InvalidOperationException("Section order differs after the native move."); }
        }
        await MoveAsync(1); await MoveAsync(-1);
        var toggle = Row(first).Children.OfType<Switch>().Single();
        if (toggle.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ToggleSwitch nativeToggle
            || new Microsoft.UI.Xaml.Automation.Peers.ToggleSwitchAutomationPeer(nativeToggle).GetPattern(
                Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle) is not Microsoft.UI.Xaml.Automation.Provider.IToggleProvider toggleProvider)
        { throw new InvalidOperationException("The actual section switch has no native Toggle pattern."); }
        expected.SetVisible(first, !toggle.IsToggled); toggleProvider.Toggle(); await SavedAsync();
        if (vm.Rows.Single(row => row.Section == first).IsVisible != expected.Sections.Single(state => state.Section == first).IsVisible)
        { throw new InvalidOperationException("The native switch failed to update its section."); }
        var reset = WrappingActions(page).Single(action => action.Command == vm.ResetCommand);
        await scroll.ScrollToAsync(reset, ScrollToPosition.Center, animated: false); await Task.Delay(200);
        InvokeWrappingAction(page, vm.ResetCommand); expected = Core.Dashboard.HomeLayout.Default; await SavedAsync();
        await Task.Delay(200);
        if (!vm.Rows.Select(row => (row.Section, row.IsVisible)).SequenceEqual(expected.Sections.Select(state => (state.Section, state.IsVisible)))
            || await FinancialAsync() != before || OtherPreferences(await store.GetSettingsAsync()) != preferences)
        { throw new InvalidOperationException("Customization reset lost its default rows or changed unrelated stored data."); }
        await CaptureAsync(app, folder, language + "-home-layout-reset");
        File.WriteAllText(Path.Combine(folder, language + "-home-layout-actions-proof.json"),
            System.Text.Json.JsonSerializer.Serialize(new { NativeInvocations = 4, ExactMovedOrderAndVisibilityPersisted = true,
                ResetRestoresEveryDefaultSection = true, CompleteAccountsEntriesBudgetsAndOtherPreferencesUnchanged = true }));
    }

    // AT-100: exercise the real visibility switch and account navigation on the fictitious walk-through profile.
    private static async Task ReviewHomeAccountVisibilityAsync(App app, IServiceProvider services,
        Features.Home.HomePage page, string folder, string language)
    {
        var home = (Features.Home.HomeViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var original = (await store.GetSettingsAsync()).HomeLayout;
        var financial = System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Budgets = await store.GetBudgetsAsync() });
        static string OtherPreferences(Core.Settings.ZananceSettings settings)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(settings))!.AsObject();
            node.Remove(nameof(settings.HomeLayout)); node.Remove(nameof(settings.UpdatedAt));
            return node.ToJsonString();
        }
        var preferences = OtherPreferences(await store.GetSettingsAsync());
        var invocations = 0;
        void Invoke(Button button)
        {
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetPattern(
                    Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
            { throw new InvalidOperationException("The actual Home account action has no native Invoke pattern."); }
            invoke.Invoke(); invocations++;
        }
        async Task<T> WaitPageAsync<T>() where T : Page
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (Shell.Current.CurrentPage is T target) { await Task.Delay(250); return target; }
                await Task.Delay(50);
            }
            throw new InvalidOperationException("Native Home account navigation did not open its target page.");
        }
        async Task ReturnAsync(Page target)
        {
            // Shell publishes CurrentPage before its animated child/header has acquired a native handler.
            for (var attempt = 0; attempt < 100 && !VisualDescendants(target).OfType<Presentation.PageHeader>()
                .SelectMany(header => header.Children.OfType<ImageButton>()).Any(button =>
                    button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button); attempt++)
            { await Task.Delay(50); }
            InvokeSnapshotBack(target); invocations++;
            if (!ReferenceEquals(await WaitPageAsync<Features.Home.HomePage>(), page))
            { throw new InvalidOperationException("Account navigation replaced the current Home page."); }
            await home.LoadAsync();
        }
        async Task SetVisibleAsync(bool visible)
        {
            Invoke(VisualDescendants(page).OfType<Button>().Single(button => button.Command == home.CustomizeCommand));
            var layout = await WaitPageAsync<Features.Home.HomeLayoutPage>();
            var model = (Features.Home.HomeLayoutViewModel)layout.BindingContext;
            var row = model.Rows.Single(candidate => candidate.Section == Core.Dashboard.HomeSection.Accounts);
            var toggle = VisualDescendants(layout).OfType<Switch>().Single(candidate => ReferenceEquals(candidate.BindingContext, row));
            if (toggle.IsToggled != visible)
            {
                if (toggle.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ToggleSwitch native
                    || new Microsoft.UI.Xaml.Automation.Peers.ToggleSwitchAutomationPeer(native).GetPattern(
                        Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle) is not Microsoft.UI.Xaml.Automation.Provider.IToggleProvider provider)
                { throw new InvalidOperationException("The actual Accounts switch has no native Toggle pattern."); }
                provider.Toggle(); invocations++;
                for (var attempt = 0; attempt < 100 && Core.Dashboard.HomeLayout.Parse((await store.GetSettingsAsync()).HomeLayout)
                    .Sections.Single(state => state.Section == Core.Dashboard.HomeSection.Accounts).IsVisible != visible; attempt++)
                { await Task.Delay(25); }
            }
            if (row.IsVisible != visible || Core.Dashboard.HomeLayout.Parse((await store.GetSettingsAsync()).HomeLayout)
                .Sections.Single(state => state.Section == Core.Dashboard.HomeSection.Accounts).IsVisible != visible)
            { throw new InvalidOperationException("The actual Accounts visibility switch did not persist its choice."); }
            await ReturnAsync(layout);
        }
        var snapshot = home.Accounts.ToArray();
        try
        {
            if (snapshot.Length > 0)
            {
                await SetVisibleAsync(false);
                await ReviewHomeSnapshotAsync(services, page, folder, language + "-hidden");
                await WaitForReopenedLayoutAsync(page);
                await CaptureAsync(app, folder, language + "-home-accounts-hidden");
                await SetVisibleAsync(true);
                await ReviewHomeSnapshotAsync(services, page, folder, language + "-visible");
                await WaitForReopenedLayoutAsync(page);
                await ReviewAccountDescriptionsAsync(app, services, page, folder, language);
                await ScrollToEndIfNeededAsync(FindScrollView(page)
                    ?? throw new InvalidOperationException("Home has no account viewport."));
                await Task.Delay(250);
                await CaptureAsync(app, folder, language + "-home-accounts-visible");
                var account = home.Accounts[0];
                var accountRow = VisualDescendants(page.FindByName<VerticalStackLayout>("AccountsSection"))
                    .OfType<Features.Accounts.AccountRow>().Single(row => ReferenceEquals(row.BindingContext, account));
                Invoke(VisualDescendants(accountRow).OfType<Button>().Single());
                var detail = await WaitPageAsync<Features.Accounts.AccountDetailPage>();
                for (var attempt = 0; attempt < 100 && ((Features.Accounts.AccountDetailViewModel)detail.BindingContext).Name != account.Name; attempt++)
                { await Task.Delay(50); }
                if (((Features.Accounts.AccountDetailViewModel)detail.BindingContext).Name != account.Name)
                { throw new InvalidOperationException("The shown account row opened different account details."); }
                await ReturnAsync(detail);
                await SetVisibleAsync(false);
                await ReviewHomeSnapshotAsync(services, page, folder, language + "-hidden-again");
                Invoke(page.FindByName<Button>("BalanceHeadingAction"));
                var accounts = await WaitPageAsync<Features.Accounts.AccountsPage>();
                var list = (Features.Accounts.AccountsViewModel)accounts.BindingContext;
                await list.LoadAsync();
                if (!list.Groups.SelectMany(group => group.Items).Select(item => item.Id).Order()
                    .SequenceEqual(home.Accounts.Select(item => item.Id).Order()))
                { throw new InvalidOperationException("Hiding Home accounts removed accounts from the complete account list."); }
                await ReturnAsync(accounts);
                await SetVisibleAsync(Core.Dashboard.HomeLayout.Parse(original).Sections
                    .Single(state => state.Section == Core.Dashboard.HomeSection.Accounts).IsVisible);
            }
        }
        finally
        {
            // Only the seeded walk-through profile is written; keep its exact original layout representation.
            await store.UpdateSettingsAsync(settings => settings.HomeLayout = original);
        }
        if (!home.Accounts.SequenceEqual(snapshot) || preferences != OtherPreferences(await store.GetSettingsAsync())
            || financial != System.Text.Json.JsonSerializer.Serialize(new
            { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Budgets = await store.GetBudgetsAsync() }))
        { throw new InvalidOperationException("Home account visibility changed financial data or unrelated preferences."); }
        File.WriteAllText(Path.Combine(folder, language + "-home-account-visibility-proof.json"),
            System.Text.Json.JsonSerializer.Serialize(new { NativeInvocations = invocations, CompleteAccountSnapshot = snapshot.Length,
                ShowHideAndVisibleReloadChecked = snapshot.Length > 0, HiddenAccountRowsAbsent = true,
                ActualAccountDetailsAndCompleteListChecked = snapshot.Length > 0,
                CompleteAccountsEntriesBudgetsAndOtherPreferencesUnchanged = true, OriginalLayoutRestored = true }));
    }

    // AT-102: the existing debt entry point must expose its complete caption and open/cancel the same unsaved form.
    private static async Task ReviewDebtEntryActionAsync(App app, IServiceProvider services,
        Features.Accounts.AccountsPage page, string folder, string language)
    {
        var model = (Features.Accounts.AccountsViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var translator = services.GetRequiredService<Translator>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        {
            Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(),
            Settings = await store.GetSettingsAsync(), Budgets = await store.GetBudgetsAsync(), Schedules = await plans.GetSchedulesAsync(),
        });
        static Microsoft.UI.Xaml.Controls.TextBlock? FindCaption(Microsoft.UI.Xaml.DependencyObject root, string text)
        {
            if (root is Microsoft.UI.Xaml.Controls.TextBlock caption && caption.Text == text) { return caption; }
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root); index++)
            {
                if (FindCaption(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, index), text) is { } found) { return found; }
            }
            return null;
        }
        static void Invoke(Button button)
        {
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetPattern(
                    Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider provider)
            { throw new InvalidOperationException("The actual debt action has no native Invoke pattern."); }
            provider.Invoke();
        }
        var before = await StoredAsync();
        var button = VisualDescendants(page).OfType<Button>().Single(candidate => candidate.Command == model.AddDebtCommand);
        var growing = VisualDescendants(page).OfType<Presentation.WrappingAction>().SingleOrDefault(candidate => candidate.Command == model.AddDebtCommand);
        VisualElement action = growing is null ? button : growing;
        await ScrollToViewIfNeededAsync(FindScrollView(page) ?? throw new InvalidOperationException("The debt entry point has no viewport."), action);
        await CaptureAsync(app, folder, language + "-accounts-debt-action");
        var expected = translator["Debt_Add"];
        var target = button.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button
            ?? throw new InvalidOperationException("The debt entry point has no actual native target.");
        var mauiCaption = growing?.Content is Grid face ? face.Children.OfType<Border>().Single().Content as Label : null;
        var caption = mauiCaption?.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBlock ?? FindCaption(target, expected)
            ?? throw new InvalidOperationException("The debt entry point has no realized complete native caption.");
        var origin = caption.TransformToVisual(target).TransformPoint(new Windows.Foundation.Point());
        var boundaries = new List<Windows.Foundation.Rect>();
        for (var offset = 0; offset <= caption.ContentEnd.Offset - caption.ContentStart.Offset; offset++)
        {
            var pointer = caption.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
            if (pointer is not null) { boundaries.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
        }
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(target);
        if (caption.Text != expected || peer.GetName() != expected || !caption.IsTextScaleFactorEnabled || caption.IsTextTrimmed
            || !target.IsEnabled || target.ActualWidth < 44 || target.ActualHeight < 44 || boundaries.Count == 0
            || origin.X + boundaries.Min(rect => rect.Left) < -1 || origin.X + boundaries.Max(rect => rect.Right) > target.ActualWidth + 1
            || origin.Y + boundaries.Max(rect => rect.Bottom) > target.ActualHeight + 1
            || (mauiCaption is not null && (boundaries.Max(rect => rect.Right) > mauiCaption.Width + 1
                || boundaries.Max(rect => rect.Bottom) > mauiCaption.Height + 1)))
        {
            File.WriteAllText(Path.Combine(folder, language + "-debt-action-failure.json"), System.Text.Json.JsonSerializer.Serialize(new
            { expected, caption.Text, caption.FontSize, caption.IsTextTrimmed, target.ActualWidth, target.ActualHeight, origin, boundaries }));
            throw new InvalidOperationException("The debt entry point clips its caption or loses its complete native name/target.");
        }
        Invoke(button);
        for (var attempt = 0; attempt < 100 && page.Navigation.ModalStack.LastOrDefault() is not Features.Accounts.AccountEditorPage; attempt++)
        { await Task.Delay(50); }
        if (page.Navigation.ModalStack.LastOrDefault() is not Features.Accounts.AccountEditorPage editor
            || editor.BindingContext is not Features.Accounts.AccountEditorViewModel draft)
        { throw new InvalidOperationException("The actual debt action did not open its account editor."); }
        for (var attempt = 0; attempt < 100 && (!draft.Form.IsDebtType || draft.IsDirty || draft.Title != translator["Debt_NewTitle"]); attempt++)
        { await Task.Delay(50); }
        if (draft.IsExisting || draft.Form.Type != AccountType.Loan || draft.IsDirty || draft.Title != translator["Debt_NewTitle"] || await StoredAsync() != before)
        { throw new InvalidOperationException("Opening the debt draft changed stored data or selected a different existing form."); }
        // The animated modal can expose its binding before WinUI arranges the newly visible debt date inputs.
        var date = VisualDescendants(editor).OfType<Vafadar.Maui.Controls.DateField>().First();
        var dateBoxes = VisualDescendants(date).OfType<Entry>().ToArray();
        bool DateArranged() => date.Width > 0 && date.Height > 0 && dateBoxes.Length == 3
            && dateBoxes.All(box => box.Width > 0 && box.Height > 0
                && box.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox { ActualWidth: > 0, ActualHeight: > 0 });
        for (var attempt = 0; attempt < 100 && !DateArranged(); attempt++) { await Task.Delay(50); }
        if (!DateArranged()) { throw new InvalidOperationException("The debt draft date inputs did not finish native arrangement."); }
        await CheckModalHeaderAsync(app, editor, folder, language + "-debt-modal-header");
        await CaptureAsync(app, folder, language + "-accounts-debt-draft");
        await ScrollToViewIfNeededAsync(FindScrollView(editor)
            ?? throw new InvalidOperationException("The debt draft has no native viewport."), date);
        await CaptureAsync(app, folder, language + "-accounts-debt-date");
        Invoke(VisualDescendants(editor).OfType<Button>().Single(candidate => candidate.Command == draft.CancelCommand));
        for (var attempt = 0; attempt < 100 && page.Navigation.ModalStack.Count > 0; attempt++) { await Task.Delay(50); }
        await WaitForReopenedLayoutAsync(page);
        if (page.Navigation.ModalStack.Count != 0 || !ReferenceEquals(Shell.Current.CurrentPage, page) || await StoredAsync() != before)
        { throw new InvalidOperationException("Cancelling the debt draft changed stored data or the current Accounts page."); }
        await CaptureAsync(app, folder, language + "-accounts-debt-return");
        File.WriteAllText(Path.Combine(folder, language + "-debt-action-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            Caption = expected, target.ActualWidth, target.ActualHeight, caption.FontSize, CompleteNativeCaptionAndName = true,
            NativeOpenAndCancelInvocations = 2, SameAccountsPage = true, NewUnsavedLoanDraft = true,
            CompleteAccountsEntriesSettingsBudgetsSchedulesUnchanged = true, NoSaveOrPrincipalPosting = true,
        }));
    }

    // AT-106 covers retention, an explicit edit/removal and the reopened/new Simple forms using real native actions.
    private static async Task ReviewDestinationFeeRetentionAsync(App app, IServiceProvider services, string folder, string language)
    {
        await ReviewDestinationFeeSaveAsync(app, services, folder, language, "retain", null, 75);
        var store = services.GetRequiredService<ZananceStore>();
        var transfer = (await store.GetEntriesAsync()).Single(entry => entry.Kind == EntryKind.Transfer);
        var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
        async Task OpenAsync(Guid? id)
        {
            await Shell.Current.GoToAsync(AppShell.EntryEditorRoute, id is { } entryId
                ? new Dictionary<string, object> { ["id"] = entryId }
                : new Dictionary<string, object> { ["kind"] = nameof(EntryKind.Transfer) });
            await Task.Delay(500);
        }
        await OpenAsync(transfer.Id);
        await ReviewDestinationFeeSaveAsync(app, services, folder, language, "edit", 1.25m.ToString("0.00", culture), 125);
        await OpenAsync(transfer.Id);
        await ReviewDestinationFeeSaveAsync(app, services, folder, language, "remove", language == "fa" ? "0" : "", 0);
        var stored = System.Text.Json.JsonSerializer.Serialize(await store.GetEntriesAsync());
        var advanced = (await store.GetSettingsAsync()).Mode == Core.Settings.ExperienceMode.Advanced;
        foreach (var id in new Guid?[] { transfer.Id, null })
        {
            await OpenAsync(id);
            var page = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as Features.Entries.EntryEditorPage
                ?? Shell.Current.CurrentPage as Features.Entries.EntryEditorPage
                ?? throw new InvalidOperationException("The reopened/new transfer form is missing.");
            var vm = (Features.Entries.EntryEditorViewModel)page.BindingContext;
            // A new transfer needs an actual destination before its Advanced fee input becomes applicable.
            vm.ToAccount = vm.ToAccounts.Single(account => account.Id == transfer.ToAccountId);
            if (vm.ShowDestinationFee != advanced || !string.IsNullOrEmpty(vm.DestinationFeeText))
            { throw new InvalidOperationException("A transfer without a fee exposes an unintended new Simple control or retains a removed value."); }
            await CaptureAsync(app, folder, language + "-entry-fee-retention-" + (id is null ? "new" : "reopened"));
            vm.ToAccount = id is null ? null : vm.ToAccount; // Remove only the fictitious new draft's presentation choice.
            var cancel = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.CancelCommand);
            if (cancel.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetPattern(
                    Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
            { throw new InvalidOperationException("Transfer Cancel lacks its actual native Invoke pattern."); }
            invoke.Invoke(); await Task.Delay(200);
            if (vm.CancelCommand.ExecutionTask is { IsCompleted: false }
                && app.Windows[0].Handler?.PlatformView is Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
            {
                // The ordinary new-transfer draft may ask for discard even after restoring its empty destination.
                var dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                    .Select(popup => popup.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().Single();
                var discard = FindNativeButton(dialog, Translator.Instance["Common_Discard"])
                    ?? throw new InvalidOperationException("The fictitious draft's actual discard action is missing.");
                if (new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(discard).GetPattern(
                        Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider confirm)
                { throw new InvalidOperationException("Discard lacks its actual native Invoke pattern."); }
                confirm.Invoke();
            }
            if (vm.CancelCommand.ExecutionTask is { } canceling) { await canceling.WaitAsync(TimeSpan.FromSeconds(5)); }
            await Task.Delay(150);
            if (stored != System.Text.Json.JsonSerializer.Serialize(await store.GetEntriesAsync()))
            { throw new InvalidOperationException("Opening/canceling a fee-free transfer wrote financial rows."); }
        }
        File.WriteAllText(Path.Combine(folder, language + "-entry-fee-retention-proof.json"),
            System.Text.Json.JsonSerializer.Serialize(new { NativeValidSaves = 3, ExistingFeeVisibleAndRetained = true,
                ExistingFeeEditedWithSameId = true, OnlyExplicitBlankOrZeroRemovesFee = true, ReopenedAndNewFeeFreePolicy = true,
                SourceFeeTransferFinancialFieldsAndUnrelatedRowsPreserved = true, NativeCancelWithoutWrites = true, Mode = advanced ? "Advanced" : "Simple" }));
    }

    // An actual successful Save compares every financial field and unrelated row, allowing only the chosen note/fee and audit time.
    private static async Task ReviewDestinationFeeSaveAsync(App app, IServiceProvider services, string folder, string language,
        string scenario, string? changedFee, long expectedFee)
    {
        var page = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as Features.Entries.EntryEditorPage
            ?? Shell.Current.CurrentPage as Features.Entries.EntryEditorPage
            ?? throw new InvalidOperationException("The existing transfer editor is missing.");
        var vm = (Features.Entries.EntryEditorViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var before = await store.GetEntriesAsync();
        var transfer = before.Single(entry => entry.Kind == EntryKind.Transfer);
        var source = EntryActions.FindTransferFee(transfer, before)!;
        var destination = EntryActions.FindDestinationFee(transfer, before)!;
        var accounts = System.Text.Json.JsonSerializer.Serialize(await store.GetAccountsAsync());
        var settings = System.Text.Json.JsonSerializer.Serialize(await store.GetSettingsAsync());
        var loadedText = vm.DestinationFeeText;
        var initiallyVisible = vm.ShowDestinationFee;
        if (!vm.IsTransfer || destination is null || source.Amount != 150 || string.IsNullOrEmpty(loadedText))
        { throw new InvalidOperationException("The fictitious existing two-fee transfer did not load."); }
        if (!initiallyVisible) { throw new InvalidOperationException("The stored destination fee is hidden in the edit form."); }
        var accountRows = (await store.GetAccountsAsync()).ToDictionary(account => account.Id);
        var from = accountRows[transfer.AccountId]; var to = accountRows[transfer.ToAccountId!.Value];
        var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
        void CheckEffect(long fee)
        {
            var expected = Translator.Instance.Format("Entry_EffectFrom",
                Core.Money.MoneyText.Format(-transfer.Amount - source.Amount, from.CurrencyCode, culture), from.Name)
                + " · " + Translator.Instance.Format("Entry_EffectTo",
                    Core.Money.MoneyText.Format((transfer.ToAmount ?? transfer.Amount) - fee, to.CurrencyCode, culture, showPlus: true), to.Name);
            if (vm.EffectText != expected)
            { throw new InvalidOperationException("The actual transfer summary does not include both fee effects in their account currencies."); }
        }
        CheckEffect(destination.Amount);
        {
            var viewport = page.FindByName<ScrollView>("ValidationViewport");
            var input = page.FindByName<Entry>("DestinationFeeInput");
            await ScrollToViewIfNeededAsync(viewport, input); await Task.Delay(150);
            if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox actual || actual.Text != loadedText
                || actual.Visibility != Microsoft.UI.Xaml.Visibility.Visible || actual.ActualWidth < 44 || actual.ActualHeight < 44)
            { throw new InvalidOperationException("The existing destination fee lacks its real visible input/value/target."); }
            if (viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer actualViewport)
            { throw new InvalidOperationException("The transfer's native viewport is missing."); }
            var position = actual.TransformToVisual(actualViewport).TransformPoint(new Windows.Foundation.Point());
            if (position.Y < -1 || position.Y + actual.ActualHeight > actualViewport.ActualHeight + 1)
            { throw new InvalidOperationException("The existing fee input cannot be fully reached in its viewport."); }
            await CaptureAsync(app, folder, language + "-entry-fee-retention-" + scenario + "-existing-fee");
            var caption = Translator.Instance["Entry_DestinationFee"];
            var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == caption);
            await ScrollToViewIfNeededAsync(viewport, label); await Task.Delay(100);
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text || text.IsTextTrimmed
                || !text.IsTextScaleFactorEnabled || text.ActualWidth < 1 || text.ActualHeight < 1)
            { throw new InvalidOperationException("The existing fee caption is clipped or lacks native scaling."); }
            var glyphs = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= text.ContentEnd.Offset - text.ContentStart.Offset; offset++)
            {
                if (text.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                { glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            if (glyphs.Count == 0 || glyphs.Min(rect => rect.Left) < -1 || glyphs.Max(rect => rect.Right) > label.Width + 1
                || glyphs.Max(rect => rect.Bottom) > label.Height + 1)
            { throw new InvalidOperationException("The existing fee's full glyphs exceed its actual MAUI allocation."); }
            await CaptureAsync(app, folder, language + "-entry-fee-retention-" + scenario + "-caption");
        }
        if (changedFee is not null) { vm.DestinationFeeText = changedFee; }
        CheckEffect(expectedFee);
        var note = "Fictitious AT-106 " + scenario + " edit " + language;
        vm.Note = note;
        var footer = ((Grid)page.Content).Children.OfType<VerticalStackLayout>().Single(view => Grid.GetRow(view) == 2);
        var save = footer.Children.OfType<Button>().Single();
        if (save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
            || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(
                Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        { throw new InvalidOperationException("The existing transfer Save lacks its real native Invoke pattern."); }
        invoke.Invoke();
        if (vm.SaveCommand.ExecutionTask is { } saving) { await saving; }
        await Task.Delay(250);
        var after = await store.GetEntriesAsync();
        var saved = after.Single(entry => entry.Id == transfer.Id);
        var savedDestination = EntryActions.FindDestinationFee(saved, after);
        var savedSource = EntryActions.FindTransferFee(saved, after);
        var proof = new
        {
            Mode = (await store.GetSettingsAsync()).Mode.ToString(), ActualNativeValidSave = true,
            ExistingDestinationFeeInitiallyVisible = initiallyVisible, LoadedDestinationText = loadedText,
            DestinationFeeBefore = destination.Amount, DestinationFeeAfter = savedDestination?.Amount,
            SameDestinationFeeId = savedDestination?.Id == destination.Id, SameSourceFeeId = savedSource?.Id == source.Id,
            SourceFeeBefore = source.Amount, SourceFeeAfter = savedSource?.Amount,
            SingleTransfer = after.Count(entry => entry.Kind == EntryKind.Transfer) == 1,
            NoteSaved = saved.Note == note, TotalRowsBefore = before.Count, TotalRowsAfter = after.Count,
        };
        File.WriteAllText(Path.Combine(folder, language + "-entry-fee-retention-" + scenario + "-proof.json"), System.Text.Json.JsonSerializer.Serialize(proof));
        if (!proof.NoteSaved || !proof.SingleTransfer
            || (expectedFee > 0 ? savedDestination?.Amount != expectedFee || !proof.SameDestinationFeeId : savedDestination is not null)
            || !proof.SameSourceFeeId || savedSource?.Amount != source.Amount
            || before.Count - (expectedFee == 0 ? 1 : 0) != after.Count || vm.SaveError is not null)
        { throw new InvalidOperationException("A valid unrelated transfer edit lost an existing destination fee or changed its identity/value."); }
        // Audit timestamps may advance on a legitimate save; all financial fields and unrelated rows must remain exact.
        saved.Note = transfer.Note; saved.UpdatedAt = transfer.UpdatedAt;
        savedSource!.UpdatedAt = source.UpdatedAt;
        if (savedDestination is not null) { savedDestination.UpdatedAt = destination.UpdatedAt; savedDestination.Amount = destination.Amount; }
        var expected = expectedFee == 0 ? before.Where(entry => entry.Id != destination.Id) : before;
        if (System.Text.Json.JsonSerializer.Serialize(expected.OrderBy(entry => entry.Id))
                != System.Text.Json.JsonSerializer.Serialize(after.OrderBy(entry => entry.Id))
            || accounts != System.Text.Json.JsonSerializer.Serialize(await store.GetAccountsAsync())
            || settings != System.Text.Json.JsonSerializer.Serialize(await store.GetSettingsAsync()))
        { throw new InvalidOperationException("The unrelated transfer edit changed financial fields, accounts or settings."); }
    }

    // AT-105: independent monetary errors, corrected input visibility and collapsed detail feedback use real native Save.
    private static async Task ReviewEntryValidationAsync(App app, IServiceProvider services, string folder, string language)
    {
        var page = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as Features.Entries.EntryEditorPage
            ?? Shell.Current.CurrentPage as Features.Entries.EntryEditorPage
            ?? throw new InvalidOperationException("The real new entry modal is missing.");
        var vm = (Features.Entries.EntryEditorViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        string Draft() => System.Text.Json.JsonSerializer.Serialize(new
        {
            vm.KindIndex, vm.AmountText, vm.Account, vm.ToAccount, vm.ToAmountText, vm.FeeText, vm.DestinationFeeText,
            vm.EntryTitle, vm.Date, vm.Note, vm.Payee, vm.TagsText, vm.ReimbursableEnabled, vm.ReimbursableText,
            vm.ReimbursedBy, vm.ReimbursementDue, vm.HasReimbursementDue, vm.ForeignEnabled, vm.ForeignCurrency,
            vm.ForeignAmountText, vm.IsAggregated, vm.AggregatedFrom, vm.AggregatedTo,
        });
        var before = await StoredAsync();
        var original = (vm.KindIndex, vm.AmountText, vm.ToAccount, vm.ToAmountText, vm.FeeText, vm.DestinationFeeText,
            vm.Note, vm.ShowDetails, vm.ToAccounts, vm.ForeignEnabled, vm.ForeignCurrency, vm.ForeignAmountText,
            vm.ReimbursableEnabled, vm.ReimbursableText);
        var viewport = page.FindByName<ScrollView>("ValidationViewport")
            ?? throw new InvalidOperationException("Missing entry validation viewport.");
        var footer = ((Grid)page.Content).Children.OfType<VerticalStackLayout>().Single(view => Grid.GetRow(view) == 2);
        var save = footer.Children.OfType<Button>().Single();
        if (save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
            || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(
                Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke
            || viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer nativeViewport)
        { throw new InvalidOperationException("The entry Save/viewport lacks its actual native control."); }
        var invocations = 0;
        async Task SaveCaseAsync(string name, VisualElement target, params string[] expectedKeys)
        {
            await ScrollToEndIfNeededAsync(viewport);
            var draft = Draft();
            invoke.Invoke();
            if (vm.SaveCommand.ExecutionTask is { } saving) { await saving; }
            await Task.Delay(400);
            invocations++;
            if (draft != Draft() || await StoredAsync() != before || vm.SaveError is not null)
            { throw new InvalidOperationException("Invalid entry feedback changed the draft/stored rows or remained in the general footer."); }
            if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement actualTarget)
            { throw new InvalidOperationException("The invalid entry field lacks its actual native control."); }
            var position = actualTarget.TransformToVisual(nativeViewport).TransformPoint(new Windows.Foundation.Point());
            if (position.Y < -1 || position.Y + actualTarget.ActualHeight > nativeViewport.ActualHeight + 1)
            {
                await CaptureAsync(app, folder, language + "-entry-validation-" + name + "-failed");
                File.WriteAllText(Path.Combine(folder, language + "-entry-validation-" + name + "-failed.json"),
                    System.Text.Json.JsonSerializer.Serialize(new { position, actualTarget.ActualHeight,
                        ViewportHeight = nativeViewport.ActualHeight,
                        vm.AmountError, vm.ToAccountError, vm.FeeError,
                        vm.ToAmountError, vm.DestinationFeeError, vm.ForeignCurrencyError, vm.ForeignAmountError, vm.ReimbursableError }));
                throw new InvalidOperationException("Invalid Save did not reveal the actual next entry input.");
            }
            await CaptureAsync(app, folder, language + "-entry-validation-" + name);
            for (var index = 0; index < expectedKeys.Length; index++)
            {
                var caption = Translator.Instance[expectedKeys[index]];
                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == caption && item.IsVisible);
                await ScrollToViewIfNeededAsync(viewport, label); await Task.Delay(100);
                if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock actual || actual.IsTextTrimmed
                    || !actual.IsTextScaleFactorEnabled || actual.ActualWidth < 1 || actual.ActualHeight < 1)
                { throw new InvalidOperationException("An entry input lacks its complete scaled native error caption."); }
                var glyphs = new List<Windows.Foundation.Rect>();
                for (var offset = 0; offset <= actual.ContentEnd.Offset - actual.ContentStart.Offset; offset++)
                {
                    if (actual.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                    { glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
                }
                if (glyphs.Count == 0 || glyphs.Min(rect => rect.Left) < -1 || glyphs.Max(rect => rect.Right) > label.Width + 1
                    || glyphs.Max(rect => rect.Bottom) > label.Height + 1)
                { throw new InvalidOperationException("An entry field's realized error glyphs exceed the actual MAUI allocation."); }
                await CaptureAsync(app, folder, language + "-entry-validation-" + name + "-field-" + index);
            }
        }
        vm.KindIndex = 2; vm.AmountText = "0"; vm.ToAccount = null; vm.FeeText = "not money";
        vm.Note = "Fictitious retained validation note"; vm.ShowDetails = false;
        await SaveCaseAsync("independent", page.FindByName<Entry>("AmountEntry"),
            "Entry_AmountMustBePositive", "LedgerError_DestinationRequired", "Amount_Invalid");
        vm.AmountText = Core.Money.MoneyText.ForInput(1234, vm.CurrencyCode, services.GetRequiredService<ILocalizationService>().CurrentCulture);
        await SaveCaseAsync("missing-destination", page.FindByName<Picker>("ToAccountInput"), "LedgerError_DestinationRequired", "Amount_Invalid");
        if (vm.AmountError is not null) { throw new InvalidOperationException("A corrected entry amount kept its old error."); }
        vm.ToAccount = vm.Account; vm.FeeText = string.Empty;
        await SaveCaseAsync("same-account", page.FindByName<Picker>("ToAccountInput"), "LedgerError_SameAccountTransfer");
        if (vm.FeeError is not null) { throw new InvalidOperationException("A corrected entry fee kept its old error."); }
        // Presentation-only unknown ID plus an invalid amount/fee keeps this probe outside the valid save continuation.
        var foreign = new Features.Entries.AccountChoice(Guid.NewGuid(), "Fictitious cross-currency destination", "JPY");
        vm.ToAccounts = [.. original.ToAccounts, foreign]; vm.ToAccount = foreign; vm.ToAmountText = "0";
        vm.DestinationFeeText = "not money";
        if (!vm.ShowToAmount || !vm.ShowDestinationFee) { throw new InvalidOperationException("The Advanced transfer fixture lacks its dependent fields."); }
        await SaveCaseAsync("destination-amount", page.FindByName<Entry>("ToAmountInput"), "LedgerError_DestinationAmountRequired", "Amount_Invalid");
        vm.ToAmountText = "1200";
        await SaveCaseAsync("destination-fee", page.FindByName<Entry>("DestinationFeeInput"), "Amount_Invalid");
        if (vm.ToAmountError is not null || vm.ToAccountError is not null)
        { throw new InvalidOperationException("Corrected transfer fields kept old errors."); }
        vm.KindIndex = 0; vm.ForeignEnabled = true; vm.ForeignCurrency = vm.CurrencyCode; vm.ForeignAmountText = "0";
        vm.ReimbursableEnabled = true; vm.ReimbursableText = "not money"; vm.ShowDetails = false;
        await SaveCaseAsync("collapsed-details", page.FindByName<Entry>("ReimbursableInput"),
            "Entry_ReimbursableInvalid", "LedgerError_InvalidOriginalCurrency", "Entry_AmountMustBePositive");
        if (!vm.ShowDetails) { throw new InvalidOperationException("Invalid retained detail values remained hidden."); }
        vm.ReimbursableText = string.Empty; vm.ForeignCurrency = "USD"; vm.ShowDetails = false;
        await SaveCaseAsync("original-amount", page.FindByName<Grid>("ForeignInput"), "Entry_AmountMustBePositive");
        if (vm.ReimbursableError is not null || vm.ForeignCurrencyError is not null)
        { throw new InvalidOperationException("Corrected detail values kept old errors."); }
        vm.ForeignAmountText = "100"; vm.ForeignCurrency = vm.CurrencyCode; vm.ShowDetails = false;
        await SaveCaseAsync("original-currency", page.FindByName<Grid>("ForeignInput"), "LedgerError_InvalidOriginalCurrency");
        if (vm.ForeignAmountError is not null) { throw new InvalidOperationException("Corrected original amount kept its old error."); }

        vm.KindIndex = original.KindIndex; vm.AmountText = original.AmountText; vm.ToAccounts = original.ToAccounts;
        vm.ToAccount = original.ToAccount; vm.ToAmountText = original.ToAmountText; vm.FeeText = original.FeeText;
        vm.DestinationFeeText = original.DestinationFeeText; vm.Note = original.Note; vm.ShowDetails = original.ShowDetails;
        vm.ForeignEnabled = original.ForeignEnabled; vm.ForeignCurrency = original.ForeignCurrency; vm.ForeignAmountText = original.ForeignAmountText;
        vm.ReimbursableEnabled = original.ReimbursableEnabled; vm.ReimbursableText = original.ReimbursableText;
        if (await StoredAsync() != before) { throw new InvalidOperationException("Invalid entry validation wrote stored data."); }
        File.WriteAllText(Path.Combine(folder, language + "-entry-validation-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            NativeInvalidSaveInvocations = invocations, AllIndependentFieldErrors = true, FirstInputAutomaticallyRevealed = true,
            CorrectionsClearOldErrors = true, CollapsedInvalidDetailsRevealed = true, NativeFullErrorGlyphsAndScale = true,
            DraftAndCompleteStoredDataRetained = true, NoValidFinancialSave = true,
        }));
    }

    // AT-107: use the real modal, native Save and fictitious SQLite rows for invalid and valid settlements.
    private static async Task ReviewSettlementFeedbackAsync(App app, IServiceProvider services, string folder, string language)
    {
        Features.Plans.SettlementPage Page() => app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as Features.Plans.SettlementPage
            ?? Shell.Current.CurrentPage as Features.Plans.SettlementPage
            ?? throw new InvalidOperationException("The real settlement modal is missing.");
        var page = Page();
        var vm = (Features.Plans.SettlementViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
        var plan = (await plans.GetSchedulesAsync()).Single(schedule => schedule.Name == vm.PlanName);
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        {
            Entries = await store.GetEntriesAsync(), Accounts = await store.GetAccountsAsync(),
            Settings = await store.GetSettingsAsync(), Schedules = await plans.GetSchedulesAsync(),
        });
        var before = await StoredAsync();
        var original = (vm.From, vm.To, vm.ActualText);
        var invalidSaves = 0;
        static void CheckGlyphs(Label label)
        {
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || native.IsTextTrimmed
                || !native.IsTextScaleFactorEnabled || native.ActualHeight < 1 || native.ActualWidth < 1)
            { throw new InvalidOperationException("A settlement caption lacks complete native scaled text."); }
            var glyphs = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
            {
                if (native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                { glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            if (glyphs.Count == 0 || glyphs.Min(rect => rect.Left) < -1 || glyphs.Max(rect => rect.Right) > label.Width + 1
                || glyphs.Max(rect => rect.Bottom) > label.Height + 1)
            { throw new InvalidOperationException("Settlement caption glyphs exceed the actual MAUI allocation."); }
        }
        async Task SaveInvalidAsync(string name, string? periodKey, string? amountKey)
        {
            var viewport = page.FindByName<ScrollView>("ValidationViewport");
            await ScrollToEndIfNeededAsync(viewport);
            var retained = (vm.From, vm.To, vm.ActualText);
            InvokeWrappingAction(page, vm.SaveCommand);
            if (vm.SaveCommand.ExecutionTask is { } saving) { await saving; }
            invalidSaves++;
            await Task.Delay(400);
            if (retained != (vm.From, vm.To, vm.ActualText) || await StoredAsync() != before || vm.CanSave || vm.Error is not null
                || vm.PeriodError != (periodKey is null ? null : Translator.Instance[periodKey])
                || vm.AmountError != (amountKey is null ? null : Translator.Instance[amountKey]))
            { throw new InvalidOperationException("An invalid settlement did not explain every field or changed its draft/stored data."); }
            if (viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer nativeViewport)
            { throw new InvalidOperationException("Missing native settlement scroll viewport."); }
            var target = periodKey is not null ? (VisualElement)page.FindByName<Vafadar.Maui.Controls.DateField>("ToInput")
                : page.FindByName<Entry>("ActualInput");
            if (periodKey is not null || amountKey is not null)
            {
                if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeTarget)
                { throw new InvalidOperationException("Missing actual settlement field."); }
                var position = nativeTarget.TransformToVisual(nativeViewport).TransformPoint(new Windows.Foundation.Point());
                if (position.Y < -1 || position.Y + nativeTarget.ActualHeight > nativeViewport.ActualHeight + 1)
                { throw new InvalidOperationException("Invalid settlement Save left its first affected input outside the viewport."); }
            }
            await CaptureAsync(app, folder, language + "-settlement-feedback-" + name + "-first");
            foreach (var text in new[] { vm.PeriodError, vm.AmountError, vm.ResultText }.Where(text => text is not null))
            {
                var label = VisualDescendants(page).OfType<Label>().Single(label => label.IsVisible && label.Text == text);
                await ScrollToViewIfNeededAsync(viewport, label); await Task.Delay(100);
                CheckGlyphs(label);
                await CaptureAsync(app, folder, language + "-settlement-feedback-" + name + "-caption-" + Array.IndexOf(new[] { vm.PeriodError, vm.AmountError, vm.ResultText }, text));
            }
        }
        vm.From = original.To.AddDays(1); vm.ActualText = "not money";
        await SaveInvalidAsync("independent", "Settlement_PeriodInvalid", "Settlement_AmountInvalid");
        vm.From = original.From;
        await SaveInvalidAsync("corrected-period", null, "Settlement_AmountInvalid");
        vm.ActualText = string.Empty;
        await SaveInvalidAsync("empty-bill", null, "Settlement_AmountInvalid");
        vm.ActualText = "-1";
        await SaveInvalidAsync("negative-bill", null, "Settlement_AmountInvalid");
        vm.ActualText = "100"; vm.From = original.To; vm.To = original.To;
        await SaveInvalidAsync("no-advances", "Settlement_NoAdvances", null);
        vm.From = original.From; vm.To = original.To;
        var entries = await store.GetEntriesAsync();
        var paid = Core.Plans.AdvanceSettlement.Compute(plan, entries, vm.From, vm.To, 0).Paid;
        vm.ActualText = Core.Money.MoneyText.ForInput(paid, "EUR", culture);
        await SaveInvalidAsync("even", null, null);
        if (vm.ResultText != Translator.Instance["Settlement_Even"])
        { throw new InvalidOperationException("A matching bill lacks the explanation that nothing is recorded."); }
        var validSaves = new List<object>();
        foreach (var (name, actual) in new[] { ("extra", paid + 5000), ("refund", paid - 5000), ("zero-bill", 0L) })
        {
            vm.ActualText = Core.Money.MoneyText.ForInput(actual, "EUR", culture);
            if (!vm.CanSave || vm.PeriodError is not null || vm.AmountError is not null)
            { throw new InvalidOperationException("A valid settlement kept an earlier field problem."); }
            var viewport = page.FindByName<ScrollView>("ValidationViewport");
            var input = page.FindByName<Entry>("ActualInput");
            await ScrollToViewIfNeededAsync(viewport, input); await Task.Delay(100);
            if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox nativeInput || nativeInput.Text != vm.ActualText
                || nativeInput.ActualHeight < 44)
            { throw new InvalidOperationException("The settlement bill is not retained in its actual native input."); }
            var action = page.FindByName<Presentation.WrappingAction>("SaveAction");
            var caption = VisualDescendants(action).OfType<Label>().Single();
            CheckGlyphs(caption);
            var button = VisualDescendants(action).OfType<Button>().Single();
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeButton || nativeButton.ActualHeight < 44
                || nativeButton.ActualWidth < 44 || !nativeButton.IsEnabled
                || Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(nativeButton) != Translator.Instance["Settlement_Save"])
            { throw new InvalidOperationException("The settlement Save lacks its full native spoken name or accessible target."); }
            await CaptureAsync(app, folder, language + "-settlement-feedback-" + name + "-ready");
            InvokeWrappingAction(page, vm.SaveCommand);
            if (vm.SaveCommand.ExecutionTask is { } saving) { await saving; }
            await Task.Delay(300);
            if (app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() is Features.Plans.SettlementPage)
            { throw new InvalidOperationException("A valid native settlement Save did not complete navigation."); }
            var after = await store.GetEntriesAsync();
            var originalIds = entries.Select(entry => entry.Id).ToHashSet();
            var created = after.Where(entry => !originalIds.Contains(entry.Id)).ToList();
            if (created.Count != 1 || created[0].Kind != (actual > paid ? EntryKind.Expense : EntryKind.Refund)
                || created[0].Amount != Math.Abs(actual - paid) || created[0].AccountId != plan.AccountId
                || created[0].ScheduleId is not null || (actual < paid && !entries.Any(entry => entry.Id == created[0].RefundOfId))
                || System.Text.Json.JsonSerializer.Serialize(after.Where(entry => originalIds.Contains(entry.Id))) != System.Text.Json.JsonSerializer.Serialize(entries))
            { throw new InvalidOperationException("A settlement Save changed original financial rows or recorded the wrong difference/refund."); }
            validSaves.Add(new { Scenario = name, Actual = actual, Paid = paid, RecordedAmount = created[0].Amount,
                Kind = created[0].Kind.ToString(), created[0].RefundOfId, OriginalFinancialFieldsAndIdsRetained = true });
            // Remove only this scenario's identified new rows from the walk-through's fictitious database.
            var removed = await store.SaveEntriesAsync([], created.Select(entry => entry.Id).ToList());
            if (!removed.Succeeded || await StoredAsync() != before)
            { throw new InvalidOperationException("The isolated settlement fixture did not return to its exact pre-save state."); }
            await Shell.Current.GoToAsync(AppShell.SettlementRoute, new Dictionary<string, object> { ["id"] = plan.Id });
            await Task.Delay(500); page = Page(); vm = (Features.Plans.SettlementViewModel)page.BindingContext;
        }
        vm.From = original.From; vm.To = original.To; vm.ActualText = original.ActualText;
        if (await StoredAsync() != before) { throw new InvalidOperationException("Settlement review left stored data changes."); }
        File.WriteAllText(Path.Combine(folder, language + "-settlement-feedback-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            NativeInvalidSaveInvocations = invalidSaves, NativeValidSaveInvocations = validSaves.Count,
            IndependentFieldErrorsAndCorrections = true, FirstAffectedInputRevealed = true,
            FullNativeScaledErrorAndActionGlyphs = true, OriginalDraftAndCompleteStoredValuesRetained = true,
            MatchingBillCreatesNothing = true, ValidSaves = validSaves, FictitiousDatabaseRestoredBetweenScenarios = true,
        }));
    }

    // AT-104: invalid unsaved plan input must expose every affected field in one native Save attempt.
    private static async Task ReviewPlanValidationAsync(App app, IServiceProvider services, string folder, string language)
    {
        var page = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as Features.Plans.PlanEditorPage
            ?? Shell.Current.CurrentPage as Features.Plans.PlanEditorPage
            ?? throw new InvalidOperationException("The real new plan modal is missing.");
        var vm = (Features.Plans.PlanEditorViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        {
            Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(),
            Settings = await store.GetSettingsAsync(), Schedules = await plans.GetSchedulesAsync(),
        });
        var before = await StoredAsync();
        var original = (vm.KindIndex, vm.Name, vm.AmountText, vm.ToAccount, vm.ToAmountText, vm.Accounts);
        vm.KindIndex = 2;
        vm.Name = string.Empty;
        vm.AmountText = "0";
        vm.ToAccount = null;
        var draft = (vm.KindIndex, vm.Name, vm.AmountText, vm.ToAccount);
        var root = (Grid)page.Content;
        var save = root.Children.OfType<Button>().Single(button => Grid.GetRow(button) == 2);
        if (save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
            || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(
                Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        { throw new InvalidOperationException("The plan Save action lacks its actual native Invoke pattern."); }
        invoke.Invoke();
        if (vm.SaveCommand.ExecutionTask is { } saving) { await saving; }
        await Task.Delay(400);
        var desired = Translator.Instance["LedgerError_DestinationRequired"];
        var destination = VisualDescendants(page).OfType<Label>().FirstOrDefault(label => label.Text == desired && label.IsVisible);
        if (string.IsNullOrEmpty(vm.NameError) || string.IsNullOrEmpty(vm.AmountError) || destination is null
            || draft != (vm.KindIndex, vm.Name, vm.AmountText, vm.ToAccount) || await StoredAsync() != before)
        {
            await CaptureAsync(app, folder, language + "-plan-validation-missing-field");
            File.WriteAllText(Path.Combine(folder, language + "-plan-validation-failure.json"), System.Text.Json.JsonSerializer.Serialize(new
            {
                vm.NameError, vm.AmountError, vm.SaveError, DestinationErrorVisible = destination is not null,
                DraftRetained = draft == (vm.KindIndex, vm.Name, vm.AmountText, vm.ToAccount),
                CompleteStoredDataUnchanged = await StoredAsync() == before, NoValidSaveInput = true,
            }));
            throw new InvalidOperationException("One invalid native plan Save did not expose every affected field.");
        }
        var viewport = page.FindByName<ScrollView>("ValidationViewport")
            ?? throw new InvalidOperationException("Missing plan validation viewport.");
        var first = page.FindByName<Entry>("NameInput")
            ?? throw new InvalidOperationException("Missing plan name input.");
        if (first.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox nativeFirst
            || viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer nativeViewport)
        { throw new InvalidOperationException("The invalid plan lacks its actual input/viewport controls."); }
        var firstPosition = nativeFirst.TransformToVisual(nativeViewport).TransformPoint(new Windows.Foundation.Point());
        if (firstPosition.Y < -1 || firstPosition.Y + nativeFirst.ActualHeight > nativeViewport.ActualHeight + 1)
        { throw new InvalidOperationException("Invalid Save left the first affected field outside the actual viewport."); }
        await CaptureAsync(app, folder, language + "-plan-validation-first-field");
        var errors = new[] { vm.NameError, vm.AmountError, vm.ToAccountError };
        foreach (var text in errors)
        {
            var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == text && item.IsVisible);
            await ScrollToViewIfNeededAsync(viewport, label);
            await Task.Delay(100);
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock actual || actual.IsTextTrimmed
                || !actual.IsTextScaleFactorEnabled || actual.ActualWidth < 1 || actual.ActualHeight < 1)
            { throw new InvalidOperationException("An affected field lacks its complete scaled native error caption."); }
            var glyphs = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= actual.ContentEnd.Offset - actual.ContentStart.Offset; offset++)
            {
                if (actual.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                { glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            if (glyphs.Count == 0 || glyphs.Min(rect => rect.Left) < -1 || glyphs.Max(rect => rect.Right) > label.Width + 1
                || glyphs.Max(rect => rect.Bottom) > label.Height + 1)
            { throw new InvalidOperationException("An affected plan field's realized error glyphs exceed the MAUI allocation."); }
            await CaptureAsync(app, folder, language + "-plan-validation-field-" + Array.IndexOf(errors, text));
        }
        async Task RetryAsync(string name, string error, VisualElement target)
        {
            await ScrollToEndIfNeededAsync(viewport);
            var retained = (vm.Name, vm.AmountText, vm.ToAccount, vm.ToAmountText, vm.Note, vm.ReminderDaysText, vm.ReminderTime);
            invoke.Invoke();
            if (vm.SaveCommand.ExecutionTask is { } attempt) { await attempt; }
            await Task.Delay(400);
            if (vm.NameError is not null || vm.AmountError is not null || vm.SaveError is not null
                || retained != (vm.Name, vm.AmountText, vm.ToAccount, vm.ToAmountText, vm.Note, vm.ReminderDaysText, vm.ReminderTime)
                || await StoredAsync() != before || target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeTarget)
            { throw new InvalidOperationException("Correcting earlier plan fields reset the draft or retained stale problems."); }
            var position = nativeTarget.TransformToVisual(nativeViewport).TransformPoint(new Windows.Foundation.Point());
            if (position.Y < -1 || position.Y + nativeTarget.ActualHeight > nativeViewport.ActualHeight + 1
                || !VisualDescendants(page).OfType<Label>().Any(label => label.IsVisible && label.Text == error))
            {
                await CaptureAsync(app, folder, language + "-plan-validation-" + name + "-failed");
                File.WriteAllText(Path.Combine(folder, language + "-plan-validation-" + name + "-failed.json"),
                    System.Text.Json.JsonSerializer.Serialize(new { position, nativeTarget.ActualHeight,
                        ViewportHeight = nativeViewport.ActualHeight, vm.NameError, vm.AmountError, vm.AccountError,
                        vm.ToAccountError, vm.ToAmountError, vm.SaveError, ExpectedCaption = error }));
                throw new InvalidOperationException("The next affected plan input and its own error were not revealed.");
            }
            await CaptureAsync(app, folder, language + "-plan-validation-" + name);
        }
        vm.Name = "Fictitious validation draft";
        vm.AmountText = Core.Money.MoneyText.ForInput(1234, vm.CurrencyCode, services.GetRequiredService<ILocalizationService>().CurrentCulture);
        var toInput = page.FindByName<Picker>("ToAccountInput");
        await RetryAsync("missing-destination", desired, toInput);
        if (vm.ToAccountError != desired) { throw new InvalidOperationException("Missing destination lost its field error."); }
        vm.ToAccount = vm.Account;
        await RetryAsync("same-account", Translator.Instance["LedgerError_SameAccountTransfer"], toInput);
        if (vm.ToAccountError != Translator.Instance["LedgerError_SameAccountTransfer"])
        { throw new InvalidOperationException("A plan transfer to its source lost the existing rejection."); }
        // Only the presentation choices grow; the missing amount prevents persistence of this fictitious destination ID.
        var foreign = new Features.Entries.AccountChoice(Guid.NewGuid(), "Fictitious cross-currency destination", "JPY");
        vm.Accounts = [.. original.Accounts, foreign];
        vm.ToAccount = foreign; vm.ToAmountText = "0";
        await RetryAsync("destination-amount", Translator.Instance["LedgerError_DestinationAmountRequired"], page.FindByName<Entry>("ToAmountInput"));
        if (vm.ToAmountError != Translator.Instance["LedgerError_DestinationAmountRequired"] || vm.ToAccountError is not null)
        { throw new InvalidOperationException("The cross-currency amount did not replace the corrected destination error."); }

        vm.KindIndex = original.KindIndex; vm.Name = original.Name; vm.AmountText = original.AmountText;
        vm.Accounts = original.Accounts; vm.ToAccount = original.ToAccount; vm.ToAmountText = original.ToAmountText;
        if (await StoredAsync() != before) { throw new InvalidOperationException("Invalid plan validation wrote stored data."); }
        File.WriteAllText(Path.Combine(folder, language + "-plan-validation-proof.json"), "{\"OneNativeSave\":true,\"NativeInvalidSaveInvocations\":4,\"AllAffectedFieldsVisible\":true,\"FirstFieldAutomaticallyRevealed\":true,\"CorrectedFieldsClearTheirErrors\":true,\"MissingSameAccountAndCrossCurrencyDestinationFeedback\":true,\"NativeErrorGlyphsAndScaling\":true,\"DraftAndStoredDataRetained\":true}");
    }

    // AT-103: modal forms keep their own title/Cancel row; a generic child header must never wrap their retained body.
    private static async Task ReviewModalHeadersAsync(App app, IServiceProvider services, Features.Accounts.AccountsPage parent,
        (string Name, string Route, Dictionary<string, object>? Query)[] cases, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var goals = services.GetRequiredService<GoalStore>();
        var holdings = services.GetRequiredService<HoldingStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        {
            Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
            Budgets = await store.GetBudgetsAsync(), Schedules = await plans.GetSchedulesAsync(), Categories = await store.GetCategoriesAsync(),
            Goals = await goals.GetGoalsAsync(), HoldingTypes = await holdings.GetTypesAsync(), HoldingEvents = await holdings.GetEventsAsync(),
        });
        static bool Visible(VisualElement element)
        {
            for (Element? ancestor = element; ancestor is not null; ancestor = ancestor.Parent)
            { if (ancestor is VisualElement { IsVisible: false }) { return false; } }
            return true;
        }
        static bool Arranged(ContentPage modal) => modal.Width > 0 && modal.Height > 0
            && VisualDescendants(modal).OfType<Vafadar.Maui.Controls.DateField>().Where(Visible).All(field => field.Width > 0 && field.Height > 0
                && VisualDescendants(field).OfType<Entry>().All(box => box.Width > 0 && box.Height > 0
                    && box.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox { ActualWidth: > 0, ActualHeight: > 0 }));
        var before = await StoredAsync();
        var parentBody = parent.Content;
        var parentContext = parent.BindingContext;
        if (VisualDescendants(parent).OfType<Presentation.PageHeader>().Count() != 1)
        { throw new InvalidOperationException("The ordinary parent must retain its one visible child header."); }
        var reviewed = new List<string>();
        foreach (var (name, route, query) in cases)
        {
            // Existing named routes load their actual seeded/new models; only the native Cancel/Close action returns.
            await (query is null ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, new Dictionary<string, object>(query)));
            for (var attempt = 0; attempt < 100 && parent.Navigation.ModalStack.LastOrDefault() is not ContentPage; attempt++)
            { await Task.Delay(50); }
            if (parent.Navigation.ModalStack.LastOrDefault() is not ContentPage modal)
            { throw new InvalidOperationException("The named modal route did not open its real page."); }
            await Task.Delay(500);
            for (var attempt = 0; attempt < 100 && !Arranged(modal); attempt++) { await Task.Delay(50); }
            if (!Arranged(modal)) { throw new InvalidOperationException("The actual modal/date inputs did not finish arrangement."); }
            await CheckModalHeaderAsync(app, modal, folder, language + "-modal-header-" + name);
            await CaptureAsync(app, folder, language + "-modal-" + name);
            if (await StoredAsync() != before) { throw new InvalidOperationException("Opening a modal wrote stored data."); }
            var header = ((Grid)modal.Content).Children.OfType<Grid>().Single(grid => Grid.GetRow(grid) == 0);
            var close = VisualDescendants(header).OfType<Button>().Single();
            if (close.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(
                    Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
            { throw new InvalidOperationException("The real own modal Cancel/Close button has no native Invoke pattern."); }
            invoke.Invoke();
            for (var attempt = 0; attempt < 100 && parent.Navigation.ModalStack.Count != 0; attempt++) { await Task.Delay(50); }
            await WaitForReopenedLayoutAsync(parent);
            if (parent.Navigation.ModalStack.Count != 0 || !ReferenceEquals(Shell.Current.CurrentPage, parent)
                || !ReferenceEquals(parent.Content, parentBody) || !ReferenceEquals(parent.BindingContext, parentContext)
                || VisualDescendants(parent).OfType<Presentation.PageHeader>().Count() != 1 || await StoredAsync() != before)
            { throw new InvalidOperationException("Native modal Cancel/Close replaced its parent or wrote stored data."); }
            reviewed.Add(modal.GetType().Name);
        }
        await CaptureAsync(app, folder, language + "-modal-parent-restored");
        InvokeSnapshotBack(parent);
        for (var attempt = 0; attempt < 100 && ReferenceEquals(Shell.Current.CurrentPage, parent); attempt++) { await Task.Delay(50); }
        if (ReferenceEquals(Shell.Current.CurrentPage, parent) || await StoredAsync() != before)
        { throw new InvalidOperationException("The ordinary parent lost its native Back or wrote data after modal return."); }
        File.WriteAllText(Path.Combine(folder, language + "-modal-headers-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            ActualModalTypes = reviewed.Distinct().ToArray(), ModalCases = cases.Length, NativeCancelCloseInvocations = reviewed.Count,
            SingleOwnHeadersAndRetainedBodies = true, SameOrdinaryParentAndHeader = true, NativeParentBack = true,
            CompleteAccountsEntriesSettingsBudgetsSchedulesCategoriesGoalsHoldingsUnchanged = true, NoInputSaveOrPosting = true,
        }));
    }

    // The actual native peer supplies a MAUI button's spoken name; a manually constructed base peer can miss it.
    private static async Task CheckModalHeaderAsync(App app, ContentPage page, string folder, string name)
    {
        var count = VisualDescendants(page).OfType<Presentation.PageHeader>().Count();
        if (count != 0)
        {
            await CaptureAsync(app, folder, name + "-duplicated");
            File.WriteAllText(Path.Combine(folder, name + "-failure.json"), System.Text.Json.JsonSerializer.Serialize(new
            {
                PageType = page.GetType().Name, page.Title, Mode = Shell.GetPresentationMode(page).ToString(),
                GenericChildHeaders = count, VisibleTitles = VisualDescendants(page).OfType<Label>()
                    .Where(label => label.Text == page.Title).Select(label => new { label.Text, label.FontSize, label.Width, label.Height }),
            }));
            throw new InvalidOperationException("The modal form has an additional generic child title/Back header.");
        }
        var body = page.Content as Grid ?? throw new InvalidOperationException("The actual modal form body is missing.");
        var header = body.Children.OfType<Grid>().Single(grid => Grid.GetRow(grid) == 0);
        var title = VisualDescendants(header).OfType<Label>().First(label => label.FontAttributes == FontAttributes.Bold);
        var close = VisualDescendants(header).OfType<Button>().Single();
        if (title.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock nativeTitle
            || close.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeClose)
        { throw new InvalidOperationException("The retained modal title/close row lacks its actual native controls."); }
        var glyphs = new List<Windows.Foundation.Rect>();
        for (var offset = 0; offset <= nativeTitle.ContentEnd.Offset - nativeTitle.ContentStart.Offset; offset++)
        {
            var pointer = nativeTitle.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
            if (pointer is not null) { glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
        }
        var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(nativeClose)
            ?? throw new InvalidOperationException("The actual modal close button has no native automation peer.");
        static Microsoft.UI.Xaml.Controls.TextBlock? FindCaption(Microsoft.UI.Xaml.DependencyObject root, string text)
        {
            if (root is Microsoft.UI.Xaml.Controls.TextBlock caption && caption.Text == text) { return caption; }
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root); index++)
            {
                if (FindCaption(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, index), text) is { } found) { return found; }
            }
            return null;
        }
        var closeCaption = FindCaption(nativeClose, close.Text)
            ?? throw new InvalidOperationException("The modal close button lacks its realized complete caption.");
        var closeGlyphs = new List<Windows.Foundation.Rect>();
        for (var offset = 0; offset <= closeCaption.ContentEnd.Offset - closeCaption.ContentStart.Offset; offset++)
        {
            var pointer = closeCaption.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
            if (pointer is not null) { closeGlyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
        }
        if (header.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeHeader)
        { throw new InvalidOperationException("The own modal header has no actual native allocation."); }
        var titleOrigin = nativeTitle.TransformToVisual(nativeHeader).TransformPoint(new Windows.Foundation.Point());
        var closeOrigin = nativeClose.TransformToVisual(nativeHeader).TransformPoint(new Windows.Foundation.Point());
        var captionOrigin = closeCaption.TransformToVisual(nativeClose).TransformPoint(new Windows.Foundation.Point());
        var titleRect = new Windows.Foundation.Rect(titleOrigin.X, titleOrigin.Y, nativeTitle.ActualWidth, nativeTitle.ActualHeight);
        var closeRect = new Windows.Foundation.Rect(closeOrigin.X, closeOrigin.Y, nativeClose.ActualWidth, nativeClose.ActualHeight);
        var overlapWidth = Math.Min(titleRect.Right, closeRect.Right) - Math.Max(titleRect.Left, closeRect.Left);
        var overlapHeight = Math.Min(titleRect.Bottom, closeRect.Bottom) - Math.Max(titleRect.Top, closeRect.Top);
        if (string.IsNullOrWhiteSpace(title.Text) || nativeTitle.Text != title.Text || nativeTitle.IsTextTrimmed
            || !nativeTitle.IsTextScaleFactorEnabled || glyphs.Count == 0 || glyphs.Min(rect => rect.Left) < -1
            || glyphs.Max(rect => rect.Right) > title.Width + 1 || glyphs.Max(rect => rect.Bottom) > title.Height + 1
            || nativeClose.ActualWidth < 44 || nativeClose.ActualHeight < 44 || !nativeClose.IsEnabled
            || close.Command is null || peer.GetName() != close.Text || closeCaption.IsTextTrimmed || !closeCaption.IsTextScaleFactorEnabled
            || closeGlyphs.Count == 0 || captionOrigin.X + closeGlyphs.Min(rect => rect.Left) < -1
            || captionOrigin.X + closeGlyphs.Max(rect => rect.Right) > nativeClose.ActualWidth + 1
            || captionOrigin.Y + closeGlyphs.Max(rect => rect.Bottom) > nativeClose.ActualHeight + 1
            || titleRect.Left < -1 || titleRect.Top < -1 || titleRect.Right > nativeHeader.ActualWidth + 1
            || titleRect.Bottom > nativeHeader.ActualHeight + 1 || closeRect.Left < -1 || closeRect.Top < -1
            || closeRect.Right > nativeHeader.ActualWidth + 1 || closeRect.Bottom > nativeHeader.ActualHeight + 1
            || overlapWidth > 1 && overlapHeight > 1)
        {
            await CaptureAsync(app, folder, name + "-own-row-failure");
            File.WriteAllText(Path.Combine(folder, name + "-own-row-failure.json"), System.Text.Json.JsonSerializer.Serialize(new
            {
                title.Text, nativeText = nativeTitle.Text, nativeTitle.IsTextTrimmed, nativeTitle.IsTextScaleFactorEnabled,
                nativeTitle.FontSize, title.Width, title.Height, nativeTitle.ActualWidth, nativeTitle.ActualHeight,
                glyphs, closeText = close.Text, hasCommand = close.Command is not null, nativeClose.IsEnabled, closeName = peer.GetName(),
                targetWidth = nativeClose.ActualWidth, targetHeight = nativeClose.ActualHeight,
                closeGlyphs, captionOrigin, titleRect, closeRect, overlapWidth, overlapHeight,
                headerWidth = nativeHeader.ActualWidth, headerHeight = nativeHeader.ActualHeight,
            }));
            throw new InvalidOperationException("The own modal title/close row clips, loses scaling or lacks its complete native action.");
        }
        // Idempotent attachment attempts must leave the same grid, live binding context and own title row in place.
        var context = body.BindingContext;
        Presentation.PageHeader.Attach(page, page.FlowDirection == FlowDirection.RightToLeft);
        if (!ReferenceEquals(page.Content, body) || !ReferenceEquals(body.BindingContext, context)
            || VisualDescendants(page).OfType<Presentation.PageHeader>().Any())
        { throw new InvalidOperationException("Generic child attachment replaced the actual modal body or bindings."); }
        File.WriteAllText(Path.Combine(folder, name + "-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            PageType = page.GetType().Name, page.Title, OwnTitle = title.Text, Mode = Shell.GetPresentationMode(page).ToString(),
            GenericChildHeaders = 0, OwnHeaderCount = 1, FullNativeTitleAndCloseName = true, nativeClose.ActualWidth,
            nativeClose.ActualHeight, RetainedBodyAndContextOnAttachment = true, NativeGlyphBoundsAndNoOverlap = true,
            Layout = titleRect.Top >= closeRect.Bottom - 1 ? "stacked" : "inline",
        }));
    }

    // AT-101: every combination of existing default/excluded/incomplete badges uses only fictitious presentation data.
    private static async Task ReviewAccountDescriptionsAsync(App app, IServiceProvider services,
        Features.Home.HomePage page, string folder, string language)
    {
        var home = (Features.Home.HomeViewModel)page.BindingContext;
        var original = home.Accounts.ToArray();
        if (original.Length == 0) { return; }
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(), Budgets = await store.GetBudgetsAsync() });
        var before = await StoredAsync();
        var section = page.FindByName<VerticalStackLayout>("AccountsSection");
        var rows = VisualDescendants(section).OfType<Features.Accounts.AccountRow>().ToArray();
        var scroll = FindScrollView(page) ?? throw new InvalidOperationException("The account descriptions have no viewport.");
        var cases = new List<object>();
        try
        {
            for (var flags = 0; flags < 8; flags++)
            {
                var values = original.ToArray();
                values[0] = original[0] with { IsDefault = (flags & 1) != 0, NotInTotals = (flags & 2) != 0, OpeningUnknown = (flags & 4) != 0 };
                home.Accounts.ReplaceAll(values);
                await WaitForReopenedLayoutAsync(page);
                var current = VisualDescendants(section).OfType<Features.Accounts.AccountRow>().ToArray();
                if (current.Length != original.Length || !rows.Zip(current).All(pair => ReferenceEquals(pair.First, pair.Second))
                    || !current.Select((row, index) => ReferenceEquals(row.BindingContext, home.Accounts[index])).All(value => value))
                { throw new InvalidOperationException("Account description changes lost complete values or rebuilt their native rows."); }
                await ScrollToViewIfNeededAsync(scroll, current[0]);
                await CaptureAsync(app, folder, language + "-account-descriptions-" + flags);
                var labels = VisualDescendants(current[0]).OfType<Label>().Where(label => label.IsVisible
                    && (ReferenceEquals(label.Parent, current[0].FindByName<FlexLayout>("AccountDescriptions")))).ToArray();
                if (labels.Length != 1 + System.Numerics.BitOperations.PopCount((uint)flags))
                { throw new InvalidOperationException("The account description fixture lost one of its existing captions."); }
                cases.Add(new { Flags = flags, ActualVisibleDescriptionCount = labels.Length, NativeRowsRetained = true,
                    CompleteSnapshot = home.Accounts.Count, OriginalAccountIdentityAndAmount = values[0].Id == original[0].Id
                        && values[0].Name == original[0].Name && values[0].BalanceText == original[0].BalanceText });
            }
        }
        finally { home.Accounts.ReplaceAll(original); }
        await WaitForReopenedLayoutAsync(page);
        if (!home.Accounts.SequenceEqual(original) || await StoredAsync() != before)
        { throw new InvalidOperationException("Account description review changed the complete snapshot or stored data."); }
        File.WriteAllText(Path.Combine(folder, language + "-account-descriptions-proof.json"),
            System.Text.Json.JsonSerializer.Serialize(new { Cases = cases, PresentationOnly = true, CompleteOriginalSnapshotRestored = true,
                CompleteAccountsEntriesSettingsBudgetsUnchanged = true, NoSaveOrFinancialCalculation = true }));
    }

    // AT-98: visible bindings retain native account rows; hidden bindings keep the full snapshot without views or Save.
    private static async Task ReviewHomeSnapshotAsync(IServiceProvider services, Page page, string folder, string language)
    {
        var home = (Features.Home.HomeViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(), Budgets = await store.GetBudgetsAsync() });
        await home.LoadAsync();
        var before = await StoredAsync();
        var accountValues = home.Accounts.ToArray();
        var section = page.FindByName<VerticalStackLayout>("AccountsSection");
        var rows = VisualDescendants(section).OfType<Features.Accounts.AccountRow>().ToArray();
        var events = new List<System.Collections.Specialized.NotifyCollectionChangedAction>();
        System.Collections.Specialized.NotifyCollectionChangedEventHandler changed = (_, e) => events.Add(e.Action);
        home.Accounts.CollectionChanged += changed;
        try { await home.LoadAsync(); }
        finally { home.Accounts.CollectionChanged -= changed; }
        var after = VisualDescendants(section).OfType<Features.Accounts.AccountRow>().ToArray();
        var expectedRows = section.IsVisible ? home.Accounts.Count : 0;
        if (rows.Length != expectedRows || after.Length != rows.Length
            || !rows.Zip(after).All(pair => ReferenceEquals(pair.First, pair.Second))
            || !after.Select((row, index) => ReferenceEquals(row.BindingContext, home.Accounts[index])).All(value => value)
            || !home.Accounts.SequenceEqual(accountValues)
            || events.Count != 1 || events[0] != System.Collections.Specialized.NotifyCollectionChangedAction.Reset
            || (!section.IsVisible && BindableLayout.GetItemsSource(page.FindByName<VerticalStackLayout>("AccountRows")) is not null)
            || await StoredAsync() != before)
        { throw new InvalidOperationException("Home reload rebuilt account rows, failed to rebind values or changed stored data."); }
        File.WriteAllText(Path.Combine(folder, language + "-home-snapshot-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            AccountSnapshotCount = home.Accounts.Count, AccountSectionVisible = section.IsVisible, AccountRows = after.Length,
            ExistingNativeRowsRetained = section.IsVisible ? true : (bool?)null,
            FreshBindingContextsAndExactValues = section.IsVisible ? true : (bool?)null,
            NoHiddenAccountViews = !section.IsVisible ? true : (bool?)null,
            OneCompleteReset = true, CompleteAccountsEntriesSettingsBudgetsUnchanged = true, NoFinancialSave = true,
        }));
    }

    // AT-97: both restore alternatives expose the full caption and return to the same unsaved onboarding draft.
    private static async Task ReviewOnboardingRestoreAsync(App app, IServiceProvider services,
        OnboardingPage page, OnboardingViewModel vm, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        string Draft() => System.Text.Json.JsonSerializer.Serialize(new
        {
            Account = vm.Account.Snapshot(), vm.ReportCurrency, vm.ThemeIndex, vm.ModeIndex, vm.SelectedCalendar,
            vm.Regional.SelectedFormat, vm.Regional.DigitIndex, vm.Regional.SelectedHolidayRegion, vm.Regional.SelectedWeekStart,
        });
        var step = vm.Step; var draft = Draft(); var before = await StoredAsync();
        var action = page.FindByName<Presentation.WrappingAction>(step == 1 ? "WelcomeRestoreAction" : "AccountRestoreAction");
        var scroll = FindScrollView(page) ?? throw new InvalidOperationException("Onboarding has no actual viewport.");
        await scroll.ScrollToAsync(action, ScrollToPosition.Center, animated: false);
        await Task.Delay(350);
        await CaptureAsync(app, folder, $"{language}-onboarding-restore-action-{step}");
        await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-onboarding-restore-action-{step}-window.png"));
        if (action.Content is not Grid face || face.Children.OfType<Button>().Single().Handler?.PlatformView
            is not Microsoft.UI.Xaml.Controls.Button native || face.Children.OfType<Border>().Single().Content
            is not Label { Handler.PlatformView: Microsoft.UI.Xaml.Controls.TextBlock caption })
        { throw new InvalidOperationException("The actual restore caption/command target is unavailable."); }
        var button = face.Children.OfType<Button>().Single();
        var expected = services.GetRequiredService<Translator>()["Onb_RestoreButton"];
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native);
        var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(caption);
        if (action.Text != expected || peer.GetName() != expected || button.Command != vm.RestoreBackupCommand
            || !native.IsEnabled || !caption.IsTextScaleFactorEnabled || caption.IsTextTrimmed
            || action.Width < 44 || action.Height < 44 || slot.Width > action.Width + 1
            || caption.DesiredSize.Height > caption.ActualHeight + 1)
        { throw new InvalidOperationException("The restore alternative clips its caption or loses its native target/name/command."); }
        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
            is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        { throw new InvalidOperationException("The restore alternative has no native Invoke pattern."); }
        invoke.Invoke();
        for (var attempt = 0; attempt < 100 && page.Navigation.ModalStack.LastOrDefault()?.BindingContext
            is not Features.Backup.BackupViewModel; attempt++) { await Task.Delay(50); }
        if (page.Navigation.ModalStack.LastOrDefault() is not Features.Backup.BackupPage restorePage
            || restorePage.BindingContext is not Features.Backup.BackupViewModel restore || !restore.IsRestoreOnly)
        { throw new InvalidOperationException("The native restore alternative did not open first-run restoration."); }
        for (var attempt = 0; attempt < 100 && restore.IsBusy; attempt++) { await Task.Delay(50); }
        if (restore.IsBusy) { throw new InvalidOperationException("The restore page did not finish loading."); }
        await Task.Delay(350);
        await CaptureAsync(app, folder, $"{language}-onboarding-restore-{step}");
        var backButton = VisualDescendants(restorePage).OfType<Button>().Single(candidate => candidate.Command == restore.BackToOnboardingCommand);
        if (backButton.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button back
            || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(back).GetPattern(
                Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider returnInvoke)
        { throw new InvalidOperationException("The actual restore return target is unavailable."); }
        returnInvoke.Invoke();
        for (var attempt = 0; attempt < 100 && page.Navigation.ModalStack.Count > 0; attempt++) { await Task.Delay(50); }
        await Task.Delay(250);
        if (page.Navigation.ModalStack.Count != 0 || !ReferenceEquals(app.Windows[0].Page, page) || vm.Step != step
            || Draft() != draft || await StoredAsync() != before)
        { throw new InvalidOperationException("Restore navigation changed the onboarding draft, stored data or current step."); }
        File.WriteAllText(Path.Combine(folder, $"{language}-onboarding-restore-proof-{step}.json"),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                Step = step, Caption = expected, action.Width, action.Height, caption.FontSize, caption.IsTextTrimmed,
                NativeRestoreAndReturnInvocations = 2, SameOnboardingPageAndDraft = true,
                CompleteAccountsEntriesSettingsUnchanged = true, NoRestoreOrAccountCreation = true,
            }));
    }

    // AT-96: disclosure names match the actual panel; native toggles retain every unsaved field without Save.
    private static async Task ReviewEntryDetailsAsync(App app, IServiceProvider services,
        Features.Entries.EntryEditorPage page, string folder, string language)
    {
        var vm = (Features.Entries.EntryEditorViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var translator = services.GetRequiredService<Translator>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        // Read the editor's actual unsaved-field contract instead of maintaining a second field list.
        string Draft() => (string)(typeof(Features.Entries.EntryEditorViewModel)
            .GetMethod("Snapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(vm, null) ?? throw new InvalidOperationException("The editor draft fingerprint is unavailable."));
        var before = await StoredAsync(); var originalDraft = Draft(); var originalDetails = vm.ShowDetails;
        var originalDirty = vm.IsDirty; var originalPayee = vm.Payee; var originalNote = vm.Note;
        var originalTags = vm.TagsText; var originalSuggestions = vm.TagSuggestions;
        var settings = await store.GetSettingsAsync();
        if (originalDetails != Core.Settings.FeaturePolicy.Shows(settings, Core.Settings.Feature.EntryDetails))
        { throw new InvalidOperationException("Initial detail visibility disagrees with the existing experience policy."); }
        var group = page.FindByName<VerticalStackLayout>("EntryDetailsActionGroup");
        var scroll = FindScrollView(page) ?? throw new InvalidOperationException("The detail disclosure has no actual viewport.");
        var steps = new List<object>();
        try
        {
            vm.Payee = "QA96 original payee"; vm.Note = "QA96 original note"; vm.TagsText = "QA96 original tag";
            var typed = Draft(); var typedDirty = vm.IsDirty;
            for (var index = 0; index < 2; index++)
            {
                await scroll.ScrollToAsync(group, ScrollToPosition.Center, animated: false);
                await Task.Delay(350); await CaptureAsync(app, folder, language + "-entry-details-action-" + index);
                var visible = group.Children.OfType<View>().Where(view => view.IsVisible).ToArray();
                if (visible.Length != 1) { throw new InvalidOperationException("The detail disclosure exposes duplicate actions."); }
                var control = visible[0];
                var button = control is Presentation.WrappingAction action && action.Content is Grid face
                    ? face.Children.OfType<Button>().Single() : control as Button;
                if (button?.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                    || group.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeGroup)
                { throw new InvalidOperationException("The real detail disclosure is not realized."); }
                var expanded = vm.ShowDetails;
                var expected = translator[expanded ? "Entry_HideDetails" : "Entry_MoreDetails"];
                var caption = control is Presentation.WrappingAction wrapping ? wrapping.Text : button.Text;
                var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native);
                var box = native.TransformToVisual(nativeGroup).TransformBounds(new Windows.Foundation.Rect(0, 0, native.ActualWidth, native.ActualHeight));
                if (caption != expected || peer.GetName() != expected || button.Command != vm.ToggleDetailsCommand
                    || box.Width < 44 || box.Height < 44 || box.Left < -1 || box.Right > nativeGroup.ActualWidth + 1)
                {
                    File.WriteAllText(Path.Combine(folder, language + "-entry-details-failure.json"), System.Text.Json.JsonSerializer.Serialize(new
                    { expanded, caption, expected, NativeName = peer.GetName(), box, CorrectCommand = button.Command == vm.ToggleDetailsCommand }));
                    throw new InvalidOperationException("The disclosure caption/name does not describe its current action or target.");
                }
                if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                    is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
                { throw new InvalidOperationException("The detail disclosure lacks its native Invoke pattern."); }
                invoke.Invoke(); await Task.Delay(250);
                if (vm.ShowDetails == expanded || Draft() != typed || vm.IsDirty != typedDirty
                    || page.FindByName<Entry>("PayeeEntry").Text != vm.Payee
                    || page.FindByName<Entry>("TagsEntry").Text != vm.TagsText
                    || page.FindByName<Editor>("NoteEditor").Text != vm.Note)
                { throw new InvalidOperationException("Native disclosure failed to toggle or changed an unsaved field."); }
                steps.Add(new { ExpandedBefore = expanded, Caption = expected, box, NativeInvokeToggles = true, ExactTypedFieldsRetained = true });
            }
            File.WriteAllText(Path.Combine(folder, language + "-entry-details-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
            { InitialExpanded = originalDetails, InitialExperiencePolicyMatches = true, Steps = steps, NoFinancialSave = true }));
        }
        finally
        {
            vm.Payee = originalPayee; vm.Note = originalNote; vm.TagsText = originalTags;
            vm.TagSuggestions = originalSuggestions; vm.ShowDetails = originalDetails;
            if (Draft() != originalDraft || vm.IsDirty != originalDirty || before != await StoredAsync())
            { throw new InvalidOperationException("Detail disclosure review changed stored rows or failed to restore the original draft."); }
            File.WriteAllText(Path.Combine(folder, language + "-entry-details-restoration-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
            { CompleteAccountsEntriesSettingsUnchanged = true, OriginalDraftRestored = true, NativeInvocations = steps.Count, NoFinancialSave = true }));
        }
    }

    // AT-95: real tag targets/captions and AddTag commands; only an unsaved fictitious presentation draft changes.
    private static async Task ReviewTagChoicesAsync(App app, IServiceProvider services,
        Features.Entries.EntryEditorPage page, string folder, string language)
    {
        var vm = (Features.Entries.EntryEditorViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await StoredAsync(); var originalTags = vm.TagsText; var originalSuggestions = vm.TagSuggestions;
        var originalDetails = vm.ShowDetails; var originalDirty = vm.IsDirty;
        string[] tags = ["short", "Long label 0123456789012345678", "Fahrzeugversicherung unterwegs", "برچسب فارسی برای بررسی نمایش"];
        var group = page.FindByName<VerticalStackLayout>("TagSuggestionsGroup");
        var scroll = FindScrollView(page) ?? throw new InvalidOperationException("The tag draft lacks its actual scroll viewport.");
        var checks = new List<object>();
        try
        {
            vm.ShowDetails = true; vm.TagSuggestions = [.. tags.Select(tag => new Features.Entries.TagSuggestion(tag))];
            await Task.Delay(500); await scroll.ScrollToAsync(group, ScrollToPosition.Center, animated: false);
            await Task.Delay(400); await CaptureAsync(app, folder, language + "-entry-tags-targets");
            if (group.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeGroup)
            { throw new InvalidOperationException("The real tag group is not realized."); }
            foreach (var raw in tags)
            {
                // Adding a tag regenerates suggestions; reacquire the actual current binding/control each time.
                vm.TagsText = originalTags;
                vm.TagSuggestions = [.. tags.Select(tag => new Features.Entries.TagSuggestion(tag))];
                await Task.Delay(300);
                var action = group.Children.OfType<Presentation.WrappingAction>().Single(item => Equals(item.CommandParameter, raw));
                await scroll.ScrollToAsync(action, ScrollToPosition.Center, animated: false);
                await Task.Delay(250);
                await CaptureAsync(app, folder, language + "-entry-tags-action-" + checks.Count);
                if (action.Content is not Grid face || face.Children.LastOrDefault() is not Button button
                    || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native)
                { throw new InvalidOperationException("The real tag button is not realized."); }
                var box = native.TransformToVisual(nativeGroup).TransformBounds(new Windows.Foundation.Rect(0, 0, native.ActualWidth, native.ActualHeight));
                if (box.Width < 44 || box.Height < 44 || box.Left < -1 || box.Right > nativeGroup.ActualWidth + 1
                    || Math.Abs(action.MaximumWidthRequest - group.Width) > 1 || button.Command != vm.AddTagCommand
                    || action.Text != new Features.Entries.TagSuggestion(raw).Caption)
                {
                    File.WriteAllText(Path.Combine(folder, language + "-entry-tags-target-failure.json"), System.Text.Json.JsonSerializer.Serialize(new
                    { raw, box, action.MaximumWidthRequest, groupWidth = group.Width, nativeGroup.ActualWidth,
                        correctCommand = button.Command == vm.AddTagCommand, action.Text }));
                    throw new InvalidOperationException("A tag target/caption/command exceeds its actual group or loses its raw identity.");
                }
                var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native);
                if (peer.GetName() != action.Text || peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                    is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
                { throw new InvalidOperationException("The tag button lacks its full native name or Invoke pattern."); }
                var checkedCaption = action.Text;
                invoke.Invoke(); await Task.Delay(150);
                if (!Core.Ledger.EntryTags.Parse(vm.TagsText).Contains(raw) || vm.TagSuggestions.Any(item => item.Value == raw))
                { throw new InvalidOperationException("A native tag action did not add the original raw tag to the unsaved draft."); }
                checks.Add(new { Text = checkedCaption, raw, box, NativeInvokeAddsOriginalTag = true });
            }
            File.WriteAllText(Path.Combine(folder, language + "-entry-tags-proof.json"), System.Text.Json.JsonSerializer.Serialize(checks));
        }
        finally
        { vm.TagsText = originalTags; vm.TagSuggestions = originalSuggestions; vm.ShowDetails = originalDetails; }
        if (vm.TagsText != originalTags || !ReferenceEquals(vm.TagSuggestions, originalSuggestions)
            || vm.ShowDetails != originalDetails || vm.IsDirty != originalDirty || before != await StoredAsync())
        { throw new InvalidOperationException("Tag-choice review changed stored rows or failed to restore the original draft."); }
        File.WriteAllText(Path.Combine(folder, language + "-entry-tags-restoration-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        { CompleteAccountsEntriesSettingsUnchanged = true, ExactOriginalDraftAndSuggestionsRestored = true, NativeInvocations = checks.Count, NoFinancialSave = true }));
    }

    // AT-94: invoke all three actual native period choices and retain stored financial/preferences rows without Save.
    private static async Task ReviewBudgetPeriodsAsync(App app, IServiceProvider services,
        Features.Budget.BudgetPage page, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
            Budgets = await store.GetBudgetsAsync(), Plans = await plans.GetSchedulesAsync() });
        var before = await StoredAsync(); var vm = (Features.Budget.BudgetViewModel)page.BindingContext;
        var original = vm.PeriodIndex; var invoked = new List<int>();
        async Task SelectAsync(int index)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page))
            { throw new InvalidOperationException("The reviewed budget page was retired before its period invocation."); }
            var choices = page.FindByName<Vafadar.Maui.Controls.ChoiceChips>("BudgetPeriods");
            var chips = VisualDescendants(choices).OfType<Grid>().Where(chip => chip.Children.OfType<Button>().Any()).ToArray();
            var button = chips[index].Children.OfType<Button>().Single();
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetPattern(
                    Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
            { throw new InvalidOperationException("Budget period lacks a real native Invoke target."); }
            invoke.Invoke(); await Task.Delay(150);
            // Join the actual serialized reload after the native click; never assign the view-model selection directly.
            await vm.LoadAsync(); await Task.Delay(300);
            if (vm.PeriodIndex != index || choices.SelectedIndex != index || vm.IsTwoWeeks != (index == 2)
                || string.IsNullOrWhiteSpace(vm.PeriodText))
            { throw new InvalidOperationException("Native period invocation did not publish the selected existing view."); }
        }
        try
        {
            for (var index = 0; index < 3; index++)
            {
                await SelectAsync(index); invoked.Add(index);
                if (FindScrollView(page) is { } outer) { await outer.ScrollToAsync(0, 0, animated: false); }
                await CaptureAsync(app, folder, language + "-budget-periods-native-" + index);
            }
        }
        finally
        {
            if (ReferenceEquals(Shell.Current.CurrentPage, page)) { await SelectAsync(original); }
        }
        if (before != await StoredAsync()) { throw new InvalidOperationException("Budget period navigation changed stored financial/settings data."); }
        await CaptureAsync(app, folder, language + "-budget-periods-restored");
        File.WriteAllText(Path.Combine(folder, language + "-budget-periods-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        { ActualNativeInvokedIndexes = invoked, AllThreeFullTargetsVisible = true, CompleteAccountsEntriesSettingsBudgetsPlansUnchanged = true,
            OriginalSelectionRestored = vm.PeriodIndex == original }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    // AT-93: exercise real compact/default readouts with in-memory presentation fixtures and native scrolling, never Save.
    private static async Task ReviewBudgetReadoutsAsync(App app, IServiceProvider services,
        Features.Budget.BudgetPage page, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
            Budgets = await store.GetBudgetsAsync(), Plans = await plans.GetSchedulesAsync() });
        var before = await StoredAsync();
        var vm = (Features.Budget.BudgetViewModel)page.BindingContext;
        if (vm.TotalLines.Count == 0) { throw new InvalidOperationException("The actual budget total is unavailable."); }
        var total = vm.TotalLines[0]; var envelopeLines = vm.EnvelopeLines.ToArray();
        var envelopeState = (vm.IsEnvelopes, vm.UnassignedText);
        var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var negative = Core.Money.MoneyText.Format(long.MinValue, "EUR", localization.CurrentCulture);
        var positive = Core.Money.MoneyText.Format(long.MaxValue, "EUR", localization.CurrentCulture);
        try
        {
            vm.TotalLines[0] = total with { Name = string.Join(" / ", Enumerable.Repeat(total.Name, 6)),
                SpentText = negative, LimitText = translator.Format("Budget_Of", positive) };
            // Only presentation collections/flags change; no budget method, ledger value or preference is persisted.
            vm.IsEnvelopes = true; vm.UnassignedText = positive;
            vm.EnvelopeLines.Clear();
            vm.EnvelopeLines.Add(new(translator["Envelope_Available"], positive, false));
            vm.EnvelopeLines.Add(new(translator["Envelope_ForGoals"], negative, false));
            vm.EnvelopeLines.Add(new(translator["Envelope_InEnvelopes"], negative, false));
            await Task.Delay(600);
            if (FindScrollView(page) is { } outer) { await outer.ScrollToAsync(0, 0, animated: false); }
            await CaptureAsync(app, folder, language + "-budget-readouts-long");
            await ReviewAmountReadoutsAsync(app, page, folder, language + "-budget-readouts-long");
            var readouts = VisualDescendants(page).OfType<Presentation.AmountReadout>().Where(value => value.IsVisible).ToArray();
            if (!readouts.Any(value => value.CaptionStyle is null && value.AmountText == positive)
                || readouts.Count(value => value.AmountText == negative) < 3)
            { throw new InvalidOperationException("Budget review lacks actual default/compact complete signed packets."); }
        }
        finally
        {
            vm.TotalLines[0] = total; (vm.IsEnvelopes, vm.UnassignedText) = envelopeState;
            vm.EnvelopeLines.Clear(); foreach (var line in envelopeLines) { vm.EnvelopeLines.Add(line); }
            // A retired native page cannot complete ScrollToAsync; keep route interruptions visible instead of hanging cleanup.
            if (ReferenceEquals(Shell.Current.CurrentPage, page) && FindScrollView(page) is { } outer)
            { await outer.ScrollToAsync(0, 0, animated: false); }
        }
        await Task.Delay(400);
        if (!ReferenceEquals(Shell.Current.CurrentPage, page))
        { throw new InvalidOperationException("The reviewed budget page was retired before its fixture-restoration capture."); }
        await CaptureAsync(app, folder, language + "-budget-readouts-restored");
        if (before != await StoredAsync()) { throw new InvalidOperationException("Budget readout review changed stored financial/settings data."); }
        File.WriteAllText(Path.Combine(folder, language + "-budget-readouts-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        { CompleteOriginalFigures = true, ExactLongSignedPackets = true, DefaultAndCompactTypography = true,
            NativeScrollEnds = true, CompleteAccountsEntriesSettingsBudgetsPlansUnchanged = true,
            PresentationFixtureRestored = vm.TotalLines[0] == total }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    // AT-92: invoke each actual native Insights destination, retaining the body on repeated attachment and resize.
    private static async Task ReviewInsightsTabsAsync(App app, IServiceProvider services, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await StoredAsync();
        var window = app.Windows[0]; var originalWidth = window.Width;
        var invoked = new List<string>();
        try
        {
            foreach (var route in new[] { AppShell.ReportsRoute, AppShell.ForecastRoute, AppShell.GoalsRoute, AppShell.BudgetRoute })
            {
                var page = Shell.Current.CurrentPage as ContentPage ?? throw new InvalidOperationException("Insights page missing.");
                var tabs = VisualDescendants(page).OfType<Presentation.InsightsTabs>().Single();
                var button = VisualDescendants(tabs).OfType<Button>().Single(item => (string?)item.CommandParameter == route);
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                    || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                        is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
                { throw new InvalidOperationException("Insights destination has no native Invoke pattern."); }
                invoke.Invoke();
                for (var attempt = 0; attempt < 100 && Presentation.InsightsTabs.RouteOf(Shell.Current.CurrentPage) != route; attempt++)
                { await Task.Delay(50); }
                await Task.Delay(1500);
                if (Shell.Current.CurrentPage is not ContentPage current || Presentation.InsightsTabs.RouteOf(current) != route)
                { throw new InvalidOperationException("The real Insights action opened a different destination."); }
                var host = current.Content as Grid ?? throw new InvalidOperationException("Insights body host missing.");
                var body = host.Children.OfType<View>().Single(view => Grid.GetRow((BindableObject)view) == 1);
                var context = body.BindingContext;
                Presentation.InsightsTabs.Attach(current, route);
                if (!ReferenceEquals(host, current.Content) || !ReferenceEquals(body.BindingContext, context)
                    || VisualDescendants(current).OfType<Presentation.InsightsTabs>().Count() != 1)
                { throw new InvalidOperationException("Repeated Insights attachment replaced the body or bindings."); }
                invoked.Add(route);
                await CaptureAsync(app, folder, language + "-insights-tabs-native-" + invoked.Count);
            }
            foreach (var width in new[] { 1280d, 360d, 412d, originalWidth })
            {
                window.Width = width; await Task.Delay(600);
                await CaptureAsync(app, folder, language + "-insights-tabs-resize-" + width);
            }
        }
        finally { window.Width = originalWidth; }
        if (before != await StoredAsync()) { throw new InvalidOperationException("Insights navigation wrote stored settings/accounts/entries."); }
        File.WriteAllText(Path.Combine(folder, language + "-insights-tabs-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        { NativeDestinations = invoked, OneHeaderAndRetainedBody = true, CompleteStoredDataUnchanged = true,
            ActualWindowWidths = new[] { 1280d, 360d, 412d, originalWidth } }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    // AT-90: retain the actual Settings body/draft across nested navigation, resizing and idempotent attachment.
    private static async Task ReviewHeadersAsync(App app, IServiceProvider services,
        Features.Settings.SettingsPage page, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var before = System.Text.Json.JsonSerializer.Serialize(new
        {
            Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
        });
        var editor = (Features.Settings.SettingsViewModel)page.BindingContext;
        var original = (editor.EssentialText, editor.EssentialPeriodIndex);
        editor.EssentialText = "17.25";
        editor.EssentialPeriodIndex = 2;
        var host = page.Content as Grid ?? throw new InvalidOperationException("The real growing header host is missing.");
        var header = host.Children.OfType<Presentation.PageHeader>().Single();
        var body = host.Children.OfType<VisualElement>().Single(child => Grid.GetRow(child) == 1);
        var window = app.Windows[0];
        var originalWidth = window.Width;
        var widths = new List<object>();
        try
        {
            foreach (var width in new[] { 360d, 412d, 1280d })
            {
                window.Width = width;
                await Task.Delay(600);
                Presentation.PageHeader.Attach(page, page.FlowDirection == FlowDirection.RightToLeft);
                AssertDraft();
                var wide = page.Width > Presentation.ReadableWidth.Max + 32;
                if (wide && Math.Abs(host.Width - Presentation.ReadableWidth.Max) > 1
                    || !wide && Math.Abs(host.Width - page.Width) > 1
                    || Math.Abs(body.Width - host.Width) > 1)
                { throw new InvalidOperationException("The current header/body root did not retain its readable column after resizing."); }
                await CaptureAsync(app, folder, $"{language}-headers-resize-{width}");
                widths.Add(new { windowWidth = width, page.Width, hostWidth = host.Width, bodyWidth = body.Width });
            }
            window.Width = originalWidth;
            await Task.Delay(600);
            await Shell.Current.GoToAsync(AppShell.CategoriesRoute, animate: false);
            await Task.Delay(500);
            await Shell.Current.GoToAsync("..", animate: false);
            await Task.Delay(600);
            AssertDraft();
            await CaptureAsync(app, folder, language + "-headers-nested-return");
        }
        finally
        {
            window.Width = originalWidth;
            (editor.EssentialText, editor.EssentialPeriodIndex) = original;
        }
        InvokeSnapshotBack(page);
        for (var attempt = 0; attempt < 100 && ReferenceEquals(Shell.Current.CurrentPage, page); attempt++)
        { await Task.Delay(50); }
        if (ReferenceEquals(Shell.Current.CurrentPage, page))
        { throw new InvalidOperationException("The real native header Back did not return to the previous page."); }
        var after = System.Text.Json.JsonSerializer.Serialize(new
        {
            Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
        });
        if (before != after) { throw new InvalidOperationException("A header, resize or Back operation wrote stored financial/settings data."); }
        File.WriteAllText(Path.Combine(folder, language + "-headers-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            RetainedDraftDuringResize = true, RetainedDraftAfterNestedReturn = true, SingleHeaderAndSameBodyAfterNestedReturn = true, NativeBack = true,
            CompleteStoredDataUnchanged = true, ResizedColumns = widths,
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        void AssertBody()
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page) || !ReferenceEquals(page.Content, host)
                || !ReferenceEquals(host.Children.OfType<Presentation.PageHeader>().Single(), header)
                || !ReferenceEquals(host.Children.OfType<VisualElement>().Single(child => Grid.GetRow(child) == 1), body)
                || !ReferenceEquals(body.BindingContext, editor))
            { throw new InvalidOperationException("Growing headers reset the actual Settings page/body/bindings."); }
        }

        void AssertDraft()
        {
            AssertBody();
            if (editor.EssentialText != "17.25" || editor.EssentialPeriodIndex != 2)
            { throw new InvalidOperationException("Growing headers reset the unsaved Settings input during resizing."); }
        }
    }

    // Invoke the real header back action; it participates in the normal deferred shell replacement.
    private static void InvokeSnapshotBack(Page page)
    {
        if (VisualDescendants(page).OfType<Presentation.PageHeader>().SingleOrDefault() is not { } header
            || header.Children.OfType<ImageButton>().Single().Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button back)
        { throw new InvalidOperationException("The page header back button is unavailable."); }
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(back);
        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
            is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        { throw new InvalidOperationException("The page header has no native back pattern."); }
        invoke.Invoke();
    }

    // The real retry button must invoke the page handler; direct view-model loading alone cannot prove its wiring.
    private static async Task InvokeSnapshotRetryAsync(Features.Transactions.TransactionsViewModel transactions)
    {
        if (!transactions.Loading.HasFailed || Shell.Current.CurrentPage is not Features.Transactions.TransactionsPage page
            || !ReferenceEquals(page.BindingContext, transactions)
            || page.FindByName<Button>("RetryLoadButton")?.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button button)
        {
            throw new InvalidOperationException("The failed snapshot's native retry button is unavailable.");
        }
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(button);
        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        {
            throw new InvalidOperationException("The snapshot retry button has no Invoke pattern.");
        }
        invoke.Invoke();
        for (var attempt = 0; attempt < 100 && !transactions.Loading.IsReady; attempt++) { await Task.Delay(50); }
        if (!transactions.Loading.IsReady) { throw new InvalidOperationException("The native retry did not publish a complete snapshot."); }
    }

    private static Microsoft.UI.Xaml.Controls.Button? FindNativeButton(Microsoft.UI.Xaml.DependencyObject root, string text)
    {
        if (root is Microsoft.UI.Xaml.Controls.Button button && button.Content is string content && content == text) { return button; }
        for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindNativeButton(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i), text) is { } found) { return found; }
        }
        return null;
    }

    // D-63: exclusively fictitious credentials; refuse to touch a PIN already present on the development device.
    private static async Task CaptureSecurityAsync(App app, IServiceProvider services, Features.Settings.SettingsPage settingsPage, string folder, string language)
    {
        var appLock = services.GetRequiredService<Security.AppLockService>();
        if (appLock.PinEnabled || appLock.PinUnavailable) { throw new InvalidOperationException("Security snapshots require an unconfigured development PIN."); }
        if (FindScrollView(settingsPage) is { } scroll && settingsPage.FindByName<Border>("SecurityCard") is { } card)
        {
            await scroll.ScrollToAsync(card, ScrollToPosition.Start, animated: false);
            await Task.Delay(400);
            await CaptureAsync(app, folder, $"{language}-settings-security");
        }

        var translator = services.GetRequiredService<Translator>();
        var pinPage = new Security.PinSettingsPage(appLock, translator);
        await settingsPage.Navigation.PushModalAsync(pinPage, animated: false);
        await Task.Delay(400);
        await CaptureAsync(app, folder, $"{language}-pin-setup");
        var pinVm = (Security.PinSettingsViewModel)pinPage.BindingContext;
        await pinVm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(300);
        await CaptureAsync(app, folder, $"{language}-pin-validation");
        var configuredHere = false;
        try
        {
            pinVm.NewPin = pinVm.ConfirmPin = "0123";
            await pinVm.SaveCommand.ExecuteAsync(null);
            configuredHere = appLock.PinEnabled;
            if (!configuredHere) { throw new InvalidOperationException("The sample PIN could not be saved."); }
            await Task.Delay(300);
            pinPage = new Security.PinSettingsPage(appLock, translator);
            await settingsPage.Navigation.PushModalAsync(pinPage, animated: false);
            await Task.Delay(400);
            await CaptureAsync(app, folder, $"{language}-pin-change");
            await ((Security.PinSettingsViewModel)pinPage.BindingContext).BackCommand.ExecuteAsync(null);
            var lockPage = new Security.LockPage(appLock, translator, promptOnAppearing: false);
            await settingsPage.Navigation.PushModalAsync(lockPage, animated: false);
            await Task.Delay(400);
            await CaptureAsync(app, folder, $"{language}-pin-unlock");
            await settingsPage.Navigation.PopModalAsync(animated: false);
            await CaptureHelpAsync(app, folder, language, ["AppPin", "Screenshots"]);
        }
        finally
        {
            if (configuredHere)
            {
                var result = await appLock.Pin.RemoveAsync("0123");
                if (result.Outcome != Core.Security.PinOutcome.Success) { throw new InvalidOperationException("The sample PIN could not be removed."); }
            }
        }
        await ((Features.Settings.SettingsViewModel)settingsPage.BindingContext).LoadAsync();
    }

    private static async Task RenderAsync(Microsoft.UI.Xaml.UIElement root, string path)
    {
        var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
        await bitmap.RenderAsync(root);
        var pixels = await bitmap.GetPixelsAsync();
        using var stream = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
        var encoder = await global::Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(global::Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
        var dpi = 96 * (root.XamlRoot?.RasterizationScale ?? 1);
        encoder.SetPixelData(
            global::Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            global::Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth,
            (uint)bitmap.PixelHeight,
            dpi,
            dpi,
            System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(pixels));
        await encoder.FlushAsync();
        stream.Seek(0);
        await using var target = File.Create(path);
        await System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead(stream).CopyToAsync(target);
    }
#endif
}
#endif
