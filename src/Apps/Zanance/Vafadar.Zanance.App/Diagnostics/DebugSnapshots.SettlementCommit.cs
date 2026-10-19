#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Preserves actual committed bill outcomes through an owned subscriber refusal and original native Back.</summary>
    private static async Task ReviewSettlementCommitAsync(App app, IServiceProvider services, string folder)
    {
        if (!AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal))
            throw new IOException("Independent settlement identity required.");
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themeService = services.GetRequiredService<ThemeService>(); var oldTheme = themeService.Choice; var oldLanguage = local.CurrentLanguage.CultureName;
        var originalAccounts = await store.GetAccountsAsync(); var originalEntries = await store.GetEntriesAsync(); var originalPlans = await plans.GetSchedulesAsync();
        var accountIds = originalAccounts.Select(a => a.Id).ToHashSet(); var entryIds = originalEntries.Select(e => e.Id).ToHashSet(); var planIds = originalPlans.Select(p => p.Id).ToHashSet();
        static string Packet<T>(IEnumerable<T> rows) => JsonSerializer.Serialize(rows);
        var captures = new List<object>(); var cases = new List<object>();
        async Task Ready(Func<bool> condition)
        {
            for (var i = 0; i < 200; i++) { if (condition()) return; await Task.Delay(50); }
            throw new IOException("Original settlement publication did not complete; retain the same operation.");
        }
        SettlementPage? Page() => app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as SettlementPage ?? Shell.Current.CurrentPage as SettlementPage;
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                var context = language + "-" + theme;
                await Shell.Current.GoToAsync("//home"); await SetStoreThemeAsync(app, themeService, theme); await SetLanguageAsync(app, local, language);
                var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var account = new Account { Name = "D397 original cash " + context, CurrencyCode = "EUR", Type = AccountType.Checking,
                    OpeningDate = day.AddDays(-8), OpeningBalance = 100000 };
                await store.SaveAccountAsync(account);
                var food = (await store.GetCategoriesAsync()).Single(c => c.SystemKey == "Food");
                var plan = new Schedule { Name = "D397 original advances " + context, Kind = EntryKind.Expense, AccountId = account.Id,
                    Amount = 1000, CategoryId = food.Id, Rule = new() { Start = day.AddDays(-7), Frequency = Frequency.Monthly } };
                await plans.SaveScheduleAsync(plan);
                var advance = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, ScheduleId = plan.Id, CategoryId = food.Id,
                    Date = day.AddDays(-7), Amount = 1000, Title = "D397 original advance", Note = "Retain original source" };
                if (!(await store.SaveEntryAsync(advance)).Succeeded) throw new IOException("Original fictitious advance refused.");
                var originalAdvance = JsonSerializer.Serialize(await store.GetEntryAsync(advance.Id));
                await Shell.Current.GoToAsync(AppShell.SettlementRoute, new Dictionary<string, object> { ["id"] = plan.Id });
                SettlementPage? page = null; await Ready(() => { page = Page(); return page?.BindingContext is SettlementViewModel { Loading.IsReady: true }; });
                var vm = (SettlementViewModel)page!.BindingContext;
                var actual = theme == ThemeChoice.Light ? 1400 : 400;
                vm.ActualText = MoneyText.ForInput(actual, "EUR", local.CurrentCulture);
                if (!vm.CanSave || !vm.CanEdit) throw new IOException("Original reviewed advance bill was not ready.");
                var notifications = 0;
                void Refuse(object? sender, EventArgs args) { notifications++; throw new IOException("Owned original post-commit subscriber refusal."); }
                store.Changed += Refuse;
                try
                {
                    InvokeWrappingAction(page, vm.SaveCommand);
                    await Ready(() => vm.SaveCommand.ExecutionTask is not null); await vm.SaveCommand.ExecutionTask!;
                    if (!vm.HasCommitted || vm.CanEdit || vm.CanSave || vm.IsBusy || vm.IsDirty
                        || vm.SaveFeedback != text["Settlement_Recorded"] || vm.Error != text["Settlement_RecordedFollowUpFailed"])
                        throw new IOException("Original committed bill was offered as a rejected or editable Save.");
                    var posted = (await store.GetEntriesAsync()).Where(e => e.AccountId == account.Id).ToList();
                    var created = posted.Single(e => e.Id != advance.Id);
                    if (posted.Count != 2 || created.Amount != Math.Abs(actual - 1000) || created.Kind != (actual > 1000 ? EntryKind.Expense : EntryKind.Refund)
                        || created.AccountId != plan.AccountId || created.CategoryId != plan.CategoryId || created.Date != day
                        || created.ScheduleId is not null || actual < 1000 && created.RefundOfId != advance.Id
                        || originalAdvance != JsonSerializer.Serialize(await store.GetEntryAsync(advance.Id)))
                        throw new IOException("Accepted bill changed its original financial source or correction.");
                    var accepted = new { Context = context, ActualMinor = actual, PaidMinor = 1000, PostedMinor = created.Amount,
                        Kind = created.Kind.ToString(), OriginalAdvanceUnchanged = true, NativeSubscriberFailureRetained = true };
                    await File.WriteAllTextAsync(Path.Combine(folder, context + "-accepted.json"), JsonSerializer.Serialize(accepted));
                    foreach (var width in new[] { 360d, 412d, 1280d })
                    {
                        app.Windows[0].Width = width; await Task.Delay(200);
                        var action = page.FindByName<WrappingAction>("SaveAction");
                        var button = VisualDescendants(action).OfType<Button>().Single();
                        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || !native.IsEnabled
                            || native.ActualHeight < 44 || native.ActualWidth < 44
                            || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetName() != text["Common_Back"])
                            throw new IOException("Original accepted Back target is incomplete or still offers Save.");
                        var input = page.FindByName<Entry>("ActualInput");
                        if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox { IsEnabled: false })
                            throw new IOException("Original accepted input is still natively editable.");
                        foreach (var caption in new[] { vm.SaveFeedback, vm.Error })
                        {
                            var label = VisualDescendants(page).OfType<Label>().Single(l => l.Text == caption && Visible(l));
                            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock block || block.IsTextTrimmed
                                || block.ActualHeight < 1 || block.ActualWidth < 1 || !block.IsTextScaleFactorEnabled)
                                throw new IOException("Accepted result/follow-through feedback is not complete native text.");
                        }
                        var name = context + "-" + width; var output = Path.Combine(folder, name); Directory.CreateDirectory(output);
                        await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "accepted.png")); captures.Add(new { Language = name, File = "accepted.png" });
                    }
                    InvokeWrappingAction(page, vm.SaveCommand);
                    await Ready(() => vm.SaveCommand.ExecutionTask is not null); await vm.SaveCommand.ExecutionTask!;
                    await Ready(() => !ReferenceEquals(Page(), page));
                    if (notifications != 1 || (await store.GetEntriesAsync()).Count(e => e.AccountId == account.Id) != 2)
                        throw new IOException("Original Back repeated a committed financial write.");
                    cases.Add(accepted);
                }
                finally { store.Changed -= Refuse; }
            }
        }
        finally { await Shell.Current.GoToAsync("//home"); await SetStoreThemeAsync(app, themeService, oldTheme); await SetLanguageAsync(app, local, oldLanguage); }
        var originalsUnchanged = Packet(originalAccounts) == Packet((await store.GetAccountsAsync()).Where(a => accountIds.Contains(a.Id)))
            && Packet(originalEntries) == Packet((await store.GetEntriesAsync()).Where(e => entryIds.Contains(e.Id)))
            && Packet(originalPlans) == Packet((await plans.GetSchedulesAsync()).Where(p => planIds.Contains(p.Id)));
        if (!originalsUnchanged) throw new IOException("Independent original base financial inputs changed.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
            ProcessId = Environment.ProcessId, Captures = captures, OnlySeparateFictitiousLedgerChanged = true,
            OriginalBaseStoreUnchanged = originalsUnchanged, Cases = cases, NoFinancialReplayAfterCommit = true }));
    }
}
#endif
