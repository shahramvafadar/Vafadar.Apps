#if DEBUG && WINDOWS
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Exercises original reconciliation controls, native consent and atomic writes only in a separate fictitious file.</summary>
    private static async Task ReviewReconciliationAsync(App app, IServiceProvider services, string folder)
    {
        if (!AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal))
            throw new IOException("Independent reconciliation identity required.");
        var continuation = Environment.GetEnvironmentVariable("VAFADAR_RECONCILIATION_CONTINUATION");
        var visualOnly = Environment.GetEnvironmentVariable("VAFADAR_RECONCILIATION_VISUAL_ONLY") == "true";
        var source = string.IsNullOrEmpty(continuation) ? null : Path.Combine(Path.GetDirectoryName(folder)!, continuation);
        if (visualOnly && source is null) throw new IOException("Visual continuation requires the original financial fixture.");
        if (source is not null && (continuation!.Length != 32 || continuation.Any(c => !Uri.IsHexDigit(c))
            || File.Exists(Path.Combine(source, "complete.json"))
            || !File.ReadAllText(Path.Combine(source, "error.txt")).Contains("Original observed value pattern required.", StringComparison.Ordinal)))
            throw new IOException("Continue only the original terminal input-pattern observation, before any financial command.");
        var path = Path.Combine(source ?? folder, "original-reconciliation.db");
        if (source is null && File.Exists(path) || source is not null && !File.Exists(path)) throw new IOException("Never reseed an observed financial review.");
        await using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>());
        var store = fixture.GetRequiredService<ZananceStore>();
        async Task<string> ReviewedInputs() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() });
        var reviewedBefore = await ReviewedInputs();
        if (visualOnly && ((await store.GetAccountsAsync()).Count != 6 || (await store.GetEntriesAsync()).Count != 6))
            throw new IOException("Original six accepted contexts required; no financial replay.");
        var originalStore = services.GetRequiredService<ZananceStore>();
        async Task<string> Original() => JsonSerializer.Serialize(new { Accounts = await originalStore.GetAccountsAsync(),
            Entries = await originalStore.GetEntriesAsync(), Settings = await originalStore.GetSettingsAsync() });
        var before = await Original();
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var captures = new List<object>(); var observations = new List<object>();
        async Task Ready(Func<bool> condition)
        {
            for (var i = 0; i < 200; i++) { if (condition()) return; await Task.Delay(50); }
            throw new IOException("Original reconciliation native publication did not finish.");
        }
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            await Shell.Current.GoToAsync("//home");
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
            var account = new Account { Name = "D396 original " + language + theme, CurrencyCode = "EUR", OpeningDate = day,
                OpeningBalance = 10000, OpeningBalanceKnown = false };
            if (visualOnly)
            {
                account = (await store.GetAccountsAsync()).Single(a => a.Name == "D396 original " + language + theme);
                if (!account.OpeningBalanceKnown || account.LastReconciledOn != day
                    || LedgerCalculator.Balance(account, await store.GetEntriesAsync(), day) != 21234)
                    throw new IOException("Accepted original context differs; no visual continuation.");
            }
            else if (source is not null && language == "en" && theme == ThemeChoice.Light)
            {
                account = (await store.GetAccountsAsync()).Single();
                if (account.Name != "D396 original enLight" || account.OpeningBalance != 10000 || account.OpeningBalanceKnown
                    || account.LastReconciledOn is not null || (await store.GetEntriesAsync()).Count != 0)
                    throw new IOException("Original untouched partial fixture differs; no replay or replacement.");
            }
            else await store.SaveAccountAsync(account);
            var reader = fixture.GetRequiredService<LoanWorkStore>();
            var installment = new LoanInstallmentSession(reader, store, text, local, services.GetRequiredService<IAppInteraction>());
            var vm = ActivatorUtilities.CreateInstance<AccountDetailViewModel>(services, store, fixture.GetRequiredService<GoalStore>(),
                fixture.GetRequiredService<HoldingStore>(), reader, installment);
            vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = account.Id });
            var page = new AccountDetailPage(vm).WithFlowDirection(local);
            await Shell.Current.Navigation.PushAsync(page, false); await Ready(() => vm.Loading.IsReady);
            async Task Input(long observed)
            {
                var entry = VisualDescendants(page).OfType<Entry>().Single(e => SemanticProperties.GetDescription(e) == text["Reconcile_Observed"]);
                if (FindScrollView(page) is { } scroll) await ScrollToViewIfNeededAsync(scroll, entry);
                await Ready(() => entry.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox);
                // Only the fictitious draft is prepared here. Commands and consent use original native Invoke;
                // an unavailable local TextBox Value pattern is retained, never called successful native typing.
                var input = MoneyText.ForInput(observed, "EUR", local.CurrentCulture); vm.ObservedText = input;
                var native = (Microsoft.UI.Xaml.Controls.TextBox)entry.Handler!.PlatformView!;
                await Ready(() => native.Text == input);
            }
            async Task Invoke(IAsyncRelayCommand command, bool? consent = null)
            {
                var button = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command) && Visible(b));
                if (FindScrollView(page) is { } scroll) await ScrollToViewIfNeededAsync(scroll, button);
                InvokeChoiceReview(button);
                if (consent is { } accepted)
                {
                    if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                        throw new IOException("Original native dialog root required.");
                    Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
                    await Ready(() => { dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                        .Select(p => p.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault(); return dialog is not null; });
                    var answer = FindNativeButton(dialog!, text[accepted ? "Reconcile_Adjust" : "Common_Cancel"])
                        ?? throw new IOException("Original reconciliation answer required.");
                    if (FrameworkElementAutomationPeer.CreatePeerForElement(answer)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                        throw new IOException("Original confirmation Invoke pattern required.");
                    invoke.Invoke();
                }
                await Ready(() => command.ExecutionTask is not null); await command.ExecutionTask!;
            }
            if (!visualOnly)
            {
                await Input(11234); await Invoke(vm.CompareCommand);
                if (!vm.CanAdjust) throw new IOException("Original comparison did not expose the reasoned adjustment.");
                await Input(21234);
                if (vm.CanAdjust || vm.ResultText is not null || vm.HintText is not null) throw new IOException("Edited amount retained stale financial authority.");
                await Invoke(vm.CompareCommand); vm.ReconcileDate = day.AddDays(-1);
                if (vm.CanAdjust || vm.ResultText is not null) throw new IOException("Edited date retained stale financial authority.");
                vm.ReconcileDate = day;
            }
            else await Input(31234);
            await Invoke(vm.CompareCommand); await Invoke(vm.AdjustCommand);
            if (vm.Error != text["Reconcile_ReasonRequired"]) throw new IOException("Required reason refusal missing.");
            foreach (var width in new[] { 360d, 412d, 1280d })
            {
                app.Windows[0].Width = width; await Task.Delay(150);
                var error = VisualDescendants(page).OfType<Label>().Single(label => label.Text == vm.Error && Visible(label));
                if (FindScrollView(page) is { } scroll) await ScrollToViewIfNeededAsync(scroll, error);
                await Ready(() => error.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock { ActualHeight: > 0 });
                var context = language + "-" + theme + "-" + width;
                var output = Path.Combine(folder, context); Directory.CreateDirectory(output);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "reason-required.png"));
                captures.Add(new { Language = context, File = "reason-required.png" });
            }
            if (visualOnly) { observations.Add(new { Language = language, Theme = theme.ToString(), RequiredReason = true, NoFinancialWrite = true }); continue; }
            vm.Reason = "D396 original reason"; await Invoke(vm.AdjustCommand, false);
            if ((await store.GetEntriesAsync()).Any(e => e.AccountId == account.Id)) throw new IOException("Cancellation posted money.");
            await Invoke(vm.AdjustCommand, true);
            var saved = (await store.GetAccountsAsync()).Single(a => a.Id == account.Id);
            var adjustment = (await store.GetEntriesAsync()).Single(e => e.AccountId == account.Id);
            if (adjustment.Kind != EntryKind.Adjustment || adjustment.Amount != 11234 || adjustment.Direction != AdjustmentDirection.Increase
                || adjustment.Note != "D396 original reason" || !saved.OpeningBalanceKnown || saved.LastReconciledOn != day
                || LedgerCalculator.Balance(saved, [adjustment], day) != 21234 || vm.ResultText != text["Reconcile_Done"])
                throw new IOException("Accepted original adjustment or atomic account metadata differs.");
            observations.Add(new { Language = language, Theme = theme.ToString(), ObservedMinor = 21234, AdjustmentMinor = adjustment.Amount,
                OriginalDraftPropertyEdits = true, StaleAmountAndDateRetired = true, RequiredReason = true, CancelDidNotPost = true, OriginalConsentSavedOnce = true });
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-" + theme + "-accepted.json"), JsonSerializer.Serialize(observations[^1]));
        }
        if (before != await Original()) throw new IOException("Original base installation inputs changed.");
        if (visualOnly && reviewedBefore != await ReviewedInputs()) throw new IOException("Visual continuation changed accepted original financial inputs.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
            ProcessId = Environment.ProcessId, Captures = captures, OnlySeparateFictitiousLedgerChanged = true,
            OriginalBaseStoreUnchanged = true, VisualOnly = visualOnly, AcceptedFinancialInputsUnchanged = visualOnly, Observations = observations }));
    }
}
#endif
