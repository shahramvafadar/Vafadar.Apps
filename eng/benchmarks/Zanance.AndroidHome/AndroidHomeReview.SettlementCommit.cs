using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Returns through the real owned modal stack rather than registering a replacement production route.</summary>
    private sealed class SettlementReviewNavigation(IAppInteraction native, INavigation navigation) : IAppInteraction
    {
        public Task AlertAsync(string title, string message, string cancel) => native.AlertAsync(title, message, cancel);
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => native.ConfirmAsync(title, message, accept, cancel);
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => native.PromptAsync(title, message, accept, cancel, initialValue, maxLength);
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => native.ChooseAsync(title, cancel, actions);
        public async Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (route != ".." || parameters is not null) throw new ReviewCheckException("Only original owned modal return is permitted.");
            await navigation.PopModalAsync(false);
        }
        public Task ShowFailureAsync(Exception exception) => native.ShowFailureAsync(exception);
    }

    /// <summary>Checks original Release/AOT bill commands and retained post-commit outcomes, with no Main-package or base-fixture writes.</summary>
    private static async Task<object> ReviewSettlementCommitAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct, bool resume, bool acceptedFirst)
    {
        var file = Path.Combine(root, "original-advance-bills.db");
        if (File.Exists(file) != resume) throw new ReviewCheckException("Never reseed or replay an observed advance-bill fixture.");
        if (resume)
        {
            var failures = Directory.GetFiles(root, "failure-*.json");
            if (failures.Length != (acceptedFirst ? 2 : 1) || Directory.GetFiles(root, "*-settlement-accepted.json").Length != 0)
                throw new ReviewCheckException("Continue only the first terminal native attachment refusal, with no accepted case.");
            var priorChecks = new List<string?>(); var captionRefusals = 0;
            foreach (var failed in failures)
            {
                using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(failed, ct));
                priorChecks.Add(prior.RootElement.GetProperty("ReviewCheck").GetString());
                if (prior.RootElement.GetProperty("Stack").GetString()?.Contains("Single[Label]", StringComparison.Ordinal) == true
                    && prior.RootElement.GetProperty("BalanceEvidence").GetProperty("Stage").GetString() == "original-save") captionRefusals++;
            }
            if (!priorChecks.Contains("Original owned settlement action is unavailable.") || acceptedFirst && captionRefusals != 1)
                throw new ReviewCheckException("Original native attachment refusal required.");
        }
        await using var fixture = new ServiceCollection().AddZananceData(file).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var plans = fixture.GetRequiredService<PlanStore>();
        if (!resume) await store.EnsureDefaultCategoriesAsync(ct);
        if (resume && ((await store.GetAccountsAsync(cancellationToken: ct)).Count != 1 || (await plans.GetSchedulesAsync(ct)).Count != 1
            || (await store.GetEntriesAsync(cancellationToken: ct)).Count != (acceptedFirst ? 2 : 1)))
            throw new ReviewCheckException("The original unaccepted first bill differs; no financial replay.");
        var originalStore = services.GetRequiredService<ZananceStore>();
        async Task<string> Base() => JsonSerializer.Serialize(new { Accounts = await originalStore.GetAccountsAsync(cancellationToken: ct),
            Entries = await originalStore.GetEntriesAsync(cancellationToken: ct), Settings = await originalStore.GetSettingsAsync(ct) });
        var before = await Base();
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var oldTheme = themes.Choice; var oldLanguage = local.CurrentLanguage.CultureName;
        var cases = new List<object>(); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        async Task Ready(Func<bool> predicate)
        {
            for (var i = 0; i < 300; i++) { if (predicate()) return; await Task.Delay(50, ct); }
            throw new ReviewCheckException("Original advance-bill native publication did not complete.");
        }
        async Task InvokeAsync(SettlementPage page, SettlementViewModel model, string stage)
        {
            var button = ReimbursementViews(page.FindByName<WrappingAction>("SaveAction")).OfType<Button>().Single(b => ReferenceEquals(b.Command, model.SaveCommand));
            await Ready(() => button.Handler?.PlatformView is Android.Widget.Button { IsAttachedToWindow: true, IsShown: true, HasWindowFocus: true, Enabled: true });
            _balanceEvidence = new { Stage = stage, model.HasCommitted, CompletedCases = cases.Count, NativeTargetAttachedShownFocusedEnabled = true };
            if (button.Handler?.PlatformView is not Android.Widget.Button native || native.Context?.PackageName != Package
                || !native.IsShown || !native.HasWindowFocus || !native.Enabled || !native.PerformClick())
                throw new ReviewCheckException("Original owned settlement action is unavailable.");
        }
        try
        {
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await Shell.Current.GoToAsync("//home");
                if (themes.Choice != theme)
                {
                    var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                    app.ShellRebuilt += Rebuilt;
                    try { themes.Set(theme); await rebuilt.Task.WaitAsync(ct); } finally { app.ShellRebuilt -= Rebuilt; }
                    await AwaitRootHomeAsync(ct);
                }
                await SetLanguageAsync(app, local, language, ct);
                var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var context = language + "-" + theme;
                var account = new Account { Name = "D397 original cash " + context, Type = AccountType.Checking, CurrencyCode = "EUR",
                    OpeningDate = day.AddDays(-8), OpeningBalance = 100000 };
                if (resume && context == "en-Light")
                {
                    account = (await store.GetAccountsAsync(cancellationToken: ct)).Single();
                    if (account.Name != "D397 original cash en-Light" || account.OpeningBalance != 100000 || account.CurrencyCode != "EUR")
                        throw new ReviewCheckException("Original first account differs.");
                }
                else await store.SaveAccountAsync(account, ct);
                var category = (await store.GetCategoriesAsync(cancellationToken: ct)).Single(c => c.SystemKey == "Food");
                var plan = new Schedule { Name = "D397 original advances " + context, Kind = EntryKind.Expense, AccountId = account.Id,
                    CategoryId = category.Id, Amount = 1000, Rule = new() { Start = day.AddDays(-7), Frequency = Frequency.Monthly } };
                if (resume && context == "en-Light")
                {
                    plan = (await plans.GetSchedulesAsync(ct)).Single();
                    if (plan.Name != "D397 original advances en-Light" || plan.AccountId != account.Id || plan.Amount != 1000 || plan.Rule.Frequency != Frequency.Monthly)
                        throw new ReviewCheckException("Original first advance plan differs.");
                }
                else await plans.SaveScheduleAsync(plan, ct);
                var advance = new LedgerEntry { AccountId = account.Id, ScheduleId = plan.Id, CategoryId = category.Id, Kind = EntryKind.Expense,
                    Date = day.AddDays(-7), Amount = 1000, Title = "D397 original advance", Note = "Retain original source" };
                if (resume && context == "en-Light")
                {
                    advance = (await store.GetEntriesAsync(cancellationToken: ct)).Single(e => e.ScheduleId == plan.Id);
                    if (advance.AccountId != account.Id || advance.ScheduleId != plan.Id || advance.Kind != EntryKind.Expense || advance.Amount != 1000
                        || advance.Note != "Retain original source" || advance.Title != "D397 original advance")
                        throw new ReviewCheckException("Original first advance differs; no replacement or financial replay.");
                }
                else if (!(await store.SaveEntryAsync(advance, ct)).Succeeded) throw new ReviewCheckException("Original independent advance refused.");
                var originalAdvance = JsonSerializer.Serialize(await store.GetEntryAsync(advance.Id, ct));
                if (acceptedFirst && context == "en-Light")
                {
                    var rows = (await store.GetEntriesAsync(cancellationToken: ct)).Where(e => e.AccountId == account.Id).ToArray();
                    var accepted = rows.Single(e => e.Id != advance.Id);
                    if (rows.Length != 2 || accepted.Kind != EntryKind.Expense || accepted.Amount != 400 || accepted.Date != day
                        || accepted.CategoryId != category.Id || accepted.ScheduleId is not null || advance.Date != day.AddDays(-7)
                        || accepted.Title != text.Format("Settlement_ExtraTitle", plan.Name))
                        throw new ReviewCheckException("Original accepted first bill differs; never repost it.");
                    var retained = new { Context = context, ActualMinor = 1400, PostedMinor = 400, Kind = "Expense",
                        OriginalAdvanceRetained = true, OriginalAcceptedReadBack = true, NoNewSaveOrBack = true,
                        RequiresOriginalHostNativeBackReceipt = true, NoFinancialReplay = true };
                    cases.Add(retained); await File.WriteAllTextAsync(Path.Combine(root, context + "-settlement-accepted.json"), JsonSerializer.Serialize(retained), ct);
                    continue;
                }
                var navigation = Shell.Current.Navigation;
                var model = new SettlementViewModel(store, text, local, services.GetRequiredService<TimeProvider>(),
                    new SettlementReviewNavigation(services.GetRequiredService<IAppInteraction>(), navigation));
                var page = new SettlementPage(model).WithFlowDirection(local);
                NavigationPage.SetHasNavigationBar(page, false);
                var modal = new NavigationPage(page).WithFlowDirection(local);
                await navigation.PushModalAsync(modal, false); await model.LoadAsync(plan.Id);
                var actual = theme == ThemeChoice.Light ? 1400 : 400; model.ActualText = MoneyText.ForInput(actual, "EUR", local.CurrentCulture);
                await Ready(() => page.FindByName<WrappingAction>("SaveAction").Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true, IsShown: true });
                var notifications = 0;
                void Refuse(object? sender, EventArgs args) { notifications++; throw new IOException("Owned original subscriber refusal after commit."); }
                store.Changed += Refuse;
                try
                {
                    await InvokeAsync(page, model, "original-save"); await Ready(() => model.SaveCommand.ExecutionTask is not null); await model.SaveCommand.ExecutionTask!.WaitAsync(ct);
                    if (!model.HasCommitted || model.CanEdit || model.CanSave || model.IsBusy || model.IsDirty
                        || model.SaveFeedback != text["Settlement_Recorded"] || model.Error != text["Settlement_RecordedFollowUpFailed"])
                        throw new ReviewCheckException("Accepted original bill did not retain its terminal result.");
                    await Ready(() => page.FindByName<Entry>("ActualInput").Handler?.PlatformView is Android.Widget.EditText { Enabled: false });
                    foreach (var caption in new[] { model.SaveFeedback, model.Error })
                    {
                        var label = ReimbursementViews(page).OfType<Label>().Single(l => l.Text == caption
                            && l.Handler?.PlatformView is Android.Widget.TextView { IsShown: true });
                        await Ready(() => label.Handler?.PlatformView is Android.Widget.TextView { Layout: not null, Height: > 0 });
                        var native = (Android.Widget.TextView)label.Handler!.PlatformView!;
                        if (native.Layout is not { } layout || layout.Height > native.Height - native.TotalPaddingTop - native.TotalPaddingBottom
                            || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                            throw new ReviewCheckException("Original accepted feedback is clipped or shortened.");
                    }
                    var posted = (await store.GetEntriesAsync(cancellationToken: ct)).Where(e => e.AccountId == account.Id).ToList();
                    var created = posted.Single(e => e.Id != advance.Id);
                    if (posted.Count != 2 || created.Amount != Math.Abs(actual - 1000) || created.Kind != (actual > 1000 ? EntryKind.Expense : EntryKind.Refund)
                        || created.Date != day || created.CategoryId != category.Id || actual < 1000 && created.RefundOfId != advance.Id
                        || originalAdvance != JsonSerializer.Serialize(await store.GetEntryAsync(advance.Id, ct)))
                        throw new ReviewCheckException("Original accepted bill/advance metadata differs.");
                    var frozen = JsonSerializer.Serialize(posted); await InvokeAsync(page, model, "accepted-back-only");
                    await Ready(() => model.SaveCommand.ExecutionTask is not null); await model.SaveCommand.ExecutionTask!.WaitAsync(ct);
                    await Ready(() => !navigation.ModalStack.Contains(modal));
                    if (notifications != 1 || frozen != JsonSerializer.Serialize((await store.GetEntriesAsync(cancellationToken: ct)).Where(e => e.AccountId == account.Id)))
                        throw new ReviewCheckException("Native return repeated or altered the accepted financial write.");
                    var result = new { Context = context, ActualMinor = actual, PostedMinor = created.Amount, Kind = created.Kind.ToString(),
                        OriginalAdvanceRetained = true, NativeInputReadOnly = true, CompleteFeedback = true, OriginalNativeSaveAndOwnedBack = true, NoFinancialReplay = true };
                    cases.Add(result); await File.WriteAllTextAsync(Path.Combine(root, context + "-settlement-accepted.json"), JsonSerializer.Serialize(result), ct);
                }
                finally { store.Changed -= Refuse; }
            }
        }
        finally { themes.Set(oldTheme); await SetLanguageAsync(app, local, oldLanguage, ct); }
        if (before != await Base()) throw new ReviewCheckException("Independent base financial inputs changed.");
        return new { Comparison = comparison, Cases = cases, OriginalBaseStoreUnchanged = true, SeparateFictitiousPlainSqliteFixture = true,
            NoMainEncryptionOrNativeRouteAdapterClaim = true, MainPackageTouched = false, Device = DeviceEvidence() };
    }
}
