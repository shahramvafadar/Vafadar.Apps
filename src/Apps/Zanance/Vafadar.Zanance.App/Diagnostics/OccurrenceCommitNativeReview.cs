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
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original native occurrence commands over separately owned fictitious data; never repeat an accepted case.</summary>
internal static class OccurrenceCommitNativeReview
{
    /// <summary>One original platform dialog, retained until its native answer completes.</summary>
    internal sealed record Dialog(bool Confirmation, string Title, string Message, string Accept, string Cancel);

    private sealed class Interaction(IAppInteraction original) : IAppInteraction
    {
        internal Dialog? Pending;
        internal Func<Task>? BeforeSavedAlert;
        internal int SavedAlerts, Confirmations;
        public async Task AlertAsync(string title, string message, string cancel)
        {
            if (BeforeSavedAlert is not null) await BeforeSavedAlert();
            SavedAlerts++; Pending = new(false, title, message, cancel, cancel);
            try { await original.AlertAsync(title, message, cancel); } finally { Pending = null; }
        }
        public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        {
            Confirmations++; Pending = new(true, title, message, accept, cancel);
            try { return await original.ConfirmAsync(title, message, accept, cancel); } finally { Pending = null; }
        }
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => original.NavigateAsync(route, parameters);
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected occurrence prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected occurrence choice.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("An accepted occurrence was reported as a rejected write.", exception);
    }

    private static Occurrence? Current(OccurrenceViewModel vm) => (Occurrence?)typeof(OccurrenceViewModel)
        .GetField("_occurrence", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm);

    /// <summary>Invokes each original command once; native callbacks only attach, invoke and answer owned dialogs.</summary>
    internal static async Task<object[]> RunAsync(IServiceProvider services, string folder,
        Func<OccurrencePage, Task> show, Func<OccurrencePage, ICommand, object?, Task> invoke,
        Func<Dialog, string, Task> answer, Func<OccurrencePage, Task> close, CancellationToken ct)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent occurrence identity required.");
        var text = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var results = new List<object>();
        foreach (var action in new[] { "partial", "full", "link", "skip", "change", "unskip", "unsettle", "unsettle-linked" })
        {
            var file = Path.Combine(folder, action + "-occurrence.db");
            if (File.Exists(file)) throw new IOException("Retain an existing occurrence fixture; never replay its accepted command.");
            using var fixture = new ServiceCollection().AddZananceData(file).BuildServiceProvider();
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var plans = fixture.GetRequiredService<PlanStore>();
            var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
            var account = new Account { Name = "D399 original cash " + action, Type = AccountType.Cash, CurrencyCode = "EUR", OpeningDate = day.AddDays(-30) };
            await store.SaveAccountAsync(account, ct);
            var plan = new Schedule { Name = "D399 original " + action, AccountId = account.Id, Amount = 1000,
                Kind = EntryKind.Expense, Rule = new() { Start = day, Frequency = Frequency.Once } };
            await plans.SaveScheduleAsync(plan, ct);
            var occurrence = Occurrences.Between(plan, [], day, day, day).Single();
            var candidate = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 950,
                Date = day, Title = "Original manual candidate", Note = "Preserve original manual money" };
            if (action is "link" or "unsettle-linked") await store.SaveEntryAsync(candidate, ct);
            if (action == "unskip") await plans.SkipAsync(occurrence, ct);
            if (action == "unsettle") await plans.SettleAsync(occurrence, Occurrences.CreateEntry(occurrence, 1000, day, ReviewState.Confirmed), ct);
            if (action == "unsettle-linked") await plans.LinkAsync(occurrence, candidate.Id, ct);
            var interaction = new Interaction(services.GetRequiredService<IAppInteraction>());
            var commands = new OccurrenceCommands(plans, text, interaction);
            var vm = ActivatorUtilities.CreateInstance<OccurrenceViewModel>(services, store, plans, commands);
            vm.ApplyQueryAttributes(new Dictionary<string, object> { ["plan"] = plan.Id, ["date"] = day });
            var page = new OccurrencePage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            await show(page); await page.PendingPublication;
            var reviewed = Current(vm) ?? throw new IOException("The original occurrence was not loaded.");
            vm.ActualAmountText = MoneyText.ForInput(action == "partial" ? 400 : 1234, "EUR", local.CurrentCulture);
            vm.ActualDate = day.AddDays(-1);
            if (action == "change")
            {
                vm.ToggleChangeCommand.Execute(null); vm.DueDate = day.AddDays(3);
                vm.OverrideAmountText = MoneyText.ForInput(1234, "EUR", local.CurrentCulture); vm.OccurrenceNote = "Original reviewed override";
            }
            ICommand command = action switch { "partial" => vm.PayPartCommand, "full" => vm.ConfirmCommand,
                "link" => vm.LinkCommand, "skip" => vm.SkipCommand, "change" => vm.SaveChangeCommand, _ => vm.UndoCommand };
            object? parameter = action == "link" ? vm.Candidates.Single(c => c.Id == candidate.Id) : null;
            var notices = 0;
            void Refuse(object? sender, EventArgs args) { notices++; throw new IOException("Original post-commit occurrence subscriber refusal."); }
            if (action is "partial" or "full" or "unsettle" or "unsettle-linked") store.Changed += Refuse; else plans.Changed += Refuse;
            object? accepted = null;
            interaction.BeforeSavedAlert = async () =>
            {
                if (ReferenceEquals(Current(vm), reviewed) || !vm.IsBusy || notices != 1)
                    throw new IOException("The accepted original occurrence was not retired before its saved alert.");
                var rows = await store.GetEntriesAsync(cancellationToken: ct); var state = (await plans.GetStatesAsync(plan.Id, ct)).Single();
                var expectedCount = action is "partial" or "full" or "link" or "unsettle-linked" ? 1 : 0;
                if (rows.Count != expectedCount) throw new IOException("The accepted occurrence has unexpected financial rows.");
                if (action == "partial" && (rows[0].Amount != 400 || !rows[0].IsPartialPayment || state.PaidAmount != 400
                    || Current(vm)?.Outstanding != 600 || vm.ActualAmountText != MoneyText.ForInput(600, "EUR", local.CurrentCulture)))
                    throw new IOException("Original partial payment was not refreshed to its actual remaining600.");
                if (action == "full" && (rows[0].Amount != 1234 || state.Status != OccurrenceStatus.Settled)) throw new IOException("Original full payment changed.");
                if (action == "link" && (rows[0].Id != candidate.Id || rows[0].Amount != 950 || rows[0].ScheduleId != plan.Id || state.EntryId != candidate.Id)) throw new IOException("Original manual candidate was replaced.");
                if (action == "skip" && state.Status != OccurrenceStatus.Skipped) throw new IOException("Original skip was not saved.");
                if (action == "change" && (state.Amount != 1234 || state.DueDate != day.AddDays(3) || state.Note != "Original reviewed override")) throw new IOException("Original override was not retained.");
                if (action == "unskip" && state.Status != OccurrenceStatus.Open) throw new IOException("Original skip was not reopened.");
                if (action.StartsWith("unsettle", StringComparison.Ordinal) && (state.Status != OccurrenceStatus.Open || !state.AutoPostSuppressed || state.EntryId is not null)) throw new IOException("Original settlement was not reopened safely.");
                if (action == "unsettle-linked" && (rows[0].Id != candidate.Id || rows[0].Amount != 950 || rows[0].Note != candidate.Note || rows[0].ScheduleId is not null || rows[0].OccurrenceDate is not null)) throw new IOException("Reopening changed actual manual money.");
                accepted = new { Action = action, OriginalSnapshotRetired = true, Notifications = notices, LedgerRows = rows.Count,
                    Amounts = rows.Select(e => e.Amount).ToArray(), state.Status, state.PaidAmount, state.AutoPostSuppressed, NoFinancialReplay = true };
                await File.WriteAllTextAsync(Path.Combine(folder, action + "-accepted.json"), JsonSerializer.Serialize(accepted), ct);
            };
            await invoke(page, command, parameter);
            var asyncCommand = (IAsyncRelayCommand)command;
            for (var i = 0; asyncCommand.ExecutionTask is null && i < 200; i++) await Task.Delay(25, ct);
            var execution = asyncCommand.ExecutionTask ?? throw new IOException("Original native occurrence action did not start.");
            Dialog? handled = null;
            while (!execution.IsCompleted)
            {
                if (interaction.Pending is { } dialog && !ReferenceEquals(dialog, handled))
                {
                    if (!vm.IsBusy) throw new IOException("Original occurrence gate opened during native consent/feedback.");
                    if (!dialog.Confirmation && dialog.Message != text["Occurrence_SavedNotificationFailed"]) throw new IOException("Original accepted feedback is not truthful.");
                    await answer(dialog, action); handled = dialog;
                }
                await Task.Delay(25, ct);
            }
            await execution;
            if (accepted is null || interaction.SavedAlerts != 1 || vm.IsBusy) throw new IOException("The original accepted occurrence did not finish once.");
            store.Changed -= Refuse; plans.Changed -= Refuse;
            results.Add(accepted); await close(page);
        }
        return results.ToArray();
    }
}
#endif
