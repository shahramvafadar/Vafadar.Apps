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

    private static async Task RunAsync(App app, IServiceProvider services, string folder)
    {
        var localization = services.GetRequiredService<ILocalizationService>();
        var languages = (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_LANGUAGES") ?? "en,fa").Split(',');

        // VAFADAR_SNAPSHOT_THEME=dark shoots the dark theme; the choice is saved, so light runs set it back.
        var dark = string.Equals(Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_THEME"), "dark", StringComparison.OrdinalIgnoreCase);
        services.GetRequiredService<Presentation.ThemeService>().Set(dark ? Presentation.ThemeChoice.Dark : Presentation.ThemeChoice.Light);

        // VAFADAR_SNAPSHOT_DIGITS=latin shows Persian with Latin digits; the choice is saved, so other runs set the default
        // (Persian digits) back.
        Presentation.DigitPreferences.Set(
            !string.Equals(Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_DIGITS"), "latin", StringComparison.OrdinalIgnoreCase), localization);

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
            foreach (var language in languages)
            {
                localization.SetLanguage(localization.SupportedLanguages.First(l => l.CultureName == language));
                await Task.Delay(500);
                await CaptureAsync(app, folder, $"{language}-onboarding-1");
            }

            vm.NextCommand.Execute(null);
            await Task.Delay(500);
            await CaptureAsync(app, folder, $"{languages[^1]}-onboarding-2");
            vm.NextCommand.Execute(null);
            await Task.Delay(500);
            await CaptureAsync(app, folder, $"{languages[^1]}-onboarding-3");
            vm.Account.OpeningText = "1250.50";
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
            ("entry-edit", AppShell.EntryEditorRoute, new() { ["id"] = expenseId }),
            ("entry-detail", AppShell.EntryDetailRoute, new() { ["id"] = expenseId }),
            ("split", AppShell.SplitRoute, new() { ["id"] = expenseId }),
            ("reimbursements", AppShell.ReimbursementsRoute, null),
            ("rules", AppShell.RulesRoute, null),
            ("plans", "//plans", null),
            ("plan-new", AppShell.PlanEditorRoute, null),
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
                await CaptureAsync(app, folder, $"{language}-{name}");
                if (FindScrollView(app.Windows[0].Page) is { } scroll && scroll.ContentSize.Height > scroll.Height + 40)
                {
                    await scroll.ScrollToAsync(0, scroll.ContentSize.Height, animated: false);
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-end");
                }

                // The PDF of the reports screen (REP-07), written next to the screenshots.
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
                    transactions.StartSelectingCommand.Execute(null);
                    transactions.SelectAllCommand.Execute(null);
                    await Task.Delay(500);
                    await CaptureAsync(app, folder, $"{language}-{name}-select");
                    transactions.StopSelectingCommand.Execute(null);

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
        var page = app.Windows[0].Page;
        var visible = page is Shell shell ? (shell.CurrentPage as VisualElement) ?? shell : page as VisualElement;
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
    private static async Task CaptureHelpAsync(App app, string folder, string language)
    {
        var topics = Resources.Strings.AppStrings.ResourceManager
            .GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!
            .Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(k => k.StartsWith("Help_", StringComparison.Ordinal) && k.EndsWith("_Title", StringComparison.Ordinal))
            .Select(k => k["Help_".Length..^"_Title".Length])
            .Order(StringComparer.Ordinal);
        foreach (var topic in topics)
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
