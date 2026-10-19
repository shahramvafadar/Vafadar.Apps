#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Actual stale-form correction refusals, each on a separate fictitious database without financial replay.</summary>
internal static class OccurrenceStateNativeReview
{
    private sealed class Interaction(IAppInteraction original, Translator text) : IAppInteraction
    {
        internal OccurrenceCommitNativeReview.Dialog? Pending;
        internal int Failures;
        internal Func<Task>? BeforeConsent;
        internal Func<Task>? BeforeFailure;
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected saved alert.");
        public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        {
            Pending = new(true, title, message, accept, cancel);
            if (BeforeConsent is not null) await BeforeConsent();
            try { return await original.ConfirmAsync(title, message, accept, cancel); }
            finally { Pending = null; }
        }
        public async Task ShowFailureAsync(Exception exception)
        {
            if (exception is not InvalidOperationException) throw new IOException("Unexpected correction refusal.", exception);
            Failures++;
            if (BeforeFailure is not null) await BeforeFailure();
            Pending = new(false, text["Error_Title"], text["Error_Unexpected"], text["Common_Ok"], text["Common_Ok"]);
            try { await original.ShowFailureAsync(exception); }
            finally { Pending = null; }
        }
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("A rejected command navigated.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected choice.");
    }

    /// <summary>Invokes four original native correction buttons once after a separate operation changes their stored state.</summary>
    internal static async Task<object[]> RunAsync(IServiceProvider services, string folder,
        Func<OccurrencePage, Task> show, Func<OccurrencePage, ICommand, object?, Task> invoke,
        Func<OccurrenceCommitNativeReview.Dialog, string, Task> answer, Func<OccurrencePage, Task> close, CancellationToken ct)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent correction identity required.");
        var text = services.GetRequiredService<Translator>(); var results = new List<object>();
        foreach (var action in new[] { "skip", "change", "unskip", "link" })
        {
            var file = Path.Combine(folder, action + "-state.db");
            if (File.Exists(file)) throw new IOException("Retain the original fixture; never replay its accepted settlement.");
            using var fixture = new ServiceCollection().AddZananceData(file).BuildServiceProvider();
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var plans = fixture.GetRequiredService<PlanStore>();
            var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
            var account = new Account { Name = "D400 owned cash", Type = AccountType.Cash, CurrencyCode = "EUR", OpeningDate = day.AddDays(-30) };
            await store.SaveAccountAsync(account, ct);
            var plan = new Schedule { Name = "D400 original " + action, AccountId = account.Id, Kind = EntryKind.Expense,
                Amount = 1000, Rule = new() { Start = day, Frequency = Frequency.Once } };
            await plans.SaveScheduleAsync(plan, ct);
            var occurrence = Occurrences.Between(plan, [], day, day, day).Single();
            var candidate = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 950, Date = day, Title = "Retained unlinked candidate" };
            if (action == "link") await store.SaveEntryAsync(candidate, ct);
            if (action == "unskip") await plans.SkipAsync(occurrence, ct);
            var interaction = new Interaction(services.GetRequiredService<IAppInteraction>(), text);
            var commands = new OccurrenceCommands(plans, text, interaction);
            var vm = ActivatorUtilities.CreateInstance<OccurrenceViewModel>(services, store, plans, commands);
            vm.ApplyQueryAttributes(new Dictionary<string, object> { ["plan"] = plan.Id, ["date"] = day });
            var page = new OccurrencePage(vm); await show(page); await page.PendingPublication;
            Occurrence? Current() => (Occurrence?)typeof(OccurrenceViewModel).GetField("_occurrence", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm);
            var reviewed = Current() ?? throw new IOException("Original reviewed occurrence missing.");
            async Task<string> Rows() => JsonSerializer.Serialize(new { Entries = await store.GetEntriesAsync(cancellationToken: ct), States = await plans.GetStatesAsync(plan.Id, ct) });
            string? before = null;
            if (action == "link") interaction.BeforeConsent = async () => { await plans.SkipAsync(occurrence, ct); before = await Rows(); };
            else
            {
                if (action == "unskip") await plans.UnskipAsync(occurrence, ct);
                if (!(await plans.SettleAsync(occurrence, Occurrences.CreateEntry(occurrence, 1000, day, ReviewState.Confirmed), ct)).Succeeded)
                    throw new IOException("Original fixture settlement refused.");
                before = await Rows();
            }
            if (action == "change") { vm.ToggleChangeCommand.Execute(null); vm.OccurrenceNote = "Retain unsaved correction"; }
            var notices = 0; plans.Changed += (_, _) => notices++;
            interaction.BeforeFailure = async () =>
            {
                // The one link-fixture skip is the concurrent operation, not a notification from the rejected command.
                if (before != await Rows() || !ReferenceEquals(Current(), reviewed) || !vm.IsBusy || notices != (action == "link" ? 1 : 0))
                    throw new IOException("Rejected correction changed accepted rows, retired its draft or released its native gate.");
                await File.WriteAllTextAsync(Path.Combine(folder, action + "-refused.json"), JsonSerializer.Serialize(new
                { Action = action, ProcessId = Environment.ProcessId, AcceptedRowsUnchanged = true, OriginalDraftRetained = true, BusyThroughNativeFailure = true }), ct);
            };
            ICommand command = action switch { "skip" => vm.SkipCommand, "change" => vm.SaveChangeCommand, "link" => vm.LinkCommand, _ => vm.UndoCommand };
            await invoke(page, command, action == "link" ? vm.Candidates.Single(c => c.Id == candidate.Id) : null);
            var execution = ((IAsyncRelayCommand)command).ExecutionTask ?? throw new IOException("Original command did not start.");
            OccurrenceCommitNativeReview.Dialog? handled = null;
            while (!execution.IsCompleted)
            {
                if (interaction.Pending is { } dialog && !ReferenceEquals(dialog, handled)) { await answer(dialog, action); handled = dialog; }
                await Task.Delay(25, ct);
            }
            await execution;
            if (interaction.Failures != 1 || vm.IsBusy || before != await Rows()) throw new IOException("Original refusal did not complete unchanged.");
            results.Add(new { Action = action, AcceptedRowsUnchanged = true, OriginalDraftRetained = ReferenceEquals(Current(), reviewed), FailureDialogs = interaction.Failures });
            await close(page);
        }
        return results.ToArray();
    }
}
#endif
