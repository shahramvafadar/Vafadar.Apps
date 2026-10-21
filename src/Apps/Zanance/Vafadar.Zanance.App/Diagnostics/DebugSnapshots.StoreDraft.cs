#if DEBUG && WINDOWS
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Rejects a nonisolated store capture before any shared service or financial startup is composed.</summary>
    internal static void ValidateStoreDraftBeforeStartup()
    {
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") != "store-draft") return;
        var run = Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_RUN");
        if (run is null || !Guid.TryParseExact(run, "N", out _))
            throw new IOException("Fresh independent store draft identity required before startup.");
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storepreview." + run[..12])
            throw StoreDraftRefusal(run, "identity");
        var expectedOutput = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "store-drafts", run));
        var output = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOTS");
        if (output is null || !string.Equals(Path.GetFullPath(output), expectedOutput, StringComparison.OrdinalIgnoreCase))
            throw StoreDraftRefusal(run, "output");
        var desktop = new StringBuilder(256);
        if (!GetUserObjectInformationW(GetThreadDesktop(GetCurrentThreadId()), 2, desktop, 512, out _)
            || !string.Equals(desktop.ToString(), "ZananceStore_" + run, StringComparison.OrdinalIgnoreCase))
            throw StoreDraftRefusal(run, "desktop");
        var repair = Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REPAIR") == "en-budget";
        if (repair)
        {
            using var previous = JsonDocument.Parse(File.ReadAllText(Path.Combine(expectedOutput, "isolation.json")));
            using var completed = JsonDocument.Parse(File.ReadAllText(Path.Combine(expectedOutput, "complete.json")));
            if (previous.RootElement.GetProperty("Package").GetString() != AppInfo.Current.PackageName
                || !previous.RootElement.GetProperty("EmptyBeforeStartup").GetBoolean()
                || completed.RootElement.GetProperty("Package").GetString() != AppInfo.Current.PackageName
                || !completed.RootElement.GetProperty("FinancialRowsUnchangedAfterSeeding").GetBoolean()
                || Directory.Exists(Path.Combine(expectedOutput, "repair-en-budget")))
                throw StoreDraftRefusal(run, "repair");
        }
        var data = Path.GetFullPath(FileSystem.AppDataDirectory);
        var installation = Path.GetDirectoryName(data)!;
        if (!string.Equals(Path.GetFileName(installation), AppInfo.Current.PackageName, StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(data), "Data", StringComparison.Ordinal)
            || !Path.GetFullPath(FileSystem.CacheDirectory).StartsWith(installation + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || (!repair && Directory.EnumerateFileSystemEntries(data).Any()))
            throw StoreDraftRefusal(run, "storage");
        if (repair) return; // Retain the original empty-installation receipt; repairs never reseed financial data.
        File.WriteAllText(Path.Combine(expectedOutput, "isolation.json"), JsonSerializer.Serialize(new
        {
            Package = AppInfo.Current.PackageName, DataDirectory = data, CacheDirectory = FileSystem.CacheDirectory,
            EmptyBeforeStartup = true, ProcessId = Environment.ProcessId, Desktop = desktop.ToString(),
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Retains a secret-free refusal only in the original owned run folder, without overwriting observations.</summary>
    private static IOException StoreDraftRefusal(string run, string phase)
    {
        var folder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "store-drafts", run));
        if (Directory.Exists(folder))
        {
            using var file = new FileStream(Path.Combine(folder, "refusal-" + Environment.ProcessId + ".json"),
                FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(file, new { Phase = phase, ProcessId = Environment.ProcessId, BeforeMauiBuilder = true });
        }
        return new IOException("Store draft startup refused: " + phase + ".");
    }

    /// <summary>Renders the current application in every existing language using one isolated fictitious database.</summary>
    private static async Task CaptureStoreDraftAsync(App app, IServiceProvider services, string folder)
    {
        var isolation = Path.Combine(folder, "isolation.json");
        if (!File.Exists(isolation)) throw new IOException("Verified store draft startup required.");
        var local = services.GetRequiredService<ILocalizationService>();
        var theme = services.GetRequiredService<ThemeService>();
        var store = services.GetRequiredService<ZananceStore>();
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REPAIR") == "en-budget")
        {
            var before = JsonSerializer.Serialize(await store.GetEntriesAsync());
            var repairFolder = Path.Combine(folder, "repair-en-budget");
            Directory.CreateDirectory(repairFolder);
            await SetStoreThemeAsync(app, theme, ThemeChoice.Light);
            await SetLanguageAsync(app, local, "en");
            await Shell.Current.GoToAsync("//home");
            await Shell.Current.GoToAsync(AppShell.BudgetRoute);
            await WaitForStorePageAsync(app);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(repairFolder, "05-budget.png"));
            if (before != JsonSerializer.Serialize(await store.GetEntriesAsync()))
                throw new IOException("Repair navigation changed fictitious financial rows.");
            File.WriteAllText(Path.Combine(repairFolder, "complete.json"), JsonSerializer.Serialize(new
            {
                Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
                Language = "en", File = "05-budget.png", FinancialRowsUnchanged = true,
            }));
            return;
        }
        local.SetCalendar(CalendarSystem.Gregorian);
        local.SetDigits(DigitStyle.LanguageDefault);
        await SetLanguageAsync(app, local, "en");
        if (app.Windows[0].Page is not OnboardingPage || (await store.GetAccountsAsync()).Count != 0)
            throw new IOException("Fresh fictitious onboarding required.");
        // Seed behind the original onboarding cover. Publishing Home before these writes races its real snapshot guard.
        var account = new Core.Accounts.Account
        {
            Name = "Daily account", Type = Core.Accounts.AccountType.Checking, CurrencyCode = "EUR",
            OpeningDate = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime),
            OpeningBalance = 200_000,
        };
        await store.SaveAccountAsync(account);
        await store.UpdateSettingsAsync(settings => { settings.ReportCurrencyCode = "EUR"; settings.DefaultAccountId = account.Id; });
        var (_, food) = await SeedAsync(services);
        await SeedPlansAsync(services);
        await SeedBudgetAsync(services, food);
        await store.UpdateSettingsAsync(settings => settings.OnboardingCompleted = true);
        app.ShowMainShell();
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-fee-action")
        {
            await ReviewHoldingFeeActionWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-fee-input")
        {
            await ReviewHoldingRefundWindowsAsync(app, services, folder, feeInput: true); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-detail")
        {
            await ReviewHoldingDetailWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-overview")
        {
            await ReviewHoldingOverviewWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "plan-form")
        {
            if (Environment.GetEnvironmentVariable("VAFADAR_HOLDING_MONEY_REVIEW") == "1") await ReviewHoldingMoneyWindowsAsync(app, services, folder);
            else if (Environment.GetEnvironmentVariable("VAFADAR_ACCOUNT_MONEY_REVIEW") == "1") await ReviewAccountMoneyWindowsAsync(app, services, folder);
            else if (Environment.GetEnvironmentVariable("VAFADAR_ENTRY_MONEY_REVIEW") == "1") await ReviewEntryMoneyWindowsAsync(app, services, folder);
            else if (Environment.GetEnvironmentVariable("VAFADAR_GOAL_BUDGET_MONEY_REVIEW") == "1") await ReviewGoalBudgetMoneyWindowsAsync(app, services, folder);
            else await ReviewPlanEditorWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "entry-workflow")
        {
            await ReviewEntryWorkflowWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "entry-form")
        {
            await ReviewEntryFormWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "entry-detail")
        {
            await ReviewEntryDetailWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "entry-save")
        {
            await ReviewEntrySaveWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "entry-attachments")
        {
            await ReviewEntryAttachmentsWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-event-publication")
        {
            await ReviewHoldingEventPublicationWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-location")
        {
            await ReviewHoldingLocationWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-type")
        {
            await ReviewHoldingTypeWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-archive")
        {
            await ReviewHoldingArchiveWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-history")
        {
            await ReviewHoldingHistoryWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-price")
        {
            await ReviewHoldingPriceWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-quantity-input")
        {
            await ReviewHoldingRefundWindowsAsync(app, services, folder, quantityInput: true); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-refund-review")
        {
            await ReviewHoldingRefundWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "holding-financial-review")
        {
            await ReviewHoldingFinancialWindowsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "split-financial-review")
        {
            await ReviewSplitFinancialWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "import-financial-review")
        {
            await ReviewImportFinancialWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "occurrence-refresh")
        {
            await ReviewOccurrenceRefreshWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "occurrence-state")
        {
            await ReviewOccurrenceCommitAsync(app, services, folder, stateConflict: true); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "occurrence-commit")
        {
            await ReviewOccurrenceCommitAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") is "home-refresh" or "home-refresh-references")
        {
            await ReviewHomeRefreshAsync(app, services, folder, referencesOnly: Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "home-refresh-references"); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "settlement-commit")
        {
            await ReviewSettlementCommitAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "reconciliation")
        {
            await ReviewReconciliationAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "plans-add-header")
        {
            await ReviewPlansAddHeaderAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "account-tab-refresh")
        {
            await ReviewAccountTabRefreshWindowsAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "ecb-reference")
        {
            await ReviewEcbAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") is "csv-input" or "csv-input-empty" or "csv-quotation")
        {
            await ReviewCsvInputAsync(app, services, folder); return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") is "home-review-count" or "home-add-header" or "getting-started")
        {
            var countCaptures = new List<object>();
            var result = await HomeReviewCountReview.RunAsync(app, services, folder, [360, 412, 1280],
                language => SetLanguageAsync(app, local, language), async (page, context) =>
                {
                    if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "home-add-header")
                        await HomeAddHeaderReview.CheckAsync(page, button =>
                        {
                            InvokeChoiceReview(button); return Task.CompletedTask;
                        }, CancellationToken.None, exerciseNavigation: false);
                    if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "getting-started")
                    {
                        foreach (var key in new[] { "Home_StartEntry", "Home_StartPlan", "Home_StartBudget" })
                        {
                            var text = services.GetRequiredService<Translator>()[key];
                            var label = VisualDescendants(page).OfType<Label>().Single(value => value.Text == text);
                            await page.FindByName<ScrollView>("ContentViewport").ScrollToAsync(label, ScrollToPosition.Center, false);
                            await Task.Delay(100); CheckCaption(label);
                            var button = VisualDescendants(page).OfType<Button>().Single(value => SemanticProperties.GetDescription(value) == text);
                            if (!button.IsVisible || !button.IsEnabled || button.Command is null || label.TextDecorations != TextDecorations.None)
                                throw new IOException("Original unfinished guidance action is unavailable.");
                        }
                    }
                    var model = (Features.Home.HomeViewModel)page.BindingContext;
                    var viewport = page.FindByName<ScrollView>("ContentViewport");
                    foreach (var text in new[] { model.ConfirmedBalanceText, model.UnreviewedText })
                    {
                        var label = VisualDescendants(page).OfType<Label>().Single(value => value.Text == text);
                        await viewport.ScrollToAsync(label, ScrollToPosition.Center, false); await Task.Delay(150);
                        CheckCaption(label);
                    }
                    var destination = Path.Combine(folder, context); Directory.CreateDirectory(destination);
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, "home-review.png"));
                    countCaptures.Add(new { Language = context, File = "home-review.png" });
                }, CancellationToken.None, gettingStarted: Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "getting-started");
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, FinancialRowsUnchangedAfterSeeding = true,
                Captures = countCaptures, Qualification = result }));
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "editor-text")
        {
            await ReviewEditorTextWindowsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "home-templates")
        {
            await ReviewHomeTemplatesAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "reimbursements")
        {
            await ReviewReimbursementsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "ledger-rows")
        {
            await ReviewLedgerRowsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "plan-rows")
        {
            await ReviewPlanRowsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "home-legends")
        {
            await ReviewHomeLegendsAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "budget-titles")
        {
            await ReviewBudgetTitlesAsync(app, services, folder);
            return;
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "report-rows")
        {
            await ReviewReportRowsAsync(app, services, folder);
            return;
        }
        var original = JsonSerializer.Serialize(await store.GetEntriesAsync());
        var captured = new List<object>();
        var screens = new (string File, string Route)[]
        {
            ("01-home", "//home"), ("02-transactions", "//transactions"),
            ("03-new-entry", AppShell.EntryEditorRoute), ("04-plans", "//plans"),
            ("05-budget", AppShell.BudgetRoute), ("06-reports", AppShell.ReportsRoute),
            ("07-accounts", AppShell.AccountsRoute),
        };
        foreach (var language in new[] { "en", "fa", "de", "es", "fr", "it" })
        {
            var destination = Path.Combine(folder, language);
            Directory.CreateDirectory(destination);
            await SetStoreThemeAsync(app, theme, ThemeChoice.Light);
            await SetLanguageAsync(app, local, language);
            foreach (var (file, route) in screens)
            {
                await Shell.Current.GoToAsync("//home");
                if (route != "//home") await Shell.Current.GoToAsync(route);
                await WaitForStorePageAsync(app);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, file + ".png"));
                captured.Add(new { Language = language, File = file + ".png", Theme = "Light", Route = route });
            }
            await Shell.Current.GoToAsync("//home");
            await SetStoreThemeAsync(app, theme, ThemeChoice.Dark);
            await WaitForStorePageAsync(app);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, "08-home-dark.png"));
            captured.Add(new { Language = language, File = "08-home-dark.png", Theme = "Dark", Route = "//home" });
        }
        if (original != JsonSerializer.Serialize(await store.GetEntriesAsync()))
            throw new IOException("Store navigation changed fictitious financial rows.");
        File.WriteAllText(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        {
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            NativeWindowsDraftOnly = true, FinancialRowsUnchangedAfterSeeding = true, Captures = captured,
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Awaits the original tab-root replacement before inspecting its newly published Home.</summary>
    private static async Task SetStoreThemeAsync(App app, ThemeService theme, ThemeChoice choice)
    {
        if (theme.Choice == choice) return;
        var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnRebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
        app.ShellRebuilt += OnRebuilt;
        try
        {
            theme.Set(choice);
            await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally { app.ShellRebuilt -= OnRebuilt; }
    }

    /// <summary>Waits for original page publication and native layout without accepting a covered or failed frame.</summary>
    private static async Task WaitForStorePageAsync(App app)
    {
        var page = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() ?? Shell.Current.CurrentPage;
        if (page is NavigationPage navigation) page = navigation.CurrentPage;
        var loading = page.BindingContext?.GetType().GetProperty("Loading")?.GetValue(page.BindingContext) as SnapshotLoadState;
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (loading is { IsLoading: true } && elapsed.Elapsed < TimeSpan.FromSeconds(30)) await Task.Delay(50);
        if (loading is not null && !loading.IsReady) throw new IOException("Store page publication did not complete.");
        if (page.BindingContext is Features.Reports.ReportsViewModel report)
        {
            while (report.ReportLoading && elapsed.Elapsed < TimeSpan.FromSeconds(30)) await Task.Delay(50);
            if (!report.ReportReady) throw new IOException("Store report publication did not complete.");
        }
        if (page.BindingContext is Features.Budget.BudgetViewModel budget)
        {
            while (budget.Actions.IsLoading && elapsed.Elapsed < TimeSpan.FromSeconds(30)) await Task.Delay(50);
            if (!budget.Actions.CanInteract) throw new IOException("Store budget publication did not complete.");
        }
        await Task.Delay(600);
        if (app.Windows[0].Handler?.PlatformView is Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }
            && Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                .Any(popup => popup.IsOpen && popup.Child is Microsoft.UI.Xaml.FrameworkElement { ActualWidth: > 0, ActualHeight: > 0 }))
            throw new IOException("An open native popup must not enter a store draft.");
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern IntPtr GetThreadDesktop(uint thread);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetUserObjectInformationW(IntPtr handle, int index, StringBuilder value, uint bytes, out uint needed);
}
#endif
