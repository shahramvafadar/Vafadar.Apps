#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Only schedule destination capture is fictitious; native cash/consent dialogs and the original writer stay real.</summary>
    private sealed class LoanReviewInteraction(IAppInteraction native) : IAppInteraction
    {
        internal IDictionary<string, object>? Query;
        public Task AlertAsync(string title, string message, string cancel) => native.AlertAsync(title, message, cancel);
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => native.ConfirmAsync(title, message, accept, cancel);
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => native.ChooseAsync(title, cancel, actions);
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1)
            => native.PromptAsync(title, message, accept, cancel, initialValue, maxLength);
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) { Query = parameters; return Task.CompletedTask; }
        public Task ShowFailureAsync(Exception exception) => native.ShowFailureAsync(exception);
    }
    // D-171 / AT-172: actual account/schedule/confirmation controls and whole original payment; no price/provider activation.
    private static async Task ReviewLoanWorkAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "loan-work.db"); var access = new ForecastReviewAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        await store.EnsureDefaultCategoriesAsync(); await ApplyOwnedReviewModeAsync(store);
        var reader = fixture.GetRequiredService<LoanWorkStore>(); var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var loan = new Account { Name = "Fictitious original loan", CurrencyCode = "EUR", Type = AccountType.Loan, OpeningBalance = -100000,
            OpeningDate = day, InterestRate = 12, Installment = 10000 };
        var cash = new Account { Name = "Fictitious original cash", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 100000 };
        var warning = new Account { Name = "Fictitious insufficient installment", CurrencyCode = "EUR", Type = AccountType.Loan,
            OpeningDate = day, OpeningBalance = -100000, InterestRate = 12, Installment = 500 };
        await store.SaveAccountAsync(loan); await store.SaveAccountAsync(cash); await store.SaveAccountAsync(warning);
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            var names = new List<string>(); using (var rows = await command.ExecuteReaderAsync()) while (await rows.ReadAsync()) names.Add(rows.GetString(0));
            if (names.Count != 25) throw new InvalidOperationException("Incomplete original loan fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var name in names)
            {
                command.CommandText = "SELECT * FROM \"" + name + "\""; using var rows = await command.ExecuteReaderAsync(); var packets = new List<string>();
                while (await rows.ReadAsync()) { var values = new object?[rows.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = rows.IsDBNull(i) ? null : rows.GetValue(i); packets.Add(JsonSerializer.Serialize(values)); }
                packets.Sort(StringComparer.Ordinal); all[name] = packets;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var effects = new LoanReviewInteraction(services.GetRequiredService<IAppInteraction>());
        var scope = new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.Parse("14141414-2525-3636-4747-585858585858"));
        async Task<(AccountDetailViewModel Model, AccountDetailPage Page)> DetailAsync(Guid id)
        {
            var session = new LoanInstallmentSession(reader, store, translator, localization, effects);
            var vm = ActivatorUtilities.CreateInstance<AccountDetailViewModel>(services, store, fixture.GetRequiredService<GoalStore>(),
                fixture.GetRequiredService<HoldingStore>(), reader, session, effects);
            vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = id }); var page = new AccountDetailPage(vm);
            await Shell.Current.Navigation.PushModalAsync(page);
            for (var attempt = 0; attempt < 60 && !vm.Loading.IsReady && !vm.Loading.HasFailed; attempt++) await Task.Delay(50);
            await Task.Delay(150);
            if (!vm.Loading.IsReady) throw new InvalidOperationException("The original account did not publish behind its cover.");
            ProfileNativeReads.Add(page); return (vm, page);
        }
        async Task InvokeAsync(ContentPage page, System.Windows.Input.ICommand command)
        {
            var action = WrappingActions(page).Single(item => Visible(item) && item.Command == command);
            if (FindScrollView(page) is { } scroll) await ScrollToViewIfNeededAsync(scroll, action);
            await Task.Delay(100); CheckCaption(VisualDescendants(action).OfType<Label>().Single(), ProfileNativeReads);
            InvokeChoiceReview(VisualDescendants(action).OfType<Button>().Single(), ProfileNativeReads);
        }
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language); access.Current = new(path, new(ProductPlan.Free, scope));
            var (denied, deniedPage) = await DetailAsync(warning.Id);
            denied.ObservedText = "123,45"; denied.Reason = "Fictitious retained reconciliation"; var originalDate = denied.ReconcileDate;
            if (denied.ShowLoanEstimate || !denied.HasLoanTerms || denied.LoanWorkFeedback is null || denied.LoanEssentialWarning != translator["Loan_NeverPaidOff"]
                || !WrappingActions(deniedPage).Any(item => Visible(item) && item.Command == denied.DebtActionCommand))
                throw new InvalidOperationException("Free lost original debt/manual action/warning or computed a paid schedule.");
            await CaptureAsync(app, folder, language + "-loan-work-retained-free"); await InvokeAsync(deniedPage, denied.RefreshLoanCommand);
            if (denied.RefreshLoanCommand.ExecutionTask is { } refresh) await refresh;
            if (denied.ObservedText != "123,45" || denied.Reason != "Fictitious retained reconciliation" || denied.ReconcileDate != originalDate)
                throw new InvalidOperationException("Explicit loan read discarded original reconciliation input.");
            await Shell.Current.Navigation.PopModalAsync();
            access.Current = new(path, new(ProductPlan.Plus, scope)); var (vm, page) = await DetailAsync(loan.Id);
            if (!vm.ShowLoanEstimate || vm.LoanNextText is null || vm.LoanPayoffText is null) throw new InvalidOperationException("Original Plus loan analysis was lost.");
            await InvokeAsync(page, vm.ShowScheduleCommand); if (vm.ShowScheduleCommand.ExecutionTask is { } opening) await opening;
            var query = effects.Query ?? throw new InvalidOperationException("Missing original typed loan destination.");
            var schedule = new LoanScheduleViewModel(reader, translator, localization, services.GetRequiredService<Vafadar.Localization.Formatting.IDateFormatter>());
            schedule.ApplyQueryAttributes(query); var schedulePage = new LoanSchedulePage(schedule); await Shell.Current.Navigation.PushModalAsync(schedulePage);
            for (var attempt = 0; attempt < 60 && !schedule.Loading.IsReady && !schedule.Loading.HasFailed; attempt++) await Task.Delay(50);
            await Task.Delay(150);
            if (!schedule.Loading.IsReady || schedule.Rows.Count == 0) throw new InvalidOperationException("The actual loan schedule was not published.");
            await CaptureAsync(app, folder, language + "-loan-work-original-schedule"); await Shell.Current.Navigation.PopModalAsync();
            await InvokeAsync(page, vm.RecordInstallmentCommand);
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) throw new InvalidOperationException("Missing loan consent root.");
            Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
            for (var attempt = 0; attempt < 100 && dialog is null; attempt++)
            { dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(item => item.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault(); if (dialog is null) await Task.Delay(50); }
            if (dialog is null || dialog.Title as string != translator["Loan_RecordInstallment"]) throw new InvalidOperationException("Original loan consent did not open.");
            await Task.Delay(150); await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-loan-work-consent-window.png"));
            var button = FindNativeButton(dialog, translator["Common_Save"]) ?? throw new InvalidOperationException("Missing native loan Save answer.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonAutomationPeer;
            if (peer?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke) throw new InvalidOperationException("Missing loan Save Invoke pattern.");
            ProfileNativeReads.Add(dialog); ProfileNativeReads.Add(button); ProfileNativeReads.Add(peer); ProfileNativeReads.Add(invoke); invoke.Invoke();
            if (vm.RecordInstallmentCommand.ExecutionTask is { } saving) await saving;
            if (vm.Installment.Result != translator["Loan_Recorded"] || vm.Installment.Error is not null) throw new InvalidOperationException("Original loan primary result was not retained.");
            await Task.Delay(200); CheckCaption(VisualDescendants(page).OfType<Label>().Single(item => Visible(item) && item.Text == vm.Installment.Result), ProfileNativeReads);
            await CaptureAsync(app, folder, language + "-loan-work-recorded");
            var added = await store.GetEntriesAsync();
            if (added.Count != 2 || added.Single(item => item.Kind == EntryKind.Transfer).Amount != 9000 || added.Single(item => item.Kind == EntryKind.Expense).Amount != 1000)
                throw new InvalidOperationException("The original principal/interest ledger classification changed.");
            // Remove only this fixture's accepted payment before restoring its complete original state.
            access.Current = CommercialWriteAccess.Inactive; await store.DeleteEntriesAsync(added.Select(item => item.Id).ToArray());
            await Shell.Current.Navigation.PopModalAsync(); if (before != await StoredAsync()) throw new InvalidOperationException("Loan fixture restoration changed another original row.");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-loan-work-proof.json"), JsonSerializer.Serialize(new
            { ActualAccountScheduleAndConsentTargets = true, FreeBalanceManualActionAndEssentialWarning = true, RetainedReconciliationDraft = true,
                OriginalPaidScheduleAndAtomicPayment = true, PrincipalTransferInterestExpense = true, Complete25OriginalRowsRestored = true,
                FictitiousCachedRightsAndScheduleNavigationOnly = true }));
        }
    }
}
#endif
