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
                await vm.RestoreBackupCommand.ExecuteAsync(null);
                await Task.Delay(500);
                await CaptureAsync(app, folder, $"{language}-onboarding-restore");
                if (onboarding.Navigation.ModalStack.LastOrDefault()?.BindingContext is Features.Backup.BackupViewModel restore)
                {
                    await restore.BackToOnboardingCommand.ExecuteAsync(null);
                }

                for (var step = 2; step <= OnboardingViewModel.StepCount; step++)
                {
                    await vm.NextCommand.ExecuteAsync(null);
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-onboarding-{step}");
                    if (FindScrollView(onboarding) is { } scroll)
                    {
                        await scroll.ScrollToAsync(0, scroll.ContentSize.Height, animated: false);
                        await Task.Delay(300);
                        await CaptureAsync(app, folder, $"{language}-onboarding-{step}-end");
                        await scroll.ScrollToAsync(0, 0, animated: false);
                    }
                }
            }
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
            ("occurrence", AppShell.OccurrenceRoute, new() { ["plan"] = planId, ["date"] = planDate }),
            ("accounts", AppShell.AccountsRoute, null),
            ("account-detail", AppShell.AccountDetailRoute, new() { ["id"] = accountId }),
            ("loan-detail", AppShell.AccountDetailRoute, new() { ["id"] = loanId }),
            ("loan-schedule", AppShell.LoanScheduleRoute, new() { ["id"] = loanId }),
            ("loan-edit", AppShell.AccountEditorRoute, new() { ["id"] = loanId }),
            ("account", AppShell.AccountEditorRoute, null),
            ("categories", AppShell.CategoriesRoute, null),
            ("category", AppShell.CategoryEditorRoute, new() { ["id"] = foodId }),
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
                await (query is null ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, query));
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

                if (name == "settings" && Shell.Current.CurrentPage is Features.Settings.SettingsPage settingsPage)
                {
                    await CaptureSecurityAsync(app, services, settingsPage, folder, language);
                }
#endif
                if (FindScrollView(app.Windows[0].Page) is { } scroll && scroll.ContentSize.Height > scroll.Height + 40)
                {
                    await scroll.ScrollToAsync(0, scroll.ContentSize.Height, animated: false);
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-end");
                }

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
                    transactions.SelectAllCommand.Execute(null);
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-select");
                    transactions.StopSelectingCommand.Execute(null);

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
        if (visible is ContentPage content) { DebugLayoutChecks.Check(content, folder, name); }
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
            && checkedPage.FindByName<VisualElement>("ContentViewport") is not null)
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
