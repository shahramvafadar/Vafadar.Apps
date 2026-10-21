#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    private sealed class AccountMoneyEffects(IAppInteraction native) : IAppInteraction
    {
        /// <inheritdoc />
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => route == ".."
            ? Task.CompletedTask : throw new IOException("Unexpected account money navigation.");
        /// <inheritdoc />
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => native.ConfirmAsync(title, message, accept, cancel);
        /// <inheritdoc />
        public Task AlertAsync(string title, string message, string cancel) => native.AlertAsync(title, message, cancel);
        /// <inheritdoc />
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Original account money failed.", exception);
        /// <inheritdoc />
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected account money prompt.");
        /// <inheritdoc />
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected account money choice.");
    }
    /// <summary>Starts only an empty original account fixture; no fictitious financial account is seeded.</summary>
    internal async Task PrepareAccountMoneyAsync(CancellationToken ct)
    {
        if ((await Store.GetAccountsAsync(cancellationToken: ct)).Count != 0) throw new IOException("Fresh account monetary fixture required.");
        Store.Changed += AccountMoneyAccepted;
        await WriteAccountMoneyCheckpointAsync(ct);
    }
    /// <summary>Continues exact stored rows and original accepted receipts, never a new seed or accepted command.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueAccountMoneyAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var result = new PlanEditorNativeReview(host, folder);
        if (await File.ReadAllTextAsync(Path.Combine(folder, "account-money-latest.json"), ct) != await result.AllMoneyRowsAsync(ct))
            throw new IOException("Original account money checkpoint differs; do not repeat accepted commands.");
        result.Store.Changed += result.Accepted; result.Store.Changed += result.AccountMoneyAccepted;
        return result;
    }
    private void AccountMoneyAccepted(object? sender, EventArgs args) =>
        Task.Run(() => WriteAccountMoneyCheckpointAsync(CancellationToken.None)).GetAwaiter().GetResult();
    private Task WriteAccountMoneyCheckpointAsync(CancellationToken ct) => WriteAccountMoneyCheckpointCoreAsync(ct);
    private async Task WriteAccountMoneyCheckpointCoreAsync(CancellationToken ct) =>
        await File.WriteAllTextAsync(Path.Combine(_folder, "account-money-latest.json"), await AllMoneyRowsAsync(ct), ct);
    /// <summary>Gets the exact new amount captions, original-unit explanations and actionable refusals.</summary>
    internal static HashSet<string> AccountMoneyCaptions(Page page)
    {
        IEnumerable<string?> captions = page.BindingContext switch
        {
            AccountEditorViewModel vm => [vm.Form.OpeningUnitName, vm.Form.InstallmentUnitName, vm.Form.OpeningFormatNote,
                vm.Form.InstallmentFormatNote, vm.Form.UnitNote, vm.Form.InstallmentUnitNote, vm.Form.AmountError, vm.Form.TermsError, vm.SaveError],
            AccountDetailViewModel vm => [vm.ObservedUnitName, vm.ObservedFormatNote, vm.UnitNote, vm.Error, vm.ResultText],
            _ => [],
        };
        return captions.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    /// <summary>Gets only original amount fields for actual native text observation.</summary>
    internal static HashSet<string> AccountMoneyRaw(Page page)
    {
        IEnumerable<string?> inputs = page.BindingContext switch
        {
            AccountEditorViewModel vm => [vm.Form.OpeningText, vm.Form.InstallmentText],
            AccountDetailViewModel vm => [vm.ObservedText],
            _ => [],
        };
        return inputs.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    /// <summary>Checks only newly observed postcommit default ownership, in two fresh distinct fixtures without replaying the money matrix.</summary>
    internal async Task AccountDefaultOwnershipAsync(string scenario, Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct)
    {
        using var first = await CreateAsync(_host, Path.Combine(_folder, "default-" + scenario + "-original"), ct);
        using var other = await CreateAsync(_host, Path.Combine(_folder, "default-" + scenario + "-foreign"), ct);
        var otherBefore = await other.AllMoneyRowsAsync(ct);
        var units = DisplayUnits.All; DisplayUnits.Set([]); string? route = null;
        var model = new AccountEditorViewModel(first.Store, first.Plans, Translator, Localization, Time,
            new AccountMoneyEffects(_host.GetRequiredService<IAppInteraction>()));
        var page = new AccountEditorPage(model);
        try
        {
            route = await first.ShowAsync(page); await WaitAsync(() => model.CanInteract && page.Width > 0, ct);
            model.Form.Name = "Original default " + scenario; model.Form.OpeningText = "12.34"; model.IsDefault = true;
            first._phase = "original-default";
            void RetireDefault(object? sender, EventArgs args)
            {
                if (scenario == "file") first.Factory.Override = other.Factory;
                else model.SetPresentationActive(false);
            }
            first.Store.Changed += RetireDefault;
            await invoke(page, model.SaveCommand, null);
            await WaitAsync(() => !model.IsBusy && model.SaveError is not null, ct);
            first.Store.Changed -= RetireDefault; first.Factory.Override = null;
            var accounts = await first.Store.GetAccountsAsync(cancellationToken: ct);
            if (accounts.Count != 1 || accounts[0].OpeningBalance != 1234 || (await first.Store.GetEntriesAsync(cancellationToken: ct)).Count != 0
                || (await first.Store.GetSettingsAsync(ct)).DefaultAccountId is not null || otherBefore != await other.AllMoneyRowsAsync(ct)
                || model.SaveError != Translator["Account_SavedNeedsReview"] || model.CanSave)
                throw new IOException("Known account acceptance or original default ownership was lost.");
            await observe(page, "native-account-default-" + scenario);
            await File.WriteAllTextAsync(Path.Combine(_folder, "default-" + scenario + "-complete.json"), JsonSerializer.Serialize(new
            { scenario, NativeAccountSaves = 1, NoLedgerPost = true, ForeignRowsUnchanged = true, DefaultNotApplied = true,
                KnownAcceptanceRetained = true, ProcessId = Environment.ProcessId, OriginalRows = await first.AllMoneyRowsAsync(ct) }), ct);
        }
        finally
        {
            first.Factory.Override = null; await first.LeaveAsync(route); DisplayUnits.Set(units);
        }
    }
    /// <summary>Checks stale observed currency and explicit recovery without a comparison metadata or financial write.</summary>
    internal async Task AccountCurrencyOwnershipAsync(Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct)
    {
        using var fixture = await CreateAsync(_host, Path.Combine(_folder, "original-observed-currency"), ct);
        var day = DateOnly.FromDateTime(Time.GetLocalNow().DateTime);
        var account = new Account { Name = "Original observed currency", CurrencyCode = "EUR", OpeningDate = day };
        fixture._phase = "currency-seed"; await fixture.Store.SaveAccountAsync(account, ct);
        var native = _host.GetRequiredService<IAppInteraction>(); var reader = fixture.Services.GetRequiredService<LoanWorkStore>();
        var installment = new LoanInstallmentSession(reader, fixture.Store, Translator, Localization, native);
        var model = ActivatorUtilities.CreateInstance<AccountDetailViewModel>(_host, fixture.Store,
            fixture.Services.GetRequiredService<GoalStore>(), fixture.Services.GetRequiredService<HoldingStore>(), reader, installment, native);
        var page = new AccountDetailPage(model); var route = await fixture.ShowAsync(page, new() { ["id"] = account.Id });
        try
        {
            await WaitAsync(() => model.Loading.IsReady && page.Width > 0, ct);
            model.ObservedText = "0";
            fixture._phase = "currency-edit"; account.CurrencyCode = "USD"; await fixture.Store.SaveAccountAsync(account, ct);
            var before = await fixture.AllMoneyRowsAsync(ct);
            foreach (var raw in new[] { "0", "12.34" })
            {
                model.ObservedText = raw; await invoke(page, model.CompareCommand, null);
                await WaitAsync(() => !model.IsBusy && model.Error is not null, ct);
                if (model.CanAdjust || model.ObservedUnitName != "EUR" || before != await fixture.AllMoneyRowsAsync(ct))
                    throw new IOException("Original observed currency was reinterpreted or wrote comparison metadata.");
                await observe(page, raw == "0" ? "native-account-currency-match-refused" : "native-account-currency-difference-refused");
            }
            await invoke(page, model.RefreshLoanCommand, null); await WaitAsync(() => model.Loading.IsReady, ct);
            await invoke(page, model.CompareCommand, null); await WaitAsync(() => !model.IsBusy && model.Error is not null, ct);
            if (model.CanAdjust || model.ObservedText != "12.34" || model.ObservedUnitName != "EUR")
                throw new IOException("Explicit account refresh replaced the original raw monetary meaning.");
            await observe(page, "native-account-currency-refresh-retained");
            model.ObservedText = string.Empty; model.ObservedText = "12.34";
            await invoke(page, model.CompareCommand, null); await WaitAsync(() => !model.IsBusy && model.CanAdjust, ct);
            if (model.ObservedUnitName != "USD" || before != await fixture.AllMoneyRowsAsync(ct))
                throw new IOException("Explicit observed currency reentry failed or changed financial data.");
            await observe(page, "native-account-currency-reentered");
            await File.WriteAllTextAsync(Path.Combine(_folder, "observed-currency-complete.json"), JsonSerializer.Serialize(new
            { NativeAccountSaves = 0, NativeAdjustments = 0, DirectFixtureAccountSeed = 1, ConcurrentFixtureCurrencyEdit = 1,
                ComparisonRowsUnchanged = true, OriginalRawRetainedUntilReentry = true, ProcessId = Environment.ProcessId }), ct);
        }
        finally { await fixture.LeaveAsync(route); }
    }
    /// <summary>Checks original account/debt/observed amounts; only the first context may create one account and one adjustment.</summary>
    internal async Task AccountMoneyContextAsync(string context, Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool>? completed = null)
    {
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName;
        var before = await AllMoneyRowsAsync(ct); string? route = null; var pendingDisplay = false;
        var native = _host.GetRequiredService<IAppInteraction>(); var effects = new AccountMoneyEffects(native);
        async Task Leave()
        {
            await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false;
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); route = null; }
        }
        try
        {
            DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
            var vm = new AccountEditorViewModel(Store, Plans, Translator, Localization, Time, effects);
            var page = new AccountEditorPage(vm); route = await ShowAsync(page);
            await WaitAsync(() => vm.CanInteract && page.Width > 0, ct);
            vm.Form.Name = "Original account monetary balance"; vm.Form.OpeningText = "1,234";
            Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; vm.Form.RefreshTexts();
            if (vm.Form.OpeningFormatNote is null || vm.Form.OpeningUnitName != "EUR") throw new IOException("Original opening meaning was replaced.");
            if (completed?.Invoke(context + "-account-original") != true) await observe(page, context + "-account-original");
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); vm.Form.RefreshTexts(); vm.Form.OpeningText += "0";
            if (vm.CanSave || vm.Form.AmountError is null || vm.Form.OpeningUnitName != "EUR") throw new IOException("Partial account edit renewed stale money.");
            if (completed?.Invoke(context + "-account-review") != true) await observe(page, context + "-account-review");
            vm.Form.OpeningText = string.Empty; vm.Form.OpeningText = "12,34";
            if (!vm.CanSave || vm.Form.OpeningUnitName != "Hundred EUR") throw new IOException("Explicit opening reentry was not adopted.");
            if ((await Store.GetAccountsAsync(cancellationToken: ct)).Count == 0)
            {
                _phase = "account-money-create";
                await File.WriteAllTextAsync(Path.Combine(_folder, "account-money-create-intention.json"), JsonSerializer.Serialize(new { context, Amount = 123400 }), ct);
                await invoke(page, vm.SaveCommand, null); await WaitAsync(() => !vm.IsBusy && !vm.CanInteract, ct);
                var saved = (await Store.GetAccountsAsync(cancellationToken: ct)).Single();
                if (saved.CurrencyCode != "EUR" || saved.OpeningBalance != 123400 || (await Store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
                    throw new IOException("Original native account amount differs.");
                await WriteAccountMoneyCheckpointAsync(ct); before = await AllMoneyRowsAsync(ct);
                await observe(page, context + "-account-saved");
            }
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only account observations changed source rows.");
            await Leave();

            DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
            vm = new(Store, Plans, Translator, Localization, Time, effects); page = new(vm); route = await ShowAsync(page, new() { ["debt"] = true });
            await WaitAsync(() => vm.CanInteract && page.Width > 0, ct);
            vm.Form.Name = "Original debt monetary fields"; vm.Form.ShowDebtTerms = true;
            vm.Form.OpeningText = "1,234"; vm.Form.InstallmentText = "1,234";
            Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; vm.Form.RefreshTexts();
            if (vm.Form.OpeningFormatNote is null || vm.Form.InstallmentFormatNote is null) throw new IOException("Debt parsers were not independently retained.");
            if (completed?.Invoke(context + "-debt-original") != true) await observe(page, context + "-debt-original");
            vm.Form.CurrencyCode = "USD"; vm.Form.OpeningUnknown = true;
            if (vm.CanSave || vm.Form.AmountError is not null || vm.Form.TermsError is null) throw new IOException("Unknown principal hid stale repayment money.");
            vm.Form.InstallmentText += "0";
            if (vm.Form.InstallmentUnitName != "EUR") throw new IOException("Partial repayment edit relabelled its original currency.");
            if (completed?.Invoke(context + "-debt-review") != true) await observe(page, context + "-debt-review");
            vm.Form.InstallmentText = string.Empty; vm.Form.InstallmentText = "12,34";
            if (!vm.CanSave || vm.Form.InstallmentUnitName != "USD") throw new IOException("Explicit repayment reentry failed.");
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Unsaved debt posted financial data.");
            await Leave();

            DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
            var account = (await Store.GetAccountsAsync(cancellationToken: ct)).Single();
            var day = DateOnly.FromDateTime(Time.GetLocalNow().DateTime);
            var balance = LedgerCalculator.Balance(account, await Store.GetEntriesAsync(cancellationToken: ct), day);
            var observed = balance + 100;
            var reader = Services.GetRequiredService<LoanWorkStore>();
            var installment = new LoanInstallmentSession(reader, Store, Translator, Localization, native);
            var detail = ActivatorUtilities.CreateInstance<AccountDetailViewModel>(_host, Store, Services.GetRequiredService<GoalStore>(),
                Services.GetRequiredService<HoldingStore>(), reader, installment, native);
            var details = new AccountDetailPage(detail); route = await ShowAsync(details, new() { ["id"] = account.Id });
            await WaitAsync(() => detail.Loading.IsReady && details.Width > 0, ct);
            detail.ObservedText = (observed / 100).ToString("N0", CultureInfo.GetCultureInfo("en-US"));
            Localization.SetFormattingCulture("de-DE"); pendingDisplay = true;
            await invoke(details, detail.CompareCommand, null); await WaitAsync(() => !detail.IsBusy && detail.CanAdjust, ct);
            if (detail.ObservedFormatNote is null) throw new IOException("Original balance parser was not retained.");
            if (completed?.Invoke(context + "-observed-original") != true) await observe(details, context + "-observed-original");
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            await invoke(details, detail.CompareCommand, null);
            await WaitAsync(() => detail.Error is not null && !detail.CanAdjust, ct);
            if (detail.ObservedUnitName != "EUR") throw new IOException("Observed balance was silently relabelled.");
            if (completed?.Invoke(context + "-observed-review") != true) await observe(details, context + "-observed-review");
            detail.ObservedText = string.Empty; detail.ObservedText = MoneyText.ForInput(observed, "EUR", Localization.CurrentCulture);
            await invoke(details, detail.CompareCommand, null); await WaitAsync(() => !detail.IsBusy && detail.CanAdjust, ct);
            if (!File.Exists(Path.Combine(_folder, "account-money-adjust-accepted.json")))
            {
                _phase = "account-money-adjust"; detail.Reason = "Original account monetary comparison";
                await File.WriteAllTextAsync(Path.Combine(_folder, "account-money-adjust-intention.json"), JsonSerializer.Serialize(new { context, observed }), ct);
                await invoke(details, detail.AdjustCommand, true); await WaitAsync(() => !detail.IsBusy && detail.ObservedText.Length == 0, ct);
                account = (await Store.GetAccountsAsync(cancellationToken: ct)).Single();
                var entries = await Store.GetEntriesAsync(cancellationToken: ct);
                if (entries.Count != 1 || entries[0].Kind != EntryKind.Adjustment || entries[0].Amount != 100
                    || LedgerCalculator.Balance(account, entries, day) != observed) throw new IOException("Original monetary reconciliation differs.");
                await WriteAccountMoneyCheckpointAsync(ct); before = await AllMoneyRowsAsync(ct);
                await observe(details, context + "-observed-saved");
            }
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only comparison changed source rows.");
            await Leave();
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(_folder, "account-money-failed-" + Environment.ProcessId + ".json"),
                JsonSerializer.Serialize(new { context, Error = error.ToString(), ProcessId = Environment.ProcessId }), CancellationToken.None); throw;
        }
        finally
        {
            await Leave(); DisplayUnits.Set(units); await SetTabMoneyCultureAsync(culture, ct);
        }
    }
}
#endif
